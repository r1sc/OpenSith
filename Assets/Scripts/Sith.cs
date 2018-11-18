using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts;
using jksharp.jklviewer.JKL;
using UnityEngine;
using Material = UnityEngine.Material;

public class Sith : MonoBehaviour
{
    public Texture2D Atlas;
    public Material StandardMaterial;

    public string JKLToLoad;
    public string GamePath;
    private GOBManager _gobManager;


    private class TexInfo
    {
        public Rect[] Rects;
        public Vector2[] Sizes;
    }

    private CMP _cmp;
    private readonly Dictionary<string, TexInfo> _materialLookup = new Dictionary<string, TexInfo>();
    private readonly Dictionary<string, GameObject> _3DOCache = new Dictionary<string, GameObject>();

    private void LoadTextures(IEnumerable<string> matFilenames)
    {
        var textures = new Dictionary<string, Texture2D[]>();

        foreach (var matFilename in matFilenames)
        {
            if (textures.ContainsKey(matFilename.ToLower()))
            {
                Debug.LogWarning("Texture " + matFilename + " defined twice.");
                continue;
            }
            var mat = new MAT();
            var matPath = @"3do\mat\" + matFilename;
            if (!_gobManager.Exists(matPath))
                matPath = @"mat\" + matFilename;

            mat.ParseMat(_cmp, _gobManager.GetStream(matPath));
            textures.Add(matFilename.ToLower(), new[] { mat.Textures[0] });
        }

        var rects = Atlas.PackTextures(textures.Values.SelectMany(x => x).ToArray(), 0);

        var rectsOffset = 0;
        foreach (var matFilename in textures.Keys)
        {
            var numTextures = textures[matFilename].Length;
            var subrects = new Rect[numTextures];

            Array.Copy(rects, rectsOffset, subrects, 0, subrects.Length);

            var sizes = new List<Vector2>();
            foreach (var texture2D in textures[matFilename])
            {
                sizes.Add(new Vector2(texture2D.width, texture2D.height));
            }

            _materialLookup[matFilename] = new TexInfo
            {
                Rects = subrects,
                Sizes = sizes.ToArray()
            };

            rectsOffset += numTextures;
        }
    }

