using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Assets.Scripts;
using jksharp.jklviewer.JKL;
using UnityEditor;
using UnityEngine;
using Material = UnityEngine.Material;

public class Sith : MonoBehaviour
{
    public QuadTree SectorQuadTree;

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

    private class ThreedoLoadResult
    {
        public _3DO Threedo { get; set; }
        public GameObject GameObject { get; set; }
    }

    private CMP _cmp;
    private Camera _camera;
    private JKL _jkl;
    private readonly Dictionary<string, TexInfo> _materialLookup = new Dictionary<string, TexInfo>();
    private readonly Dictionary<string, ThreedoLoadResult> _3DOCache = new Dictionary<string, ThreedoLoadResult>();

    private HashSet<int> DrawnSectors = new HashSet<int>();

    struct ClippedAdjoin
    {
        public List<Vector3> Polygon;
        public int SectorIndex;
    }
    private List<ClippedAdjoin> clippedAdjoins = new List<ClippedAdjoin>();

    public void Update()
    {
        DrawnSectors.Clear();
        clippedAdjoins.Clear();

        var cameraSector = SectorQuadTree.GetSectorContaining(_camera.transform.position / 10.0f);

        if (cameraSector != null)
        {
            Debug.Log("Drawing sector " + cameraSector.BoundingBox);
            var matrix = Matrix4x4.Scale(Vector3.one * 10);

            var cameraClippingPlanes = GeometryUtility.CalculateFrustumPlanes(_camera);
            DrawSector(cameraSector, matrix, cameraClippingPlanes);
        }
    }

    /*
    function DrawSector(sector, clippingPlanes):
        if HasBeenDrawnThisFrame(sector):
            return

        foreach portal in sector:
            clippedPortalPolygon = ClipPortal(portal.polygon, clippingPlanes)
            if clippedPortalPolygon.length > 0
                DrawSector(portal.othersector, clippedPortalPolygon)

    */

    void OnDrawGizmos()
    {
        if (SectorQuadTree == null)
            return;

        Gizmos.color = Color.magenta;
        var cameraSector = SectorQuadTree.GetSectorContaining(_camera.transform.position / 10.0f);
        Gizmos.DrawWireCube(cameraSector.BoundingBox.center * 10, cameraSector.BoundingBox.size * 10);

        Gizmos.color = Color.cyan;
        foreach (var adjoin in cameraSector.Adjoins)
        {
            DrawGizmoPolygon(adjoin.SurfaceVertices);
        }

        Gizmos.color = Color.green;
        foreach (var clippedAdjoin in clippedAdjoins)
        {
            DrawGizmoPolygon(clippedAdjoin.Polygon);
            Handles.Label(clippedAdjoin.Polygon[0], "Sector " + clippedAdjoin.SectorIndex);
        }

    }

    void DrawGizmoPolygon(List<Vector3> polygon)
    {
        for (int i = 0; i < polygon.Count; i++)
        {
            var v1 = polygon[i];
            var v2 = polygon[(i + 1) % polygon.Count];
            Gizmos.DrawLine(v1, v2);
        }
    }

    private void DrawSector(Sector sector, Matrix4x4 matrix, IEnumerable<Plane> clippingPlanes)
    {
        if (DrawnSectors.Contains(sector.Index))
            return;

        DrawnSectors.Add(sector.Index);
        if (sector.Index == 376)
            Graphics.DrawMesh(sector.Mesh, matrix, StandardMaterial, 0);

        foreach (var adjoin in sector.Adjoins)
        {
            var otherSurface = _jkl.WorldAdjoins[adjoin.Mirror].Surface;


            var clippedAdjoin = PolygonClipping.SutherlandHodgemanClipPolygon(adjoin.SurfaceVertices, clippingPlanes);
            if (clippedAdjoin.Count > 0)
            {
                var newClippingPlanes = PolygonClipping.CreatePlanesFromVertices(clippedAdjoin);

                // if (otherSector.Index == sector.Index)
                //     otherSector = adjoin.Surface.Sector;

                clippedAdjoins.Add(new ClippedAdjoin { Polygon = clippedAdjoin, SectorIndex = otherSurface.Sector.Index });


                DrawSector(otherSurface.Sector, matrix, newClippingPlanes);

            }

        }
    }

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
        _camera = Camera.main;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-us");

        _gobManager = new GOBManager(GamePath, new[] { Path.Combine(GamePath, "Resource\\Res1hi.gob"), Path.Combine(GamePath, "Resource\\Res2.gob"), Path.Combine(GamePath, "Episode\\JK1.GOB") });

        _cmp = new CMP();
        Atlas = new Texture2D(256, 256, TextureFormat.ARGB32, true);
        StandardMaterial.mainTexture = Atlas;

