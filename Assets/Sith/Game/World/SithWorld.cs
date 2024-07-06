using Assets.Sith.Content;
using Assets.Sith.Content.JKL;
using Assets.Sith.Game.Thing;
using Assets.Sith.Game.Player;
using Assets.Sith.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace Assets.Sith.Game.World
{
    [ExecuteInEditMode, ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    public class SithWorld : MonoBehaviour
    {
        public GeoMode GeoMode = GeoMode.Textured;
        public LightMode LightMode = LightMode.Gouraud;
        public WorldType Type = WorldType.JKDF2;

        public SithWorld() 
        {
            _geoMode   = GeoMode;
            _lightMode = LightMode;
        }

        public void Load(string levelPath)
        {
            Clear();

            this.name = name;
            _cmp = new CMP();

            var jkl       = new JKL();
            var jklParser = new JKLParser(jkl, SithAssets.Instance.GetStream(levelPath));
            jklParser.Parse();

            _type = Type = jkl.type;
            if (jkl.WorldColorMaps != null && jkl.WorldColorMaps.Length > 0)
            {
                _cmp = SithAssets.Instance.Load<CMP>(jkl.WorldColorMaps[0]);
            }

            //Physics.gravity = new Vector3(0, -jkl.WorldGravity, 0);

            BuildWorldGeometry(jkl);
            BuildThings(jkl);
        }

        private void Clear()
        {
            _modelCache.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private GeoMode _geoMode;
        private LightMode _lightMode;
        private WorldType _type;
        bool _updateGeoMode   = false;
        bool _updateLightMode = false;
        bool _updateType      = false;
        float _elapsed;

        private void OnValidate()
        {
            _updateGeoMode  = _geoMode != GeoMode;
            _updateLightMode = _lightMode != LightMode;
            _updateType      = _type != Type;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

            if (_updateGeoMode || _updateLightMode || _updateType)
            {
                _geoMode = GeoMode;
                _lightMode = LightMode;
                _type = Type;
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    for (var j = 0; j < r.materials.Length; j++)
                    {
                        r.materials[j].SetGeoMode(_geoMode);
                        r.materials[j].SetLightMode(_lightMode);
                        r.materials[j].EnableIjimShading(_type == WorldType.IJIM);
                    }
                }
                _updateGeoMode   = false;
                _updateLightMode = false;
                _updateType      = false;
            }
        }

        private string GetSectorNameByIdx(int idx)
        {
            return "sector " + idx;
        }

        private GameObject GetSectorObj(int idx)
        {
            var s = GameObject.Find(GetSectorNameByIdx(idx));
            if (s == null || s.GetComponent<SithSector>() == null)
            {
                Debug.LogError($"GetSector: Invalid sector object idx={idx} in hierarchy");
                return null;
            }
            return s;
        }

        private void BuildWorldGeometry(JKL jkl)
        {
            for (var sectorIdx = 0; sectorIdx < jkl.Sectors.Length; sectorIdx++)
            {
                var sector    = jkl.Sectors[sectorIdx];
                var sectorObj = new GameObject(GetSectorNameByIdx(sectorIdx))
                {
                    isStatic = true
                };
                sectorObj.transform.localScale = Utils.Scale(sectorObj.transform.localScale);
                sectorObj.transform.SetParent(transform, false);

                var ss = sectorObj.AddComponent<SithSector>();
                ss.Flags        = (int)sector.Flags;
                ss.AmbientLight = sector.AmbientLight;
                ss.ExtraLight   = sector.ExtraLight;
                ss.BlendTint    = jkl.Version == 1;
                ss.Tint         = sector.Tint ?? Color.white;
                ss.Sound        = sector.Sound;
                ss.Thrust       = sector?.Thrust ?? new Vector3();

                var (mesh, materials) = BuildSectorMesh(sector, jkl);
                ss.SetMesh(mesh, materials);

                for (int i = 0; i < sector.SurfaceCount; i++)
                {
                    var surfaceIdx  = sector.SurfaceStartIdx + i;
                    var jklSurf     = jkl.WorldSurfaces[surfaceIdx];
                    var surf        = ss.Surfaces[i];
                    surf.flags      = (SurfaceFlag)jklSurf.SurfaceFlags;
                    surf.GeoMode    = jklSurf.GeoMode;
                    surf.LightMode  = jklSurf.LightMode;
                    surf.ExtraLight = jklSurf.ExtraLight;
                }

                //var meshRenderer = sectorObj.GetComponent<MeshRenderer>();
                //meshRenderer.sharedMaterials = materials;

                //var meshFilter = sectorObj.GetComponent<MeshFilter>();
                //meshFilter.sharedMesh = mesh;

                //var mc = sectorObj.AddComponent<MeshCollider>();
                //mc.enabled = mesh.vertices.Length > 0;
                //mc.material.dynamicFriction = 0.0f;
                //mc.material.staticFriction = 0.0f;
                //mc.material.bounciness = 0;
                //if (mc.enabled)
                //{
                //    mc.sharedMesh = mesh;
                //    //collider.convex = true;
                //    mc.cookingOptions = MeshColliderCookingOptions.WeldColocatedVertices |
                //                        MeshColliderCookingOptions.EnableMeshCleaning |
                //                        MeshColliderCookingOptions.CookForFasterSimulation;
                //}

                if (jkl.type == WorldType.IJIM && sector.AvgLightIntensity.HasValue  && sector.AvgLightPosition.HasValue)
                {
                    float range = sector.AvgLightFalloff?.x /4 ?? mesh.bounds.extents.magnitude /2;
                    var color = sector.AvgLightIntensity.Value;
                    var avglamp = new GameObject("AvgIntensityLamp");
                    avglamp.transform.SetParent(sectorObj.transform, false);
                    var avglight        = avglamp.AddComponent<Light>();
                    avglight.enabled    = false;
                    avglight.type       = LightType.Point;
                    avglight.renderMode = LightRenderMode.ForceVertex;
                    avglight.range      = Utils.Scale(range);
                    avglight.intensity  = sector.AvgLightFalloff?.y ?? 1.0f;
                    avglight.color      = color;
                    avglight.transform.localPosition = Utils.GetUnityCoords(sector.AvgLightPosition.Value);
                }
            }
        }

        Tuple<Mesh, Material[]> BuildSectorMesh(in Sector sector, in JKL jkl)
        {
            var vertices    = new List<Vector3>();
            var submeshes   = new List<List<int>>();
            var uvs         = new List<Vector2>();
            var intensities = new List<Color>();
            var mats        = new List<Material>();
            var faceNormals = new Dictionary<int, List<Vector3>>(); // Face normals connected to the vertex

            for (int i = 0; i < sector.SurfaceCount; i++)
            {
                var surfaceIdx = sector.SurfaceStartIdx + i;
                var surface = jkl.WorldSurfaces[surfaceIdx];
                //if (surface.GeoMode == GeoMode.NotDrawn) continue;
                //bool visible = surface.GeoMode != GeoMode.NotDrawn;

                var texScale = new Vector2(1, 1);
                var matName = surface.Material?.Name;// visible ? surface.Material?.Name : null;
                //if (matName != null)
                {
                    Material mat = null;
                    //if (!mats.ContainsKey(matName))
                    {
                        var alphaShader = surface.Adjoin != null && (surface.FaceFlags & FaceFlag.Textranslucent) != 0;
                        var lm = (surface.SurfaceFlags & 0x600) != 0 ? LightMode.Unlit : surface.LightMode; // 0x600 = (HorizonSky | CeilingSky)
                        if (matName != null)
                        {
                            mat = SithAssets.Instance.Load(matName, lm, surface.FaceFlags, _cmp, alphaShader);
                            mat.SetCullMode(CullMode.Back);
                            //mat.SetGeoMode(surface.GeoMode);
                            //mat.SetLightMode(lm);
                            //mat.SetFaceExtraLight(surface.ExtraLight);
                            mat.EnableIjimShading(_type == WorldType.IJIM);

                            if ((surface.SurfaceFlags & 0x400) != 0)
                            {
                                mat.shader = Shader.Find("Sith/CeilingSky");
                                mat.SetFloat("_SkyHeight", jkl.CeilingSkyZ);
                                Vector4 skyOffset = jkl.CeilingSkyOffset;
                                mat.SetVector("_CeilingSkyOffset", skyOffset);
                            }
                        }

                        //if (alphaShader) // make sure the translucent adjoin surface is rendered last
                        //{
                        //   mat.renderQueue = 5000;
                        //}

                        mats.Add(mat);
                    }
                    if (jkl.Version == 1 && mat) // JKDF2 & MOTS
                    {
                        //Material mat = mats[matName];
                        texScale.x = 1.0f / mat.mainTexture.width;
                        texScale.y = 1.0f / mat.mainTexture.height;
                    }
                }
                //else
                //{
                //    //continue;
                //    //mats.Add(new Material(Shader.Find("Sith/NotDrawn")));
                //    //mats[i] = new Material(Shader.Find("Sith/NotDrawn"));
                //}

                for (var s = 0; s < surface.SurfaceVertexGroups.Length; s++)
                {
                    SurfaceVertexGroup t = surface.SurfaceVertexGroups[s];
                    vertices.Add(Utils.GetUnityCoords(jkl.WorldVertices[t.VertexIdx]));

                    if (!faceNormals.ContainsKey(t.VertexIdx))
                        faceNormals[t.VertexIdx] = new List<Vector3>();
                    faceNormals[t.VertexIdx].Add(Utils.GetUnityCoords(surface.SurfaceNormal));
                    intensities.Add(surface.VertColors[s]);

                    var uv = t.TextureVertex;
                    if (uv.HasValue && surface.Material != null)
                    {
                        uvs.Add(uv.Value * texScale);
                    }
                    else
                    {
                        uvs.Add(Vector2.zero);
                    }
                }

                var viStart = vertices.Count - surface.SurfaceVertexGroups.Length;
                submeshes.Add(Utils.GetTriangulationIndices(viStart, surface.SurfaceVertexGroups.Length));
                //if (!submeshes.ContainsKey(matName))
                //{
                //    submeshes[matName] = new List<int>();
                //}
                //submeshes[matName].AddRange(Utils.GetTriangulationIndices(viStart, surface.SurfaceVertexGroups.Length));
            }

            // Calculate unweighted vertex normals
            var mapVertNormals = new Dictionary<Vector3, Vector3>();
            foreach (var kv in faceNormals)
            {
                var vn = new Vector3();
                if (kv.Value.Count > 0)
                {
                    vn = new Vector3
                    (
                        kv.Value.Average(n => n.x),
                        kv.Value.Average(n => n.y),
                        kv.Value.Average(n => n.z)
                    );
                }
                mapVertNormals[Utils.GetUnityCoords(jkl.WorldVertices[kv.Key])] = vn;
            };

            var vertexNormals = new List<Vector3>();
            foreach (var v in vertices)
            {
                vertexNormals.Add(mapVertNormals[v]);
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.subMeshCount = submeshes.Count;
            for (var i = 0; i < submeshes.Count; i++)
            {
                mesh.SetTriangles(submeshes[i], i);
            }

            mesh.SetUVs(0, uvs);
            mesh.SetColors(intensities);
            mesh.SetNormals(vertexNormals);
            mesh.RecalculateBounds();

            mesh.RecalculateNormals();

            return new Tuple<Mesh, Material[]>(mesh, mats.ToArray());
        }
        private void BuildThings(JKL jkl)
        {
            foreach (var thing in jkl.Things)
            {
                GameObject thingObj = new GameObject();
                Model3DO model3DO = null;
                ThreedoLoadResult modelLoadResult = null;
                if (thing.Parameters.ContainsKey("model3d"))
                {
                    try
                    {
                        var modelFilename = thing.Parameters["model3d"];
                        modelLoadResult = LoadModel(modelFilename);
                        model3DO = modelLoadResult.Model;
                        modelLoadResult.ModelObject.transform.SetParent(thingObj.transform, false);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to load model {thing.Parameters["model3d"]}: {e.Message}");
                    }
                }
                else if (thing.Parameters["type"] == "sprite")
                {
                    DestroyImmediate(thingObj);
                    thingObj = CreateSpriteThing(thing);
                }
                else
                {
                    // Add inspector visual component
                    thingObj.AddComponent<ThingGizmo>(); 
                }

                var moveSize = Utils.Scale(0.05f);
                var modelRadius = model3DO?.Radius ?? Utils.Scale(0.2f);
                if (jkl.Version == 1)
                {
                    modelRadius /= 2;
                }

                if (thing.Parameters["type"] == "actor" || thing.Parameters["type"] == "player")
                {
                    var rb  = thingObj.AddComponent<Rigidbody>();
                    rb.mass = thing.Parameters.FloatOr("mass", 0.0f);
                    rb.useGravity = Mathf.Abs(rb.mass) > 0.0000001f; // if zero (1.7E-07) disable gravity
                    rb.useGravity = (thing.Parameters.IntOr("physflags", 0) & (0x80 | 0x2000)) == 0 && rb.useGravity; // Disable gravity for rb which should stick to the wall (0x80) or fly (0x2000)
                    rb.drag = thing.Parameters.FloatOr("airdrag", 0.0f);
                    rb.angularDrag = thing.Parameters.FloatOr("staticdrag", 0.0f);
                    rb.maxAngularVelocity = thing.Parameters.FloatOr("maxrotvel", 0.0f) * Mathf.Deg2Rad;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationX;
                    //var cc = thingObj.AddComponent<CharacterController>(); // required for PlayerMovementCharacter
                }

                if (thing.Parameters.ContainsKey("collide"))
                {
                    var ctype = thing.Parameters.IntOr("collide");
                    if (ctype == 1) // sphere collision
                    {
                        //modelLoadResult.ModelObject.transform.localPosition = Utils.Scale(Utils.GetUnityCoords(model3DO.InsertOffset));
                        //thingObj.transform.localPosition = -modelLoadResult.ModelObject.transform.localPosition;

                        var cc = thingObj.AddComponent<CapsuleCollider>();
                        cc.direction = 1;
                        cc.material.dynamicFriction = thing.Parameters.FloatOr("surfdrag", 0) * 0.10f;
                        cc.material.staticFriction = thing.Parameters.FloatOr("staticdrag", 0);
                        cc.material.bounciness = 0;

                        moveSize = Utils.Scale(thing.Parameters.FloatOr("size", 0.05f));
                        cc.radius = moveSize;
                        //cc.center = new Vector3(0, Utils.Scale(modelRadius), 0);
                        cc.height = Utils.Scale(modelRadius * 2);//Utils.Scale(thing.Parameters.FloatOr("collheight", modelRadius * 2));
                    }
                    else if (ctype == 3 && modelLoadResult != null) // face collision
                    {
                        var meshes = modelLoadResult.ModelObject.GetComponentsInChildren<MeshFilter>();
                        foreach (var m in meshes)
                        {
                            if (m.sharedMesh != null)
                            {
                                var mc = m.gameObject.AddComponent<MeshCollider>();
                                mc.sharedMesh = m.sharedMesh;
                                if (thingObj.TryGetComponent(out Rigidbody rb))
                                    rb.isKinematic = true;
                                //mc.convex                   = m.sharedMesh.vertexCount > 3;
                                mc.material.dynamicFriction = thing.Parameters.FloatOr("surfdrag", 0) * 0.10f;
                                mc.material.staticFriction = thing.Parameters.FloatOr("staticdrag", 0);
                            }
                        }
                    }

                    if (model3DO != null && thing.Parameters.ContainsKey("puppet"))
                    {
                        var puppetFilename = thing.Parameters["puppet"];
                        var puppet = SithAssets.Instance.Load<Puppet>(puppetFilename);
                        if (puppet.Modes.ContainsKey(0) && puppet.Modes[0].ContainsKey("stand"))
                        {
                            var keyFilename = puppet.Modes[0]["stand"].KeyFile;
                            var kp = new KEY();
                            var animClip = kp.Load(
                                keyFilename,
                                SithAssets.Instance.GetStream(@"3do\key\" + keyFilename),
                                model3DO,
                                thingObj.transform
                            );

                            if (!thingObj.TryGetComponent<Animation>(out var anim))
                            {
                                anim = thingObj.AddComponent<Animation>();
                                anim.AddClip(animClip, animClip.name);
                            }
                            anim.Play(animClip.name);
                        }
                    }
                }

                if (thing.Parameters["type"] == "player")
                {
                    if (GameObject.FindGameObjectWithTag("Player") == null && thingObj != null)
                    {
                        thingObj.tag   = "Player";
                        thingObj.layer = 8;

                        var pm       = thingObj.AddComponent<PlayerMovement>();
                        pm.Speed     = Utils.Scale(thing.Parameters.FloatOr("maxvel", 1.0f));
                        pm.JumpSpeed = thing.Parameters.FloatOr("jumpspeed", 1.5f) * 3;

                        //var pm = thingObj.AddComponent<PlayerMovementCharacter>();
                        //pm.AirDrag = thing.Parameters.FloatOr("airdrag", 0.0f);

                        //var sc = thingObj.AddComponent<MoveSlideCapsule>();
                        //sc.SteepestSlope = 45.0f;
                        //sc.LegHeight     = moveSize /2 + 0.05f;

                        //var movement = thingObj.AddComponent<MovePlayer>();
                        //if (thing.Parameters.ContainsKey("staticdrag"))
                        //    movement.Friction = float.Parse(thing.Parameters["staticdrag"]) / 10;
                        //movement.Acceleration = 50;
                        //if (thing.Parameters.ContainsKey("maxthrust"))
                        //    movement.Acceleration = float.Parse(thing.Parameters["maxthrust"]) * jkl.WorldGravity;
                        //movement.MaxVelocity  = 7;
                        //if (thing.Parameters.ContainsKey("maxvel"))
                        //    movement.MaxVelocity = float.Parse(thing.Parameters["maxvel"]) * jkl.WorldGravity;
                        //movement.JumpVelocity = 7;
                        //if (thing.Parameters.ContainsKey("jumpspeed"))
                        //    movement.JumpVelocity = float.Parse(thing.Parameters["jumpspeed"]) * jkl.WorldGravity;

                        var ml = thingObj.AddComponent<PlayerMouselook>();
                        ml.FirstPerson = jkl.Version == 1;

                        var camObj = new GameObject("camera");
                        camObj.transform.SetParent(thingObj.transform, false);

                        var cam = camObj.AddComponent<Camera>();
                        cam.fieldOfView = 60;
                        if (thing.Parameters.ContainsKey("fov"))
                            cam.fieldOfView = float.Parse(thing.Parameters["fov"]) * 100;
                        cam.nearClipPlane   = 0.1f;
                        cam.backgroundColor = Color.black;
                        cam.clearFlags      = CameraClearFlags.SolidColor;
                        // cam.allowHDR = false;

                        //var msc = thingGameObject.AddComponent<MoveSlideCapsule>();
                        //msc.LegHeight = 1.0f;
                        //msc.SteepestSlope = 45;

                        //var mp = thingGameObject.AddComponent<MovePlayer>();
                        //mp.Friction = 0.002f;
                        //mp.Acceleration = 50;
                        //mp.MaxVelocity = 1.0f;
                        //mp.JumpVelocity = 1.2f;

                        //var pml = thingGameObject.AddComponent<PlayerMouselook>();
                        //pml.LookSensitivity = 2;
                    }
                }

                // Place thing
                thingObj.name = thing.Name;
                thingObj.transform.position += Utils.Scale(thing.Position);
                thingObj.transform.rotation = thing.Orientation;
                thingObj.transform.SetParent(transform, false);

                var st = thingObj.AddComponent<SithThing>();
                st.Type   = thing.Parameters["type"];
                st.Sector = thing.SectorIdx;

                if (!thing.Parameters.ContainsKey("move") || thing.Parameters["move"] == "none")
                {
                    thingObj.isStatic = true;
                }

                var s = GetSectorObj(thing.SectorIdx);
                if (s == null)
                {
                    Debug.LogError($"BuildThings: Thing '{thingObj.name}' defines invalid sector idx={thing.SectorIdx} ");
                }
                else
                {
                    var sector = s.GetComponent<SithSector>();
                    thingObj.SetSectorLight(sector.AmbientLight, sector.ExtraLight);
                    if (Type != WorldType.IJIM)
                    {
                        thingObj.SetSectorTint(sector.Tint);
                    }
                }
            }
        }

        private ThreedoLoadResult LoadModel(string filename)
        {
            Model3DO model = SithAssets.Instance.Load<Model3DO>(filename);

            var geoset = model.Geosets.First();
            var root   = new GameObject(model.Name)
            {
                hideFlags = HideFlags.NotEditable
            };

            var gameObjects = new List<GameObject>();
            foreach (var hierarchyNode in model.HierarchyNodes)
            {
                var go = new GameObject(hierarchyNode.NodeName);
                hierarchyNode.Transform = go.transform;
                if (hierarchyNode.Mesh != -1)
                {
                    var tdMesh = geoset.Meshes[hierarchyNode.Mesh];
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = Build3DOMesh(model, tdMesh, hierarchyNode, out List<Material> materials);
                    go.AddComponent<MeshRenderer>().sharedMaterials = materials.ToArray();
                }
                gameObjects.Add(go);
            }

            for (int index = 0; index < model.HierarchyNodes.Length; index++)
            {
                var hierarchyNode = model.HierarchyNodes[index];
                var parent = hierarchyNode.Parent == -1 ? root : gameObjects[hierarchyNode.Parent];
                var o = gameObjects[index];
                o.transform.SetPositionAndRotation(Utils.GetUnityCoords(hierarchyNode.Position), hierarchyNode.Orientation);
                o.transform.SetParent(parent.transform, false);
                o.hideFlags = HideFlags.NotEditable;
            }

            root.transform.localScale    = Utils.Scale(root.transform.localScale);
            //root.transform.localPosition = Utils.Scale(Utils.GetUnityCoords(model.InsertOffset));
            var result = new ThreedoLoadResult
            {
                Model = model,
                ModelObject = root
            };

            if (!_modelCache.ContainsKey(filename))
                _modelCache.Add(filename, result);
            return result;
        }

        private Mesh Build3DOMesh(Model3DO model, Mesh3DO tdMesh, HierarchyNode hierarchyNode, out List<Material> materials)
        {
            var vertices    = new List<Vector3>();
            var normals     = new List<Vector3>();
            var uvs         = new List<Vector2>();
            var intensities = new List<Color>();
            var mats        = new Dictionary<string, Material>();
            var submeshes   = new Dictionary<string, List<int>>(); // for every texture a list of polygon vertex indices is created

            for (var faceIndex = 0; faceIndex < tdMesh.Faces.Length; faceIndex++)
            {
                var face    = tdMesh.Faces[faceIndex];
                var matName = model.Materials[face.Material];
                if (!mats.TryGetValue(matName, out Material mat))
                {
                    mat = SithAssets.Instance.Load(matName, face.LightMode, face.FaceFlags, _cmp, /*transparentShader=*/true);
                    mat.enableInstancing = true;
                    mat.SetGeoMode(face.GeoMode);
                    mat.SetLightMode(face.LightMode);
                    mat.SetFaceExtraLight(face.ExtraLight);
                    mat.EnableIjimShading(_type == WorldType.IJIM);
                    mats.Add(matName, mat);
                }

                mat.SetCullMode((face.FaceFlags & FaceFlag.DoubleSided) != 0 ? CullMode.Off : CullMode.Back);// The last shared material sets the cull mode for them all

                foreach (VertexGroup t in face.Vertices)
                {
                    vertices.Add(Utils.GetUnityCoords(tdMesh.Vertices[t.VertexIndex] + hierarchyNode.Pivot));
                    normals.Add(Utils.GetUnityCoords(tdMesh.VertexNormals[t.VertexIndex]));

                    Vector2 uv;
                    if (t.TextureIndex < tdMesh.TextureVertices.Length)
                    {
                        uv = tdMesh.TextureVertices[t.TextureIndex];
                        if (_type != WorldType.IJIM && model.Version == 2.1f)
                        {
                            uv.x /= mat.mainTexture.width;
                            uv.y /= mat.mainTexture.height;
                        }
                    }
                    else
                    {
                        uv = new Vector2();
                    }

                    uvs.Add(new Vector2(uv.x, uv.y));
                    intensities.Add(tdMesh.VertColors[t.VertexIndex]);
                }

                // Triangulate polygon face and add it's vertex indices to submeshes
                if (!submeshes.ContainsKey(matName))
                {
                    submeshes[matName] = new List<int>();
                }
                var viStart = vertices.Count - face.Vertices.Length;
                submeshes[matName].AddRange(Utils.GetTriangulationIndices(viStart, face.Vertices.Length));
            }

            var mesh = new Mesh
            {
                name = tdMesh.Name
            };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = submeshes.Count;
            for (var i = 0; i < submeshes.Count; i++)
            {
                mesh.SetTriangles(submeshes.ElementAt(i).Value, i);
            }

            mesh.SetNormals(normals);
            mesh.SetColors(intensities);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();

            materials = mats.Values.ToList();
            return mesh;
        }

        public class RotateToCamera : MonoBehaviour
        {
            public Camera Camera;

            void Update()
            {
                Camera cam = (Camera != null) ? Camera : Camera.current;
                if (cam != null)
                    transform.rotation = Quaternion.LookRotation(-cam.transform.forward);
            }
        }
        private GameObject CreateSpriteThing(in Content.JKL.Thing thing)
        {
            // TODO: Refactor that this function adds sprite to existing game object and removes any other renderer e.g. 3DO model renderer
            if (!thing.Parameters.ContainsKey("sprite"))
            {
                Debug.LogWarning($"Found sprite Thing '{thing.Name}' with no sprite parameter, skipping...");
                return null;
            }

            var sprFilename = thing.Parameters["sprite"];
            SPR spr = SithAssets.Instance.Load<SPR>(sprFilename);

            var mat     = SithAssets.Instance.Load(spr.MatFile, spr.LightMode, FaceFlag.Textranslucent, _cmp, /*transparentShader=*/true);
            var tex     = mat.mainTexture;
            var sprite  = Sprite.Create((Texture2D)tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sprite.name = sprFilename;

            var spriteObj  = new GameObject(thing.Name);
            var rdspr      = spriteObj.AddComponent<SpriteRenderer>();
            rdspr.name     = sprFilename;
            rdspr.drawMode = SpriteDrawMode.Sliced;
            rdspr.sprite   = sprite;
            rdspr.size     = Utils.Scale(spr.Size);
            rdspr.transform.localPosition = sprite.pivot;

            var rtc = spriteObj.AddComponent<RotateToCamera>();
            rtc.enabled = spr.Type == SPRType.FaceCamera;
            return spriteObj;
        }

        private class ThreedoLoadResult
        {
            public Model3DO Model { get; set; }
            public GameObject ModelObject { get; set; }
        }

        private CMP _cmp;
        private readonly Dictionary<string, ThreedoLoadResult> _modelCache = new Dictionary<string, ThreedoLoadResult>();
    }
}
