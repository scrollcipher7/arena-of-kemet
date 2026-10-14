using UnityEngine;
using UnityEngine.UI;

// Raises the player's FACE — the stage the HUD painter reads by name
// (HpBar, CdQ, AnnounceText, EndPanel...). Built in the League
// pattern, as the maker ordered for the PC version: the bottom bar
// with portrait, blood and energy, the four powers and the gear; the
// score and clock riding top-center; announcements in gold above the
// field. Everything is raised in Awake so HUD's Start finds it all —
// including DeathVeil and EndPanel, which must be born AWAKE or
// GameObject.Find walks past them (HUD puts them to sleep itself).
public class HudStage : MonoBehaviour
{
    static readonly Color Gold = new Color(0.92f, 0.76f, 0.35f);
    static readonly Color Stone = new Color(0.02f, 0.02f, 0.04f, 0.88f);
    static readonly Color PanelDark = new Color(0.05f, 0.05f, 0.08f, 0.92f);
    Text portraitLetter;
    Text portraitName;
    bool portraitSet = false;
    readonly Text[] capTexts = new Text[4];

    void Awake()
    {
        var canvasGo = new GameObject("HudCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        // ----- top center: the war's account, on a dark stone backing
        // so the gold never drowns in the bright roads beneath it -----
        Panel(root, "ScoreBack", new Color(0, 0, 0, 0.45f), new Vector2(0.5f, 1),
            new Vector2(0, -32), new Vector2(620, 68));
        var score = Txt(root, "ScoreText", "", 20, Gold, TextAnchor.MiddleCenter);
        Place(score.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(560, 28));
        var clock = Txt(root, "ClockText", "00:00", 16, Color.white, TextAnchor.MiddleCenter);
        Place(clock.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(200, 24));

        // ----- top right: the player's own tally (below the gear) -----
        var kd = Txt(root, "KdText", "", 15, Color.white, TextAnchor.MiddleRight);
        Place(kd.rectTransform, new Vector2(1, 1), new Vector2(-18, -102), new Vector2(240, 24));

        // ----- the golden voice: announcements mid-field -----
        var ann = Txt(root, "AnnounceText", "", 38, Gold, TextAnchor.MiddleCenter);
        Place(ann.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1000, 54));
        var sub = Txt(root, "AnnounceSub", "", 18, Color.white, TextAnchor.MiddleCenter);
        Place(sub.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(0, -54), new Vector2(900, 30));

        // ----- the bottom bar: the League face -----
        var bar = Panel(root, "HudBar", PanelDark, new Vector2(0.5f, 0), new Vector2(0, 57), new Vector2(840, 106));
        var bt = bar.transform;

        var portrait = Panel(bt, "Portrait", new Color(0.16f, 0.13f, 0.05f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(-356, 6), new Vector2(84, 84));
        portraitLetter = Txt(portrait.transform, "PortraitLetter", "?", 38, Gold, TextAnchor.MiddleCenter);
        portraitLetter.rectTransform.anchorMin = Vector2.zero;
        portraitLetter.rectTransform.anchorMax = Vector2.one;
        portraitLetter.rectTransform.offsetMin = Vector2.zero;
        portraitLetter.rectTransform.offsetMax = Vector2.zero;
        // Name and level live INSIDE the portrait tile — never on the rim.
        portraitName = Txt(portrait.transform, "PortraitName", "", 11, Color.white, TextAnchor.MiddleCenter);
        Place(portraitName.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(110, 16));
        var level = Txt(portrait.transform, "LevelText", "Level 1", 12, Gold, TextAnchor.MiddleCenter);
        Place(level.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(110, 16));

        MakeBar(bt, "HpBar", new Vector2(-140, 27), new Vector2(370, 24), new Color(0.32f, 0.78f, 0.28f));
        var hpT = Txt(bt, "HpText", "", 13, Color.white, TextAnchor.MiddleCenter);
        Place(hpT.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-140, 27), new Vector2(370, 24));
        MakeBar(bt, "MpBar", new Vector2(-140, 1), new Vector2(370, 18), new Color(0.25f, 0.45f, 0.95f));
        var mpT = Txt(bt, "MpText", "", 12, Color.white, TextAnchor.MiddleCenter);
        Place(mpT.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-140, 1), new Vector2(370, 18));
        MakeBar(bt, "XpBar", new Vector2(-140, -17), new Vector2(370, 8), new Color(0.62f, 0.4f, 0.9f));

