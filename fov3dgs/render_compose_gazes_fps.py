#
# Copyright (C) 2023, Inria
# GRAPHDECO research group, https://team.inria.fr/graphdeco
# All rights reserved.
#

import os
import math
import torch
from tqdm import tqdm
from os import makedirs
from argparse import ArgumentParser

from scene import Scene
from gaussian_renderer_fov import render, GaussianModel
from utils.general_utils import safe_state
from arguments import ModelParams, PipelineParams, get_combined_args


def generate_gaze_trace(n: int, traj: str = "scan", margin: float = 0.08, amp: float = 0.35):
    """
    生成长度为 n 的合成 gaze 轨迹，坐标范围均为 [0,1]。
    traj:
      - "scan": x 左右往返扫，y 固定 0.5
      - "circle": 圆周
      - "lissajous": Lissajous 曲线
    """
    if n <= 0:
        return []

    trace = []
    for i in range(n):
        t = 0.0 if n == 1 else i / (n - 1)

        if traj == "scan":
            # 三角波：0->1->0
            u = 2.0 * t
            if u <= 1.0:
                x = margin + (1 - 2 * margin) * u
            else:
                x = margin + (1 - 2 * margin) * (2.0 - u)
            y = 0.5

        elif traj == "circle":
            ang = 2.0 * math.pi * t
            x = 0.5 + amp * math.cos(ang)
            y = 0.5 + amp * math.sin(ang)

        elif traj == "lissajous":
            x = 0.5 + amp * math.sin(2.0 * math.pi * 1.0 * t)
            y = 0.5 + amp * math.sin(2.0 * math.pi * 2.0 * t + math.pi / 2.0)

        else:
            raise ValueError(f"Unknown traj: {traj}")

        # clamp 到安全范围
        x = max(margin, min(1.0 - margin, x))
        y = max(margin, min(1.0 - margin, y))
        trace.append((x, y))
    return trace


def render_set(out_path, name, iteration, views, gaussians, pipeline, background,
               highest_levels, shs_dcs, opacities):
    n_views = len(views)
    print(f"[INFO] Split={name}, Num views={n_views}")
    if n_views == 0:
        print("[WARN] No views found, skip.")
        return

    # 输出目录（可选存图）
    base_dir = os.path.join(out_path, name, f"ours_{iteration}")
    render_path = os.path.join(base_dir, f"renders_synth_{args.traj}")
    if args.save_images:
        makedirs(render_path, exist_ok=True)

    shs_dcs = shs_dcs.cuda()
    highest_levels = highest_levels.cuda()
    opacities = opacities.cuda()

    starter, ender = torch.cuda.Event(enable_timing=True), torch.cuda.Event(enable_timing=True)

    # 合成 gaze 序列：每帧一个 gaze
    gaze_trace = generate_gaze_trace(n_views, traj=args.traj, margin=args.margin, amp=args.amp)

    # warm-up（用第一帧、第一 gaze）
    first_view = views[0]
    g0 = gaze_trace[0]
    gazeArray0 = torch.tensor([g0[0], g0[1]]).float().cuda()
    for _ in range(10):
        _ = render(
            first_view, gaussians, background,
            alpha=0.05, gazeArray=gazeArray0, blending=True,
            starter=starter, ender=ender,
            highest_levels=highest_levels, shs_dcs=shs_dcs, opacities=opacities
        )["render"]
        torch.cuda.synchronize()

    # 全局统计：总帧数 / 总耗时（推荐汇报用这个）
    total_render_ms = 0.0
    per_view_fps = []

    for idx, view in enumerate(tqdm(views, desc="Rendering progress")):
        gx, gy = gaze_trace[idx]
        gazeArray = torch.tensor([gx, gy]).float().cuda()

        time_ms_5 = 0.0
        last_render = None

        # 同一帧重复 5 次（同一个 gaze）计时
        for _ in range(5):
            last_render = render(
                view, gaussians, background,
                alpha=0.05, gazeArray=gazeArray, blending=True,
                starter=starter, ender=ender,
                highest_levels=highest_levels, shs_dcs=shs_dcs, opacities=opacities
            )["render"]
            torch.cuda.synchronize()
            time_ms_5 += starter.elapsed_time(ender)

        render_ms = time_ms_5 / 5.0
        total_render_ms += render_ms

        fps = 1000.0 / render_ms
        per_view_fps.append(fps)

        # 可选存图（不计入计时）
        if args.save_images and last_render is not None:
            import torchvision
            out_img = last_render.detach().clamp(0, 1).cpu()  # [3,H,W]
            _, H, W = out_img.shape

            # gaze (0~1) -> 像素坐标
            px = int(gx * (W - 1))
            py = int(gy * (H - 1))

            # 画一个小十字（红色），半径 r
            r = 6
            x0, x1 = max(0, px - r), min(W - 1, px + r)
            y0, y1 = max(0, py - r), min(H - 1, py + r)

            # 横线
            out_img[0, py, x0:x1 + 1] = 1.0
            out_img[1, py, x0:x1 + 1] = 0.0
            out_img[2, py, x0:x1 + 1] = 0.0
            # 竖线
            out_img[0, y0:y1 + 1, px] = 1.0
            out_img[1, y0:y1 + 1, px] = 0.0
            out_img[2, y0:y1 + 1, px] = 0.0

            fname = f"{idx:05d}_g{gx:.3f}_{gy:.3f}.png"
            torchvision.utils.save_image(out_img, os.path.join(render_path, fname))

    n = float(n_views)
    fps_global = n / ((total_render_ms / 1000.0))
    fps_mean = sum(per_view_fps) / n

    print("\n========== RESULT ==========")
    print(f"[Global] Render-only FPS: {fps_global:.2f}")
    print(f"[Mean ] Render-only FPS: {fps_mean:.2f}")
    if args.save_images:
        print(f"[Saved] Frames -> {render_path}")
    print("===========================\n")


