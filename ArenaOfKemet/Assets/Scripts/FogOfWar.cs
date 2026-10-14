using System.Collections.Generic;
using UnityEngine;

// THE FOG — as in the maker's games: the field lies under a dark
// veil, and only where his court has EYES does the veil lift. Heroes
// see far, minions less, towers hold their ground's sight. Enemies
// step out of the jungle dark only when they enter that sight — or
// betray themselves by striking (a blow reveals its maker, briefly).
// Structures are never hidden: stone is known to both courts, though
// the fog still shades it. The minimap obeys the same law.
public class FogOfWar : MonoBehaviour
{
    public static FogOfWar Instance;

    const int N = 128;              // the veil is woven 128 x 128
    const float Half = 60f;         // the world spans -60..60
    const float FogAlpha = 0.62f;

    Texture2D tex;
    Color32[] pixels;
    float[] clarity;
    float tick = 0f;
    Team viewer = Team.Kemet;
    readonly Dictionary<Unit, Renderer[]> bodyCache = new Dictionary<Unit, Renderer[]>();

    void Start()
    {
        Instance = this;
        clarity = new float[N * N];
        pixels = new Color32[N * N];
        tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(8, 10, 16, (byte)(FogAlpha * 255));
        tex.SetPixels32(pixels);
        tex.Apply();

        var go = new GameObject("FogVeil");
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-Half, 0, -Half), new Vector3(Half, 0, -Half),
            new Vector3(Half, 0, Half), new Vector3(-Half, 0, Half)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);
        m.renderQueue = 4000;
        m.SetTexture("_BaseMap", tex);
        mr.material = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        go.transform.position = new Vector3(0, 8.5f, 0);
    }

    void Update()
    {
        tick -= Time.deltaTime;
        if (tick > 0f) return;
        tick = 0.15f;
        var gm = GameManager.Instance;
        if (gm == null || gm.playerHero == null) return;
        viewer = gm.playerHero.team;
        Recompute(gm);
        ApplyVisibility(gm);
    }

    void Stamp(Vector3 pos, float radius)
    {
        float cell = (Half * 2f) / N;
        int cx = Mathf.FloorToInt((pos.x + Half) / cell);
        int cz = Mathf.FloorToInt((pos.z + Half) / cell);
        int r = Mathf.CeilToInt(radius / cell);
        for (int j = cz - r; j <= cz + r; j++)
        {
            if (j < 0 || j >= N) continue;
            for (int i = cx - r; i <= cx + r; i++)
            {
                if (i < 0 || i >= N) continue;
                float wx = -Half + (i + 0.5f) * cell;
                float wz = -Half + (j + 0.5f) * cell;
                float d = Mathf.Sqrt((wx - pos.x) * (wx - pos.x) + (wz - pos.z) * (wz - pos.z)) / radius;
                float v = d >= 1f ? 0f : (d > 0.78f ? (1f - d) / 0.22f : 1f);
                int k = j * N + i;
                if (v > clarity[k]) clarity[k] = v;
            }
        }
    }

    void Recompute(GameManager gm)
    {
        System.Array.Clear(clarity, 0, clarity.Length);
        foreach (var u in gm.units)
        {
            if (u == null || u.team != viewer || !u.IsAlive) continue;
            Stamp(u.transform.position, u is Hero ? 11.5f : 7f);
        }
        foreach (var s in gm.structures)
        {
            if (s == null || s.team != viewer || s.health.dead) continue;
            Stamp(s.transform.position, s.isCore ? 11f : 9.5f);
        }
        for (int i = 0; i < pixels.Length; i++)
        {
            byte a = (byte)((1f - clarity[i]) * FogAlpha * 255f);
            pixels[i] = new Color32(8, 10, 16, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply(false);
    }

    public bool IsSeen(Vector3 pos)
    {
        float cell = (Half * 2f) / N;
        int i = Mathf.Clamp(Mathf.FloorToInt((pos.x + Half) / cell), 0, N - 1);
        int j = Mathf.Clamp(Mathf.FloorToInt((pos.z + Half) / cell), 0, N - 1);
        return clarity[j * N + i] > 0.35f;
    }

    void ApplyVisibility(GameManager gm)
    {
        foreach (var u in gm.units)
        {
            if (u == null) continue;
            bool vis;
            if (u is JungleBeast) vis = IsSeen(u.transform.position); // the jungle keeps its secrets from both courts
            else if (u.team == viewer) vis = true;
            else vis = IsSeen(u.transform.position) || Time.time < u.revealUntil;
            if (!bodyCache.TryGetValue(u, out var rends))
            {
                rends = u.GetComponentsInChildren<Renderer>(true);
                bodyCache[u] = rends;
            }
            foreach (var r in rends) if (r != null) r.enabled = vis;
            var bar = u.GetComponent<OverheadBar>();
            if (bar != null) bar.fogVisible = vis;
        }
        // Fallen bodies leave the cache.
        var dead = new List<Unit>();
        foreach (var kv in bodyCache) if (kv.Key == null) dead.Add(kv.Key);
        foreach (var d in dead) bodyCache.Remove(d);
    }
}
