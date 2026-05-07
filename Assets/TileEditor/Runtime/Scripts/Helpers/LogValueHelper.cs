using UnityEngine;

namespace Fabros.TileEditor
{
    public class LogValueHelper : MonoBehaviour
    {
        public void Log(int value)
        {
            Debug.Log($"Int value: {value}", gameObject);
        }
    }
}