        if (!string.IsNullOrEmpty(JKLToLoad))
        {
            _jkl = new JKL();
            var jklParser = new JKLParser(_jkl, _gobManager.GetStream(@"jkl\" + JKLToLoad)); //new FileStream(@"Extracted\jkl\" + JKLToLoad, FileMode.Open));
            jklParser.Parse();

            _cmp.ParseCMP(_gobManager.GetStream(@"misc\cmp\" + _jkl.WorldColorMaps[0]));
            LoadTextures(_jkl.Materials.Take(_jkl.ActualNumberOfMaterials).Select(m => m.Name).ToArray());
            BuildMapSectors();

            foreach (var thing in _jkl.Things)
            {
                if (thing.Template.Parameters.ContainsKey("model3d"))
                {
                    var modelFilename = thing.Template.Parameters["model3d"];

                    _3DO threedo;
                    GameObject thingGameObject;
                    if (_3DOCache.ContainsKey(modelFilename))
                    {
                        var threedoLoadResult = _3DOCache[modelFilename];
                        threedo = threedoLoadResult.Threedo;
                        thingGameObject = Instantiate(threedoLoadResult.GameObject);
                    }
                    else
                    {
                        var threedoLoadResult = Load3DO(modelFilename);
                        threedo = threedoLoadResult.Threedo;
                        thingGameObject = threedoLoadResult.GameObject;
                    }

                    thingGameObject.transform.position = new Vector3(thing.X * 10, thing.Z * 10, thing.Y * 10);
                    thingGameObject.transform.rotation = Quaternion.Euler(thing.Pitch, thing.Yaw, thing.Roll);

                    if (thing.Template.Parameters.ContainsKey("puppet"))
                    {
                        var puppetFilename = thing.Template.Parameters["puppet"];
                        var puppet = PUPPETParser.Parse(_gobManager.GetStream(@"misc\pup\" + puppetFilename));
                        var kp = new KEYParser();

                        if (puppet.Modes.ContainsKey(0) && puppet.Modes[0].ContainsKey("stand"))
                        {
                            var keyFilename = puppet.Modes[0]["stand"].KeyFile;
                            var animClip = kp.Parse(threedo, thingGameObject.transform, keyFilename,
                                _gobManager.GetStream(@"3do\key\" + keyFilename));

                            var anim = thingGameObject.GetComponent<Animation>();
                            if (anim == null)
                            {
                                anim = thingGameObject.AddComponent<Animation>();
                                anim.AddClip(animClip, animClip.name);
                            }
                            anim.Play(animClip.name);
                        }
                    }
                }


            }
        }
    }

    private void BuildMapSectors()
    {
        var minmax = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var sector in _jkl.Sectors)
        {
            minmax.Encapsulate(sector.BoundingBox);
        }

        SectorQuadTree = new QuadTree(minmax);

        for (var sectorIdx = 0; sectorIdx < _jkl.Sectors.Length; sectorIdx++)
        {
            var sector = _jkl.Sectors[sectorIdx];
            // var go = new GameObject("Sector " + sectorIdx);
            // go.transform.SetParent(transform, false);

            // var meshFilter = go.AddComponent<MeshFilter>();
            // var meshRenderer = go.AddComponent<MeshRenderer>();
            // go.AddComponent<GizmoBBox>();
            // meshRenderer.enabled = false;
            // var sectorObj = go.AddComponent<SectorObject>();
            // sectorObj.Sector = sector;

            // meshRenderer.sharedMaterial = StandardMaterial;

            sector.Mesh = new Mesh();

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var atlasPosSize = new List<Vector4>();
            var intensities = new List<Color>();

            for (int i = 0; i < sector.SurfaceCount; i++)
            {
                var surfaceIdx = sector.SurfaceStartIdx + i;

                var surface = _jkl.WorldSurfaces[surfaceIdx];
                if (surface.Adjoin != null)
                {
                    if (surface.Adjoin.SurfaceVertices.Count > 0)
                        Debug.LogError("WTF");
                    surface.Adjoin.SurfaceVertices = new List<Vector3>();
                    for (var s = 0; s < surface.SurfaceVertexGroups.Length; s++)
                    {
                        SurfaceVertexGroup t = surface.SurfaceVertexGroups[s];
                        var vertIndex = t.VertexIdx;
                        var vert = new Vector3(_jkl.WorldVertices[vertIndex].x, _jkl.WorldVertices[vertIndex].z,
                            _jkl.WorldVertices[vertIndex].y);
                        surface.Adjoin.SurfaceVertices.Add(vert * 10);
                    }
                    sector.Adjoins.Add(surface.Adjoin);
                    continue;
                }

                var viStart = vertices.Count;
                for (var s = 0; s < surface.SurfaceVertexGroups.Length; s++)
                {
                    SurfaceVertexGroup t = surface.SurfaceVertexGroups[s];
                    var vertIndex = t.VertexIdx;
                    var vert = new Vector3(_jkl.WorldVertices[vertIndex].x, _jkl.WorldVertices[vertIndex].z,
                        _jkl.WorldVertices[vertIndex].y);
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

            sector.Mesh.SetVertices(vertices);
            sector.Mesh.SetUVs(0, uvs);
            sector.Mesh.SetUVs(1, atlasPosSize);
            sector.Mesh.SetColors(intensities);
            sector.Mesh.SetNormals(normals);
            sector.Mesh.SetTriangles(triangles, 0);
            sector.Mesh.bounds = sector.BoundingBox;

            // var collider = go.AddComponent<MeshCollider>();
            // collider.cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices |
            //                           MeshColliderCookingOptions.EnableMeshCleaning |
            //                           MeshColliderCookingOptions.CookForFasterSimulation;

            // collider.sharedMesh = mesh;

            SectorQuadTree.AddSector(sector);
        }
    }

    private ThreedoLoadResult Load3DO(string filename)
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

        var result = new ThreedoLoadResult
        {
            Threedo = threedo,
            GameObject = root
        };
        _3DOCache.Add(filename, result);
        return result;
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
