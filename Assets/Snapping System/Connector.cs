using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// PositiveEnd → attached to every top stud (the "male" connector)
/// When any one stud touches a bottom tube → forces PERFECT alignment
/// </summary>
[RequireComponent(typeof(Collider))]
public class PositiveEnd : MonoBehaviour
{
    [Header("Snap Behavior")]
    public bool smoothSnap = true;
    public float snapDuration = 0.12f;

    [Header("Feedback")]
    public AudioClip snapSound;

    // ─────────────────────────────────────────────────────────────────────
    private Collider myTriggerCollider;
    private bool isConnected = false;

    private void Awake()
    {
        myTriggerCollider = GetComponent<Collider>();
        if (myTriggerCollider == null || !myTriggerCollider.isTrigger)
            Debug.LogError($"[PositiveEnd] {name} needs a TRIGGER collider!", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isConnected) return;

        NegativeEnd negative = other.GetComponent<NegativeEnd>();
        if (negative == null || negative.IsConnected) return;

        // Disable both triggers immediately so nothing else fires mid-snap
        myTriggerCollider.enabled = false;
        other.enabled = false;

        StartCoroutine(ForcePerfectSnap(negative));
    }

    private IEnumerator ForcePerfectSnap(NegativeEnd negativeTube)
    {
        yield return new WaitForEndOfFrame();

        // Find the actual bricks (climb until we hit the Rigidbody)
        Transform bottomBrick = transform;
        Transform topBrick = negativeTube.transform;

        while (bottomBrick != null && bottomBrick.GetComponent<Rigidbody>() == null)
            bottomBrick = bottomBrick.parent;
        while (topBrick != null && topBrick.GetComponent<Rigidbody>() == null)
            topBrick = topBrick.parent;

        if (bottomBrick == null || topBrick == null)
        {
            Debug.LogWarning("Couldn't find brick with Rigidbody!");
            yield break;
        }

        // Temporarily ignore all collisions on the moving brick
        Rigidbody topRigidbody = topBrick.GetComponent<Rigidbody>();
        Collider[] allTopColliders = topBrick.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in allTopColliders)
            c.isTrigger = true;

        // Perfect center-to-center offset
        Vector3 perfectOffset = transform.position - negativeTube.transform.position;

        // Smooth movement
        if (smoothSnap)
        {
            Vector3 startPos = topBrick.position;
            Quaternion startRot = topBrick.rotation;
            float timer = 0f;

            while (timer < snapDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, timer / snapDuration);
                topBrick.position = Vector3.Lerp(startPos, startPos + perfectOffset, progress);
                topBrick.rotation = Quaternion.Slerp(startRot, bottomBrick.rotation, progress);
                yield return null;
            }
        }

        // Final hard snap
        topBrick.position += perfectOffset;
        topBrick.rotation = bottomBrick.rotation;

        // Restore normal physics
        foreach (Collider c in allTopColliders)
            c.isTrigger = false;
        topRigidbody?.WakeUp();

        // Lock bricks together
        FixedJoint joint = topBrick.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = bottomBrick.GetComponent<Rigidbody>();

        // Sound + haptics (FIXED LINE!)
        if (snapSound != null)
            AudioSource.PlayClipAtPoint(snapSound, transform.position);

        foreach (var controller in FindObjectsOfType<XRBaseController>())
            if (controller.enableInputActions)
                controller.SendHapticImpulse(0.7f, 0.1f);

        // Mark connection as complete
        isConnected = true;
        negativeTube.SetConnected(true);
    }
}