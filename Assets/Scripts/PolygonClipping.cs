using System;
using System.Collections.Generic;
using jksharp.jklviewer.JKL;
using UnityEngine;

namespace Assets.Scripts
{
    public static class PolygonClipping
    {
        private static Vector3? LinePlaneIntersect(Vector3 a, Vector3 b, Plane plane)
        {
            var lineNormal = (b - a).normalized;
            float vdot = Vector3.Dot(lineNormal, plane.normal);
            float ndot = -Vector3.Dot(a, plane.normal) - plane.distance;

            if (vdot == 0) // line parallell to plane
                return null;

            float t = ndot / vdot;

            var intersection = a + lineNormal * t;
            return intersection;

        }

        private static bool IsInside(Plane plane, Vector3 point)
        {
            return Vector3.Dot(plane.normal, point) + plane.distance >= 0;
        }

        public static List<Vector3> SutherlandHodgemanClipPolygon(List<Vector3> polygon, IEnumerable<Plane> clippingPlanes)
        {
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
                var isV1Inside = plane.GetSide(v1);
                var isV2Inside = plane.GetSide(v2);

                if (isV1Inside && isV2Inside) // both inside
                    clippedVertices.Add(v2);
                else
                {
                    var intersection = LinePlaneIntersect(v1, v2, plane);
                    if (intersection == null)
                        continue;

                    if (isV1Inside) // leaving
                        clippedVertices.Add(intersection.Value);
                    else if (isV2Inside)
                    { // entering
                        clippedVertices.Add(intersection.Value);
                        clippedVertices.Add(v2);
                    }
                }
            }
            return clippedVertices;
        }

        public static IEnumerable<Plane> CreatePlanesFromVertices(List<Vector3> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                var v1 = polygon[i];
                var v2 = polygon[(i + 1) % polygon.Count];
                var v3 = Camera.main.transform.position;
                yield return new Plane(v1, v2, v3);
            }
        }
    }
}