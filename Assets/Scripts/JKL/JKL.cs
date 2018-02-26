using System.Collections.Generic;
using UnityEngine;

namespace jksharp.jklviewer.JKL
{
    class JKL
    {
        public float GouraudDistance { get; set; }
        public float PerspectiveDistance { get; set; }
        public Vector4 LODDistances { get; set; }
        public Vector4 MipMapDistances { get; set; }
        public Vector2 CeilingSkyOffset { get; set; }
        public Vector2 HorizonSkyOffset { get; set; }
        public float HorizonPixelsPerRev { get; set; }
        public float HorizonDistance { get; set; }
        public float CeilingSkyZ { get; set; }
        public float WorldGravity { get; set; }
        public int Version { get; set; }
        public string[] Sounds { get; set; }
        public Material[] Materials { get; set; }
        public string[] WorldColorMaps { get; set; }
        public Vector3[] WorldVertices { get; set; }
        public Vector2[] WorldTextureVertices { get; set; }
        public Adjoin[] WorldAdjoins { get; set; }
        public WorldSurface[] WorldSurfaces { get; set; }
        public Sector[] Sectors { get; set; }
        public int ActualNumberOfMaterials { get; set; }
    }
}