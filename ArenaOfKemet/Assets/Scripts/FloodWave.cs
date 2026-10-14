using System.Collections.Generic;
using UnityEngine;

public class FloodWave : MonoBehaviour
{
    public Team team;
    public GameObject source;
    public int dir = 1;
    public float speed = 11f;
    readonly HashSet<Health> hit = new HashSet<Health>();

    void Update()
    {
        transform.position += Vector3.right * dir * speed * Time.deltaTime;
        var hits = Physics.OverlapBox(transform.position, new Vector3(1.8f, 1.5f, 5.5f));
        foreach (var h in hits)
        {
            var hp = h.GetComponent<Health>();
            if (hp == null || hp.dead || hit.Contains(hp)) continue;
            var u = h.GetComponent<Unit>();
            var s = h.GetComponent<Structure>();
            Team? t = u != null ? u.team : (s != null ? s.team : (Team?)null);
            if (!t.HasValue || t.Value == team) continue;
            hit.Add(hp);
            bool isHeroT = u is Hero;
            hp.TakeDamage(isHeroT ? 240f : (s != null ? 160f : 190f), source);
        }
        if (transform.position.x > 40 || transform.position.x < -40)
            Destroy(gameObject);
    }
}
