using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Transform target;
    public float damage = 10f;
    public float speed = 22f;
    public float slowSeconds = 0f;
    public float structMult = 1f;
    public bool pierce = false;   // tower laser vs heroes: ignores armor
    public GameObject source;

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }
        var hp = target.GetComponent<Health>();
        if (hp == null || hp.dead) { Destroy(gameObject); return; }
        Vector3 aim = target.position + Vector3.up * 1.0f;
        transform.position = Vector3.MoveTowards(transform.position, aim, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, aim) < 0.45f)
        {
            float dmg = damage;
            if (structMult != 1f && target.GetComponent<Structure>() != null) dmg *= structMult;
            hp.TakeDamage(dmg, source, pierce);
            if (AudioDirector.Instance != null) AudioDirector.Instance.PlayHit();
            // The strike is SEEN, not just heard: a burst of court-light.
            var su = source != null ? source.GetComponent<Unit>() : null;
            var flashCol = su != null && su.team == Team.Olympus
                ? new Color(0.6f, 0.79f, 0.98f) : new Color(1f, 0.8f, 0.3f);
            HitFlash.Spawn(transform.position, flashCol, target.GetComponent<Structure>() != null ? 2.2f : 1f);
            if (slowSeconds > 0)
            {
                var u = target.GetComponent<Unit>();
                if (u != null) { u.slowT = Mathf.Max(u.slowT, slowSeconds); u.slowPct = 0.45f; }
            }
            Destroy(gameObject);
        }
    }
}
