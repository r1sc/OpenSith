using System;
using System.Collections.Generic;
using jksharp.jklviewer.JKL;
using UnityEngine;

namespace Assets.Scripts
{
    public static class PolygonClipping
    {
        private static Vector3 LinePlaneIntersect(Vector3 a, Vector3 b, Plane plane)
        {
            var lineNormal = (b - a).normalized;
            float vdot = Vector3.Dot(lineNormal, plane.normal);
            float ndot = -Vector3.Dot(a, plane.normal) - plane.distance;

            if (vdot == 0) // line parallell to plane
                throw new Exception("Should never happen!");

            float t = ndot / vdot;

            var intersection = a + lineNormal * t;
            return intersection;

        }

        private static bool IsInside(Plane plane, Vector3 point)
        {
            var dist = Vector3.Dot(plane.normal, point) + plane.distance;
            if (dist > 0.001f)
                return true;
            
            return false;
        }

        public static List<Vector3> SutherlandHodgemanClipPolygon(Vector3 pov, Vector3 normal, List<Vector3> polygon, IEnumerable<Plane> clippingPlanes)
        {
            //var iscw = IsCW(polygon);            
            var polygonPlane = new Plane(normal, polygon[0]);
            //if (!iscw)
            //    polygonPlane.Flip();
            //polygonPlane.normal = normal;
            if (!polygonPlane.GetSide(pov))
                return new List<Vector3>();

            var clippedVertices = new List<Vector3>(polygon);
            foreach (var plane in clippingPlanes)
            {
                clippedVertices = SutherlandHodgemanClipPlane(clippedVertices, plane);
            }
            return clippedVertices;
        }

        private static List<Vector3> SutherlandHodgemanClipPlane(List<Vector3> polygon, Plane plane)
        {
            var clippedVertices = new List<Vector3>();
            for (var i = 0; i < polygon.Count; i++)
            {
                var v1 = polygon[i];
                var v2 = polygon[(i + 1) % polygon.Count];
                var isV1Inside = IsInside(plane, v1);
                var isV2Inside = IsInside(plane, v2);

                if (!isV1Inside && !isV2Inside)
                    continue;

                if (isV1Inside && isV2Inside) // both inside
                    clippedVertices.Add(v2);
                else
                {
                    if (isV1Inside && !isV2Inside)
                    {
                        // leaving
                        var intersection = LinePlaneIntersect(v1, v2, plane);
                        clippedVertices.Add(intersection);
                    }
                    else if (!isV1Inside && isV2Inside)
                    {
                        // entering
                        var intersection = LinePlaneIntersect(v1, v2, plane);
                        clippedVertices.Add(intersection);
                        clippedVertices.Add(v2);
                    }
                }
            }
            return clippedVertices;
        }

        public static List<Plane> CreatePlanesFromVertices(List<Vector3> polygon)
        {
            var v3 = Camera.main.transform.position;
            var result = new List<Plane>();
            for (int i = 0; i < polygon.Count; i++)
            {
                var v1 = polygon[i];
                var v2 = polygon[(i + 1) % polygon.Count];
                
                var plane = new Plane(v1, v2, v3);
                result.Add(plane);
            }
            return result;
        }

        public static bool IsCW(List<Vector3> polygon)
        {
            /*
            Sum over the edges, (x2 − x1)(y2 + y1). If the result is positive the curve is clockwise, if it's negative the curve is counter-clockwise. (The result is twice the enclosed area, with a +/- convention.)
            */
            float sum = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                var v1 = polygon[i];
                var v2 = polygon[(i + 1) % polygon.Count];
                sum += (v2.x - v1.x) * (v2.y + v1.y);
            }
            return sum > 0;
        }

        public static void DrawPlane(Plane plane)
        {
            var origo = plane.distance * -plane.normal;
            var oldMat = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(origo, Quaternion.LookRotation(plane.normal), Vector3.one);
            var color = Color.red;
            color.a = 1f;
            Gizmos.color = color;
            Gizmos.DrawCube(Vector3.zero, new Vector3(10, 10, 0.1f));
            Gizmos.matrix = oldMat;

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origo, origo + plane.normal);
        }
    }
}