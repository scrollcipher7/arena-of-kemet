using UnityEngine;
using UnityEngine.UI;

// THE SELECTION HALL — the first face of the game, as in the maker's
// reference: a wall of god portraits, name and role beneath each face,
// all ten in ONE pool. The law he laid down: pantheon is a god's
// ORIGIN, not his chain — the player chooses any god, and the court
// he fights for. The mirror god of the same role takes the same seat
// on the other court, so no role ever stands empty and no god ever
// stands twice. The war waits (time is held by the maker's door
// behind this hall) until the choice is made.
public class SelectionHall : MonoBehaviour
{
    static readonly string[] Gods =
        { "Medjay", "Thoth", "Sekhmet", "Anubis", "Isis", "Ares", "Artemis", "Zeus", "Hades", "Athena" };
    static readonly string[] GodNames =
        { "THE MEDJAY", "THOTH", "SEKHMET", "ANUBIS", "ISIS", "ARES", "ARTEMIS", "ZEUS", "HADES", "ATHENA" };
    static readonly string[] GodRoles =
        { "MARKSMAN", "MAGE", "WARRIOR", "ASSASSIN", "SUPPORT", "WARRIOR", "MARKSMAN", "MAGE", "ASSASSIN", "SUPPORT" };
    static readonly string[] RoleKeys =
        { "marksman", "mage", "warrior", "assassin", "support", "warrior", "marksman", "mage", "assassin", "support" };

    int pick = 0;                 // index into Gods
    Team court = Team.Kemet;
    GameObject canvasGo;
    Image[] rings;
    Button kemetBtn, olyBtn;
    Text kemetBtnText, olyBtnText;

    struct Seat { public int aiRole; public int lane; public Vector3 spawn; }

    static Seat SeatFor(Team t, string role)
    {
        // The seats are the builder's own, read off the default court:
        // each (court, role) keeps its post, its road, and its ground.
        if (t == Team.Kemet)
        {
            switch (role)
            {
                case "marksman": return new Seat { aiRole = 0, lane = 2, spawn = new Vector3(-43, 1, -43) };
                case "mage": return new Seat { aiRole = 0, lane = 1, spawn = new Vector3(-41, 1, -45) };
                case "warrior": return new Seat { aiRole = 0, lane = 0, spawn = new Vector3(-45, 1, -41) };
                case "assassin": return new Seat { aiRole = 1, lane = 1, spawn = new Vector3(-47, 1, -43) };
                default: return new Seat { aiRole = 2, lane = 1, spawn = new Vector3(-43, 1, -47) };
            }
        }
        switch (role)
        {
            case "marksman": return new Seat { aiRole = 0, lane = 0, spawn = new Vector3(45, 1, 41) };
            case "mage": return new Seat { aiRole = 0, lane = 1, spawn = new Vector3(41, 1, 45) };
            case "warrior": return new Seat { aiRole = 0, lane = 2, spawn = new Vector3(43, 1, 43) };
            case "assassin": return new Seat { aiRole = 1, lane = 1, spawn = new Vector3(47, 1, 43) };
            default: return new Seat { aiRole = 2, lane = 1, spawn = new Vector3(43, 1, 47) };
        }
    }

    void Start()
    {
        Time.timeScale = 0f;
        pick = Mathf.Max(0, System.Array.IndexOf(Gods, PlayerPrefs.GetString("aok_god", "Medjay")));
        court = PlayerPrefs.GetInt("aok_court", 0) == 0 ? Team.Kemet : Team.Olympus;
        BuildUI();
        RefreshSelection();
    }

    void BuildUI()
    {
        canvasGo = new GameObject("SelectionCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;
        var gold = new Color(0.92f, 0.76f, 0.35f);

        Panel(root, "Veil", new Color(0.01f, 0.01f, 0.02f, 1f), true);

        var title = Txt(root, "CHOOSE YOUR GOD", 54, gold, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -58), new Vector2(1100, 66));
        var sub = Txt(root, "One pool. No chains. Any god — either court.", 20,
            new Color(0.85f, 0.83f, 0.78f), TextAnchor.MiddleCenter);
        Place(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -106), new Vector2(1100, 30));

        rings = new Image[Gods.Length];
        for (int i = 0; i < Gods.Length; i++)
        {
            int col = i % 5, row = i / 5;
            float x = (col - 2) * 304f;
            float y = row == 0 ? -284f : -598f;
            var cell = Panel(root, "Cell_" + Gods[i], new Color(0.06f, 0.06f, 0.09f, 1f), false);
            Place(cell.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(x, y), new Vector2(280, 290));

            var ring = Panel(cell.transform, "Ring", gold, false);
            var rrt = ring.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(-6, -6); rrt.offsetMax = new Vector2(6, 6);
            ring.transform.SetAsFirstSibling();
            rings[i] = ring.GetComponent<Image>();

            var tex = Resources.Load<Texture2D>("Portraits/" + Gods[i]);
            if (tex != null)
            {
                var pic = new GameObject("Face");
                pic.transform.SetParent(cell.transform, false);
                var prt = pic.AddComponent<RectTransform>();
                Stretch(prt);
                var img = pic.AddComponent<Image>();
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                img.preserveAspect = false;
                img.raycastTarget = false;
            }
            else
            {
                var letter = Txt(cell.transform, Gods[i].Substring(0, 1), 90, gold, TextAnchor.MiddleCenter);
                Stretch(letter.rectTransform);
            }
            var strip = Panel(cell.transform, "NameStrip", new Color(0.02f, 0.02f, 0.03f, 0.88f), false);
            var srt = strip.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(0.5f, 0);
            srt.offsetMin = new Vector2(0, 0); srt.offsetMax = new Vector2(0, 64);
            strip.GetComponent<Image>().raycastTarget = false;
            var nm = Txt(strip.transform, GodNames[i], 22, Color.white, TextAnchor.MiddleCenter);
            Place(nm.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(272, 28));
            var rl = Txt(strip.transform, GodRoles[i], 14, gold, TextAnchor.MiddleCenter);
            Place(rl.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(272, 20));

            int idx = i;
            cell.AddComponent<Button>().onClick.AddListener(() => { pick = idx; Confirm(); });
        }

