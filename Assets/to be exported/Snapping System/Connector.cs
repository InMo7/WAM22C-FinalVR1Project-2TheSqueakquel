using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;  // Needed for haptics

[RequireComponent(typeof(Collider))]
public class PositiveEnd : MonoBehaviour
{
    [Header("Lego Settings")]
    public bool alignRotation = false;           // Keep false for Lego — we handle rotation manually below
    public AudioClip snapSound;                  // Drag your Lego "click" sound here (or it auto-loads from Resources)
    [Range(0f, 1f)] public float hapticStrength = 0.7f;
    [Range(0f, 1f)] public float hapticDuration = 0.1f;

    private bool isConnected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isConnected) return;

        NegativeEnd negativeEnd = other.GetComponent<NegativeEnd>();
        if (negativeEnd != null && !negativeEnd.IsConnected)
        {
            ConnectTo(negativeEnd);
        }
    }

    private void ConnectTo(NegativeEnd negativeEnd)
    {
        // Find the actual Lego bricks (the GameObjects that have the Rigidbody)
        Transform stationaryBrick = FindBrickRoot(transform);        // Lower brick (has the top stud)
        Transform movingBrick = FindBrickRoot(negativeEnd.transform); // Upper brick (has the bottom tube)

        if (stationaryBrick == null || movingBrick == null)
        {
            Debug.LogError("Brick root with Rigidbody not found!", this);
            return;
        }

        // === POSITION SNAP ===
        Vector3 offset = transform.position - negativeEnd.transform.position;
        movingBrick.position += offset;

        // === ROTATION SNAP — LEGO PERFECT FIX ===
        movingBrick.rotation = stationaryBrick.rotation;  // This is the magic line — no flips ever

        // === PHYSICAL CONNECTION ===
        FixedJoint joint = movingBrick.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = stationaryBrick.GetComponent<Rigidbody>();

        // === DISABLE ALL CONNECTORS ON BOTH BRICKS (prevents jitter/multiple snaps) ===
        DisableAllConnectors(stationaryBrick);
        DisableAllConnectors(movingBrick);

        // === SOUND & HAPTICS ===
        PlaySnapFeedback();

        isConnected = true;
        negativeEnd.SetConnected(true);

        Debug.Log($"SNAP! {movingBrick.name} → {stationaryBrick.name}");
    }

    private Transform FindBrickRoot(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            if (t.GetComponent<Rigidbody>() != null)
                return t;
            t = t.parent;
        }
        return null;
    }

    private void DisableAllConnectors(Transform brick)
    {
        foreach (PositiveEnd p in brick.GetComponentsInChildren<PositiveEnd>(true))
        {
            p.isConnected = true;
            var col = p.GetComponent<Collider>();
            if (col) col.enabled = false;
        }
        foreach (NegativeEnd n in brick.GetComponentsInChildren<NegativeEnd>(true))
        {
            n.SetConnected(true);
            var col = n.GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }

    private void PlaySnapFeedback()
    {
        // Sound
        if (snapSound != null)
            AudioSource.PlayClipAtPoint(snapSound, transform.position);
        else if (Resources.Load<AudioClip>("LegoClick"))  // Auto-load if you put a file named "LegoClick" in a Resources folder
            AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("LegoClick"), transform.position);

        // Haptics (works on Quest, Index, Vive, etc.)
        var controllers = FindObjectsOfType<XRBaseController>();
        foreach (var controller in controllers)
        {
            if (controller.enableInputActions)
                controller.SendHapticImpulse(hapticStrength, hapticDuration);
        }
    }

    // Optional: visually debug in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}