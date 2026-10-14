using UnityEngine;
using UnityEngine.UI;

// The floating face of the war, as in the reference: a health bar over
// every fighter's head in his court's color — and for the gods, their
// NAME above it and their LEVEL in a badge at the bar's left, so one
// glance tells you who outranks who before you trade blows.
public class OverheadBar : MonoBehaviour
{
    public float height = 2.7f;
    public bool showText = false;

    Unit unit;
    Hero hero;
    GameObject root;
    Image fill;
    Text levelText;
    bool built = false;

    void Start()
    {
        unit = GetComponent<Unit>();
        hero = GetComponent<Hero>();
        if (unit == null || unit.health == null) { enabled = false; return; }

        root = new GameObject("OverheadBar_" + gameObject.name);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        root.transform.localScale = Vector3.one * 0.0085f;

        float W = showText ? 132f : 92f;
        var bg = Bar(root.transform, new Vector2(W, 13f), new Vector2(0, 0), new Color(0, 0, 0, 0.55f));
        fill = Bar(root.transform, new Vector2(W - 4f, 9f), new Vector2(0, 0),
            unit.team == Team.Kemet ? new Color(0.98f, 0.74f, 0.22f) : new Color(0.60f, 0.79f, 0.98f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        // The fill sits inside the dark frame, nudged so it starts at its left edge.
        fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        fill.rectTransform.anchoredPosition = new Vector2(0, 0);

        if (showText && hero != null)
        {
            // Level badge: a dark square at the bar's left, number in gold.
            var badge = Bar(root.transform, new Vector2(21f, 21f), new Vector2(-W / 2f - 13f, 0), new Color(0.08f, 0.08f, 0.1f, 0.85f));
            levelText = Label(badge.gameObject, "1", 15, new Color(1f, 0.85f, 0.4f));
            Label(root, hero.heroName, 12, Color.white).rectTransform.anchoredPosition = new Vector2(0, 16f);
        }
        built = true;
    }

    // After the Selection Hall moves a god to the other court, his bar
    // must change its colors — gold for Kemet, pale for Olympus.
    public void RefreshTeam()
    {
        if (fill != null && unit != null)
            fill.color = unit.team == Team.Kemet
                ? new Color(0.98f, 0.74f, 0.22f) : new Color(0.60f, 0.79f, 0.98f);
    }

    Image Bar(Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject("bar");
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return img;
    }

    Text Label(GameObject parent, string content, int size, Color color)
    {
        var go = new GameObject("label");
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        rt.sizeDelta = new Vector2(150f, 24f);
        return t;
    }

    void LateUpdate()
    {
        if (!built) return;
        bool alive = unit != null && unit.IsAlive;
        root.SetActive(alive);
        if (!alive) return;
        root.transform.position = unit.transform.position + Vector3.up * height;
        var cam = Camera.main;
        if (cam != null) root.transform.rotation = cam.transform.rotation;
        fill.fillAmount = unit.health.Frac;
        if (levelText != null && hero != null) levelText.text = hero.level.ToString();
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root);
    }
}
