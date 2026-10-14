using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public readonly List<Unit> units = new List<Unit>();
    public readonly List<Structure> structures = new List<Structure>();
    public readonly List<JungleBeast> beasts = new List<JungleBeast>();
    public readonly List<Hero> heroes = new List<Hero>();
    public Hero playerHero;
    public Hero enemyHero;

    public float matchTime = 0f;
    public bool gameOver = false;
    public int scoreK = 0, scoreO = 0;
    bool firstBlood = false;
    Hero streakHero;
    int streakCount = 0;
    float streakT = -99f;
    float trickle = 0f;

    public Action<string, string> OnAnnounce;
    public Action<bool> OnGameOver;   // arg: playerWon

    // Boss summons: felling Apophis or the Sphinx earns a team one summon.
    string tokenK = null, tokenO = null;

    public bool HasToken(Team t) => t == Team.Kemet ? tokenK != null : tokenO != null;

    public void GrantSummon(Team t, string kind)
    {
        if (t == Team.Kemet) tokenK = kind; else tokenO = kind;
        Announce(kind.ToUpper() + " IS SLAIN",
            (t == Team.Kemet ? "Kemet" : "Olympus") + " earned his summon." +
            (t == Team.Kemet ? " Guardian — press T to call him into your strongest lane." : ""));
    }

    public bool TrySummon(Hero caller)
    {
        string kind = caller.team == Team.Kemet ? tokenK : tokenO;
        if (kind == null || gameOver) return false;

        // The boss joins the lane where caller's wave has pushed deepest.
        int bestLane = 1; float bestProg = float.NegativeInfinity;
        for (int li = 0; li < 3; li++)
        {
            float prog = 0f; bool any = false;
            foreach (var u in units)
            {
                if (u.team != caller.team || u is Hero || !u.IsAlive) continue;
                if (u is Minion mm && mm.lane != li) continue;
                float p = PathProgress(li, u.transform.position, caller.team);
                prog = any ? Mathf.Max(prog, p) : p; any = true;
            }
            if (any && prog > bestProg) { bestProg = prog; bestLane = li; }
        }

        var core = CoreOf(caller.team);
        var path = Lanes[bestLane];
        int wp0 = caller.team == Team.Kemet ? 1 : path.Length - 2;
        Vector3 pos = core.transform.position
            + (path[wp0] - core.transform.position).normalized * 3f;
        pos.y = 0;

        GameObject go;
        Material mat = new Material(TeamUtil.LitShader);
        if (kind == "Apophis")
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(2.0f, 1.5f, 5.8f);
            pos.y = 0.9f;
            mat.color = new Color(0.16f, 0.10f, 0.22f);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(3.4f, 2.8f, 4.4f);
            pos.y = 1.4f;
            mat.color = new Color(0.78f, 0.66f, 0.42f);
        }
        go.name = kind + "Summoned";
        go.transform.position = pos;
        go.GetComponent<Renderer>().material = mat;

        var hp = go.AddComponent<Health>();
        hp.Init(3800f);
        hp.armor = 10f;
        var sb = go.AddComponent<SummonedBoss>();
        sb.team = caller.team; sb.lane = bestLane; sb.wp = wp0;
        sb.moveSpeed = 3.0f;
        sb.attackDamage = 55f;
        sb.attackRange = 2.4f;
        sb.attackRate = 1.3f;
        sb.vsStructureMult = 3f;
        sb.health = hp;
        RegisterUnit(sb);
        hp.OnDied += killer => OnMinionDied(sb, killer);

        if (caller.team == Team.Kemet) tokenK = null; else tokenO = null;
        Announce(kind.ToUpper() + " RISES FOR " + (caller.team == Team.Kemet ? "KEMET" : "OLYMPUS"),
            "A boss marches the lane. Break him or be broken.");
        return true;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (var h in FindObjectsByType<Hero>())
        {
            heroes.Add(h);
            if (h.isPlayer) playerHero = h;
            else if (enemyHero == null) enemyHero = h;
        }
        // Self-heal the registries: any structure/unit that missed its
        // registration window gets swept in here, so towers always count.
        foreach (var s in FindObjectsByType<Structure>())
            RegisterStructure(s);
        foreach (var u in FindObjectsByType<Unit>())
            RegisterUnit(u);
        // Bosses sleep (inactive) until level 5 — register them anyway.
        foreach (var b in FindObjectsByType<JungleBeast>(FindObjectsInactive.Include))
            RegisterBeast(b);
        StartCoroutine(WaveLoop());
    }

    public void RegisterUnit(Unit u) { if (!units.Contains(u)) units.Add(u); }
    public void RegisterBeast(JungleBeast b) { if (!beasts.Contains(b)) beasts.Add(b); }
    public void RegisterStructure(Structure s) { if (!structures.Contains(s)) structures.Add(s); }

    public Structure CoreOf(Team t)
    {
        foreach (var s in structures) if (s.team == t && s.isCore) return s;
        return null;
    }

    // First standing structure of team t that attackers meet: the one
    // standing closest to the enemy's home corner, on any road geometry.
    public Structure NextStructure(Team t)
    {
        var foeCore = CoreOf(TeamUtil.Opponent(t));
        if (foeCore == null) return null;
        Structure best = null; float bd = float.MaxValue;
        foreach (var s in structures)
        {
            if (s.team != t || s.health.dead) continue;
            float d = Vector3.Distance(s.transform.position, foeCore.transform.position);
            if (d < bd) { bd = d; best = s; }
        }
        return best;
    }

    IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(3f);
        while (!gameOver)
        {
            SpawnWave(Team.Kemet);
            SpawnWave(Team.Olympus);
            yield return new WaitForSeconds(22f);
        }
    }

    // The three roads of the Gorge, Kemet corner (-50,-50) to Olympus (50,50).
    // TOP hugs the northwest rim, MID runs the diagonal, BOT the southeast rim.
    public static readonly Vector3[][] Lanes =
    {
        new[] { new Vector3(-50, 0, -50), new Vector3(-50, 0, 38), new Vector3(-38, 0, 50), new Vector3(38, 0, 50), new Vector3(50, 0, 38), new Vector3(50, 0, 50) },
        new[] { new Vector3(-50, 0, -50), new Vector3(0, 0, 0), new Vector3(50, 0, 50) },
        new[] { new Vector3(-50, 0, -50), new Vector3(-38, 0, -50), new Vector3(38, 0, -50), new Vector3(50, 0, -38), new Vector3(50, 0, 50) },
    };

    public static float PathTotal(int lane)
    {
        var p = Lanes[lane]; float s = 0f;
        for (int i = 0; i < p.Length - 1; i++) s += Vector3.Distance(p[i], p[i + 1]);
        return s;
    }

    // How far along the lane `pos` has marched, measured from `from`'s home.
    public static float PathProgress(int lane, Vector3 pos, Team from)
    {
        var path = Lanes[lane];
        float best = float.MaxValue, prog = 0f, acc = 0f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 a = path[i], b = path[i + 1];
            Vector3 ab = b - a; float len = ab.magnitude;
            float t = len > 0 ? Mathf.Clamp01(Vector3.Dot(pos - a, ab) / (len * len)) : 0f;
            float d = Vector3.Distance(pos, a + ab * t);
            if (d < best) { best = d; prog = acc + len * t; }
            acc += len;
        }
        if (from == Team.Olympus) prog = acc - prog;
        return prog;
    }

    // Distance from `pos` to the lane's road.
    public static float PathDistance(int lane, Vector3 pos)
    {
        var path = Lanes[lane];
        float best = float.MaxValue;
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 a = path[i], b = path[i + 1];
            Vector3 ab = b - a; float len = ab.magnitude;
            float t = len > 0 ? Mathf.Clamp01(Vector3.Dot(pos - a, ab) / (len * len)) : 0f;
            float d = Vector3.Distance(pos, a + ab * t);
            if (d < best) best = d;
        }
        return best;
    }

    // The point at fraction t of the road, measured from `from`'s home.
    public static Vector3 PathPoint(int lane, float t, Team from)
    {
        var path = Lanes[lane];
        if (from == Team.Olympus) t = 1f - t;
        float want = PathTotal(lane) * Mathf.Clamp01(t);
        for (int i = 0; i < path.Length - 1; i++)
        {
            float len = Vector3.Distance(path[i], path[i + 1]);
            if (want <= len) return Vector3.Lerp(path[i], path[i + 1], len > 0 ? want / len : 0f);
            want -= len;
        }
        return path[path.Length - 1];
    }

    // Which road a structure stands on, by best fit to the paths.
    public static int LaneOf(Structure s)
    {
        int best = 1; float bd = float.MaxValue;
        for (int li = 0; li < 3; li++)
        {
            float d = PathDistance(li, s.transform.position);
            if (d < bd) { bd = d; best = li; }
        }
        return best;
    }

    // True when the enemy's GATE tower on this road has fallen — the law
    // that turns a wave's front fighter into a super minion.
    bool EnemyInnerDown(Team foe, int li)
    {
        foreach (var s in structures)
        {
            if (s.team != foe || s.isCore) continue;
            if (LaneOf(s) != li) continue;
            float prog = PathProgress(li, s.transform.position, foe) / PathTotal(li);
            if (prog > 0.05f && prog < 0.20f) return s.health.dead;
        }
        return false;
    }

    Minion SpawnMinion(Team side, int li, int wp0, Vector3 pos, string kind,
        float hp, float dmg, float range, float rate, float speed, float scale, float structMult)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = (side == Team.Kemet ? "Kemet " : "Olympus ") + kind;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<Renderer>().material = TeamUtil.Mat(side);
        var h = go.AddComponent<Health>();
        h.Init(hp);
        h.armor = li == 1 ? 6f : 2f;   // mid-road minions carry heavier armor, as in HoK
        var m = go.AddComponent<Minion>();
        m.team = side; m.ranged = range > 2f; m.lane = li; m.wp = wp0;
        m.moveSpeed = speed;
        m.attackDamage = dmg;
        m.attackRange = range;
        m.attackRate = rate;
        m.vsStructureMult = structMult;
        m.health = h;
        var mbar = go.AddComponent<OverheadBar>();
        mbar.height = 1.8f; mbar.showText = false;
        RegisterUnit(m);
        h.OnDied += killer => OnMinionDied(m, killer);
        return m;
    }

    void SpawnWave(Team side)
    {
        var core = CoreOf(side);
        if (core == null) return;
        Team foe = TeamUtil.Opponent(side);
        for (int li = 0; li < 3; li++)
        {
            var path = Lanes[li];
            int wp0 = side == Team.Kemet ? 1 : path.Length - 2;
            Vector3 fwd = path[wp0] - core.transform.position; fwd.y = 0; fwd.Normalize();
            Vector3 perp = new Vector3(-fwd.z, 0, fwd.x);
            Vector3 At(float depth, float off) =>
                core.transform.position + fwd * depth + perp * off + Vector3.up * 0.9f;

            // The HoK wave: one fighter in front, two mages behind him.
            bool super = EnemyInnerDown(foe, li);
            SpawnMinion(side, li, wp0, At(3.4f, 0f), super ? "Super Warrior" : "Warrior",
                super ? 1000f : 430f, super ? 34f : 20f, 0.9f, 1.0f, 3.4f, super ? 1.35f : 0.9f, 1f);
            SpawnMinion(side, li, wp0, At(2.2f, -1.7f), "Mage", 300f, 26f, 4.6f, 1.0f, 3.4f, 0.9f, 1f);
            SpawnMinion(side, li, wp0, At(2.2f, 1.7f), "Mage", 300f, 26f, 4.6f, 1.0f, 3.4f, 0.9f, 1f);
            // Siege joins from 4:00; the cannon takes its place from 10:00.
            if (matchTime >= 240f)
            {
                bool cannon = matchTime >= 600f;
                SpawnMinion(side, li, wp0, At(1.0f, 0f), cannon ? "Cannon" : "Ballista",
                    cannon ? 1400f : 950f, cannon ? 40f : 32f, 1.4f, 1.5f, 2.9f,
                    cannon ? 1.55f : 1.35f, cannon ? 3f : 2f);
            }
        }
    }

    bool ancientsAwake = false;
    bool campsAwake = false;
    bool temperamentsTold = false;

    void Update()
    {
        if (gameOver) return;
        matchTime += Time.deltaTime;

        // The jungle camps wake at 0:30, as the reference commands.
        if (!campsAwake && matchTime >= 30f)
        {
            campsAwake = true;
            foreach (var b in beasts)
                if (!b.isBoss) b.gameObject.SetActive(true);
        }

        // The HoK law: at 4:00 the tower shields FALL, all at once —
        // the bubbles burst, and hero mitigation steps down with them.
        if (matchTime >= 240f)
        {
            bool any = false;
            foreach (var s in structures)
            {
                if (s == null || s.isCore) continue;
                var b = s.transform.Find("TowerShield");
                if (b != null && b.gameObject.activeSelf) { s.DropShield(); any = true; }
            }
            if (any) Announce("THE TOWER SHIELDS FALL",
                "Four minutes in — the stone cracks. Hero wounds bite deeper now.");
        }

        // At the war's first breath, Olympus' temperaments are revealed —
        // read them, guardian, and know what kind of war this will be.
        if (!temperamentsTold && matchTime > 2f)
        {
            temperamentsTold = true;
            string line = "";
            foreach (var h in heroes)
            {
                if (h.team != Team.Olympus) continue;
                var ai = h.GetComponent<HeroAI>();
                if (ai == null) continue;
                if (line.Length > 0) line += "  ·  ";
                line += h.heroName + ": " + HeroAI.TemperamentName(ai.temperament);
            }
            if (line.Length > 0) Announce("OLYMPUS RIDES TO WAR", line);
        }

        // The ancients wake when the first hero reaches level 5.
        if (!ancientsAwake)
        {
            foreach (var h in heroes)
            {
                if (h.level < 5) continue;
                ancientsAwake = true;
                foreach (var b in beasts)
                    if (b.isBoss) b.gameObject.SetActive(true);
                Announce("THE ANCIENTS AWAKEN",
                    "A hero has reached level 5 — Apophis and the Sphinx stir in their jungle homes.");
                break;
            }
        }
        trickle += Time.deltaTime;
        if (trickle >= 2f)
        {
            trickle = 0f;
            foreach (var h in heroes) h.gold += 3;
        }
        // Fountain healing.
        foreach (var h in heroes)
        {
            if (h == null || !h.IsAlive) continue;
            var core = CoreOf(h.team);
            if (core != null && Vector3.Distance(h.transform.position, core.transform.position) < 5.5f)
                h.health.Heal(h.health.maxHp * 0.14f * Time.deltaTime);
        }
        units.RemoveAll(u => u == null);
    }

    void OnMinionDied(Minion m, GameObject killer)
    {
        units.Remove(m);
        Team foeTeam = TeamUtil.Opponent(m.team);
        // Lane economy, per the reference: mage minions on the GOLD road
        // (Kemet's Bot, Olympus' Top) pay +40 gold to the last hitter;
        // on the EXP road (Kemet's Top, Olympus' Bot) they pay +65 XP.
        bool goldRoad = (m.team == Team.Kemet && m.lane == 2) || (m.team == Team.Olympus && m.lane == 0);
        bool xpRoad = (m.team == Team.Kemet && m.lane == 0) || (m.team == Team.Olympus && m.lane == 2);
        foreach (var h in heroes)
        {
            if (h.team != foeTeam) continue;
            if (Vector3.Distance(h.transform.position, m.transform.position) < 14f)
                h.GainXp(22f + (xpRoad && m.ranged ? 65f : 0f));
        }
        var kh = killer != null ? killer.GetComponent<Hero>() : null;
        if (kh != null) kh.gold += 12 + (goldRoad && m.ranged ? 40 : 0);
        Destroy(m.gameObject, 0.1f);
    }

    public void OnHeroDied(Hero hero, GameObject killer)
    {
        hero.deaths++;
        var kh = killer != null ? killer.GetComponent<Hero>() : null;
        if (kh != null) { kh.kills++; kh.gold += 300; kh.GainXp(120f); }
        // The killer speaks — every god has a tongue for this moment.
        if (kh != null && AudioDirector.Instance != null)
            AudioDirector.Instance.PlayKillLine(kh.heroName);
        if (hero.team == Team.Kemet) scoreO++; else scoreK++;

        // Streaks, in the tongue of his games: kill again before ten
        // seconds cool, and the arena NAMES it — in gold and in voice.
        if (kh != null)
        {
            if (streakHero == kh && matchTime - streakT < 10f) streakCount++;
            else streakCount = 1;
            streakHero = kh; streakT = matchTime;
        }
        if (kh != null && streakCount >= 2)
        {
            string title = streakCount >= 5 ? "PENTAKILL" : streakCount == 4 ? "QUADRA KILL"
                : streakCount == 3 ? "TRIPLE KILL" : "DOUBLE KILL";
            Announce(title + "!", kh.heroName + " — " + streakCount + " gods have fallen to him.");
            if (AudioDirector.Instance != null)
                AudioDirector.Instance.PlayAnnouncer(
                    streakCount >= 5 ? "penta" : streakCount == 4 ? "quadra"
                    : streakCount == 3 ? "triple" : "double");
        }
        else if (!firstBlood)
        {
            firstBlood = true;
            Announce("FIRST BLOOD", (kh != null ? kh.heroName : "THE JUNGLE") + " draws first blood. The Mother watches.");
            if (AudioDirector.Instance != null) AudioDirector.Instance.PlayAnnouncer("firstblood");
        }
        else
        {
            Announce(hero.heroName + " HAS FALLEN",
                hero.team == Team.Kemet ? "Kemet mourns — but the Archive remembers."
                                         : "Olympus bleeds. The river runs gold.");
        }
        StartCoroutine(Respawn(hero));
    }

    IEnumerator Respawn(Hero hero)
    {
        // The HoK law: the grave holds you longer every time you fall —
        // first death 10 seconds, second 15, third 20, and +5 each after.
        float wait = 5f + hero.deaths * 5f;
        hero.gameObject.SetActive(false);
        yield return new WaitForSeconds(wait);
        if (gameOver) yield break;
        var core = CoreOf(hero.team);
        float dir = hero.team == Team.Kemet ? 1f : -1f;
        hero.transform.position = core.transform.position + new Vector3(dir * 3.5f, 0.9f, 0);
        hero.health.dead = false;
        hero.health.hp = hero.health.maxHp;
        hero.health.shield = 0;
        hero.mp = hero.maxMp;
        hero.moveTarget = null;
        hero.gameObject.SetActive(true);
        Announce(hero.heroName + " RETURNS", "");
    }

    public void OnStructureDown(Structure s, GameObject killer)
    {
        // Stone must be HEARD to fall — and seen: a dust-bright burst.
        if (AudioDirector.Instance != null) AudioDirector.Instance.PlayTowerFall();
        HitFlash.Spawn(s.transform.position + Vector3.up * 2.5f,
            new Color(0.85f, 0.7f, 0.45f), s.isCore ? 7f : 4.5f, 0.5f);
        if (s.isCore)
        {
            EndGame(s.team == Team.Olympus);
            return;
        }
        // High-ground law: when a GATE tower falls, the enemy wave
        // caught in its ground staggers — 75% slow for 3 seconds — so
        // the victors' wave clears the choke clean. (Super minions on
        // this road already rise by the EnemyInnerDown law.)
        int li = LaneOf(s);
        float prog = PathProgress(li, s.transform.position, s.team) / PathTotal(li);
        if (prog > 0.05f && prog < 0.20f)
        {
            foreach (var u in units)
                if (u != null && u.team != s.team && u is Minion && u.IsAlive &&
                    Vector3.Distance(u.transform.position, s.transform.position) < s.attackRange + s.radius + 0.5f)
                { u.slowT = 3f; u.slowPct = 0.75f; }
        }
        Announce(s.team == Team.Olympus ? "GREEK TOWER FALLEN" : "EGYPTIAN TOWER FALLEN",
            s.team == Team.Olympus ? "Kemet advances. The river runs gold." : "Olympus advances. Hold the river!");
        var kh = killer != null ? killer.GetComponent<Hero>() : null;
        if (kh != null) { kh.gold += 250; kh.GainXp(60f); }
    }

    void EndGame(bool playerWon)
    {
        gameOver = true;
        OnGameOver?.Invoke(playerWon);
    }

    public void Announce(string main, string sub) => OnAnnounce?.Invoke(main, sub);
}
