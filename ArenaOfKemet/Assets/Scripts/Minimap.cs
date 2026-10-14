using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// THE MINIMAP — how a player always knows where he stands in the war.
// It raises its own canvas in the top-left: the whole Gorge in miniature,
// every god and tower a mark upon it. YOUR god is the big white-ringed
// mark — find him at a glance, anywhere, any time. A pale box shows what
// your camera is looking at. Built at runtime: no scene wiring needed.
public class Minimap : MonoBehaviour
{
    const float World = 113f;      // the field spans -56.5..56.5
    const float Size = 232f;       // panel size in pixels
    RectTransform panel;
    RectTransform viewBox;
    readonly Dictionary<Object, RectTransform> dots = new Dictionary<Object, RectTransform>();
    readonly List<RectTransform> minionDots = new List<RectTransform>();

    RectTransform MakeDot(Transform parent, float size, Color color)
    {
        var go = new GameObject("Dot");
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    void Start()
    {
        var canvasGo = new GameObject("MinimapCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panelGo = new GameObject("MinimapPanel");
        panel = panelGo.AddComponent<RectTransform>();
        panelGo.transform.SetParent(canvasGo.transform, false);
        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.05f, 0.78f);
        bg.raycastTarget = false;
        panel.sizeDelta = new Vector2(Size, Size);
        panel.anchorMin = panel.anchorMax = new Vector2(0, 1);
        panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = new Vector2(18, -18);

        viewBox = MakeDot(panel, 10, new Color(1, 1, 1, 0.13f));
    }

    Vector2 ToMap(Vector3 w)
    {
        float u = (w.x + World / 2f) / World;
        float v = (w.z + World / 2f) / World;
        return new Vector2(u * Size, -(1f - v) * Size);
    }

    void Place(Object key, Vector3 worldPos, float size, Color color)
    {
        if (!dots.TryGetValue(key, out var rt) || rt == null)
        {
            rt = MakeDot(panel, size, color);
            dots[key] = rt;
        }
        rt.anchoredPosition = ToMap(worldPos);
        rt.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || panel == null) return;

        // Towers and cores: still squares of their court's color.
        foreach (var s in gm.structures)
        {
            if (s.health.dead) { if (dots.TryGetValue(s, out var d) && d != null) d.gameObject.SetActive(false); continue; }
            Place(s, s.transform.position, s.isCore ? 13f : 8f,
                s.team == Team.Kemet ? new Color(0.95f, 0.72f, 0.2f) : new Color(0.62f, 0.78f, 0.9f));
        }

        // Beasts: embers in the jungle, only while they live and wake.
        foreach (var b in gm.beasts)
        {
            if (b == null || b.health == null) continue;
            if (b.health.dead || !b.gameObject.activeSelf)
            { if (dots.TryGetValue(b, out var d) && d != null) d.gameObject.SetActive(false); continue; }
            Place(b, b.transform.position, b.isBoss ? 11f : 6f, new Color(1f, 0.45f, 0.15f));
        }

        // The gods. Yours is the white-ringed mark, bigger than any other.
        foreach (var h in gm.heroes)
        {
            if (!h.IsAlive) { if (dots.TryGetValue(h, out var d) && d != null) d.gameObject.SetActive(false); continue; }
            if (h.isPlayer)
            {
                Place(h, h.transform.position, 15f, Color.white);
                // a team-colored heart inside the white ring
                if (dots.TryGetValue(h, out var ring) && ring != null && ring.childCount == 0)
                {
                    var heart = MakeDot(ring, 9f, new Color(0.95f, 0.72f, 0.2f));
                    heart.anchoredPosition = Vector2.zero;
                    heart.anchorMin = heart.anchorMax = new Vector2(0.5f, 0.5f);
                }
            }
            else if (FogOfWar.Instance != null && gm.playerHero != null && h.team != gm.playerHero.team
                     && !FogOfWar.Instance.IsSeen(h.transform.position) && Time.time >= h.revealUntil)
            {
                // The fog holds him: no mark on the map.
                if (dots.TryGetValue(h, out var d) && d != null) d.gameObject.SetActive(false);
            }
            else
            {
                Place(h, h.transform.position, 10f,
                    h.team == Team.Kemet ? new Color(0.95f, 0.72f, 0.2f) : new Color(0.62f, 0.78f, 0.9f));
            }
        }

        // Minions: a shared pool of small marks, recycled each frame.
        int mi = 0;
        foreach (var u in gm.units)
        {
            if (u is Hero || !u.IsAlive) continue;
            // The fog's law on the small map too: enemy specks and the
            // jungle's beasts show only where the court has eyes.
            if (FogOfWar.Instance != null && gm.playerHero != null
                && (u is JungleBeast || u.team != gm.playerHero.team)
                && !FogOfWar.Instance.IsSeen(u.transform.position)
                && Time.time >= u.revealUntil) continue;
            if (mi >= 90) break;
            while (minionDots.Count <= mi) minionDots.Add(MakeDot(panel, 4f, Color.white));
            var rt = minionDots[mi++];
            rt.GetComponent<Image>().color = u.team == Team.Kemet
                ? new Color(0.95f, 0.72f, 0.2f, 0.75f) : new Color(0.62f, 0.78f, 0.9f, 0.75f);
            rt.anchoredPosition = ToMap(u.transform.position);
            rt.gameObject.SetActive(true);
        }
        for (int i = mi; i < minionDots.Count; i++) minionDots[i].gameObject.SetActive(false);

        // The camera's eye: a pale box around what you are looking at.
        var cam = Camera.main;
        if (cam != null && viewBox != null)
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 focus = ray.GetPoint(enter);
                viewBox.anchoredPosition = ToMap(focus);
                viewBox.sizeDelta = new Vector2(46f / World * Size, 34f / World * Size);
            }
        }
    }
}