        string[] keys = { "Q", "W", "E", "R" };
        string[] powerNames = { "DASH", "FALCON", "SHIELD", "FLOOD" };
        string[] btnNames = { "AbQ", "AbW", "AbE", "AbR" };
        string[] cdNames = { "CdQ", "CdW", "CdE", "CdR" };
        for (int i = 0; i < 4; i++)
        {
            // Tiles end at x=274 — the gear column begins at 288. No burial.
            float x = 40 + i * 68;
            var tile = Panel(bt, btnNames[i], new Color(0.1f, 0.1f, 0.14f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(x, 15), new Vector2(60, 60));
            tile.AddComponent<Button>();
            var letter = Txt(tile.transform, "Key", keys[i], 24, i == 3 ? Gold : Color.white, TextAnchor.MiddleCenter);
            letter.rectTransform.anchorMin = Vector2.zero;
            letter.rectTransform.anchorMax = Vector2.one;
            letter.rectTransform.offsetMin = Vector2.zero;
            letter.rectTransform.offsetMax = Vector2.zero;
            // The cooldown shade — a radial image the HUD fills by name.
            var cdGo = new GameObject(cdNames[i]);
            cdGo.transform.SetParent(tile.transform, false);
            var cdRt = cdGo.AddComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero; cdRt.anchorMax = Vector2.one;
            cdRt.offsetMin = Vector2.zero; cdRt.offsetMax = Vector2.zero;
            var cdImg = cdGo.AddComponent<Image>();
            cdImg.color = new Color(0, 0, 0, 0.68f);
            cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Radial360;
            cdImg.fillAmount = 0f;
            cdImg.raycastTarget = false;
            // Under each tile, the power's NAME — not the letter twice.
            var cap = Txt(bt, "Cap" + keys[i], powerNames[i], 10, Color.white, TextAnchor.MiddleCenter);
            capTexts[i] = cap;
            Place(cap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -27), new Vector2(66, 16));
        }

        // ----- the gear column: gold above, three roads of iron -----
        var goldT = Txt(bt, "GoldText", "0 gold", 16, Gold, TextAnchor.MiddleRight);
        Place(goldT.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(348, 40), new Vector2(132, 24));
        string[] gearNames = { "BladeBtn", "ArmorBtn", "BootsBtn" };
        string[] gearLabels = { "BLADE +", "ARMOR +", "BOOTS +" };
        for (int i = 0; i < 3; i++)
        {
            var b = Panel(bt, gearNames[i], new Color(0.2f, 0.16f, 0.06f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(352, 16 - i * 27), new Vector2(128, 24));
            b.AddComponent<Button>();
            var lt = Txt(b.transform, "Label", gearLabels[i], 12, Color.white, TextAnchor.MiddleCenter);
            lt.rectTransform.anchorMin = Vector2.zero;
            lt.rectTransform.anchorMax = Vector2.one;
            lt.rectTransform.offsetMin = Vector2.zero;
            lt.rectTransform.offsetMax = Vector2.zero;
        }

        // ----- death's veil (born awake; HUD sleeps it) -----
        var veil = Panel(root, "DeathVeil", new Color(0.25f, 0.02f, 0.02f, 0.45f),
            Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(veil.GetComponent<RectTransform>());
        var dt = Txt(veil.transform, "DeathText", "", 26, Color.white, TextAnchor.MiddleCenter);
        Place(dt.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(900, 44));

        // ----- the end of the war (born awake; HUD sleeps it) -----
        var end = Panel(root, "EndPanel", new Color(0.01f, 0.01f, 0.03f, 0.94f),
            Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(end.GetComponent<RectTransform>());
        var et = Txt(end.transform, "EndTitle", "", 48, Gold, TextAnchor.MiddleCenter);
        Place(et.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1000, 64));
        var es = Txt(end.transform, "EndSub", "", 18, Color.white, TextAnchor.MiddleCenter);
        es.horizontalOverflow = HorizontalWrapMode.Wrap;
        Place(es.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0, -70), new Vector2(760, 60));
    }

    // The Selection Hall may re-seat the player after the portrait
    // already learned the default god — let it learn again.
    public void ResetPortrait()
    {
        portraitSet = false;
        // The tiles must speak the CHOSEN god's tongue.
        var gm = GameManager.Instance;
        if (gm != null && gm.playerHero != null)
        {
            var names = Hero.KitNames(gm.playerHero.heroName);
            for (int i = 0; i < 4; i++)
                if (capTexts[i] != null) capTexts[i].text = names[i];
        }
    }

    void Update()
    {
        // The portrait learns its god once the player takes his seat.
        if (portraitSet) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.playerHero == null) return;
        string n = gm.playerHero.heroName;
        string shortName = n.StartsWith("THE ") ? n.Substring(4) : n;
        portraitLetter.text = shortName.Substring(0, 1);
        portraitName.text = n;
        portraitSet = true;
    }

    void MakeBar(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Place(rt, new Vector2(0.5f, 0.5f), pos, size);
        var slider = go.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        var bg = Panel(go.transform, "BG", new Color(0, 0, 0, 0.55f), Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().raycastTarget = false;
        var fill = Panel(go.transform, "Fill", fillColor, Vector2.zero, Vector2.zero, Vector2.zero);
        var frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1);
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().raycastTarget = false;
        slider.fillRect = frt;
    }

    static GameObject Panel(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Place(rt, anchor, pos, size);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static Text Txt(Transform parent, string name, string content, int size, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
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

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
