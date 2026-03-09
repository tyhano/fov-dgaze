from plyfile import PlyData, PlyElement
import numpy as np
import sys

inp = sys.argv[1]
out = sys.argv[2]

ply = PlyData.read(inp)
v = ply['vertex'].data
names = v.dtype.names

keep = ["x","y","z",
        "f_dc_0","f_dc_1","f_dc_2"] \
       + [f"f_rest_{i}" for i in range(45)] \
       + ["opacity","scale_0","scale_1","scale_2","rot_0","rot_1","rot_2","rot_3"]

missing = [k for k in keep if k not in names]
if missing:
    raise RuntimeError(f"Missing fields in {inp}: {missing}")

# 强制 float32，避免 Unity 端类型/对齐问题
dtype = [(k, 'f4') for k in keep]
out_arr = np.empty(len(v), dtype=dtype)
for k in keep:
    out_arr[k] = np.asarray(v[k], dtype=np.float32)

el = PlyElement.describe(out_arr, 'vertex')
PlyData([el], text=False, byte_order='<').write(out)
print("Wrote:", out)
