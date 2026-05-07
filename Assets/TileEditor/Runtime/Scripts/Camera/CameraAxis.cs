using UnityEngine;

public class CameraAxis : MonoBehaviour
{
    [SerializeField] private Transform _axisTransform;
    [SerializeField] private Transform _cameraTransform;

    public void RotateAxis(Vector3 localEulerAngles)
    {
        _cameraTransform.localEulerAngles = localEulerAngles;
        _axisTransform.position =_cameraTransform.position + _cameraTransform.forward * 5;
    }
}
