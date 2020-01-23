using UnityEngine;

public class GizmoBBox : MonoBehaviour
{
    public Bounds BoundingBox;

    void Start()
    {
        BoundingBox = GetComponent<Renderer>().bounds;
    }

    // Draws a wireframe sphere in the Scene view, fully enclosing
    // the object.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(BoundingBox.center, BoundingBox.size);
    }
}