using UnityEngine;

namespace TileEditor
{
    public class TransformHelper : MonoBehaviour
    {
        public void SetLocalX(float x)
        {
            transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
        }

        public void SetLocalY(float y)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, y, transform.localPosition.z);
        }

        public void SetLocalZ(float z)
        {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
        }

        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        public void SetScaleX(float x)
        {
            transform.localScale = new Vector3(x, transform.localScale.y, transform.localScale.z);
        }

        public void SetScaleY(float y)
        {
            transform.localScale = new Vector3(transform.localScale.x, y, transform.localScale.z);
        }

        public void SetScaleZ(float z)
        {
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, z);
        }

        public void SetRotationX(float x)
        {
            var rotation = transform.rotation.eulerAngles;
            rotation.x = x;
            transform.rotation = Quaternion.Euler(rotation);
        }
        
        public void SetRotationY(float y)
        {
            var rotation = transform.rotation.eulerAngles;
            rotation.y = y;
            transform.rotation = Quaternion.Euler(rotation);
        }
        
        public void SetRotationZ(float z)
        {
            var rotation = transform.rotation.eulerAngles;
            rotation.z = z;
            transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