def render_sets(dataset: ModelParams, iteration: int, pipeline: PipelineParams,
                skip_train: bool, skip_test: bool):
    with torch.no_grad():
        gaussians = GaussianModel(dataset.sh_degree)
        scene = Scene(dataset, gaussians, load_iteration=None, shuffle=False, fps_mode=True)

        finest_gs_path = (
            args.base_folder
            + f"1_PS1_{args.layer_num}_{args.max_pooling_size}/point_cloud/iteration_55000/point_cloud.ply"
        )
        gaussians.load_ply(finest_gs_path)

        comp_dir = os.path.join(args.base_folder, f"composed_{args.layer_num}_{args.max_pooling_size}")
        highest_levels = torch.load(os.path.join(comp_dir, "highest_levels.pt"))
        shs_dcs = torch.load(os.path.join(comp_dir, "shs_dcs.pt"))
        opacities = torch.load(os.path.join(comp_dir, "opacities.pt"))

        bg_color = [1, 1, 1] if dataset.white_background else [0, 0, 0]
        background = torch.tensor(bg_color, dtype=torch.float32, device="cuda")

        out_path = comp_dir

        if args.split == "train":
            views = scene.getTrainCameras()
            render_set(out_path, "train", 0, views, gaussians, pipeline, background,
                       highest_levels, shs_dcs, opacities)
        elif args.split == "all":
            views = scene.getTrainCameras() + scene.getTestCameras()
            render_set(out_path, "all", 0, views, gaussians, pipeline, background,
                       highest_levels, shs_dcs, opacities)
        else:
            views = scene.getTestCameras()
            render_set(out_path, "test", 0, views, gaussians, pipeline, background,
                       highest_levels, shs_dcs, opacities)


if __name__ == "__main__":
    parser = ArgumentParser(description="FoV FPS with synthetic gaze trace (clean)")
    model = ModelParams(parser, sentinel=True)
    pipeline = PipelineParams(parser)

    parser.add_argument("--iteration", default=-1, type=int)
    parser.add_argument("--skip_train", action="store_true")
    parser.add_argument("--skip_test", action="store_true")
    parser.add_argument("--quiet", action="store_true")
    parser.add_argument("--base_folder", type=str, required=True)
    parser.add_argument("--layer_num", type=int)
    parser.add_argument("--max_pooling_size", type=int)

    # 合成 gaze / 是否存图 / 使用哪个 split
    parser.add_argument("--traj", type=str, default="scan", choices=["scan", "circle", "lissajous"])
    parser.add_argument("--save_images", action="store_true", help="save rendered frames (not included in timing)")
    parser.add_argument("--split", type=str, default="test", choices=["test", "train", "all"])
    parser.add_argument("--margin", type=float, default=0.08, help="clamp gaze into [margin,1-margin]")
    parser.add_argument("--amp", type=float, default=0.35, help="amplitude for circle/lissajous")

    args = get_combined_args(parser)
    print("Rendering " + args.model_path)

    safe_state(args.quiet)
    render_sets(model.extract(args), args.iteration, pipeline.extract(args), args.skip_train, args.skip_test)