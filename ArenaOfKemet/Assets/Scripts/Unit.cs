using UnityEngine;

// Base combat unit: heroes and minions share movement, targeting and auto-attack.
public class Unit : MonoBehaviour
{
    public Team team;
    public Health health;
    public float moveSpeed = 4.5f;
    public float attackRange = 3.2f;
    public float attackDamage = 20f;
    public float attackRate = 1.0f;      // seconds between attacks
    public float sightRange = 30f;
    public float vsStructureMult = 1f;   // summoned bosses hit structures x3

    [System.NonSerialized] public Vector3? moveTarget;
    [HideInInspector] public float slowT = 0f;
    [HideInInspector] public float slowPct = 0.45f;   // how deep the current slow cuts

    protected float atkCd = 0f;
    protected float thinkCd = 0f;
    protected Transform currentTarget;

    public bool IsAlive => health != null && !health.dead;

    protected virtual void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (health == null) health = gameObject.AddComponent<Health>();
    }

    public float SpeedNow => moveSpeed * (slowT > 0 ? 1f - slowPct : 1f);

    protected virtual void Update()
    {
        if (!IsAlive) return;
        slowT = Mathf.Max(0, slowT - Time.deltaTime);
        atkCd -= Time.deltaTime;
        thinkCd -= Time.deltaTime;
        if (thinkCd <= 0)
        {
            thinkCd = 0.18f;
            currentTarget = FindTarget();
        }
        TickCombat();
    }

    protected virtual Transform FindTarget()
    {
        return Combat.FindNearestEnemy(transform.position, team, sightRange);
    }

    protected virtual void TickCombat()
    {
        if (currentTarget != null)
        {
            float d = Vector3.Distance(transform.position, currentTarget.position);
            float reach = attackRange + Combat.RadiusOf(currentTarget);
            if (d <= reach)
            {
                moveTarget = null;
                if (atkCd <= 0)
                {
                    atkCd = attackRate;
                    Combat.SpawnProjectile(transform.position + Vector3.up * 1.4f,
                        currentTarget, attackDamage, gameObject, team, false, 0f, vsStructureMult);
                }
                return;
            }
            moveTarget = currentTarget.position;
        }
        MoveStep();
    }

    protected void MoveStep()
    {
        if (moveTarget == null) return;
        Vector3 to = moveTarget.Value - transform.position;
        to.y = 0;
        if (to.magnitude < 0.25f) { moveTarget = null; return; }
        Vector3 dir = to.normalized;
        transform.position += dir * SpeedNow * Time.deltaTime;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 12f * Time.deltaTime);
        }
    }
}

public static class Combat
{
    public static float RadiusOf(Transform t)
    {
        var s = t.GetComponent<Structure>();
        if (s != null) return s.radius;
        var u = t.GetComponent<Unit>();
        if (u != null) return 0.8f;
        return 0.5f;
    }

    public static Transform FindNearestEnemy(Vector3 from, Team myTeam, float maxDist)
    {
        if (GameManager.Instance == null) return null;
        Transform best = null;
        float bd = maxDist;
        foreach (var s in GameManager.Instance.structures)
        {
            if (s.team == myTeam || s.health.dead) continue;
            float d = Vector3.Distance(from, s.transform.position) - s.radius;
            if (d < bd) { bd = d; best = s.transform; }
        }
        foreach (var u in GameManager.Instance.units)
        {
            if (u.team == myTeam || !u.IsAlive) continue;
            float d = Vector3.Distance(from, u.transform.position) - 0.8f;
            if (d < bd) { bd = d; best = u.transform; }
        }
        // Jungle beasts are neutral: only worth fighting at spitting distance,
        // so lane units marching past never pick a fight with the jungle.
        foreach (var b in GameManager.Instance.beasts)
        {
            if (b == null || b.health == null || b.health.dead) continue;
            float d = Vector3.Distance(from, b.transform.position) - 1.2f;
            if (d < bd && d < 6.5f) { bd = d; best = b.transform; }
        }
        return best;
    }

    public static void SpawnProjectile(Vector3 from, Transform target, float dmg,
        GameObject source, Team team, bool falcon, float slowSeconds, float structMult = 1f,
        bool pierce = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = falcon ? "FalconStrike" : "Shot";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = from;
        go.transform.localScale = Vector3.one * (falcon ? 0.45f : 0.28f);
        var rend = go.GetComponent<Renderer>();
        rend.material = new Material(TeamUtil.LitShader);
        rend.material.color = falcon ? new Color(1f, 0.89f, 0.48f) : TeamUtil.AccentOf(team);
        rend.material.EnableKeyword("_EMISSION");
        rend.material.SetColor("_EmissionColor", rend.material.color * 1.6f);
        var p = go.AddComponent<Projectile>();
        p.target = target; p.damage = dmg; p.source = source; p.slowSeconds = slowSeconds;
        p.structMult = structMult;
        p.pierce = pierce;
        p.speed = falcon ? 26f : 22f;
        // A ribbon of light behind every shot — the eye must be able to
        // FOLLOW a blow across the field, like the beams in his games.
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.34f;
        trail.startWidth = falcon ? 0.55f : 0.38f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.1f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        var tc = falcon ? new Color(1f, 0.89f, 0.48f) : TeamUtil.AccentOf(team);
        trail.startColor = new Color(tc.r, tc.g, tc.b, 0.85f);
        trail.endColor = new Color(tc.r, tc.g, tc.b, 0f);
        // The body answers the shot: a god with flesh swings/casts/looses.
        var ba = source != null ? source.GetComponent<BodyAnimator>() : null;
        if (ba != null) ba.PlayAttack();
        // And the shot is HEARD — bow, spell, or the tower's stone-bow.
        if (AudioDirector.Instance != null) AudioDirector.Instance.PlayShot(source);
    }
}
