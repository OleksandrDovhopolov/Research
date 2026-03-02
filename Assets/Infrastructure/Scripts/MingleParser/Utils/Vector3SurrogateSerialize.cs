using System.Runtime.Serialization;
using UnityEngine;

namespace Infrastructure
{
    public class Vector3SurrogateSerialize : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var v3 = (Vector3)obj;
            info.AddValue("x", v3.x);
            info.AddValue("y", v3.y);
            info.AddValue("z", v3.z);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var v3 = (Vector3)obj;
            v3.x = (float)info.GetValue("x", TypeOf<float>.Raw);
            v3.y = (float)info.GetValue("y", TypeOf<float>.Raw);
            v3.z = (float)info.GetValue("z", TypeOf<float>.Raw);
            obj = v3;
            return obj;
        }
    }
}
