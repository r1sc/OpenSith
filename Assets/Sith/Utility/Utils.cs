using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Sith.Utility
{
    public class Utils
    {
        public const float geoScaleFactor = 10.0f;
        public static float Scale(float x)
        {
            return x * geoScaleFactor;
        }

        public static Vector2 Scale(Vector2 v)
        {
            return v * geoScaleFactor;
        }

        public static Vector3 Scale(Vector3 v)
        {
            return v * geoScaleFactor;
        }

        public static Vector3 GetUnityCoords(Vector3 pos)
        {
            return new Vector3(pos.x, pos.z, pos.y);
        }

        //public static Vector3 GetScaledUinityCoords(Vector3 sithPos)
        //{
        //    var pos = GetUnityCoords(sithPos);
        //    return Scale(pos);
        //}

        internal static Quaternion ToThingOrientation(Quaternion rotation)
        {
            return Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y /*+ 180.0f*/, rotation.eulerAngles.z);
        }

        public static Quaternion FromMeshOrientation(float pitch, float yaw, float roll)
        {
            return Quaternion.Euler(-pitch, -yaw, -roll);
        }
        public static Quaternion FromThingOrientation(float pitch, float yaw, float roll)
        {
            return Quaternion.Euler(-pitch, -yaw, -roll);
        }

        public static List<int> GetTriangulationIndices(int startVertIdx, int numVertices)
        {
            var triIndices = new List<int>();
            if (numVertices <= 3)
            {
                triIndices.Add(startVertIdx + 1); // switch 0 & 1 for unity coords
                triIndices.Add(startVertIdx);
                triIndices.Add(startVertIdx + 2);
            }
            else
            {
                var totalTris   = numVertices - 2;
                var ofsTriVer0 = 0;
                var ofsTriVer1 = 1;
                var ofsTriVer2 = numVertices - 1;
                for (var idx = 0; idx < totalTris; idx++)
                {
                    triIndices.Add(startVertIdx + ofsTriVer1); // switch 0 & 1 for unity coords
                    triIndices.Add(startVertIdx + ofsTriVer0);
                    triIndices.Add(startVertIdx + ofsTriVer2);

                    if ((~idx & 1) != 0)
                    {
                        ofsTriVer0 = ofsTriVer1++;
                    }
                    else
                    {
                        ofsTriVer0 = ofsTriVer2;
                        ofsTriVer2 = ofsTriVer2 - 1;
                    }
                }
            }
            return triIndices;
        }
    }
}
