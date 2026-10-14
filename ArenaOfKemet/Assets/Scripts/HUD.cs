using UnityEngine;
using UnityEngine.UI;

// Reads the player hero every frame and paints the HUD.
// Every element is looked up by name; anything missing is skipped safely.
// Expected names: HpBar(Slider) HpText, MpBar(Slider) MpText, XpBar(Slider),
// LevelText, GoldText, KdText, ScoreText, ClockText, AnnounceText, AnnounceSub,
// CdQ CdW CdE CdR (Image, radial/filled), DeathVeil(+DeathText), EndPanel(+EndTitle, EndSub)
public class HUD : MonoBehaviour
{
    Slider hpBar, mpBar, xpBar;
    Text hpText, mpText, levelText, goldText, kdText, scoreText, clockText;
    Text announceText, announceSub, deathText, endTitle, endSub;
    Image cdQ, cdW, cdE, cdR;
    GameObject deathVeil, endPanel;
    float announceT = 0f;
    Hero P => GameManager.Instance != null ? GameManager.Instance.playerHero : null;

    T Find<T>(string name) where T : Component
    {
        var go = GameObject.Find(name);
        return go != null ? go.GetComponent<T>() : null;
    }

    void Start()
    {
        hpBar = Find<Slider>("HpBar"); hpText = Find<Text>("HpText");
        mpBar = Find<Slider>("MpBar"); mpText = Find<Text>("MpText");
        xpBar = Find<Slider>("XpBar");
        levelText = Find<Text>("LevelText"); goldText = Find<Text>("GoldText");
        kdText = Find<Text>("KdText"); scoreText = Find<Text>("ScoreText");
        clockText = Find<Text>("ClockText");
        announceText = Find<Text>("AnnounceText"); announceSub = Find<Text>("AnnounceSub");
        deathText = Find<Text>("DeathText");
        endTitle = Find<Text>("EndTitle"); endSub = Find<Text>("EndSub");
        cdQ = Find<Image>("CdQ"); cdW = Find<Image>("CdW");
        cdE = Find<Image>("CdE"); cdR = Find<Image>("CdR");
        deathVeil = GameObject.Find("DeathVeil");
        endPanel = GameObject.Find("EndPanel");
        if (deathVeil != null) deathVeil.SetActive(false);
        if (endPanel != null) endPanel.SetActive(false);

        var gm = GameManager.Instance;
        gm.OnAnnounce += (main, sub) =>
        {
            if (announceText != null) announceText.text = main;
            if (announceSub != null) announceSub.text = sub;
            announceT = 2.6f;
        };
        gm.OnGameOver += won =>
        {
            if (endPanel != null) endPanel.SetActive(true);
            if (endTitle != null) endTitle.text = won ? "VICTORY — KEMET STANDS" : "THE TEMPLE HAS FALLEN";
            if (endSub != null) endSub.text = won
                ? "The Shrine of Olympus lies in the river mud. THE ARCHIVE REMEMBERS."
                : "A Medjay does not end. He returns, he learns the lane, and he comes back with the flood.";
        };
        WireButton("BladeBtn", "blade");
        WireButton("ArmorBtn", "armor");
        WireButton("BootsBtn", "boots");
        WireCast("AbQ", 0); WireCast("AbW", 1); WireCast("AbE", 2); WireCast("AbR", 3);
    }

    void WireButton(string name, string gear)
    {
        var b = Find<Button>(name);
        if (b != null) b.onClick.AddListener(() => { if (P != null) P.BuyGear(gear); });
    }

    void WireCast(string name, int idx)
    {
        var b = Find<Button>(name);
        if (b == null) return;
        b.onClick.AddListener(() =>
        {
            if (P == null) return;
            if (idx == 0) P.CastQ(); else if (idx == 1) P.CastW();
            else if (idx == 2) P.CastE(); else P.CastR();
        });
    }

    void Update()
    {
        var p = P;
        if (p == null) return;
        var gm = GameManager.Instance;

        if (hpBar != null) { hpBar.maxValue = p.health.maxHp; hpBar.value = p.health.hp; }
        if (hpText != null) hpText.text = Mathf.Ceil(p.health.hp) + " / " + Mathf.Ceil(p.health.maxHp) +
            (p.health.shield > 0 ? "  (+" + Mathf.Ceil(p.health.shield) + " shield)" : "");
        if (mpBar != null) { mpBar.maxValue = p.maxMp; mpBar.value = p.mp; }
        if (mpText != null) mpText.text = Mathf.Floor(p.mp) + " / " + p.maxMp;
        if (xpBar != null) { xpBar.maxValue = p.xpNext; xpBar.value = p.xp; }
        if (levelText != null) levelText.text = "Level " + p.level;
        if (goldText != null) goldText.text = p.gold + " gold";
        if (kdText != null) kdText.text = "K / D  " + p.kills + " / " + p.deaths;
        if (scoreText != null) scoreText.text = "KEMET " + gm.scoreK + "  —  " + gm.scoreO + " OLYMPUS";
        if (clockText != null)
        {
            int mm = Mathf.FloorToInt(gm.matchTime / 60f), ss = Mathf.FloorToInt(gm.matchTime % 60f);
            clockText.text = mm.ToString("00") + ":" + ss.ToString("00");
        }
        SetCd(cdQ, p, 0); SetCd(cdW, p, 1); SetCd(cdE, p, 2); SetCd(cdR, p, 3);

        if (announceT > 0)
        {
            announceT -= Time.deltaTime;
            if (announceT <= 0)
            {
                if (announceText != null) announceText.text = "";
                if (announceSub != null) announceSub.text = "";
            }
        }
        if (deathVeil != null)
        {
            bool dead = p.health.dead;
            if (deathVeil.activeSelf != dead) deathVeil.SetActive(dead);
            if (dead && deathText != null) deathText.text = "YOU HAVE FALLEN — THE ARCHIVE HOLDS YOUR PLACE";
        }
    }

    void SetCd(Image img, Hero p, int idx)
    {
        if (img == null) return;
        img.fillAmount = p.cdMax[idx] <= 0 ? 0 : p.cd[idx] / p.cdMax[idx];
    }
}
