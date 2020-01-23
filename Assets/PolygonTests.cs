using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

[ExecuteInEditMode]
public class PolygonTests : MonoBehaviour
{
    List<Vector3> polygon = new List<Vector3>() {
        new Vector3(-9.0f, -28.0f, -10.0f),
        new Vector3(9.0f, -28.0f, -10.0f),
        new Vector3(9.0f, -27.0f, -10.0f),
        new Vector3(-9.0f, -27.0f, -10.0f)
    };

    List<Vector3> polygon2 = new List<Vector3>() {
        new Vector3(-10, -20, 10),
        new Vector3(10, -20, 10),
        new Vector3(10, -30, 10),
        new Vector3(-10, -30, 10),
    };

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        DrawGizmoPolygon(polygon);
        Gizmos.color = Color.blue;
        DrawGizmoPolygon(polygon2);

        var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        var clippedPolygon = PolygonClipping.SutherlandHodgemanClipPolygon(polygon, planes);

        if (clippedPolygon.Count > 0)
        {
            Gizmos.color = Color.green;
            DrawGizmoPolygon(clippedPolygon);

            var newPlanes = PolygonClipping.CreatePlanesFromVertices(clippedPolygon);
            var clippedPolygon2 = PolygonClipping.SutherlandHodgemanClipPolygon(polygon2, newPlanes);

            Gizmos.color = Color.magenta;
            DrawGizmoPolygon(clippedPolygon2);
        }
    }

    void DrawGizmoPolygon(List<Vector3> polygon)
    {
        for (int i = 0; i < polygon.Count; i++)
        {
            var v1 = polygon[i];
            var v2 = polygon[(i + 1) % polygon.Count];
            Gizmos.DrawLine(v1, v2);
        }
    }
}
