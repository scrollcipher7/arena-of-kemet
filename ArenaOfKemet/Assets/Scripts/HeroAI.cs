using UnityEngine;

// A god's brain. Every AI hero plays the Gorge by his POST, the way the
// reference teaches: laners hold their road and rotate only for a crisis,
// the assassin farms the jungle and strikes lanes, the support walks with
// an ally and keeps him standing. All of them retreat when hurt, recall
// when it is safe, summon a beaten boss when a push is ripe, and never
// dive a tower without minions to eat the shots.
public class HeroAI : MonoBehaviour
{
    public const int Laner = 0, Jungler = 1, Support = 2;
    public int role = Laner;
    public int homeLane = 1;   // 0 Top, 1 Mid, 2 Bot — the road that is HIS

    // TEMPERAMENT — the MGG lesson: a god who always wars the same way
    // gets solved. Each god carries a strategy that bends his judgment,
    // and some matches he wakes in a different mood entirely.
    public const int Push = 0, Farm = 1, Hunt = 2, Split = 3;
    public int temperament = Push;

    public static string TemperamentName(int t)
    {
        switch (t)
        {
            case Farm: return "THE HARVEST";
            case Hunt: return "THE HUNT";
            case Split: return "THE SPLIT PUSH";
            default: return "ALL-OUT PUSH";
        }
    }

    Hero me;
    float think = 0f;
    float laneThink = 0f;
    int curLane = 1;

    void Start()
    {
        me = GetComponent<Hero>();
        curLane = homeLane;
        // One match in three, the god wakes in a different mood.
        if (Random.value < 0.35f) temperament = Random.Range(0, 4);
    }

    bool InLane(Unit u, int lane)
    {
        if (u is Minion m) return m.lane == lane;
        return GameManager.PathDistance(lane, u.transform.position) < 9f;
    }

    float LaneScore(int lane)
    {
        float total = GameManager.PathTotal(lane);
        float score = 0f;
        float defMult = temperament == Farm ? 0.55f : 1f;  // the harvester answers cries late
        Hero foeHero = NearestFoeHero();
        foreach (var u in GameManager.Instance.units)
        {
            if (!u.IsAlive || !InLane(u, lane)) continue;
            float prog = GameManager.PathProgress(lane, u.transform.position, u.team);
            if (u.team != me.team)
            {
                // Enemy wave deep on our half = a road crying for defense.
                if (prog > total * 0.35f) score += (20f + (prog / total) * 30f) * defMult;
                // Enemy wave at the gates of one of our towers = emergency.
                foreach (var s in GameManager.Instance.structures)
                    if (s.team == me.team && !s.health.dead &&
                        GameManager.PathDistance(lane, s.transform.position) < 9f &&
                        Vector3.Distance(u.transform.position, s.transform.position) < 10f)
                        score += 45f * defMult;
            }
            else if (!(u is Hero))
            {
                // Our own wave deep in enemy land = a push worth joining.
                if (prog > total * 0.45f)
                    score += (12f + (prog / total) * 14f) * (temperament == Push ? 2f : 1f);
            }
        }
        // The nearest enemy god haunting a road draws attention to it —
        // and the HUNTER is drawn like iron to a lodestone.
        if (foeHero != null && foeHero.IsAlive && GameManager.PathDistance(lane, foeHero.transform.position) < 9f)
            score += temperament == Hunt ? 40f : 14f;
        // The SPLIT blade loves the road the enemy god has abandoned.
        if (temperament == Split &&
            (foeHero == null || !foeHero.IsAlive || GameManager.PathDistance(lane, foeHero.transform.position) >= 9f))
            score += 16f;
        if (lane == 1) score += 4f;
        return score;
    }

    void ChooseLane()
    {
        float best = float.NegativeInfinity;
        for (int li = 0; li < 3; li++)
        {
            float s = LaneScore(li) + (li == curLane ? 3f : 0f); // a little loyalty, no jitter
            if (role == Laner && li == homeLane)                 // a laner holds his post —
                s += temperament == Farm ? 34f : temperament == Split ? 10f : 22f; // the harvester deepest, the split blade loosest
            if (s > best) { best = s; curLane = li; }
        }
    }

