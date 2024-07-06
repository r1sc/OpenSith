using Assets.Sith.Content.JKL;
using Assets.Sith.Gui;
using Assets.Sith.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Sith.Game.World
{
    [FlagsAttribute]
    public enum SurfaceFlag
    {
        Floor      = 0x1,
        Collision  = 0x4,
        HorizonSky = 0x200,
        CeilingSky = 0x400
    };

    [ExecuteAlways, HideInInspector]
    public class SithSurface : MonoBehaviour
    {
        [ReadOnly]
        public int num = -1;

        public SurfaceFlag flags = 0;
        public GeoMode GeoMode = GeoMode.Textured;
        public LightMode LightMode = LightMode.Gouraud;
        public Color ExtraLight;

        private SurfaceFlag _surfflags = (SurfaceFlag)(-1);
        private GeoMode _geomode = GeoMode.Textured;
        private LightMode _lightMode = LightMode.Gouraud;

        private SithSector _sector;
        private int[] _surfaceTiangles;

        private Color _extraLight;
        private bool _updateExtraLight = true;

        private const SurfaceFlag _skyMask = SurfaceFlag.HorizonSky | SurfaceFlag.CeilingSky;

        void Start()
        {
            _sector = gameObject.GetComponent<SithSector>();
            if (TryGetComponent<MeshFilter>(out var mf))
            {
                // This solves the chicken-egg problem when entering the play mode. i.e. sector is inited after surface.
                _surfaceTiangles = mf.sharedMesh.GetTriangles(num);
            }
        }

        private void Update()
        {
            if (_surfflags != flags)
            {
                UpdateFlags();
            }

            if (_geomode != GeoMode)
            {
                UpdateGeomode();
            }

            if (_lightMode != LightMode)
            {
                UpdateLightMode();
            }

            if (_updateExtraLight)
            {
                var mat = GetMaterial();
                if (mat != null)
                {
                    mat.SetFaceExtraLight(ExtraLight);
                    
                }
                _extraLight = ExtraLight;
                _updateExtraLight = false; // Note: if mat is null and later changes to instance, that instance won't have correct geomode set
            }
        }

        private void EnableCollision(Boolean enable)
        {
            int[] triangles = null;
            if (enable)
            {
                triangles = _surfaceTiangles;
            }

            var collider = GetComponent<MeshCollider>();
            var mesh = collider.sharedMesh;
            mesh.SetTriangles(triangles, num);
            collider.sharedMesh = mesh;
        }

        private void UpdateGeomode()
        {
            int[] triangles = null;
            switch (GeoMode)
            {
                case GeoMode.Solid:
                case GeoMode.Textured:
                    triangles = _surfaceTiangles;
                    break;
            }
            _sector.Mesh.SetTriangles(triangles, num);

            var mat = GetMaterial();
            if (mat != null)
            {
                mat.SetGeoMode(GeoMode);
            }
            _geomode = GeoMode; // Note: if mat is null and later changes to instance, that instance won't have correct geomode set
        }

        private void UpdateLightMode()
        {
            if ((flags & _skyMask) != 0)
            {
                LightMode = LightMode.Unlit;
            }

            var mat = GetMaterial();
            if (mat != null)
            {
                mat.SetLightMode(LightMode);
            }
            _lightMode = LightMode; // Note: if mat is null and later changes to instance, that instance won't have correct light mode set
        }

        private void OnValidate()
        {
            _updateExtraLight = _updateExtraLight || (_extraLight != ExtraLight);
        }

        private UnityEngine.Material GetMaterial()
        {
            UnityEngine.Material mat = null;
            if (TryGetComponent<MeshRenderer>(out var mr))
            {
                mat = mr.sharedMaterials[num];
            }
            return mat;
        }

        private void UpdateFlags()
        {
            if ((flags & SurfaceFlag.Collision) != (_surfflags & SurfaceFlag.Collision))
            {
                EnableCollision((flags & SurfaceFlag.Collision) != 0);
            }

            if ((flags & _skyMask) != (_surfflags & _skyMask))
            {
                if ((flags & _skyMask) != 0)
                {
                    UpdateLightMode(); // Should set the surface to unlit
                }
            }
            _surfflags = flags;
        }
    }
}