        // The court: two banners. The god's origin does not choose for him.
        var kGo = Panel(root, "CourtKemet", new Color(0.2f, 0.16f, 0.05f, 1f), false);
        Place(kGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(-205, -778), new Vector2(380, 46));
        kemetBtnText = Txt(kGo.transform, "FIGHT FOR KEMET", 19, Color.white, TextAnchor.MiddleCenter);
        Stretch(kemetBtnText.rectTransform);
        kemetBtn = kGo.AddComponent<Button>();
        kemetBtn.onClick.AddListener(() => { court = Team.Kemet; RefreshSelection(); });

        var oGo = Panel(root, "CourtOlympus", new Color(0.1f, 0.13f, 0.18f, 1f), false);
        Place(oGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(205, -778), new Vector2(380, 46));
        olyBtnText = Txt(oGo.transform, "FIGHT FOR OLYMPUS", 19, Color.white, TextAnchor.MiddleCenter);
        Stretch(olyBtnText.rectTransform);
        olyBtn = oGo.AddComponent<Button>();
        olyBtn.onClick.AddListener(() => { court = Team.Olympus; RefreshSelection(); });

    void RefreshSelection()
    {
        if (rings == null) return;
        for (int i = 0; i < rings.Length; i++)
            rings[i].color = i == pick
                ? new Color(0.92f, 0.76f, 0.35f, 1f)
                : new Color(0.92f, 0.76f, 0.35f, 0.12f);
        if (kemetBtnText != null)
            kemetBtnText.color = court == Team.Kemet ? new Color(0.92f, 0.76f, 0.35f) : Color.white;
        if (olyBtnText != null)
            olyBtnText.color = court == Team.Olympus ? new Color(0.6f, 0.79f, 0.98f) : Color.white;
    }

    void Confirm()
    {
        Reseat(pick, court);
        PlayerPrefs.SetString("aok_god", Gods[pick]);
        PlayerPrefs.SetInt("aok_court", court == Team.Kemet ? 0 : 1);
        PlayerPrefs.Save();
        canvasGo.SetActive(false);
    }

    void Reseat(int godIdx, Team chosenCourt)
    {
        var gm = GameManager.Instance;
        var heroes = FindObjectsByType<Hero>();
        Hero chosen = null;
        foreach (var h in heroes)
            if (h.heroName == GodNames[godIdx]) chosen = h;
        if (chosen == null) return;
        string roleKey = RoleKeys[godIdx];
        Team other = chosenCourt == Team.Kemet ? Team.Olympus : Team.Kemet;

        foreach (var h in heroes)
        {
            int gi = System.Array.IndexOf(GodNames, h.heroName);
            if (gi < 0) continue;
            bool isChosen = h == chosen;
            bool isMirror = RoleKeys[gi] == roleKey && !isChosen;
            Team seatCourt = isChosen ? chosenCourt : isMirror ? other : h.team;
            var seat = SeatFor(seatCourt, RoleKeys[gi]);
            h.team = seatCourt;
            h.transform.position = seat.spawn;
            h.isPlayer = isChosen;
            var ai = h.GetComponent<HeroAI>();
            if (isChosen)
            {
                if (ai != null) ai.enabled = false;
            }
            else
            {
                if (ai == null) ai = h.gameObject.AddComponent<HeroAI>();
                ai.enabled = true;
                ai.role = seat.aiRole;
                ai.homeLane = seat.lane;
            }
        }
        if (gm != null)
        {
            gm.playerHero = chosen;
            gm.enemyHero = null;
            foreach (var h in heroes)
                if (h.team != chosen.team) { gm.enemyHero = h; break; }
        }
        foreach (var b in FindObjectsByType<OverheadBar>()) b.RefreshTeam();
        var fc = FindFirstObjectByType<FollowCamera>();
        if (fc != null) fc.Retarget();
        var hs = FindFirstObjectByType<HudStage>();
        if (hs != null) hs.ResetPortrait();
    }

    static GameObject Panel(Transform parent, string name, Color color, bool stretch)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        if (stretch) Stretch(rt);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static Text Txt(Transform parent, string content, int size, Color color, TextAnchor align)
    {
        var go = new GameObject("Text");
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
