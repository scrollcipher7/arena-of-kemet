using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

// The flesh driver: makes a god's 3D body obey the game's heartbeat.
// It watches how the hero truly moves across the ground and blends the
// body's clips — idle, walk, run — fires the attack clip the moment a
// shot leaves his hands (Combat calls PlayAttack), and lays the body
// down with the death clip when he falls, until he rises again.
public class BodyAnimator : MonoBehaviour
{
    public AnimationClip idleClip, walkClip, runClip, attackClip, deathClip;

    Animator anim;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable[] plays;
    readonly float[] weights = new float[5];
    int cur = 0;                 // 0 idle · 1 walk · 2 run · 3 attack · 4 death
    float attackT = 0f;
    Vector3 lastPos;
    Unit unit;
    bool deadShown = false;
    GameObject shieldBubble;

    AnimationClipPlayable MakePlayable(int i, AnimationClip clip, bool loop)
    {
        if (clip == null) return default;
        if (loop) clip.wrapMode = WrapMode.Loop;
        var p = AnimationClipPlayable.Create(graph, clip);
        graph.Connect(p, 0, mixer, i);
        return p;
    }

    void Start()
    {
        unit = GetComponent<Unit>();
        anim = GetComponentInChildren<Animator>();
        if (anim == null || idleClip == null) { enabled = false; return; }
        anim.applyRootMotion = false;
        anim.runtimeAnimatorController = null;

        graph = PlayableGraph.Create("Body_" + gameObject.name);
        mixer = AnimationMixerPlayable.Create(graph, 5);
        var output = AnimationPlayableOutput.Create(graph, "out", anim);
        output.SetSourcePlayable(mixer);
        plays = new AnimationClipPlayable[5];
        plays[0] = MakePlayable(0, idleClip, true);
        plays[1] = MakePlayable(1, walkClip, true);
        plays[2] = MakePlayable(2, runClip, true);
        plays[3] = MakePlayable(3, attackClip, false);
        plays[4] = MakePlayable(4, deathClip, false);
        weights[0] = 1f;
        mixer.SetInputWeight(0, 1f);
        graph.Play();
        lastPos = transform.position;
    }

    public void PlayAttack()
    {
        if (plays == null || attackClip == null || deadShown) return;
        plays[3].SetTime(0);
        attackT = attackClip.length * 0.92f;
    }

    void Update()
    {
        // A shielded god wears his shield where the eye can see it —
        // a bubble of court-light, like the champions in his games.
        bool shielded = unit != null && unit.health != null && unit.health.shield > 0 && unit.IsAlive;
        if (shielded && shieldBubble == null)
        {
            shieldBubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldBubble.name = "ShieldBubble";
            shieldBubble.transform.SetParent(transform, false);
            shieldBubble.transform.localPosition = new Vector3(0, 1.1f, 0);
            shieldBubble.transform.localScale = Vector3.one * 2.6f;
            var bc = shieldBubble.GetComponent<Collider>();
            if (bc != null) Destroy(bc);
            var bm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bm.SetFloat("_Surface", 1f);
            bm.renderQueue = 3000;
            var cc = unit.team == Team.Kemet
                ? new Color(1f, 0.8f, 0.3f, 0.22f) : new Color(0.6f, 0.79f, 0.98f, 0.22f);
            bm.color = cc;
            shieldBubble.GetComponent<Renderer>().material = bm;
        }
        if (shieldBubble != null) shieldBubble.SetActive(shielded);

        if (plays == null) return;
        float dt = Time.deltaTime;
        Vector3 delta = transform.position - lastPos;
        delta.y = 0;
        lastPos = transform.position;
        float speed = dt > 0 ? delta.magnitude / dt : 0f;

        bool dead = unit == null || !unit.IsAlive;
        int want;
        if (dead)
        {
            want = 4;
            if (!deadShown)
            {
                deadShown = true;
                if (deathClip != null) { plays[4].SetTime(0); plays[4].SetSpeed(1); }
            }
            else if (deathClip != null && plays[4].GetTime() >= deathClip.length - 0.05)
                plays[4].SetSpeed(0);   // the body rests where it fell
        }
        else
        {
            if (deadShown) { deadShown = false; if (deathClip != null) plays[4].SetTime(0); }
            attackT = Mathf.Max(0, attackT - dt);
            if (attackT > 0 && attackClip != null) want = 3;
            else if (speed > 4.2f && runClip != null) want = 2;
            else if (speed > 0.35f && walkClip != null) want = 1;
            else want = 0;
        }
        cur = want;

        for (int i = 0; i < 5; i++)
        {
            float target = i == cur ? 1f : 0f;
            weights[i] = Mathf.MoveTowards(weights[i], target, dt * 7f);
            if (plays[i].IsValid()) mixer.SetInputWeight(i, weights[i]);
        }
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
