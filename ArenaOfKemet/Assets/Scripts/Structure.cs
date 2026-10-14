using UnityEngine;

public class Structure : MonoBehaviour
{
    public Team team;
    public bool isCore = false;
    public float radius = 2.2f;
    public Health health;
    public float attackDamage = 95f;
    public float attackRange = 7.5f;
    float atkCd = 0f;
    Transform rampTarget;
    int rampStreak = 0;

    void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (health == null) health = gameObject.AddComponent<Health>();
        health.Init(isCore ? 2600f : 1500f);
        health.armor = 8f;
        if (!isCore) health.mitigation = Mitigation;
        health.OnDied += killer => GameManager.Instance.OnStructureDown(this, killer);
    }

    // The HoK tower laws, judging every wound before it lands:
    // - Time mitigation vs HEROES: -25% before 4:00, -10% until 10:00,
    //   then the floor of -5% forever.
    // - Anti-backdoor: with no enemy minions inside its ground, the
    //   stone hardens — all damage cut by 55%.
    float Mitigation(GameObject src)
    {
        float m = 1f;
        var gm = GameManager.Instance;
        float t = gm != null ? gm.matchTime : 0f;
        if (src != null && src.GetComponent<Hero>() != null)
            m *= t < 240f ? 0.75f : (t < 600f ? 0.90f : 0.95f);
        if (gm != null)
        {
            bool minionNear = false;
            foreach (var u in gm.units)
                if (u != null && u.team != team && u is Minion && u.IsAlive &&
                    Vector3.Distance(transform.position, u.transform.position) < attackRange + radius + 0.5f)
                { minionNear = true; break; }
            if (!minionNear) m *= 0.45f;
        }
        return m;
    }

    public void DropShield()
    {
        var bubble = transform.Find("TowerShield");
        if (bubble != null) bubble.gameObject.SetActive(false);
    }

    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.RegisterStructure(this);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.units == null || health == null || health.dead || isCore) return;
        atkCd -= Time.deltaTime;
        if (atkCd > 0) return;
        Transform best = null; float bd = attackRange;
        // AGGRO LAW: a hero who wounds an allied hero under this tower's
        // eye becomes the tower's mark — minions or no minions.
        foreach (var h in gm.heroes)
        {
            if (h == null || h.team != team || !h.IsAlive || h.health == null) continue;
            if (Time.time - h.health.lastAttackTime > 2.5f) continue;
            var atk = h.health.lastAttacker != null ? h.health.lastAttacker.GetComponent<Hero>() : null;
            if (atk == null || !atk.IsAlive) continue;
            float d = Vector3.Distance(transform.position, atk.transform.position) - radius;
            if (d < bd) { bd = d; best = atk.transform; }
        }
        // Otherwise: minions, ballista and cannon before any hero.
        if (best == null)
            foreach (var u in gm.units)
            {
                if (u == null || u.team == team || !u.IsAlive || u is Hero) continue;
                float d = Vector3.Distance(transform.position, u.transform.position) - radius;
                if (d < bd) { bd = d; best = u.transform; }
            }
        if (best == null)
            foreach (var u in gm.units)
            {
                if (u == null || u.team == team || !u.IsAlive) continue;
                float d = Vector3.Distance(transform.position, u.transform.position) - radius;
                if (d < bd) { bd = d; best = u.transform; }
            }
        if (best != null)
        {
            atkCd = 1.05f;
            // THE RAMPING LASER: against a hero, each unbroken hit burns
            // 60% harder than the last, to +300% — and it pierces armor.
            float dmg = attackDamage;
            bool pierce = false;
            if (best.GetComponent<Hero>() != null)
            {
                if (rampTarget == best) rampStreak++;
                else { rampTarget = best; rampStreak = 1; }
                dmg = attackDamage * (1f + Mathf.Min(0.6f * (rampStreak - 1), 3.0f));
                pierce = true;
            }
            else rampTarget = null;
            Combat.SpawnProjectile(transform.position + Vector3.up * 5.2f, best,
                dmg, gameObject, team, false, 0f, 1f, pierce);
        }
    }
}
