using System.Collections;
using UnityEngine;

// A neutral jungle beast. Guards its camp, bites whoever steps in,
// pays gold and XP to the hero who fells it, then returns after a while.
// The croc also grants SOBEK'S BLESSING: +25% damage, +12% speed, 60 seconds.
public class JungleBeast : MonoBehaviour
{
    public string beastName = "Scarab";
    public Health health;
    public float damage = 14f;
    public float attackRange = 3.6f;
    public float aggroRange = 7f;
    public float leashRange = 10f;
    public float moveSpeed = 3.4f;
    public int goldReward = 50;
    public float xpReward = 60f;
    public bool grantsBuff = false;
    public float respawnSeconds = 50f;

    // Bosses (Apophis, the Sphinx): bigger, slam the ground, and the team
    // that lands the killing blow earns the right to SUMMON them.
    public bool isBoss = false;
    public string bossKind = "";
    public float slamDamage = 90f;
    public float slamRange = 4.6f;
    public float slamCdMax = 4f;
    float slamCd = 0f;

    Vector3 home;
    float beastMax = 550f;
    float atkCd = 0f;
    float thinkCd = 0f;
    Transform target;
    Renderer[] rends;
    Collider col;

    void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (health == null) health = gameObject.AddComponent<Health>();
        // Bosses sleep in their lairs until a hero reaches level 5.
        if (isBoss) gameObject.SetActive(false);
    }

    void Start()
    {
        home = transform.position;
        beastMax = health.maxHp;
        rends = GetComponentsInChildren<Renderer>();
        col = GetComponent<Collider>();
        health.OnDied += OnDied;
        GameManager.Instance.RegisterBeast(this);
    }

    void Update()
    {
        if (health.dead) return;
        atkCd -= Time.deltaTime;
        thinkCd -= Time.deltaTime;
        slamCd -= Time.deltaTime;

        // Boss slam: everything standing close gets crushed, friend or foe.
        if (isBoss && slamCd <= 0 && target != null &&
            Vector3.Distance(transform.position, target.position) < slamRange + 1.2f)
        {
            slamCd = slamCdMax;
            var hits = Physics.OverlapSphere(transform.position, slamRange);
            foreach (var h in hits)
            {
                var hp = h.GetComponent<Health>();
                var u = h.GetComponent<Unit>();
                if (hp != null && u != null) hp.TakeDamage(slamDamage, gameObject);
            }
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.position = transform.position + Vector3.up * 0.2f;
            ring.transform.localScale = new Vector3(slamRange * 2f, 0.15f, slamRange * 2f);
            var rm = new Material(TeamUtil.LitShader);
            rm.color = new Color(0.9f, 0.3f, 0.15f);
            rm.EnableKeyword("_EMISSION");
            rm.SetColor("_EmissionColor", rm.color * 1.6f);
            ring.GetComponent<Renderer>().material = rm;
            Destroy(ring, 0.45f);
        }
        if (thinkCd <= 0)
        {
            thinkCd = 0.2f;
            target = FindPrey();
        }

        if (target != null)
        {
            float d = Vector3.Distance(transform.position, target.position);
            if (d > attackRange)
            {
                Vector3 to = target.position - transform.position; to.y = 0;
                transform.position += to.normalized * moveSpeed * Time.deltaTime;
                if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to.normalized);
            }
            else if (atkCd <= 0)
            {
                atkCd = 1.1f;
                var tu = target.GetComponent<Unit>();
                Combat.SpawnProjectile(transform.position + Vector3.up * 0.8f, target,
                    damage, gameObject, tu != null ? tu.team : Team.Kemet, false, 0f);
            }
        }
        else
        {
            // No prey: walk home and mend.
            Vector3 to = home - transform.position; to.y = 0;
            if (to.magnitude > 0.4f)
                transform.position += to.normalized * moveSpeed * Time.deltaTime;
            else
                health.Heal(beastMax * 0.12f * Time.deltaTime);
        }
    }

    Transform FindPrey()
    {
        Transform best = null; float bd = aggroRange;
        foreach (var u in GameManager.Instance.units)
        {
            if (u == null || !u.IsAlive) continue;
            float d = Vector3.Distance(home, u.transform.position);
            if (d > leashRange) continue;
            float dd = Vector3.Distance(transform.position, u.transform.position);
            if (dd < bd) { bd = dd; best = u.transform; }
        }
        return best;
    }

    void OnDied(GameObject killer)
    {
        var kh = killer != null ? killer.GetComponent<Hero>() : null;

        // A boss death grants the killing side a SUMMON, whoever struck last.
        if (isBoss)
        {
            Team? side = null;
            if (kh != null) side = kh.team;
            else if (killer != null)
            {
                var ku = killer.GetComponent<Unit>();
                if (ku != null) side = ku.team;
            }
            if (side.HasValue)
                GameManager.Instance.GrantSummon(side.Value, bossKind);
        }

        if (kh != null)
        {
            kh.gold += goldReward;
            kh.GainXp(xpReward);
            if (grantsBuff)
            {
                kh.GrantBuff();
                GameManager.Instance.Announce("SOBEK'S BLESSING",
                    kh.isPlayer ? "The river's strength runs in your blows. 60 seconds."
                                : "Ares took the croc's blessing. Careful, guardian.");
            }
            else if (kh.isPlayer)
            {
                GameManager.Instance.Announce(beastName.ToUpper() + " FELLED",
                    "+" + goldReward + " gold from the jungle.");
            }
        }
        StartCoroutine(Respawn());
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(0.15f);
        foreach (var r in rends) r.enabled = false;
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(respawnSeconds);
        transform.position = home;
        health.dead = false;
        health.hp = beastMax;
        health.shield = 0;
        foreach (var r in rends) r.enabled = true;
        if (col != null) col.enabled = true;
    }
}
