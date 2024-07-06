using Assets.Sith.Content.JKL;
using Assets.Sith.Gui;
using Assets.Sith.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Sith.Game.World
{
    //[ExcludeFromPreset, ExcludeFromObjectFactory]
    [ExecuteAlways, DisallowMultipleComponent]
    [Serializable]
    public class SithSector : MonoBehaviour
    {
        [Notify] // Delayed
        public int test;

        [HexField, Tooltip("Sector flags")]
        public int Flags;

        [Space(10)]
        public Color AmbientLight;
        public Color ExtraLight;

        [Tooltip("Enables blending sector tint color. (JKDF2 & MOTS)")]
        public bool BlendTint = false;

        [ConditionalHide("BlendTint", true)]
        public Color Tint;

        [Space(10)]
        [SerializeField]
        public Content.JKL.Sector.AmbientSound Sound; // optional
        public Vector3 Thrust;

        public Bounds BoundBox { get { return _boundBox; } }
        public Vector3 Center { get { return _center; } }
        public float Radius { get { return _radius; } }

        [Header("Sector Geometry")]
        [BoundsMinMaxViewAttribute]
        [SerializeField]
        private Bounds _boundBox;

        [ReadOnly]
        [SerializeField]
        private Vector3 _center;

        [ReadOnly]
        [SerializeField]
        private float _radius;

        private Color _ambientLight;
        private bool _updateAmbientLight;
        private Color _extraLight;
        private bool _updateExtraLight;
        private Color _tint;
        private bool _updateTint;

        private float _update = 1.1f;

        [HideInInspector]
        public Mesh Mesh { get; private set; }

        [HideInInspector]
        public SithSurface[] Surfaces { get {
            return gameObject.GetComponents<SithSurface>();
        }}

        public void SetMesh(Mesh mesh, UnityEngine.Material[] materials)
        {
            Mesh = mesh;
            if (!TryGetComponent<MeshRenderer>(out var mr))
            {
                mr = gameObject.AddComponent<MeshRenderer>();
            }
            mr.sharedMaterials = materials;

            if (!TryGetComponent<MeshFilter>(out var mf))
            {
                mf = gameObject.AddComponent<MeshFilter>();
            }
            mf.sharedMesh = Mesh;

            _updateAmbientLight = true;
            _updateExtraLight = true;
            _updateTint = BlendTint;
            UpdateLight();
            UpdateBounds();
            if (Mesh != null)
            {
                InitSurfaces();
            }
        }

        void Start()
        {
            if (Mesh == null && TryGetComponent<MeshFilter>(out var mf))
            {
                Mesh = mf.sharedMesh;
            }

            _updateAmbientLight = true;
            _updateExtraLight = true;
            _updateTint = BlendTint;
            UpdateLight();
            UpdateBounds();
        }

        void Reset()
        {
            //_updateAmbientLight = true;
            //_updateExtraLight   = true;
            //_updateTint         = BlendTint;
            //UpdateLight();
            //UpdateBounds();

            // Do not put in start function
            if (Mesh == null && TryGetComponent<MeshFilter>(out var mf))
            {
                Mesh = mf.sharedMesh;
                if (Mesh != null)
                {
                    InitSurfaces();
                }
            }
        }

        void Update()
        {
            _update += Time.deltaTime;
            if (_update > 1.0f)
            {
                _update = 0.0f;
            }

            UpdateBounds();
            UpdateLight();
        }

        private void OnDestroy()
        {
            ClearSurfaces();
        }

        private void ClearSurfaces()
        {
            foreach (SithSurface surf in gameObject.GetComponents<SithSurface>())
            {
                if (EditorApplication.isPlaying)
                {
                    Destroy(surf);
                }
                else
                {
                    DestroyImmediate(surf);
                }
            }
        }

        private void InitSurfaces()
        {
            if (!TryGetComponent<MeshCollider>(out var mc))
            {
                mc = gameObject.AddComponent<MeshCollider>();
            }

            mc.enabled = Mesh.vertices.Length > 0;
            mc.material.dynamicFriction = 0.0f;
            mc.material.staticFriction = 0.0f;
            mc.material.bounciness = 0;
            if (mc.enabled)
            {
                mc.sharedMesh = Instantiate(Mesh); // Duplicate mesh so the geomode and collision surface flags don't overlap
                //collider.convex = true;
                mc.cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices |
                                    MeshColliderCookingOptions.EnableMeshCleaning |
                                    MeshColliderCookingOptions.CookForFasterSimulation;
            }

            // Clear any existing surface(s) first
            ClearSurfaces();

            // Add surfaces
            if (Mesh != null)
            {
                for (var i = 0; i < Mesh.subMeshCount; i++)
                {
                    var surf = gameObject.AddComponent<SithSurface>();
                    surf.num = i;
                }
            }
        }

        public void testChanged()
        {
            //EditorUtility.SetDirty(this);
            Debug.Log($"In SithSector::testChanged: {test}");
        }

        void UpdateLight()
        {
            if (_updateAmbientLight || _updateExtraLight)
            {
                // Sector surfaces are only lit by extra light
                if (_updateExtraLight)
                {
                    gameObject.SetSectorLight(Color.black, ExtraLight);
                }

                _ambientLight       = AmbientLight;
                _updateAmbientLight = false;
                _extraLight         = ExtraLight;
                _updateExtraLight   = false;
            }

            if (_updateTint)
            {
                if (BlendTint || _tint != Color.black)
                {
                    var color = Tint;
                    if (!BlendTint)
                    {
                        _tint = Color.black;
                        color = _tint;
                    }
                    gameObject.SetSectorTint(color);
                    if (BlendTint) _tint = Tint;
                    _updateTint = false;
                }
            }
        }

        void UpdateBounds()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                _boundBox = mf.sharedMesh.bounds;
                _center = mf.sharedMesh.bounds.center;
                _radius = mf.sharedMesh.bounds.extents.magnitude;
            }
        }

        private void OnValidate()
        {
            _updateAmbientLight = _ambientLight != AmbientLight;
            _updateExtraLight   = _extraLight != ExtraLight;
            _updateTint         = _tint != Tint || !BlendTint;
        }
    }
}
