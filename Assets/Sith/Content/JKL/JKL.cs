using System.Collections.Generic;
using UnityEngine;

namespace Assets.Sith.Content.JKL
{
    class Fog
    {
        public bool Enabled { get; set; }
        public Color Color { get; set; }
        public float Start { get; set; }
        public float End { get; set; }
    }

    public class Material
    {
        public string Name { get; set; }
        public float XTile { get; set; }
        public float YTile { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    class JKL
    {
        public WorldType type;

        public Fog Fog { get; set; }
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
        public Surface[] WorldSurfaces { get; set; }
        public Sector[] Sectors { get; set; }
        public int ActualNumberOfMaterials { get; set; }
        public List<string> Models { get; set; }
        public Dictionary<string, Thing> Templates { get; set; }
        public List<Thing> Things { get; set; }
    }
}