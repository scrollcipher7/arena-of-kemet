using UnityEngine;

// Marches its lane's road — waypoint by waypoint, around the bends like
// the Gorge demands — and fights whatever crosses it. At the road's end
// waits the enemy home.
public class Minion : Unit
{
    public bool ranged = false;
    public int lane = 1;
    public int wp = 1;

    protected override Transform FindTarget()
    {
        // Engage what is close; otherwise march on.
        var near = Combat.FindNearestEnemy(transform.position, team, 13f);
        if (near != null) return near;
        return null;
    }

    protected override void TickCombat()
    {
        if (currentTarget == null)
        {
            var path = GameManager.Lanes[lane];
            int step = team == Team.Kemet ? 1 : -1;
            Vector3 goal = path[wp];
            Vector3 flat = new Vector3(transform.position.x, 0, transform.position.z);
            if (Vector3.Distance(flat, goal) < 2.4f)
            {
                int next = wp + step;
                if (next >= 0 && next < path.Length) { wp = next; goal = path[wp]; }
            }
            moveTarget = goal;
            MoveStep();
            return;
        }
        base.TickCombat();
    }
}
