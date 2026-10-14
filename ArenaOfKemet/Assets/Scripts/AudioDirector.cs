using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The voice of the arena. A war theme loops under everything — drone,
// drums and a plucked lament off the river — and when a god takes a
// life, the KILLER SPEAKS, while the music ducks its head to listen.
// Shots sing by who loosed them; towers fall like the world ends.
//
// PLAYER LAW: the one who plays rules the sound. Two sliders — MUSIC
// and EFFECTS — behind the gear at the top-right (or the Esc key),
// saved between sessions, on PC and mobile alike. Sounds live in
// Resources/Music, Resources/Kills, Resources/Sfx; a missing file
// never breaks the war — silence, then carry on.
public class AudioDirector : MonoBehaviour
{
    public static AudioDirector Instance;
    AudioSource music;
    AudioSource voice;
    AudioSource sfx;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> lastPlay = new Dictionary<string, float>();

    float musicVol = 0.8f;   // 0..1, the player's word
    float sfxVol = 0.9f;     // 0..1 — voices and effects ride together
    GameObject panel;

    float MusicNow() { return 0.5f * musicVol; }

    void Awake()
    {
        Instance = this;
        musicVol = PlayerPrefs.GetFloat("aok_music", 0.8f);
        sfxVol = PlayerPrefs.GetFloat("aok_sfx", 0.9f);

        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        voice = gameObject.AddComponent<AudioSource>();
        sfx = gameObject.AddComponent<AudioSource>();
        ApplyVolumes();

        var theme = Resources.Load<AudioClip>("Music/theme");
        if (theme != null) { music.clip = theme; music.Play(); }
        foreach (var n in new[] { "bow", "magic", "tower", "hit", "towerfall" })
        {
            var c = Resources.Load<AudioClip>("Sfx/" + n);
            if (c != null) clips[n] = c;
        }
        BuildSettingsUI();
    }

    void ApplyVolumes()
    {
        if (music != null) music.volume = MusicNow();
        if (voice != null) voice.volume = sfxVol;
        if (sfx != null) sfx.volume = sfxVol;
    }

