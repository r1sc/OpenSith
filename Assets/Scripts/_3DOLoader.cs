using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    class _3DO
    {
        public string[] Materials { get; set; }
        public float Radius { get; set; }
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

        public void Load3DO(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);

            string section = "";
            Geoset currentGeoset = null;
            _3DOMesh currentMesh = null;

            using (sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    ReadLine();
                    
                    if (_line.StartsWith("SECTION: "))
                    {
                        section = _line;
                    }
                    else if (section == "SECTION: MODELRESOURCE")
                    {
                        if (_line.StartsWith("MATERIALS"))
                        {
                            Materials = new string[int.Parse(_line.Split(' ')[1])];
                        }
                        else
                        {
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
                            currentGeoset.Meshes = new _3DOMesh[int.Parse(_args[1])];
                        }
                        else if (_line.StartsWith("MESH"))
                        {
                            var newMesh = int.Parse(_args[1]);
                            currentMesh = currentGeoset.Meshes[newMesh] = new _3DOMesh();
                        }
                        else if (_line.StartsWith("NAME"))
                        {
                            currentMesh.Name = _args[1];
                        }
                        else if (_line.StartsWith("VERTICES"))
                        {
                            currentMesh.Vertices = new Vector4[int.Parse(_args[1])];
                            for (int i = 0; i < currentMesh.Vertices.Length; i++)
                            {
                                ReadLine();
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                var x = float.Parse(_args[1]);
                                var y = float.Parse(_args[2]);
                                var z = float.Parse(_args[3]);
                                var intensity = float.Parse(_args[4]);
                                currentMesh.Vertices[idx] = new Vector4(x, y, z, intensity);
                            }
                        }
                        else if (_line.StartsWith("TEXTURE VERTICES"))
                        {
                            currentMesh.TextureVertices = new Vector2[int.Parse(_args[2])];
                            for (int i = 0; i < currentMesh.TextureVertices.Length; i++)
                            {
                                ReadLine();
                                var idx = int.Parse(_args[0].Replace(":",""));
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
                                var face = currentMesh.Faces[i] = new Face();
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                face.Material = int.Parse(_args[1]);
                                var type = _args[2];
                                var geo = int.Parse(_args[3]);
                                var light = int.Parse(_args[4]);
                                var tex = int.Parse(_args[5]);
                                var extraLight = float.Parse(_args[6]);
                                var numVerts = int.Parse(_args[7]);
                                face.Vertices = new VertexGroup[numVerts];
                                for (int j = 0; j < numVerts*2; j+=2)
                                {
                                    var vIdx = j/2;
                                    var vvIdx = int.Parse(_args[8 + j + 0].Replace(",", ""));
                                    var tvIdx = int.Parse(_args[8 + j + 1].Replace(",", ""));
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
                                var idx = int.Parse(_args[0].Replace(":", ""));
                                var flags = _args[1];
                                var type = _args[2];
                                var mesh = int.Parse(_args[3]);
                                var parent = int.Parse(_args[4]);
                                var child = int.Parse(_args[5]);
                                var sibling = int.Parse(_args[6]);
                                var numChildren = int.Parse(_args[7]);
                                var x = float.Parse(_args[8]);
                                var y = float.Parse(_args[9]);
                                var z = float.Parse(_args[10]);
                                var pitch = float.Parse(_args[11]);
                                var yaw = float.Parse(_args[12]);
                                var roll = float.Parse(_args[13]);
                                var pivotx = float.Parse(_args[14]);
                                var pivoty = float.Parse(_args[15]);
                                var pivotz = float.Parse(_args[16]);
                                var hnodename = _args[17];
                                HierarchyNodes[i] = new HierarchyNode
                                {
                                    Flags = flags,
                                    Type = type,
                                    Mesh = mesh,
                                    Parent = parent,
                                    Child = child,
                                    Sibling = sibling,
                                    NumChildren = numChildren,
                                    Translation = new Vector3(x, y, z),
                                    Rotation = new Vector3(pitch, yaw, roll),
                                    Pivot = new Vector3(pivotx, pivoty, pivotz),
                                    NodeName = hnodename
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
        public string Flags { get; set; }
        public string Type { get; set; }
        public int Mesh { get; set; }
        public int Parent { get; set; }
        public int Child { get; set; }
        public int Sibling { get; set; }
        public int NumChildren { get; set; }
        public Vector3 Translation { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Pivot { get; set; }
        public string NodeName { get; set; }
        public Transform Transform { get; set; }
    }

    class _3DOLoader
    {
        
    }

    internal class VertexGroup
    {
        public int VertexIndex { get; set; }
        public int TextureIndex { get; set; }
    }

    internal class Face {
        public VertexGroup[] Vertices { get; set; }
        public int Material { get; set; }
    }

    internal class _3DOMesh {
        public float Radius { get; set; }
        public Vector4[] Vertices { get; set; }
        public Vector2[] TextureVertices { get; set; }
        public Vector3[] VertexNormals { get; set; }
        public Face[] Faces { get; set; }
        public Vector3[] FaceNormals { get; set; }
        public string Name { get; set; }
    }

    internal class Geoset {
        public _3DOMesh[] Meshes { get; set; }
    }
}
