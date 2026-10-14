using UnityEngine;

// THE HERO — rebuilt from zero on the simplest input road there is:
// direct key reads, no device layers between the player's hand and the man.
//
// THE CONTROL LAW (Scroll Cipher's word):
// - KEYS move him — ONLY while held. The instant a key is let go, he
//   stops dead. He NEVER walks alone, never drifts, never goes anywhere
//   by himself. Full control means his feet answer to a held hand, only.
// - THE MOUSE ONLY SHOOTS. Click any enemy you can see and a golden shot
//   flies to it — the Medjay carries a bow. The mouse never moves him.
// - He hunts NOTHING uninvited: no auto-chasing, towers never auto-target.
//   He shoots on his own only what already stands within his reach.
// - Q dash / W Falcon Strike / E Shield of Ma'at / R Tiamat's Flood,
//   B recall / F restore / T summon a beaten boss / 1-2-3 run to a lane.
public class Hero : Unit
{
    public bool isPlayer = false;
    public string heroName = "HERO";

    // Role shaping, set by the builder: the lioness is harder to kill,
    // the assassin hits harder and runs faster, the bowman is frailer.
    public float roleHpMult = 1f;
    public float roleDmgMult = 1f;
    public float roleSpdMult = 1f;

    public float maxMp = 220f;
    public float mp = 220f;
    public int level = 1;
    public float xp = 0f;
    public float xpNext = 90f;
    public int gold = 150;
    public int kills = 0;
    public int deaths = 0;
    public int gearBlade = 0, gearArmor = 0, gearBoots = 0;

    public float[] cd = new float[4];
    public float[] cdMax = { 6f, 8f, 12f, 55f };
    public float[] mpCost = { 30f, 40f, 35f, 80f };
    float shieldT = 0f;
    public float buffT = 0f;

    public float healCd = 0f;
    public float recallT = -1f;
    public const float RecallChannel = 5f;
    float recallLastHp = 0f;
    GameObject recallRing;

    protected override void Awake()
    {
        base.Awake();
        health.Init(640f);
        health.armor = 6f;
        ApplyStats(true);
    }

    void Start()
    {
        GameManager.Instance.RegisterUnit(this);
        health.OnDied += killer => GameManager.Instance.OnHeroDied(this, killer);
    }

    void ApplyStats(bool keepFrac)
    {
        float frac = 1f;
        if (keepFrac) frac = health.Frac;
        health.maxHp = (640f + (level - 1) * 78f + gearArmor * 180f) * roleHpMult;
        health.armor = 6f + (level - 1) * 1.2f + gearArmor * 4f;
        health.hp = health.maxHp * frac;
        maxMp = 220f + (level - 1) * 26f;

        float dmgMult = 1f;
        float speedMult = 1f;
        if (buffT > 0f)
        {
            dmgMult = 1.25f;
            speedMult = 1.12f;
        }

        float baseMove = 7.0f;
        if (isPlayer) baseMove = 7.4f;

        attackDamage = (58f + (level - 1) * 8f + gearBlade * 22f) * dmgMult * roleDmgMult;
        moveSpeed = (baseMove + gearBoots * 0.9f) * speedMult * roleSpdMult;
    }

    public void GrantBuff() { buffT = 60f; ApplyStats(true); }

    // Whom the hero fights on his own. The PLAYER hero only shoots what
    // already stands within his weapon's reach — nothing is ever chased,
    // and structures are never chosen for him. The AI hero hunts freely.
    protected override Transform FindTarget()
    {
        if (!isPlayer) return base.FindTarget();

        Transform best = null; float bd = attackRange;
        if (GameManager.Instance == null) return null;
        foreach (var u in GameManager.Instance.units)
        {
            if (u.team == team || !u.IsAlive) continue;
            float d = Vector3.Distance(transform.position, u.transform.position) - 0.8f;
            if (d < bd) { bd = d; best = u.transform; }
        }
        return best;
    }

