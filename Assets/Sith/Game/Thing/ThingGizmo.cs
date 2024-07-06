using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Sith.Game.Thing
{
    [DisallowMultipleComponent]
    public class ThingGizmo : MonoBehaviour
    {
        public bool show = true;
        public Vector3 size = new Vector3(0.25f, 0.25f, 0.25f);
        public Color color = Color.white;

        void OnDrawGizmos()
        {
            if (show)
            {
                Gizmos.color = color;
                Gizmos.DrawCube(transform.position, size);
            }
        }
    }
}