    Structure LaneTarget()
    {
        Structure best = null; float bp = float.MaxValue;
        foreach (var s in GameManager.Instance.structures)
        {
            if (s.team == me.team || s.health.dead) continue;
            if (GameManager.PathDistance(curLane, s.transform.position) > 9f) continue;
            float p = GameManager.PathProgress(curLane, s.transform.position, me.team);
            if (p < bp) { bp = p; best = s; }
        }
        return best;
    }

    Hero NearestFoeHero()
    {
        Hero best = null; float bd = float.MaxValue;
        foreach (var h in GameManager.Instance.heroes)
        {
            if (h.team == me.team || !h.IsAlive) continue;
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bd) { bd = d; best = h; }
        }
        return best;
    }

    void Update()
    {
        if (me == null || !me.IsAlive) return;
        think -= Time.deltaTime;
        if (think > 0) return;
        think = 0.22f;

        float frac = me.health.Frac;
        var gm = GameManager.Instance;
        var myCore = gm.CoreOf(me.team);
        Vector3 myCorePos = myCore != null ? myCore.transform.position : Vector3.zero;
        Hero foe = NearestFoeHero();

        laneThink -= 0.22f;
        if (laneThink <= 0) { laneThink = 1.1f; ChooseLane(); }

        // Spend a boss summon when a push is ripe: his wave at a tower's throat.
        if (gm.HasToken(me.team))
        {
            bool ripe = gm.matchTime > (temperament == Push ? 150f : 300f);
            if (!ripe)
                foreach (var s in gm.structures)
                    if (s.team != me.team && !s.health.dead)
                        foreach (var u in gm.units)
                            if (u.team == me.team && !(u is Hero) && u.IsAlive &&
                                Vector3.Distance(u.transform.position, s.transform.position) < 8f)
                                ripe = true;
            if (ripe) { gm.TrySummon(me); return; }
        }

        // While recalling, hold the channel unless danger walks up.
        if (me.recallT >= 0)
        {
            bool danger = foe != null && foe.IsAlive &&
                Vector3.Distance(transform.position, foe.transform.position) < 8f;
            if (!danger) return;
            me.CancelRecall("");
        }

        // Hurt: recall home if the coast is clear, else fight his way back.
        // (The harvester values his life earliest of all.)
        if (frac < (temperament == Farm ? 0.40f : 0.30f))
        {
            bool enemyNear = foe != null && foe.IsAlive &&
                Vector3.Distance(transform.position, foe.transform.position) < 9f;
            var foeCore = gm.CoreOf(TeamUtil.Opponent(me.team));
            bool deepSafe = foeCore == null ||
                Vector3.Distance(transform.position, foeCore.transform.position) > 30f;
            if (!enemyNear && deepSafe)
            {
                me.StartRecall();
            }
            else
            {
                me.moveTarget = GameManager.PathPoint(curLane, 0.12f, me.team);
                if (me.cd[2] <= 0) me.CastE();
                if (me.healCd <= 0 && frac < 0.18f) me.TryHeal();
            }
            return;
        }
        if (frac < 0.45f && me.healCd <= 0 &&
            Combat.FindNearestEnemy(transform.position, me.team, 7f) == null)
            me.TryHeal();
        if (Vector3.Distance(transform.position, myCorePos) > 58f && frac < 0.92f)
        {
            me.moveTarget = GameManager.PathPoint(curLane, 0.10f, me.team);
            return;
        }

        // Fight the nearest enemy god when close.
        if (foe != null && foe.IsAlive)
        {
            float dp = Vector3.Distance(transform.position, foe.transform.position);
            // THE HUNT: he runs his prey down from far off, while he is
            // the healthier god — patience is not in his nature.
            if (temperament == Hunt && dp < 17f && dp >= 6.5f && frac > 0.5f &&
                frac > foe.health.Frac - 0.05f)
            {
                me.moveTarget = foe.transform.position;
                return;
            }
            if (dp < 12.5f && me.cd[1] <= 0) me.CastW();
            if (dp < 6.5f && frac > 0.45f)
            {
                me.moveTarget = foe.transform.position;
                if (me.cd[0] <= 0 && Random.value < 0.45f) me.CastQ();
                return;
            }
            if (dp < 10f && frac <= 0.45f)
            {
                Vector3 away = transform.position - foe.transform.position;
                away.y = 0;
                me.moveTarget = transform.position + away.normalized * 6f;
                return;
            }
        }
        if (frac < 0.6f && me.cd[2] <= 0 && Combat.FindNearestEnemy(transform.position, me.team, 8f) != null)
            me.CastE();

        // Ultimate into a clustered enemy wave on his road.
        if (me.level >= 6 && me.cd[3] <= 0)
        {
            int n = 0;
            foreach (var u in gm.units)
                if (u.team != me.team && u.IsAlive &&
                    GameManager.PathDistance(curLane, u.transform.position) < 6f &&
                    Vector3.Distance(u.transform.position, transform.position) < 9f)
                    n++;
            if (n >= 3) { me.CastR(); return; }
        }

        // ---- ROLE WORK ----

        if (role == Jungler)
        {
            // A woken Ancient with allies nearby is a team kill worth taking.
            if (frac > 0.7f)
            {
                foreach (var b in gm.beasts)
                {
                    if (b == null || !b.isBoss || b.health == null || b.health.dead) continue;
                    if (!b.gameObject.activeSelf) continue;
                    int allies = 0;
                    foreach (var h in gm.heroes)
                        if (h.team == me.team && h != me && h.IsAlive &&
                            Vector3.Distance(h.transform.position, b.transform.position) < 16f)
                            allies++;
                    if (allies >= 1) { me.moveTarget = b.transform.position; return; }
                }
            }
            // Otherwise: farm the nearest living camp.
            JungleBeast camp = null; float cb = float.MaxValue;
            foreach (var b in gm.beasts)
            {
                if (b == null || b.isBoss || b.health == null || b.health.dead) continue;
                if (!b.gameObject.activeSelf) continue;
                float d = Vector3.Distance(transform.position, b.transform.position);
                if (d < cb) { cb = d; camp = b; }
            }
            if (camp != null) { me.moveTarget = camp.transform.position; return; }
            // No camps standing: fall through and work the neediest road.
        }

        if (role == Support)
        {
            // Walk with the nearest ally god who is out in the war (not the
            // player — the player walks his own road).
            Hero ally = null; float ad = float.MaxValue;
            foreach (var h in gm.heroes)
            {
                if (h == me || h.team != me.team || !h.IsAlive || h.isPlayer) continue;
                if (Vector3.Distance(h.transform.position, myCorePos) < 10f) continue;
                float d = Vector3.Distance(transform.position, h.transform.position);
                if (d < ad) { ad = d; ally = h; }
            }
            if (ally != null)
            {
                // Raise his shield when a fight burns near his ally.
                if (me.cd[2] <= 0 && Combat.FindNearestEnemy(ally.transform.position, me.team, 8f) != null)
                    me.CastE();
                Vector3 back = myCorePos - ally.transform.position;
                back.y = 0;
                me.moveTarget = ally.transform.position + back.normalized * 3.2f;
                return;
            }
            // Nobody to escort: fall through and hold mid.
            curLane = 1;
        }

        // ---- LANE WORK (laners; jungler/support fall back to it) ----
        // Stand behind his frontline wave on the road, and never dive a
        // tower without minions to eat the shots.
        Unit front = null; float bestProg = float.NegativeInfinity;
        foreach (var u in gm.units)
            if (u.team == me.team && !(u is Hero) && u.IsAlive && InLane(u, curLane))
            {
                float p = GameManager.PathProgress(curLane, u.transform.position, me.team);
                if (p > bestProg) { bestProg = p; front = u; }
            }

        Vector3 dest;
        if (front != null)
        {
            Vector3 back = myCorePos - front.transform.position;
            back.y = 0;
            dest = front.transform.position + back.normalized * 2.4f;
        }
        else dest = GameManager.PathPoint(curLane, 0.30f, me.team);

        var targetStruct = LaneTarget();
        bool minionsNearStruct = false;
        if (targetStruct != null)
            foreach (var u in gm.units)
                if (u.team == me.team && !(u is Hero) && u.IsAlive &&
                    Vector3.Distance(u.transform.position, targetStruct.transform.position) < 7f)
                    minionsNearStruct = true;
        if (targetStruct != null && !minionsNearStruct &&
            Vector3.Distance(transform.position, targetStruct.transform.position) < targetStruct.attackRange + targetStruct.radius + 1.5f)
        {
            Vector3 away = transform.position - targetStruct.transform.position;
            away.y = 0;
            dest = transform.position + away.normalized * 5f; // back off, wait for the wave
        }

        me.moveTarget = dest;
    }
}
