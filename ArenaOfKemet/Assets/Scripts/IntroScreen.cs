using UnityEngine;
using UnityEngine.UI;

// THE DOOR OF THE ARENA. Before the first sword is drawn, the maker
// speaks. This scroll opens over a held breath — the war waits at
// timeScale 0 — and only moves when the player presses ENTER THE
// ARENA. The words below are the maker's own, set in stone here and
// in INTRODUCTION.md beside the project; if he speaks truer ones,
// they replace these EXACTLY as spoken.
public class IntroScreen : MonoBehaviour
{
    const string TITLE = "MEDJAY";
    const string SUBTITLE = "ARENA OF KEMET";
    const string WORDS =
        "I am Scroll Cipher — the Medjay of the Archive of History,\n" +
        "a Taíno son of Borikén.\n\n" +
        "I built this arena with my own hands, because no one had\n" +
        "ever built it for us. I am the first Taíno to raise a game\n" +
        "such as this, and I raise it representing Borikén.\n\n" +
        "Here the gods of the Nile meet the gods of Greece —\n" +
        "five against five, three roads, one river — and every\n" +
        "player chooses his god freely.\n\n" +
        "I leave this game to the world. When they ask who made it,\n" +
        "let the answer stand: one of the first people of Borikén\n" +
        "made it — and he did not ask permission.";

    void Start()
    {
        Time.timeScale = 0f;
        var canvasGo = new GameObject("IntroCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var gold = new Color(0.92f, 0.76f, 0.35f);

        var veil = Panel(canvasGo.transform, new Color(0.01f, 0.01f, 0.02f, 0.97f),
            Vector2.zero, Vector2.zero, Vector2.zero, true);

        var title = TextOn(veil.transform, TITLE, 64, gold, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(900, 84));
        var sub = TextOn(veil.transform, SUBTITLE, 26, Color.white, TextAnchor.MiddleCenter);
        Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -168), new Vector2(900, 40));
        var line = Panel(veil.transform, gold, new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(420, 3), false);

        var words = TextOn(veil.transform, WORDS, 21, new Color(0.92f, 0.9f, 0.84f), TextAnchor.UpperCenter);
        words.horizontalOverflow = HorizontalWrapMode.Wrap;
        words.verticalOverflow = VerticalWrapMode.Overflow;
        Place(words.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(760, 340));

        var begin = Panel(veil.transform, new Color(0.25f, 0.2f, 0.08f, 1f),
            new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(280, 54), false);
        var beginText = TextOn(begin.transform, "ENTER THE ARENA", 20, Color.white, TextAnchor.MiddleCenter);
        beginText.rectTransform.anchorMin = Vector2.zero;
        beginText.rectTransform.anchorMax = Vector2.one;
        beginText.rectTransform.offsetMin = Vector2.zero;
        beginText.rectTransform.offsetMax = Vector2.zero;
        begin.AddComponent<Button>().onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            canvasGo.SetActive(false);
        });
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    static GameObject Panel(Transform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool stretch)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        else Place(rt, anchor, pos, size);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static Text TextOn(Transform parent, string content, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
