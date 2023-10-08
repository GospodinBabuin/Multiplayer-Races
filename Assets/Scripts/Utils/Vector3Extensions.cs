using UnityEngine;

namespace Utils
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Sets any values of the Vector3
        /// </summary>
        public static Vector3 With(this Vector3 vector3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector3.x, y ?? vector3.y, z ?? vector3.z);
        }

        /// <summary>
        /// Adds to any values of the Vector3
        /// </summary>
        public static Vector3 Add(this Vector3 vector3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(vector3.x + (x ?? 0), vector3.y + (y ?? 0), vector3.z + (z ?? 0));
        }
    }
}
