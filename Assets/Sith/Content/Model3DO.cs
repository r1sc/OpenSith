using Assets.Sith.Utility;
using System;
using System.IO;
using UnityEngine;

namespace Assets.Sith.Content
{
    class Model3DO : ISithAsset
    {
        public float Version;
        public string[] Materials { get; set; }
        public float Radius { get; set; }
        public Vector3 InsertOffset { get; set; }
        public Geoset[] Geosets { get; set; }
        public string Name { get; set; }
        public HierarchyNode[] HierarchyNodes { get; set; }

        private StreamReader sr;
        private string _line;
        private string[] _args;

        private void ReadLine()
        {
            while (!sr.EndOfStream)
            {
                _line = sr.ReadLine();
                if (_line.StartsWith("#") || _line.Trim() == "")
                    continue;
                _args = _line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                break;
            }
        }

        public void Load(string fileName, Stream dataStream)
        {
            Name = Path.GetFileName(fileName);

            string section = "";
            Geoset currentGeoset = null;
            Mesh3DO currentMesh = null;
            bool rgbColors = false;

            using (sr = new StreamReader(dataStream))
            {
                while (!sr.EndOfStream)
                {
                    ReadLine();

                    if (_line.StartsWith("SECTION: "))
                    {
                        section = _line;
                    }
                    else if (section == "SECTION: HEADER")
                    {
                        if (_line.StartsWith("3DO"))
                        {
                            Version = float.Parse(_line.Split(' ')[1]);
                            rgbColors = Version > 2.1;
                            if (Version < 2.1f || Version > 2.3f)
                            {
                                throw new Exception("Unsupported 3DO file version: " + Version.ToString());
                            }
                        }
                    }
                    else if (section == "SECTION: MODELRESOURCE")
                    {
                        if (_line.StartsWith("MATERIALS"))
                        {
                            Materials = new string[int.Parse(_line.Split(' ')[1])];
                        }
                        else
                        {
                            if (_args.Length == 1)
                            {
                                _args = _args[0].Split(':');
                            }
                            var idx = int.Parse(_args[0].Replace(":", ""));
                            var name = _args[1];
                            Materials[idx] = name;
                        }
                    }
                    else if (section == "SECTION: GEOMETRYDEF")
                    {
                        if (_line.StartsWith("RADIUS"))
                        {
                            var value = float.Parse(_args[1]);
                            if (currentMesh != null)
                            {
                                currentMesh.Radius = value;
                            }
                            else
                            {
                                Radius = value;
                            }
                        }
                        else if (_line.StartsWith("INSERT OFFSET"))
                        {
                            var x = float.Parse(_args[2]);
                            var y = float.Parse(_args[3]);
                            var z = float.Parse(_args[4]);
                            InsertOffset = new Vector3(x, y, z);
                        }
                        else if (_line.StartsWith("GEOSETS"))
                        {
                            Geosets = new Geoset[int.Parse(_args[1])];
                        }
                        else if (_line.StartsWith("GEOSET"))
                        {
                            var newGeoset = int.Parse(_args[1]);
                            currentGeoset = Geosets[newGeoset] = new Geoset();
                        }
                        else if (_line.StartsWith("MESHES"))
                        {
                            currentGeoset.Meshes = new Mesh3DO[int.Parse(_args[1])];
                        }
                        else if (_line.StartsWith("MESH"))
                        {
                            var newMesh = int.Parse(_args[1]);
                            currentMesh = currentGeoset.Meshes[newMesh] = new Mesh3DO();
                        }
                        else if (_line.StartsWith("NAME"))
                        {
                            currentMesh.Name = _args[1];
                        }
                        else if (_line.StartsWith("VERTICES"))
                        {
                            currentMesh.Vertices = new Vector3[int.Parse(_args[1])];
                            currentMesh.VertColors = new Color[currentMesh.Vertices.Length];
                            for (int i = 0; i < currentMesh.Vertices.Length; i++)
                            {
                                ReadLine();
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                var x = float.Parse(_args[1]);
                                var y = float.Parse(_args[2]);
                                var z = float.Parse(_args[3]);
                                currentMesh.Vertices[idx] = new Vector3(x, y, z);
                                if (rgbColors)
                                {
                                    currentMesh.VertColors[idx] = new Color
                                    (
                                        Mathf.Clamp01(float.Parse(_args[4])),
                                        Mathf.Clamp01(float.Parse(_args[5])),
                                        Mathf.Clamp01(float.Parse(_args[5])),
                                        Mathf.Clamp01(float.Parse(_args[6]))
                                    );
                                }
                                else
                                {
                                    var intensity = Mathf.Clamp01(float.Parse(_args[4]));
                                    currentMesh.VertColors[idx] = new Color(intensity, intensity, intensity, 1);
                                }
                                
                            }
                        }
                        else if (_line.StartsWith("TEXTURE VERTICES"))
                        {
                            currentMesh.TextureVertices = new Vector2[int.Parse(_args[2])];
                            for (int i = 0; i < currentMesh.TextureVertices.Length; i++)
                            {
                                ReadLine();
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                var x = float.Parse(_args[1]);
                                var y = float.Parse(_args[2]);
                                currentMesh.TextureVertices[idx] = new Vector2(x, y);
                            }
                        }
                        else if (_line.StartsWith("VERTEX NORMALS"))
                        {
                            currentMesh.VertexNormals = new Vector3[currentMesh.Vertices.Length];
                            for (int i = 0; i < currentMesh.VertexNormals.Length; i++)
                            {
                                ReadLine();
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                var x = float.Parse(_args[1]);
                                var y = float.Parse(_args[2]);
                                var z = float.Parse(_args[3]);
                                currentMesh.VertexNormals[idx] = new Vector3(x, y, z);
                            }
                        }
                        else if (_line.StartsWith("FACES"))
                        {
                            currentMesh.Faces = new Face[int.Parse(_args[1])];
                            for (int i = 0; i < currentMesh.Faces.Length; i++)
                            {
                                ReadLine();
                                int argsPos    = 0;
                                var idx        = int.Parse(_args[argsPos++].Replace(":", ""));

                                var face       = currentMesh.Faces[i] = new Face();
                                face.Material  = int.Parse(_args[argsPos++]);
                                face.FaceFlags = (JKL.FaceFlag)Convert.ToUInt32(_args[argsPos++], 16); // type aka face flags
                                face.GeoMode   = (JKL.GeoMode)uint.Parse(_args[argsPos++]);
                                face.LightMode = (JKL.LightMode)uint.Parse(_args[argsPos++]);
                                var tex        = int.Parse(_args[argsPos++]);

                                if (rgbColors)
                                {
                                    var extraLight = _args[argsPos++];
                                    extraLight = extraLight.Trim(new[] { '(', ')' });
                                    var rgb = extraLight.Split(new[]{ '/'});
                                    face.ExtraLight = new Color
                                    (
                                        Mathf.Clamp01(float.Parse(rgb[0])),
                                        Mathf.Clamp01(float.Parse(rgb[1])),
                                        Mathf.Clamp01(float.Parse(rgb[2])),
                                        Mathf.Clamp01(float.Parse(rgb[3]))
                                    );
                                }
                                else
                                {
                                    var intensity = Mathf.Clamp01(float.Parse(_args[argsPos++]));
                                    face.ExtraLight = new Color(intensity, intensity, intensity, 1);
                                }

                                var numVerts = int.Parse(_args[argsPos++]);
                                face.Vertices = new VertexGroup[numVerts];
                                for (int j = 0; j < numVerts * 2; j += 2)
                                {
                                    var vIdx = j / 2;
                                    var vvIdx = int.Parse(_args[argsPos + j + 0].Replace(",", ""));
                                    var tvIdx = int.Parse(_args[argsPos + j + 1].Replace(",", ""));
                                    face.Vertices[vIdx] = new VertexGroup
                                    {
                                        VertexIndex = vvIdx,
                                        TextureIndex = tvIdx
                                    };
                                }
                            }
                        }
                        else if (_line.StartsWith("FACE NORMALS"))
                        {
                            currentMesh.FaceNormals = new Vector3[currentMesh.Faces.Length];
                            for (int i = 0; i < currentMesh.FaceNormals.Length; i++)
                            {
                                ReadLine();
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                var x = float.Parse(_args[1]);
                                var y = float.Parse(_args[2]);
                                var z = float.Parse(_args[3]);
                                currentMesh.FaceNormals[idx] = new Vector3(x, y, z);
                            }
                        }
                    }
                    else if (section == "SECTION: HIERARCHYDEF")
                    {
                        if (_line.StartsWith("HIERARCHY NODES"))
                        {
                            HierarchyNodes = new HierarchyNode[int.Parse(_args[2])];
                            for (int i = 0; i < HierarchyNodes.Length; i++)
                            {
                                ReadLine();
                                var idx         = int.Parse(_args[0].Replace(":", ""));
                                var flags       = Convert.ToUInt32(_args[1], 16);
                                var type        = Convert.ToUInt32(_args[2], 16);
                                var mesh        = int.Parse(_args[3]);
                                var parent      = int.Parse(_args[4]);
                                var child       = int.Parse(_args[5]);
                                var sibling     = int.Parse(_args[6]);
                                var numChildren = int.Parse(_args[7]);
                                var x           = float.Parse(_args[8]);
                                var y           = float.Parse(_args[9]);
                                var z           = float.Parse(_args[10]);
                                var pitch       = float.Parse(_args[11]);
                                var yaw         = float.Parse(_args[12]);
                                var roll        = float.Parse(_args[13]);
                                var pivotx      = float.Parse(_args[14]);
                                var pivoty      = float.Parse(_args[15]);
                                var pivotz      = float.Parse(_args[16]);
                                var hnodename   = _args[17];

                                HierarchyNodes[i] = new HierarchyNode
                                {
                                    Flags       = flags,
                                    Type        = type,
                                    Mesh        = mesh,
                                    Parent      = parent,
                                    Child       = child,
                                    Sibling     = sibling,
                                    NumChildren = numChildren,
                                    Position    = new Vector3(x, y, z),
                                    Orientation = Utils.FromMeshOrientation(pitch, yaw, roll),
                                    Pivot       = new Vector3(pivotx, pivoty, pivotz),
                                    NodeName    = hnodename
                                };
                            }
                        }
                    }
                }
            }
        }
    }