    // Use this for initialization
    void Start()
    {
        _gobManager = new GOBManager(GamePath, new[] { Path.Combine(GamePath, "Resource\\Res1hi.gob"), Path.Combine(GamePath, "Resource\\Res2.gob"), Path.Combine(GamePath, "Episode\\JK1.GOB") });

        _cmp = new CMP();
        Atlas = new Texture2D(256, 256, TextureFormat.ARGB32, true);
        StandardMaterial.mainTexture = Atlas;

        if (!string.IsNullOrEmpty(JKLToLoad))
        {
            var jkl = new JKL();
            var jklParser = new JKLParser(jkl, _gobManager.GetStream(@"jkl\" + JKLToLoad)); //new FileStream(@"Extracted\jkl\" + JKLToLoad, FileMode.Open));
            jklParser.Parse();

            _cmp.ParseCMP(_gobManager.GetStream(@"misc\cmp\" + jkl.WorldColorMaps[0]));
            LoadTextures(jkl.Materials.Take(jkl.ActualNumberOfMaterials).Select(m => m.Name).ToArray());
            BuildMapSectors(jkl);

            foreach (var thing in jkl.Things)
            {
                if (thing.Template.Parameters.ContainsKey("model3d"))
                {
                    var modelFilename = thing.Template.Parameters["model3d"];
                    GameObject thingGameObject;
                    if (_3DOCache.ContainsKey(modelFilename))
                        thingGameObject = Instantiate(_3DOCache[modelFilename]);
                    else
                        thingGameObject = Load3DO(modelFilename);

                    thingGameObject.transform.position = new Vector3(thing.X * 10, thing.Z * 10, thing.Y * 10);
                    thingGameObject.transform.rotation = Quaternion.Euler(thing.Pitch, thing.Yaw, thing.Roll);
                }
            }
        }
    }

    private void BuildMapSectors(JKL jkl)
    {
        for (var sectorIdx = 0; sectorIdx < jkl.Sectors.Length; sectorIdx++)
        {
            var sector = jkl.Sectors[sectorIdx];
            var go = new GameObject("Sector " + sectorIdx);
            go.transform.SetParent(transform, false);

            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = StandardMaterial;

            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var atlasPosSize = new List<Vector4>();
            var intensities = new List<Color>();

            for (int i = 0; i < sector.SurfaceCount; i++)
            {
                var surfaceIdx = sector.SurfaceStartIdx + i;

                var surface = jkl.WorldSurfaces[surfaceIdx];
                if (surface.Adjoin != null)
                    continue;

                var viStart = vertices.Count;
                for (var s = 0; s < surface.SurfaceVertexGroups.Length; s++)
                {
                    SurfaceVertexGroup t = surface.SurfaceVertexGroups[s];
                    var vertIndex = t.VertexIdx;
                    var vert = new Vector3(jkl.WorldVertices[vertIndex].x, jkl.WorldVertices[vertIndex].z,
                        jkl.WorldVertices[vertIndex].y);
                    vertices.Add(vert);
                    normals.Add(new Vector3(surface.SurfaceNormal.x, surface.SurfaceNormal.z, surface.SurfaceNormal.y));

                    var intensity = Mathf.Clamp01(surface.Intensities[s]);
                    intensities.Add(new Color(intensity, intensity, intensity));

                    var uv = t.TextureVertex;
                    if (uv.HasValue)
                    {
                        var material = _materialLookup[surface.Material.Name.ToLower()];

                        var uv2 = uv.Value;
                        atlasPosSize.Add(new Vector4(material.Rects[0].x, material.Rects[0].y, material.Rects[0].width,
                            material.Rects[0].height));

                        uv2.x = uv2.x / material.Sizes[0].x;
                        uv2.y = uv2.y / material.Sizes[0].y;

                        uvs.Add(uv2);
                    }
                    else
                    {
                        uvs.Add(Vector2.zero);
                        atlasPosSize.Add(new Vector4(0, 0, 0, 0));
                    }
                }

                var numTriangles = surface.SurfaceVertexGroups.Length - 3 + 1;
                for (var t = 1; t <= numTriangles; t++)
                {
                    triangles.Add(viStart + t);
                    triangles.Add(viStart);
                    triangles.Add(viStart + t + 1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, atlasPosSize);
            mesh.SetColors(intensities);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);

            meshFilter.sharedMesh = mesh;

            var collider = go.AddComponent<MeshCollider>();
            collider.cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices |
                                      MeshColliderCookingOptions.InflateConvexMesh |
                                      MeshColliderCookingOptions.EnableMeshCleaning |
                                      MeshColliderCookingOptions.CookForFasterSimulation;

            collider.sharedMesh = mesh;
        }
    }

    private GameObject Load3DO(string filename)
    {
        var threedo = new _3DO();
        threedo.Load3DO(filename, _gobManager.GetStream(@"3do\" + filename));

        var geoset = threedo.Geosets.First();
        var root = new GameObject(threedo.Name);
        var gameObjects = new List<GameObject>();
        foreach (var hierarchyNode in threedo.HierarchyNodes)
        {
            var go = new GameObject(hierarchyNode.NodeName);
            hierarchyNode.Transform = go.transform;
            if (hierarchyNode.Mesh != -1)
            {
                var tdMesh = geoset.Meshes[hierarchyNode.Mesh];
                go.AddComponent<MeshFilter>().sharedMesh = Build3DOMesh(threedo, tdMesh, hierarchyNode);
                go.AddComponent<MeshRenderer>().sharedMaterial = StandardMaterial;
            }
            gameObjects.Add(go);
        }
        for (int index = 0; index < threedo.HierarchyNodes.Length; index++)
        {
            var hierarchyNode = threedo.HierarchyNodes[index];
            var go = gameObjects[index];
            var parent = hierarchyNode.Parent == -1 ? root : gameObjects[hierarchyNode.Parent];
            go.transform.position = hierarchyNode.Translation;
            go.transform.SetParent(parent.transform, false);
        }

        root.transform.localScale = new Vector3(10, 10, 10);
        //root.SetActive(false);

        _3DOCache.Add(filename, root);
        return root;
    }

    private Mesh Build3DOMesh(_3DO threedo, _3DOMesh tdMesh, HierarchyNode hierarchyNode)
    {
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        var atlasPosSize = new List<Vector4>();

        for (var faceIndex = 0; faceIndex < tdMesh.Faces.Length; faceIndex++)
        {
            var face = tdMesh.Faces[faceIndex];
            var viStart = vertices.Count;

            var matName = threedo.Materials[face.Material].ToLower();
            if (!_materialLookup.ContainsKey(matName))
            {
                throw new Exception("Material " + matName + " not cached. Cannot continue.");
            }
            var material = _materialLookup[matName];
            foreach (VertexGroup t in face.Vertices)
            {
                var vert = tdMesh.Vertices[t.VertexIndex];
                var vert3 = new Vector3(-vert.x, vert.z, -vert.y);
                vertices.Add(hierarchyNode.Pivot + vert3);
                normals.Add(tdMesh.FaceNormals[faceIndex]);

                var uv = tdMesh.TextureVertices[t.TextureIndex];
                atlasPosSize.Add(new Vector4(material.Rects[0].x, material.Rects[0].y, material.Rects[0].width, material.Rects[0].height));

                uv.x = uv.x / material.Sizes[0].x;
                uv.y = uv.y / material.Sizes[0].y;

                uvs.Add(uv);
            }

            var numTriangles = face.Vertices.Length - 3 + 1;
            for (var t = 1; t <= numTriangles; t++)
            {
                triangles.Add(viStart + t);
                triangles.Add(viStart);
                triangles.Add(viStart + t + 1);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, atlasPosSize);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);


        mesh.RecalculateBounds();
        //mesh.RecalculateNormals();
        return mesh;
    }
}
