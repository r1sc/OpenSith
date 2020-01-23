using System.Collections.Generic;
using UnityEngine;

public class RectDrawer : MonoBehaviour
{
    public List<Rect> RectsToDraw;
    public Material Material;

    void OnPostRender()
    {
        GL.PushMatrix();
        Material.SetPass(0);
        // GL.LoadProjectionMatrix(Matrix4x4.Ortho(-1, 1, -1, 1, -1, 1));
        GL.LoadOrtho();

        GL.Begin(GL.LINES);
        GL.Color(Color.green);
        foreach (var rect in RectsToDraw)
        {
            GL.Vertex3(rect.xMin, rect.yMax, 0);
            GL.Vertex3(rect.xMax, rect.yMax, 0);
            GL.Vertex3(rect.xMax, rect.yMax, 0);
            GL.Vertex3(rect.xMax, rect.yMin, 0);
            GL.Vertex3(rect.xMax, rect.yMin, 0);
            GL.Vertex3(rect.xMin, rect.yMin, 0);
            GL.Vertex3(rect.xMin, rect.yMin, 0);
            GL.Vertex3(rect.xMin, rect.yMax, 0);
        }
        GL.End();
        GL.PopMatrix();
    }
}