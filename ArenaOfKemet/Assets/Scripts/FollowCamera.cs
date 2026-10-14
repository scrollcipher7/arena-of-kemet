using UnityEngine;

// The camera that rides with your god. It follows whichever hero is
// YOURS — any of the ten, for either court — at the war angle, smooth,
// clamped so the field never leaves the frame. Press Y to break free
// to the full-map overview and Y again to lock back onto your hero.
public class FollowCamera : MonoBehaviour
{
    public bool follow = true;
    Vector3 overviewPos = new Vector3(0, 68, -50);
    Quaternion overviewRot = Quaternion.Euler(56, 0, 0);
    Vector3 offset = new Vector3(0, 32, -19);   // high and behind: the MOBA angle
    Transform target;

    void Start()
    {
        // A calmer lens: 50 degrees, not 60 — the field's edges stop
        // stretching, and the angle the maker approved does not move.
        var cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = 50f;
        // Open the lens to the movie filter (the builder's PostFX
        // volume): without this switch the grade never reaches the eye.
        var data = GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (data != null) data.renderPostProcessing = true;
    }

    // Forget the old god — the rider follows whoever the player chose
    // in the Selection Hall, and finds him again on the next frame.
    public void Retarget()
    {
        target = null;
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Y)) follow = !follow;

        if (!follow)
        {
            transform.position = Vector3.Lerp(transform.position, overviewPos, 5f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, overviewRot, 5f * Time.deltaTime);
            return;
        }

        if (target == null && GameManager.Instance != null && GameManager.Instance.playerHero != null)
            target = GameManager.Instance.playerHero.transform;
        if (target == null) return;

        Vector3 want = target.position + offset;
        want.x = Mathf.Clamp(want.x, -40f, 40f);
        want.z = Mathf.Clamp(want.z, -46f, 34f);
        want.y = offset.y;

        // A respawn or recall throws the hero across the map: snap, don't sweep.
        if (Vector3.Distance(transform.position, want) > 45f) transform.position = want;
        else transform.position = Vector3.Lerp(transform.position, want, 6f * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, overviewRot, 8f * Time.deltaTime);
    }
}
