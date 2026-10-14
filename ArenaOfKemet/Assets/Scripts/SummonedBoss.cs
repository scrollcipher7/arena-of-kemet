using UnityEngine;

// A boss reborn as an ally. Earned by felling Apophis or the Sphinx in the
// jungle, summoned with T (or by Ares when his push is ripe). It marches
// its lane like a one-beast siege: structures first, everything else second,
// and it stays until the enemy kills it.
public class SummonedBoss : Minion
{
    protected override Transform FindTarget()
    {
        // Structures above all: a summoned boss exists to break temples.
        Transform best = null; float bd = 60f;
        foreach (var s in GameManager.Instance.structures)
        {
            if (s.team == team || s.health.dead) continue;
            float d = Vector3.Distance(transform.position, s.transform.position) - s.radius;
            if (d < bd) { bd = d; best = s.transform; }
        }
        var near = Combat.FindNearestEnemy(transform.position, team, 12f);
        if (near != null)
        {
            float dn = Vector3.Distance(transform.position, near.position);
            if (best == null || dn < bd) return near;
        }
        return best;
    }
}
