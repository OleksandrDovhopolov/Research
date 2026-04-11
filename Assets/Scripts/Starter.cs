using UnityEngine;

public class Starter : MonoBehaviour
{
    void Start()
    {
        Debug.LogWarning($"[Debug] Start {GetType().Name}");
    }
}