    public void SetMusic(float v)
    {
        musicVol = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("aok_music", musicVol);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    public void SetSfx(float v)
    {
        sfxVol = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("aok_sfx", sfxVol);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    // ---------- the sounds of war ----------

    void PlaySfx(string n, float vol)
    {
        if (!clips.TryGetValue(n, out var c)) return;
        // Ten bows at once must not become one loud crack — throttle.
        if (lastPlay.TryGetValue(n, out var t) && Time.time - t < 0.06f) return;
        lastPlay[n] = Time.time;
        sfx.PlayOneShot(c, vol);
    }

    // Every shot in the war passes one gate (Combat.SpawnProjectile) —
    // the sound is chosen by WHO loosed it: tower, caster, or bow.
    public void PlayShot(GameObject source)
    {
        if (source == null) return;
        string n = "bow";
        if (source.GetComponent<Structure>() != null) n = "tower";
        else
        {
            var h = source.GetComponent<Hero>();
            if (h != null && (h.heroName == "THOTH" || h.heroName == "ZEUS" || h.heroName == "ISIS"))
                n = "magic";
        }
        PlaySfx(n, n == "tower" ? 0.9f : 0.55f);
    }

    public void PlayHit()
    {
        PlaySfx("hit", 0.4f);
    }

    // A tower comes DOWN — boom, cracking stone, rubble striking earth.
    public void PlayTowerFall()
    {
        PlaySfx("towerfall", 1f);
    }

    static string ClipName(string heroName)
    {
        switch (heroName)
        {
            case "THE MEDJAY": return "Medjay";
            case "THOTH": return "Thoth";
            case "SEKHMET": return "Sekhmet";
            case "ANUBIS": return "Anubis";
            case "ISIS": return "Isis";
            case "ARES": return "Ares";
            case "ARTEMIS": return "Artemis";
            case "ZEUS": return "Zeus";
            case "HADES": return "Hades";
            case "ATHENA": return "Athena";
            default: return null;
        }
    }

    // The arena's own announcer — FIRST BLOOD, DOUBLE KILL, and the
    // rest of the litany, spoken over the ducked music.
    public void PlayAnnouncer(string n)
    {
        var clip = Resources.Load<AudioClip>("Announcer/" + n);
        if (clip == null) return;
        voice.PlayOneShot(clip);
        StartCoroutine(Duck(clip.length));
    }

    public void PlayKillLine(string heroName)
    {
        string n = ClipName(heroName);
        if (n == null) return;
        var clip = Resources.Load<AudioClip>("Kills/" + n);
        if (clip == null) return;
        voice.PlayOneShot(clip);
        StartCoroutine(Duck(clip.length));
    }

    IEnumerator Duck(float seconds)
    {
        music.volume = MusicNow() * 0.35f;
        yield return new WaitForSeconds(seconds + 0.15f);
        music.volume = MusicNow();
    }

    // ---------- the settings panel (built by hand, like the minimap) ----------

    void BuildSettingsUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
        var canvasGo = new GameObject("SettingsCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var gold = new Color(0.92f, 0.76f, 0.35f);

        // The gear — always awake at the top-right corner.
        var gear = MakePanel(canvasGo.transform, "Gear", new Color(0, 0, 0, 0.55f),
            new Vector2(1, 1), new Vector2(-34, -34), new Vector2(52, 52));
        var gearText = MakeText(gear.transform, "⚙", 30, gold, TextAnchor.MiddleCenter);
        gearText.rectTransform.anchorMin = Vector2.zero;
        gearText.rectTransform.anchorMax = Vector2.one;
        gearText.rectTransform.offsetMin = Vector2.zero;
        gearText.rectTransform.offsetMax = Vector2.zero;
        gear.AddComponent<Button>().onClick.AddListener(() => panel.SetActive(!panel.activeSelf));

        // The panel itself — dark stone, two sliders, the player's law.
        panel = MakePanel(canvasGo.transform, "SettingsPanel", new Color(0.03f, 0.03f, 0.05f, 0.92f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 230));
        var title = MakeText(panel.transform, "SETTINGS", 22, gold, TextAnchor.MiddleCenter);
        Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(300, 34));

        MakeRow(panel.transform, font, gold, "MUSIC", -78, musicVol, SetMusic);
        MakeRow(panel.transform, font, gold, "EFFECTS", -128, sfxVol, SetSfx);

        var close = MakePanel(panel.transform, "Close", new Color(0.25f, 0.2f, 0.08f, 1f),
            new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(120, 36));
        var closeText = MakeText(close.transform, "CLOSE", 16, Color.white, TextAnchor.MiddleCenter);
        closeText.rectTransform.anchorMin = Vector2.zero;
        closeText.rectTransform.anchorMax = Vector2.one;
        closeText.rectTransform.offsetMin = Vector2.zero;
        closeText.rectTransform.offsetMax = Vector2.zero;
        close.AddComponent<Button>().onClick.AddListener(() => panel.SetActive(false));

        panel.SetActive(false);
    }

    void MakeRow(Transform parent, Font font, Color gold, string label, float y, float value,
        UnityEngine.Events.UnityAction<float> onChange)
    {
        var lab = MakeText(parent, label, 15, gold, TextAnchor.MiddleLeft);
        Place(lab.rectTransform, new Vector2(0.5f, 1), new Vector2(-155, y), new Vector2(90, 26));

        var sliderGo = new GameObject(label + "Slider");
        sliderGo.transform.SetParent(parent, false);
        var rt = sliderGo.AddComponent<RectTransform>();
        Place(rt, new Vector2(0.5f, 1), new Vector2(35, y), new Vector2(230, 22));
        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;

        var bg = MakePanel(sliderGo.transform, "BG", new Color(1, 1, 1, 0.18f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 8));
        bg.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
        bg.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
        var fill = MakePanel(sliderGo.transform, "Fill", gold,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 8));
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0.5f);
        fillRt.anchorMax = new Vector2(1, 0.5f);
        slider.fillRect = fillRt;
        var knob = MakePanel(sliderGo.transform, "Knob", Color.white,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
        slider.handleRect = knob.GetComponent<RectTransform>();
        slider.targetGraphic = knob.GetComponent<Image>();
        slider.onValueChanged.AddListener(onChange);
    }

    static GameObject MakePanel(Transform parent, string name, Color color,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Place(rt, anchor, pos, size);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static Text MakeText(Transform parent, string content, int size, Color color, TextAnchor anchor)
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
