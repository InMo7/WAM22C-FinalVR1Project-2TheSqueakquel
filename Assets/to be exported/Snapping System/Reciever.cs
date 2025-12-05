using UnityEngine;

public class NegativeEnd : MonoBehaviour
{
    private bool isConnected = false;

    public bool IsConnected => isConnected;

    public void SetConnected(bool connected)
    {
        isConnected = connected;
    }
}