    protected override void Update()
    {
        if (!IsAlive) return;
        if (buffT > 0f)
        {
            buffT -= Time.deltaTime;
            if (buffT <= 0f) { buffT = 0f; ApplyStats(true); }
        }
        for (int i = 0; i < 4; i++) cd[i] = Mathf.Max(0, cd[i] - Time.deltaTime);
        if (shieldT > 0f)
        {
            shieldT -= Time.deltaTime;
            if (shieldT <= 0f) health.shield = 0;
        }
        healCd = Mathf.Max(0, healCd - Time.deltaTime);
        mp = Mathf.Min(maxMp, mp + 3.2f * Time.deltaTime);
        health.Heal(1.1f * Time.deltaTime);
        if (isPlayer) ReadInput();
        if (recallT >= 0f) { TickRecall(); return; }
        base.Update();
    }

    GameObject SpawnRing(Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = transform.position + Vector3.up * 0.15f;
        go.transform.localScale = new Vector3(2.8f, 0.12f, 2.8f);
        var rend = go.GetComponent<Renderer>();
        var mat = new Material(TeamUtil.LitShader);
        mat.color = c;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c * 1.8f);
        rend.material = mat;
        return go;
    }

    public void StartRecall()
    {
        if (recallT >= 0f || !IsAlive) return;
        recallT = 0f;
        recallLastHp = health.hp;
        moveTarget = null;
        recallRing = SpawnRing(new Color(0.35f, 0.75f, 1f));
        if (isPlayer) GameManager.Instance.Announce("RECALLING", "Hold still, guardian — the temple calls you home.");
    }

    public void CancelRecall(string why)
    {
        if (recallT < 0f) return;
        recallT = -1f;
        if (recallRing != null) Destroy(recallRing);
        if (isPlayer && !string.IsNullOrEmpty(why)) GameManager.Instance.Announce(why, "");
    }

    void TickRecall()
    {
        if (health.hp < recallLastHp - 0.01f) { CancelRecall("RECALL BROKEN"); return; }
        recallLastHp = health.hp;
        recallT += Time.deltaTime;
        if (recallRing != null)
        {
            recallRing.transform.position = transform.position + Vector3.up * 0.15f;
            float p = 1f + recallT / RecallChannel * 0.6f;
            recallRing.transform.localScale = new Vector3(2.8f * p, 0.12f, 2.8f * p);
        }
        if (recallT >= RecallChannel)
        {
            recallT = -1f;
            if (recallRing != null) Destroy(recallRing);
            var core = GameManager.Instance.CoreOf(team);
            float dir = -1f;
            if (team == Team.Kemet) dir = 1f;
            transform.position = core.transform.position + new Vector3(dir * 3.5f, 0.9f, 0);
            moveTarget = null;
            if (isPlayer) GameManager.Instance.Announce("HOME AGAIN", "The fountain restores you.");
        }
    }

    public void TryHeal()
    {
        if (healCd > 0f || !IsAlive) return;
        healCd = 90f;
        health.Heal(health.maxHp * 0.20f);
        var ring = SpawnRing(new Color(0.3f, 1f, 0.45f));
        Destroy(ring, 0.7f);
        if (isPlayer) GameManager.Instance.Announce("RESTORED", "The healers' gift: +20% health.");
    }

    void ReadInput()
    {
        // Direct key reads — nothing between the hand and the hero.
        if (Input.GetKeyDown(KeyCode.B)) StartRecall();
        if (Input.GetKeyDown(KeyCode.F)) TryHeal();
        if (Input.GetKeyDown(KeyCode.T)) GameManager.Instance.TrySummon(this);

        if (recallT >= 0f)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
                || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.W)
                || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.R))
            {
                CancelRecall("RECALL CANCELLED");
            }
            return;
        }

        // --- Movement: keys only. ---
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.z += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.z -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;

        if (move.sqrMagnitude > 0.01f)
        {
            move.Normalize();
            transform.position += move * moveSpeed * Time.deltaTime;
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, -56f, 56f),
                transform.position.y,
                Mathf.Clamp(transform.position.z, -56f, 56f));
            transform.rotation = Quaternion.LookRotation(move);
            moveTarget = null;
        }
        // No key held = he does not move. Not one step. Ever.

        // --- The mouse ONLY shoots. ---
        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                bool fired = false;
                if (Physics.Raycast(ray, out RaycastHit hit, 300f))
                {
                    var hp = hit.collider.GetComponent<Health>();
                    Unit u = hit.collider.GetComponent<Unit>();
                    Structure s = hit.collider.GetComponent<Structure>();
                    var beast = hit.collider.GetComponent<JungleBeast>();
                    Team? hitTeam = null;
                    if (u != null) hitTeam = u.team;
                    else if (s != null) hitTeam = s.team;

                    if (hp != null && !hp.dead && ((hitTeam.HasValue && hitTeam.Value != team) || beast != null))
                    {
                        // The Medjay carries a BOW: a clicked enemy is shot
                        // at any distance the eye can see.
                        Combat.SpawnProjectile(transform.position + Vector3.up * 1.4f,
                            hit.collider.transform, attackDamage, gameObject, team, false, 0f, 1f);
                        var look = hit.collider.transform.position - transform.position;
                        look.y = 0;
                        if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look);
                        fired = true;
                    }
                }
                if (!fired)
                {
                    // No enemy under the cursor: a red flash marks the spot
                    // — and still, he does not move.
                    var plane = new Plane(Vector3.up, Vector3.zero);
                    if (plane.Raycast(ray, out float enter))
                    {
                        var ring = SpawnRing(new Color(1f, 0.25f, 0.2f));
                        ring.transform.position = ray.GetPoint(enter) + Vector3.up * 0.15f;
                        Destroy(ring, 0.4f);
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Q)) CastQ();
        if (Input.GetKeyDown(KeyCode.W)) CastW();
        if (Input.GetKeyDown(KeyCode.E)) CastE();
        if (Input.GetKeyDown(KeyCode.R)) CastR();
        if (Input.GetKeyDown(KeyCode.Alpha1)) RunToLane(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) RunToLane(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) RunToLane(2);
    }

    void RunToLane(int laneIdx)
    {
        var path = GameManager.Lanes[laneIdx];
        Vector3 dest = path[path.Length / 2];   // the road's river crossing
        Unit front = null; float bestProg = float.NegativeInfinity;
        foreach (var u in GameManager.Instance.units)
        {
            if (u.team != team || u is Hero || !u.IsAlive) continue;
            if (u is Minion mm && mm.lane != laneIdx) continue;
            float p = GameManager.PathProgress(laneIdx, u.transform.position, team);
            if (p > bestProg) { bestProg = p; front = u; }
        }
        if (front != null) dest = front.transform.position;
        moveTarget = dest;
    }

    Transform BestAbilityTarget(float range) { return Combat.FindNearestEnemy(transform.position, team, range); }

    // ── THE GOD-GIVEN KITS ─────────────────────────────────────────
    // Every god fights with HIS OWN hands now. The gates (cooldown,
    // mana, level) are one law for all; what happens past the gate is
    // the god's alone. The AI casts through these same doors.

    public static string[] KitNames(string god)
    {
        switch (god)
        {
            case "THOTH": return new[] { "STEP", "BOLT", "WARD", "THE WORD" };
            case "SEKHMET": return new[] { "POUNCE", "ROAR", "FRENZY", "PLAGUE" };
            case "ANUBIS": return new[] { "SHADOW", "WEIGHING", "MIST", "JUDGMENT" };
            case "ISIS": return new[] { "GLIDE", "WINGS", "EMBRACE", "REBIRTH" };
            case "ARES": return new[] { "CHARGE", "RUIN", "BLOOD", "WRATH" };
            case "ARTEMIS": return new[] { "ROLL", "MOON", "SNARE", "VOLLEY" };
            case "ZEUS": return new[] { "STORM", "BOLT", "AEGIS", "WRATH" };
            case "HADES": return new[] { "STYX", "REND", "SHROUD", "KINGDOM" };
            case "ATHENA": return new[] { "DASH", "SPEAR", "AEGIS", "PHALANX" };
            default: return new[] { "DASH", "FALCON", "SHIELD", "FLOOD" };
        }
    }

    Color CourtColor()
    {
        return team == Team.Kemet ? new Color(1f, 0.8f, 0.3f) : new Color(0.6f, 0.79f, 0.98f);
    }

    void DashForward(float dist, float dmg)
    {
        Vector3 dir = transform.forward;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) dir = team == Team.Kemet ? Vector3.right : Vector3.left;
        dir.Normalize();
        Vector3 from = transform.position;
        Vector3 to = from + dir * dist;
        to.x = Mathf.Clamp(to.x, -56.5f, 56.5f);
        to.z = Mathf.Clamp(to.z, -56.5f, 56.5f);
        to.y = from.y;
        transform.position = to;
        transform.rotation = Quaternion.LookRotation(dir);
        if (dmg > 0f)
        {
            foreach (Vector3 c in new[] { (from + to) / 2f, to })
            {
                var hits = Physics.OverlapSphere(c, 2.6f);
                foreach (var h in hits)
                {
                    var hp = h.GetComponent<Health>();
                    var su = h.GetComponent<Unit>();
                    var ss = h.GetComponent<Structure>();
                    Team? t = null;
                    if (su != null) t = su.team;
                    else if (ss != null) t = ss.team;
                    if (hp != null && t.HasValue && t.Value != team) hp.TakeDamage(dmg, gameObject);
                }
            }
        }
        HitFlash.Spawn(to + Vector3.up * 1.1f, CourtColor(), 1.4f, 0.2f);
    }

    bool PowerShot(float dmg, bool pierce, float slowSec, float range, bool heavy)
    {
        var t = BestAbilityTarget(range);
        if (t == null) return false;
        Combat.SpawnProjectile(transform.position + Vector3.up * 2.2f, t, dmg,
            gameObject, team, heavy, slowSec, 1f, pierce);
        return true;
    }

    void RingBurst(float dmg, float radius, float slowSec, float slowPct)
    {
        var hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var h in hits)
        {
            var u = h.GetComponent<Unit>();
            if (u == null || u.team == team || !u.IsAlive) continue;
            u.health.TakeDamage(dmg, gameObject);
            if (slowSec > 0f) { u.slowT = Mathf.Max(u.slowT, slowSec); u.slowPct = slowPct; }
        }
        HitFlash.Spawn(transform.position + Vector3.up * 1f, CourtColor(), radius * 0.55f, 0.35f);
    }

    void ShieldOn(Hero h, float amt)
    {
        if (h == null) return;
        h.health.shield = amt;
        h.shieldT = 5f;
    }

    Hero NearestAllyHero(float range, bool includeSelf)
    {
        Hero bestH = null;
        float bd = range;
        foreach (var h in FindObjectsByType<Hero>())
        {
            if (h.team != team || !h.IsAlive) continue;
            if (h == this && !includeSelf) continue;
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bd) { bd = d; bestH = h; }
        }
        return bestH;
    }

    System.Collections.Generic.List<Hero> EnemyHeroesIn(float range)
    {
        var list = new System.Collections.Generic.List<Hero>();
        foreach (var h in FindObjectsByType<Hero>())
        {
            if (h.team == team || !h.IsAlive) continue;
            if (Vector3.Distance(transform.position, h.transform.position) <= range) list.Add(h);
        }
        list.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
        return list;
    }

    public void CastQ()
    {
        if (cd[0] > 0 || mp < mpCost[0] || !IsAlive) return;
        mp -= mpCost[0];
        cd[0] = cdMax[0];
        switch (heroName)
        {
            case "SEKHMET": DashForward(9f, 95f + level * 10f); break;
            case "ANUBIS": DashForward(8f, 0f); break;
            case "HADES": DashForward(8f, 0f); break;
            case "ATHENA":
                DashForward(6.5f, 60f + level * 7f);
                ShieldOn(this, 60f + level * 6f);
                break;
            case "THOTH": case "ZEUS": case "ISIS": case "ARTEMIS":
                DashForward(6.5f, 0f); break;
            default: DashForward(6.5f, 85f + level * 9f); break; // THE MEDJAY, ARES
        }
    }

    public void CastW()
    {
        if (cd[1] > 0 || mp < mpCost[1] || !IsAlive) return;
        bool fired = true;
        switch (heroName)
        {
            case "THOTH":
                fired = PowerShot(150f + level * 16f, false, 0.8f, 14f, true); break;
            case "SEKHMET":
                RingBurst(90f + level * 10f, 6f, 0f, 0f);
                health.Heal(health.maxHp * 0.05f); break;
            case "ANUBIS": {
                var t = BestAbilityTarget(14f);
                if (t == null) { fired = false; break; }
                float dmg = 120f + level * 14f;
                var th = t.GetComponent<Hero>();
                if (th != null && th.health.Frac < 0.4f) dmg *= 1.8f; // the heart is weighed
                Combat.SpawnProjectile(transform.position + Vector3.up * 2.2f, t, dmg,
                    gameObject, team, true, 0f);
                break; }
            case "ISIS": {
                Hero ally = null;
                float worst = 1f;
                foreach (var h in FindObjectsByType<Hero>())
                {
                    if (h.team != team || !h.IsAlive) continue;
                    if (Vector3.Distance(transform.position, h.transform.position) > 14f) continue;
                    if (h.health.Frac < worst) { worst = h.health.Frac; ally = h; }
                }
                if (ally == null || worst >= 1f) { fired = false; break; }
                ally.health.Heal(170f + level * 20f);
                HitFlash.Spawn(ally.transform.position + Vector3.up * 1.4f,
                    new Color(0.45f, 0.95f, 0.6f), 1.8f, 0.3f);
                break; }
            case "ARES":
                fired = PowerShot(135f + level * 15f, true, 0f, 14f, false); break;
            case "ARTEMIS":
                fired = PowerShot(145f + level * 16f, true, 0f, 18f, false); break;
            case "ZEUS":
                fired = PowerShot(165f + level * 18f, false, 0f, 14f, true); break;
            case "HADES": {
                var foes = EnemyHeroesIn(13f);
                Transform t = foes.Count > 0 ? foes[0].transform : BestAbilityTarget(13f);
                if (t == null) { fired = false; break; }
                float dmg = 125f + level * 14f;
                t.GetComponent<Health>().TakeDamage(dmg, gameObject);
                health.Heal(dmg * 0.6f);
                HitFlash.Spawn(t.position + Vector3.up * 1.2f, new Color(0.62f, 0.45f, 0.95f), 1.6f, 0.25f);
                break; }
            case "ATHENA":
                fired = PowerShot(120f + level * 14f, true, 0f, 14f, false); break;
            default: { // THE MEDJAY — the Falcon, unchanged law
                var t = BestAbilityTarget(13f);
                if (t == null) { fired = false; break; }
                Combat.SpawnProjectile(transform.position + Vector3.up * 2.2f, t,
                    110f + level * 13f, gameObject, team, true, 1.6f);
                break; }
        }
        if (!fired) return;
        mp -= mpCost[1];
        cd[1] = cdMax[1];
    }

    public void CastE()
    {
        if (cd[2] > 0 || mp < mpCost[2] || !IsAlive) return;
        mp -= mpCost[2];
        cd[2] = cdMax[2];
        switch (heroName)
        {
            case "THOTH":
                ShieldOn(this, 120f + level * 15f);
                mp = Mathf.Min(maxMp, mp + maxMp * 0.4f); break;
            case "SEKHMET":
                health.Heal(health.maxHp * 0.16f);
                ShieldOn(this, 70f + level * 8f); break;
            case "ANUBIS":
                ShieldOn(this, 175f + level * 20f); break;
            case "ISIS": {
                var ally = NearestAllyHero(12f, true);
                ShieldOn(ally != null ? ally : this, 150f + level * 20f); break; }
            case "ARES":
                ShieldOn(this, 120f + level * 15f);
                health.Heal(health.maxHp * 0.07f); break;
            case "ARTEMIS":
                RingBurst(40f + level * 5f, 6.5f, 2.2f, 0.5f); break;
            case "ZEUS":
                ShieldOn(this, 145f + level * 17f);
                mp = Mathf.Min(maxMp, mp + maxMp * 0.3f); break;
            case "HADES":
                ShieldOn(this, 165f + level * 19f); break;
            case "ATHENA": {
                var ally = NearestAllyHero(12f, true);
                ShieldOn(ally != null ? ally : this, 175f + level * 22f); break; }
            default:
                ShieldOn(this, 130f + level * 18f); break; // Shield of Ma'at
        }
    }

    public void CastR()
    {
        if (level < 6 || cd[3] > 0 || mp < mpCost[3] || !IsAlive) return;
        bool fired = true;
        switch (heroName)
        {
            case "THOTH":
                RingBurst(240f + level * 30f, 9.5f, 1.2f, 0.45f);
                GameManager.Instance.Announce("THE WRITTEN WORD", "Thoth has read your ending aloud."); break;
            case "SEKHMET":
                RingBurst(300f + level * 38f, 11f, 0f, 0f);
                GameManager.Instance.Announce("THE PLAGUE YEAR", "Sekhmet walks — and the field sickens."); break;
            case "ANUBIS": {
                var foes = EnemyHeroesIn(20f);
                if (foes.Count == 0) { fired = false; break; }
                Combat.SpawnProjectile(transform.position + Vector3.up * 2.2f, foes[0].transform,
                    400f + level * 48f, gameObject, team, true, 0f);
                GameManager.Instance.Announce("JUDGMENT OF THE DEAD", "Anubis has weighed a heart — and found it light."); break; }
            case "ISIS": {
                foreach (var h in FindObjectsByType<Hero>())
                {
                    if (h.team != team || !h.IsAlive) continue;
                    if (Vector3.Distance(transform.position, h.transform.position) > 12f) continue;
                    h.health.Heal(230f + level * 28f);
                    h.slowT = 0f;
                    HitFlash.Spawn(h.transform.position + Vector3.up * 1.4f,
                        new Color(0.45f, 0.95f, 0.6f), 2f, 0.35f);
                }
                GameManager.Instance.Announce("RESURRECTION WIND", "Isis breathes — and her court rises."); break; }
            case "ARTEMIS": {
                var t = BestAbilityTarget(16f);
                if (t == null) { fired = false; break; }
                for (int i = 0; i < 3; i++)
                {
                    var tt = Combat.FindNearestEnemy(transform.position, team, 16f);
                    Combat.SpawnProjectile(transform.position + Vector3.up * (2.2f + i * 0.35f),
                        tt != null ? tt : t, 135f + level * 15f, gameObject, team, false, 0f);
                }
                GameManager.Instance.Announce("VOLLEY OF THE HUNT", "The moon rains arrows."); break; }
            case "ZEUS": {
                var foes = EnemyHeroesIn(14f);
                if (foes.Count == 0) { fired = false; break; }
                for (int i = 0; i < foes.Count && i < 3; i++)
                {
                    foes[i].health.TakeDamage(235f + level * 29f, gameObject);
                    HitFlash.Spawn(foes[i].transform.position + Vector3.up * 1.6f,
                        new Color(0.75f, 0.85f, 1f), 2f, 0.3f);
                }
                GameManager.Instance.Announce("WRATH OF OLYMPUS", "The sky keeps its promises."); break; }
            case "HADES":
                RingBurst(265f + level * 33f, 10f, 1.5f, 0.5f);
                GameManager.Instance.Announce("KINGDOM COME", "Hades opens his gates — inward."); break;
            case "ATHENA": {
                foreach (var h in FindObjectsByType<Hero>())
                {
                    if (h.team != team || !h.IsAlive) continue;
                    if (Vector3.Distance(transform.position, h.transform.position) > 11f) continue;
                    ShieldOn(h, 185f + level * 23f);
                    h.health.Heal(95f + level * 11f);
                }
                GameManager.Instance.Announce("THE PHALANX STANDS", "Athena's wall has no gate."); break; }
            default:
                CastWaveUlt(); break; // THE MEDJAY's Flood, ARES' Wrath
        }
        if (!fired) return;
        mp -= mpCost[3];
        cd[3] = cdMax[3];
    }

    // The two wave-ults, born of the first build — the Medjay's Flood
    // and Ares' Wrath share one rolling wall of doom.
    void CastWaveUlt()
    {
        string objName = "WarWave";
        Color col = new Color(0.9f, 0.35f, 0.2f);
        string title = "THE WRATH OF ARES";
        string msg = "Survive the wave, guardian.";
        if (team == Team.Kemet)
        {
            objName = "TiamatFlood";
            col = new Color(0.2f, 0.55f, 0.95f);
            title = "TIAMAT'S FLOOD";
            msg = "The Mother answers her guardian.";
        }
        var go = new GameObject(objName);
        go.transform.position = new Vector3(transform.position.x, 0.6f, transform.position.z);
        var rend = go.AddComponent<MeshRenderer>();
        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        go.transform.localScale = new Vector3(3.2f, 1.4f, 11f);
        var mat = new Material(TeamUtil.LitShader);
        mat.color = col;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 1.4f);
        rend.material = mat;
        var f = go.AddComponent<FloodWave>();
        f.team = team;
        f.source = gameObject;
        if (team == Team.Kemet) f.dir = 1;
        else f.dir = -1;
        GameManager.Instance.Announce(title, msg);
    }

    public void GainXp(float amt)
    {
        if (level >= 12) return;
        xp += amt;
        while (xp >= xpNext && level < 12)
        {
            xp -= xpNext;
            level++;
            // Rising has a look: a breath of gold around the god.
            HitFlash.Spawn(transform.position + Vector3.up * 1.2f,
                new Color(1f, 0.85f, 0.35f), 2f, 0.35f);
            xpNext = Mathf.Round(xpNext * 1.32f);
            float keepFrac = health.Frac;
            ApplyStats(false);
            health.hp = Mathf.Min(health.maxHp, health.maxHp * keepFrac + health.maxHp * 0.35f);
            mp = maxMp;
            if (isPlayer)
            {
                string m = "The Mother strengthens her guardian.";
                if (level == 6) m = "Tiamat's Flood is ready. Press R.";
                GameManager.Instance.Announce("LEVEL " + level, m);
            }
        }
    }

    public bool BuyGear(string kind)
    {
        int cost = 250;
        if (kind != "boots") cost = 300;
        int cur = gearBoots;
        if (kind == "blade") cur = gearBlade;
        if (kind == "armor") cur = gearArmor;
        if (gold < cost || cur >= 3) return false;
        gold -= cost;
        if (kind == "blade") gearBlade++;
        else if (kind == "armor") gearArmor++;
        else gearBoots++;
        float f = health.Frac;
        ApplyStats(false);
        float add = 0f;
        if (kind == "armor") add = 180f;
        health.hp = health.maxHp * f + add;
        return true;
    }
}
