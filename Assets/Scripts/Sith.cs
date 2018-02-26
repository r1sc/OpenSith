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

    public string _3DOToLoad;
    public string AnimKEYToLoad;

    struct TexInfo
    {
        public Rect[] Rects;
        public Vector2[] Sizes;
    }

    private CMP _cmp;
    private Dictionary<string, TexInfo> _materialLookup = new Dictionary<string, TexInfo>();

    private void LoadTextures(string[] matFilenames)
    {
        var textures = new Dictionary<string, Texture2D[]>();

        foreach (var matFilename in matFilenames)
        {
            var mat = new MAT();
            var matPath = @"Extracted\3do\mat\" + matFilename;
            if (!File.Exists(matPath))
                matPath = @"Extracted\mat\" + matFilename;

            mat.ParseMat(_cmp, matPath);
            textures.Add(matFilename, new[] { mat.Textures[0] });
        }

        var rects = Atlas.PackTextures(textures.Values.SelectMany(x => x).ToArray(), 0);

        var rectsOffset = 0;
        foreach (var matFilename in matFilenames)
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
        var jkl = new JKL();
        var jklParser = new JKLParser(jkl, new FileStream(@"Extracted\Episode\JK1.GOB\jkl\01narshadda.jkl", FileMode.Open));

        _cmp = new CMP();
        _cmp.ParseCMP(@"Extracted\misc\cmp\" + jkl.WorldColorMaps[0]);


        Atlas = new Texture2D(256, 256, TextureFormat.ARGB32, true);
        //Atlas.alphaIsTransparency = true;

        LoadTextures(jkl.Materials.Take(jkl.ActualNumberOfMaterials).Select(m => m.Name).ToArray());
        StandardMaterial.mainTexture = Atlas;

        //var materials = new List<Material>();
        //var textures = new List<Texture2D>();
        //for (int i = 0; i < jkl.Materials.Length; i++)
        //{
        //    if (jkl.Materials[i] == null)
        //        continue;
        //    var matName = jkl.Materials[i].Name;
        //    if (matName == "DFLT.MAT")
        //    {
        //        materials.Add(StandardMaterial);
        //    }
        //    else
        //    {
        //        var material = Instantiate(StandardMaterial);
        //        var mat = new MAT();
        //        var matPath = @"Extracted\3do\mat\" + matName;
        //        if (!File.Exists(matPath))
        //            matPath = @"Extracted\mat\" + matName;
        //        mat.ParseMat(cmp, matPath);
        //        textures.Add(mat.Textures.First());
        //        material.mainTexture = mat.Textures[0];

        //        materials.Add(material);
        //    }
        //}
        //Textures = textures.ToArray();
        //Materials = materials.ToArray();


        BuildMapSectors(jkl);

        //Atlas = new Texture2D(2048, 2048);
        //Atlas.filterMode = FilterMode.Point;
        //var textureRects = Atlas.PackTextures(textures.ToArray(), 0);

        //var matRects = new List<MatTextureRect>();
        //for (int i = 0; i < textureRects.Length; i++)
        //{
        //    matRects.Add(new MatTextureRect
        //    {
        //        Texture2D = textures[i],
        //        Rect = textureRects[i],
        //    });
        //}
        //Material = Instantiate(StandardMaterial);
        //Material.mainTexture = Atlas;

        // var go1 = Load3DO(cmp, @"Extracted\3do\seqp.3do");
        // var go2 = Load3DO(cmp, @"Extracted\3do\r2.3do");
        // go2.transform.position += Vector3.right * 2;
        //var threedo = new _3DO();
        //threedo.Load3DO(@"Extracted\3do\" + _3DOToLoad);
        //var st = Load3DO(cmp, threedo);


        //if (AnimKEYToLoad != "")
        //{
        //    var keyParser = new KEYParser();
        //    var animClip = keyParser.Parse(threedo, st.transform, @"Extracted\3do\key\" + AnimKEYToLoad);
        //    var anim = (Animation)st.AddComponent(typeof(Animation));

        //    animClip.wrapMode = WrapMode.Loop;
        //    anim.clip = animClip;
        //    anim.AddClip(animClip, keyParser.Name);
        //    anim.Play();
        //}
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
            var colors = new List<Color>();

            for (int i = 0; i < sector.SurfaceCount; i++)
            {
                var surfaceIdx = sector.SurfaceStartIdx + i;
                //if (surfaceIdx != 79)
                //    continue;

                var surface = jkl.WorldSurfaces[surfaceIdx];
                if (surface.Adjoin != null)
                    continue;
                
                var viStart = vertices.Count;
                for (int j = 0; j < surface.SurfaceVertexGroups.Length; j++)
                {
                    var vertIndex = surface.SurfaceVertexGroups[j].VertexIdx;
                    var vert = new Vector3(jkl.WorldVertices[vertIndex].x, jkl.WorldVertices[vertIndex].z,
                        jkl.WorldVertices[vertIndex].y);
                    vertices.Add(vert);
                    normals.Add(new Vector3(surface.SurfaceNormal.x, surface.SurfaceNormal.z, surface.SurfaceNormal.y));

                    var uv = surface.SurfaceVertexGroups[j].TextureVertex;
                    if (uv.HasValue)
                    {
                        var material = _materialLookup[surface.Material.Name];

                        var uv2 = uv.Value;
                        colors.Add(new Color(material.Rects[0].x, material.Rects[0].y, material.Rects[0].width, material.Rects[0].height));

                        uv2.x = uv2.x / material.Sizes[0].x;
                        uv2.y = uv2.y / material.Sizes[0].y;
                        
                        uvs.Add(uv2);
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

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            meshFilter.sharedMesh = mesh;

            //var collider = go.AddComponent<MeshCollider>();
            //collider.cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices |
            //                          MeshColliderCookingOptions.InflateConvexMesh |
            //                          MeshColliderCookingOptions.EnableMeshCleaning |
            //                          MeshColliderCookingOptions.CookForFasterSimulation;

            //collider.sharedMesh = mesh;
        }
    }

    private GameObject Load3DO(CMP cmp, _3DO threedo)
    {
        var geoset = threedo.Geosets.First();
        var root = new GameObject(threedo.Name);
        var gameObjects = new List<GameObject>();
        for (int index = 0; index < threedo.HierarchyNodes.Length; index++)
        {
            var hierarchyNode = threedo.HierarchyNodes[index];
            var go = new GameObject(hierarchyNode.NodeName);
            hierarchyNode.Transform = go.transform;
            if (hierarchyNode.Mesh != -1)
            {
                var tdMesh = geoset.Meshes[hierarchyNode.Mesh];
                go.AddComponent<MeshFilter>().sharedMesh = BuildMesh(tdMesh, hierarchyNode);

                //var faceMaterials = tdMesh.Faces.Select(x => x.Material).Distinct();
                go.AddComponent<MeshRenderer>().sharedMaterial =
                    StandardMaterial; //faceMaterials.Select(x => Materials[x]).ToArray();
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

        root.transform.rotation = Quaternion.AngleAxis(-90, Vector3.right);
        root.transform.localScale = new Vector3(10, 10, 10);
        return root;
    }

    private Mesh BuildMesh(_3DOMesh tdMesh, HierarchyNode hierarchyNode)
    {
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        //var facesGroupedByMaterial = tdMesh.Faces.GroupBy(x => x.Material).ToArray();
        //mesh.subMeshCount = facesGroupedByMaterial.Length;
        //var submeshTriangles = new Dictionary<int, List<int>>();
        //foreach (var facegroup in facesGroupedByMaterial)
        //{
        //submeshTriangles[facegroup.Key] = new List<int>();

        for (var faceIndex = 0; faceIndex < tdMesh.Faces.Length; faceIndex++)
        {
            //var faceIndex = tdMesh.Faces.ToList().IndexOf(face);
            var face = tdMesh.Faces[faceIndex];
            var viStart = vertices.Count;

            foreach (VertexGroup t in face.Vertices)
            {
                vertices.Add(hierarchyNode.Pivot + (Vector3)tdMesh.Vertices[t.VertexIndex]);
                normals.Add(tdMesh.FaceNormals[faceIndex]);

                var material = StandardMaterial; //Materials[face.Material];
                var uv = tdMesh.TextureVertices[t.TextureIndex];
                uv.x = uv.x / material.mainTexture.width;
                uv.y = uv.y / material.mainTexture.height;

                uvs.Add(uv);
            }

            var numTriangles = face.Vertices.Length - 3 + 1;
            for (var t = 1; t <= numTriangles; t++)
            {
                triangles.Add(viStart + t + 1);
                triangles.Add(viStart);
                triangles.Add(viStart + t);
            }
        }
        //}

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        //var i = 0;
        //foreach (var submeshTriangle in submeshTriangles)
        //{
        //    mesh.SetTriangles(submeshTriangles[submeshTriangle.Key].ToArray(), i);
        //    i++;
        //}
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }
}
