using System.Runtime.Serialization;
using UnityEngine;

namespace core
{
    public class Vector3IntSurrogateSerialize : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var v3 = (Vector3Int)obj;
            info.AddValue("x", v3.x);
            info.AddValue("y", v3.y);
            info.AddValue("z", v3.z);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            var v3 = (Vector3Int)obj;
            v3.x = (int)info.GetValue("x", TypeOf<int>.Raw);
            v3.y = (int)info.GetValue("y", TypeOf<int>.Raw);
            v3.z = (int)info.GetValue("z", TypeOf<int>.Raw);
            obj = v3;
            return obj;
        }
    }
}