    internal class HierarchyNode
    {
        public uint Flags { get; set; }
        public uint Type { get; set; }
        public int Mesh { get; set; }
        public int Parent { get; set; }
        public int Child { get; set; }
        public int Sibling { get; set; }
        public int NumChildren { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public Vector3 Pivot { get; set; }
        public string NodeName { get; set; }
        public Transform Transform { get; set; }
    }

    internal class VertexGroup
    {
        public int VertexIndex { get; set; }
        public int TextureIndex { get; set; }
    }

    internal class Face
    {
        public VertexGroup[] Vertices { get; set; }
        public int Material { get; set; }
        public JKL.FaceFlag FaceFlags;
        public JKL.GeoMode GeoMode;
        public JKL.LightMode LightMode;
        public Color ExtraLight { get; set; }
    }

    internal class Mesh3DO
    {
        public float Radius { get; set; }
        public Vector3[] Vertices { get; set; }
        public Color[] VertColors { get; set; }
        public Vector2[] TextureVertices { get; set; }
        public Vector3[] VertexNormals { get; set; }
        public Face[] Faces { get; set; }
        public Vector3[] FaceNormals { get; set; }
        public string Name { get; set; }
    }

    internal class Geoset
    {
        public Mesh3DO[] Meshes { get; set; }
    }
}
