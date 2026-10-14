using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHp = 500f;
    public float hp = 500f;
    public float armor = 0f;
    public float shield = 0f;
    public float damageMult = 1f;   // tower shields: 0.1 while the shield stands
    public bool dead = false;

    public Action<GameObject> OnDied;   // arg = killer (may be null)
    public Action OnDamaged;

    public float Frac => maxHp <= 0 ? 0 : Mathf.Clamp01(hp / maxHp);

    public void Init(float max)
    {
        maxHp = max; hp = max; shield = 0; dead = false;
    }

    // Structures set this to judge every wound by the HoK tower laws
    // (time mitigation vs heroes, anti-backdoor) before it lands.
    public System.Func<GameObject, float> mitigation;
    public GameObject lastAttacker;
    public float lastAttackTime = -99f;

    public void TakeDamage(float raw, GameObject source, bool pierce = false)
    {
        if (dead) return;
        float dmg = pierce ? raw : Mathf.Max(1f, raw - armor);
        dmg *= damageMult;
        if (mitigation != null) dmg *= mitigation(source);
        lastAttacker = source;
        lastAttackTime = Time.time;
        if (shield > 0)
        {
            float absorbed = Mathf.Min(shield, dmg);
            shield -= absorbed;
            dmg -= absorbed;
        }
        if (dmg > 0) hp -= dmg;
        OnDamaged?.Invoke();
        if (hp <= 0)
        {
            hp = 0; dead = true;
            OnDied?.Invoke(source);
        }
    }

    public void Heal(float amt)
    {
        if (dead) return;
        hp = Mathf.Min(maxHp, hp + amt);
    }
}
