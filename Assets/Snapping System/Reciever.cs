using UnityEngine;

/// <summary>
/// NegativeEnd → attached to every bottom tube (the "female" connector)
/// Very simple — just tracks whether this tube has already been used
/// </summary>
public class NegativeEnd : MonoBehaviour
{
    private bool isConnected = false;

    // Public getter so PositiveEnd can check
    public bool IsConnected => isConnected;

    // Called by PositiveEnd when a connection is made
    public void SetConnected(bool value) => isConnected = value;
}