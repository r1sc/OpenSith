using jksharp.jklviewer.JKL;
using UnityEngine;

public class SectorObject : MonoBehaviour
{
    public Sector Sector;
    public bool Drawn;

    // Draws a wireframe sphere in the Scene view, fully enclosing
    // the object.
    void OnDrawGizmos()
    {
        if (Drawn)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Sector.BoundingBox.center, Sector.BoundingBox.size);
        }
    }
}