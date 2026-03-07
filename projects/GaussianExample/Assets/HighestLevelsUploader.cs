using System.IO;
using UnityEngine;

public class HighestLevelsUploader : MonoBehaviour
{
    public string fileName = "highest_levels_u8.bin"; // 放到 StreamingAssets
    GraphicsBuffer _buf;

    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError("HighestLevelsUploader: file not found: " + path);
            return;
        }

        byte[] data = File.ReadAllBytes(path);
        int N = data.Length; // uint8 per point
        _buf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, N, sizeof(uint));

        // 转成 uint（shader 读起来方便）
        uint[] u = new uint[N];
        for (int i = 0; i < N; i++) u[i] = data[i];

        int c0 = 0, c1 = 0, c2 = 0, c3 = 0, other = 0;
        for (int i = 0; i < u.Length; i++)
        {
            uint v = u[i];
            if (v == 0) c0++;
            else if (v == 1) c1++;
            else if (v == 2) c2++;
            else if (v == 3) c3++;
            else other++;
        }
        Debug.Log($"HighestLevels stats: 0={c0} 1={c1} 2={c2} 3={c3} other={other} N={u.Length}");
        Debug.Log($"First10: {u[0]},{u[1]},{u[2]},{u[3]},{u[4]},{u[5]},{u[6]},{u[7]},{u[8]},{u[9]}");

        _buf.SetData(u);
        Shader.SetGlobalBuffer("_HighestLevels", _buf);
        Shader.SetGlobalInt("_HighestLevelsCount", N);

        Debug.Log($"HighestLevelsUploader: loaded N={N}");
    }

    void OnDestroy()
    {
        _buf?.Dispose();
    }
}