using UnityEngine;

// The juice — the small bright spirit that makes a hit FEEL struck:
// a sphere of light that swells and dies in a fifth of a second.
// Struck flesh, struck stone, a god rising in level, a tower's last
// breath. Cheap, bright, everywhere, like the games he studies.
public class HitFlash : MonoBehaviour
{
    Material mat;
    float t = 0f;
    float life = 0.22f;
    float maxScale = 1f;

    public static void Spawn(Vector3 pos, Color color, float scale = 1f, float life = 0.22f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HitFlash";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.3f * scale;
        var c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);
        var f = go.AddComponent<HitFlash>();
        f.maxScale = scale;
        f.life = life;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f);
        m.renderQueue = 3100;
        m.color = new Color(color.r, color.g, color.b, 0.55f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * 1.6f);
        go.GetComponent<Renderer>().material = m;
        f.mat = m;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = t / life;
        if (k >= 1f) { Destroy(gameObject); return; }
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.7f, k) * maxScale;
        var c = mat.color;
        c.a = 0.55f * (1f - k);
        mat.color = c;
    }
}
