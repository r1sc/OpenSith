using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DrawTestPlanes : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        var vertices = new List<Vector3>()
        {
            new Vector3(-10, 10, 0),
            new Vector3(10, 10, 0),
            new Vector3(10, -10, 0)
        };

        Gizmos.color = Color.green;
        DrawPolygon(vertices);

        //var planes = new List<Plane>(GeometryUtility.CalculateFrustumPlanes(Camera.main)); //
        var planes  = PolygonClipping.CreatePlanesFromVertices(vertices);

        bool insideAll = true;
        foreach (var plane in planes)
        {
            if (!plane.GetSide(transform.position))
                insideAll = false;
            PolygonClipping.DrawPlane(plane);
        }
        Debug.Log(insideAll);
    }


    private void DrawPolygon(List<Vector3> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var v1 = vertices[i];
            var v2 = vertices[(i + 1) % vertices.Count];
            Gizmos.DrawLine(v1, v2);
        }
    }
}
