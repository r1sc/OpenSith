using Assets.Sith.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Sith.Game.Thing
{
    [SelectionBase, DisallowMultipleComponent]
    public class SithThing : MonoBehaviour
    {
        public string Type { get => _type; set { _type = value; } }
        public int Sector { get => _sector; set { _sector = value; } }

        [ReadOnly]
        [SerializeField]
        private string _type;

        [ReadOnly]
        [SerializeField]
        private int _sector;

        [Range(0.0f, 1.0f)]
        public float opacity = 1.0f;
        private float _thingOpacity = 1.0f;
        private float _update = 1.1f;

        void Update()
        {
            _update += Time.deltaTime;
            if (_update > 1.0f)
            {
                _update = 0.0f;
            }

            if (_thingOpacity != opacity)
            {
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    //var mats = new Material[rend.materials.Length];
                    for (var j = 0; j < r.materials.Length; j++)
                    {
                        var mat = r.materials[j];
                        mat.SetFloat("_Alpha", opacity);
                        mat.renderQueue = Mathf.Clamp(mat.renderQueue + (opacity < 1.0 ? + 1000 : -1000), 3000, 4000);
                    }
                }
                _thingOpacity = opacity;
            }
        }
    }
}
