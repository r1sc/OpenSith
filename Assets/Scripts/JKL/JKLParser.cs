using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace jksharp.jklviewer.JKL
{
    class JKLParser
    {
        private readonly JKL _jkl;
        private string _line;
        private List<string> _args;
        private StreamReader _streamReader;

        private bool Match(string match)
        {
            if (_line.StartsWith(match))
            {
                _line = _line.Remove(0, match.Length).Trim();
                _args = new List<string>();
                var buffer = "";
                foreach (var c in _line)
                {
                    if (c == ' ' || c == '\t')
                    {
                        _args.Add(buffer);
                        buffer = "";
                        continue;
                    }
                    buffer += c;
                }
                _args.Add(buffer);
                return true;
            }
            return false;
        }

        private void ReadLine()
        {
            do
            {
                _line = _streamReader.ReadLine();
            } while (_line.StartsWith("#") || _line.Trim().Length == 0);
        }

        public JKLParser(JKL jkl, Stream jklStream)
        {
            _jkl = jkl;
            _streamReader = new StreamReader(jklStream);

            ReadLine();
            while (!_streamReader.EndOfStream)
            {
                switch (_line.ToUpper())
                {
                    case "SECTION: JK":
                    case "SECTION: COPYRIGHT":
                        do
                        {
                            ReadLine();
                        } while (!_line.StartsWith("SECTION: "));
                        break;
                    case "SECTION: HEADER":
                        ParseHeader();
                        break;
                    case "SECTION: SOUNDS":
                        ParseSounds();
                        break;
                    case "SECTION: MATERIALS":
                        ParseMaterials();
                        break;
                    case "SECTION: GEORESOURCE":
                        ParseGeoResource();
                        break;
                    case "SECTION: SECTORS":
                        ParseSectors();
                        _streamReader.Dispose();
                        return;
                    default:
                        throw new ArgumentException();
                }
            }

        }

        private void ParseSectors()
        {
            Sector currentSector = null;
            var currentSectorIdx = 0;
            while (true)
            {
                ReadLine();
                if (_line.StartsWith("SECTION: ", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentSector != null)
                    {
                        _jkl.Sectors[currentSectorIdx] = currentSector;
                    }
                    break;
                }

                if (Match("World sectors"))
                {
                    var numSectors = int.Parse(_args[0]);
                    _jkl.Sectors = new Sector[numSectors];
                }
                else if (Match("SECTOR"))
                {
                    // Store old sector, make new one
                    if (currentSector != null)
                    {
                        _jkl.Sectors[currentSectorIdx] = currentSector;
                    }
                    currentSectorIdx = int.Parse(_args[0]);
                    currentSector = new Sector();
                }
                else if (Match("FLAGS"))
                {
                    currentSector.Flags = (uint)ParseHex(_args[0]);
                }
                else if (Match("VERTICES"))
                {
                    var numVertices = int.Parse(_args[0]);
                    currentSector.VertexIndices = new int[numVertices];
                    for (int i = 0; i < numVertices; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var sourceIdx = int.Parse(args[0].Replace(":", ""));
                        var vertexIdx = int.Parse(args[1]);
                        currentSector.VertexIndices[sourceIdx] = vertexIdx;
                    }
                }
                else if (Match("SURFACES"))
                {
                    currentSector.SurfaceStartIdx = int.Parse(_args[0]);
                    currentSector.SurfaceCount = int.Parse(_args[1]);

                    for (int i = 0; i < currentSector.SurfaceCount; i++)
                    {
                        var surface = _jkl.WorldSurfaces[currentSector.SurfaceStartIdx + i];
                        surface.Sector = currentSector;
                    }
                }
            }
        }

        private void ParseGeoResource()
        {
            while (true)
            {
                ReadLine();
                if (_line.StartsWith("SECTION: ", StringComparison.OrdinalIgnoreCase))
                    break;
                if (Match("World Colormaps"))
                {
                    _jkl.WorldColorMaps = new string[int.Parse(_args[0])];
                    for (int i = 0; i < _jkl.WorldColorMaps.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var idx = int.Parse(args[0].Replace(":", ""));
                        _jkl.WorldColorMaps[idx] = args[1];
                    }
                }
                else if (Match("World vertices"))
                {
                    _jkl.WorldVertices = new Vector3[int.Parse(_args[0])];
                    for (int i = 0; i < _jkl.WorldVertices.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var idx = int.Parse(args[0].Replace(":", ""));
                        _jkl.WorldVertices[idx] = new Vector3(float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
                    }
                }
                else if (Match("World texture vertices"))
                {
                    _jkl.WorldTextureVertices = new Vector2[int.Parse(_args[0])];
                    for (int i = 0; i < _jkl.WorldTextureVertices.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var idx = int.Parse(args[0].Replace(":", ""));
                        _jkl.WorldTextureVertices[idx] = new Vector2(float.Parse(args[1]), float.Parse(args[2]));
                    }
                }
                else if (Match("World adjoins"))
                {
                    _jkl.WorldAdjoins = new Adjoin[int.Parse(_args[0])];
                    for (int i = 0; i < _jkl.WorldAdjoins.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var idx = int.Parse(args[0].Replace(":", ""));
                        _jkl.WorldAdjoins[idx] = new Adjoin
                        {
                            Flags = (uint)ParseHex(args[1]),
                            Mirror = int.Parse(args[2]),
                            Distance = float.Parse(args[3])
                        };
                    }
                }
                else if (Match("World surfaces"))
                {
                    _jkl.WorldSurfaces = new WorldSurface[int.Parse(_args[0])];
                    for (int i = 0; i < _jkl.WorldSurfaces.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var idx = int.Parse(args[0].Replace(":", ""));
                        var matIdx = int.Parse(args[1]);
                        var adjoinIdx = int.Parse(args[7]);
                        Material material = matIdx == -1 ? null : _jkl.Materials[matIdx];
                        Adjoin adjoin = adjoinIdx == -1 ? null : _jkl.WorldAdjoins[adjoinIdx];
                        var surface = new WorldSurface
                        {
                            Material = material,
                            SurfaceFlags = (uint)ParseHex(args[2]),
                            FaceFlags = (uint)ParseHex(args[3]),
                            Geo = int.Parse(args[4]),
                            Light = int.Parse(args[5]),
                            Tex = int.Parse(args[6]),
                            Adjoin = adjoin,
                            ExtraLight = float.Parse(args[8]),
                            SurfaceVertexGroups = new SurfaceVertexGroup[int.Parse(args[9])],
                            Sector = null
                        };
                        if(adjoin != null)
                            adjoin.Surface = surface;

                        _jkl.WorldSurfaces[idx] = surface;

                        for (int k = 0; k < _jkl.WorldSurfaces[idx].SurfaceVertexGroups.Length; k++)
                        {
                            var surfaceVertexGroup = new SurfaceVertexGroup();
                            var group = args[10 + k].Split(',');
                            var vIdx = int.Parse(group[0]);
                            var tvIdx = int.Parse(group[1]);
                            if (vIdx == -1)
                                throw new Exception("Null vertex??");

                            surfaceVertexGroup.VertexIdx = vIdx;
                            surfaceVertexGroup.TextureVertex = tvIdx == -1 ? null : (Vector2?)_jkl.WorldTextureVertices[tvIdx];

                            _jkl.WorldSurfaces[idx].SurfaceVertexGroups[k] = surfaceVertexGroup;
                        }
                        _jkl.WorldSurfaces[idx].Intensities = new float[_jkl.WorldSurfaces[idx].SurfaceVertexGroups.Length];
                        for (int k = 0; k < _jkl.WorldSurfaces[idx].SurfaceVertexGroups.Length; k++)
                        {
                            var intensityIdx = _jkl.WorldSurfaces[idx].SurfaceVertexGroups.Length + 10 + k;
                            _jkl.WorldSurfaces[idx].Intensities[k] = float.Parse(args[intensityIdx]);

                        }
                    }

                    // Surface normals
                    for (int i = 0; i < _jkl.WorldSurfaces.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var idx = int.Parse(args[0].Replace(":", ""));
                        _jkl.WorldSurfaces[idx].SurfaceNormal = new Vector3(float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
                    }
                }
            }
        }

        private static int ParseHex(string str)
        {
            return int.Parse(str.Replace("0x", ""), NumberStyles.AllowHexSpecifier);
        }

        private void ParseMaterials()
        {
            ReadLine();
            if (Match("World materials"))
            {
                _jkl.Materials = new Material[int.Parse(_args[0])];
                for (int i = 0; i < _jkl.Materials.Length; i++)
                {
                    ReadLine();
                    if (_line == "end")
                    {
                        _jkl.ActualNumberOfMaterials = i;
                        ReadLine();
                        break;
                    }
                    var args = _line.Split('\t');
                    var idx = int.Parse(args[0].Replace(":", ""));
                    _jkl.Materials[idx] = new Material
                    {
                        Name = args[1],
                        XTile = float.Parse(args[2]),
                        YTile = float.Parse(args[3])
                    };
                }
            }
        }

        private void ParseSounds()
        {
            ReadLine();
            if (Match("World sounds"))
            {
                _jkl.Sounds = new string[int.Parse(_args[0])];
                int i = 0;
                while (true)
                {
                    ReadLine();
                    if (_line == "end")
                    {
                        ReadLine();
                        break;
                    }
                    _jkl.Sounds[i++] = _line;
                }
            }
        }

        private void ParseHeader()
        {
            while (true)
            {
                ReadLine();
                if (_line.StartsWith("SECTION: "))
                    break;

                if (Match("Version"))
                {
                    _jkl.Version = int.Parse(_args[0]);
                }
                else if (Match("World Gravity"))
                {
                    _jkl.WorldGravity = float.Parse(_args[0]);
                }
                else if (Match("Ceiling Sky Z"))
                {
                    _jkl.CeilingSkyZ = float.Parse(_args[0]);
                }
                else if (Match("Horizon Distance"))
                {
                    _jkl.HorizonDistance = float.Parse(_args[0]);
                }
                else if (Match("Horizon Pixels per Rev"))
                {
                    _jkl.HorizonPixelsPerRev = float.Parse(_args[0]);
                }
                else if (Match("Horizon Sky Offset"))
                {
                    _jkl.HorizonSkyOffset = new Vector2(float.Parse(_args[0]), float.Parse(_args[1]));
                }
                else if (Match("Ceiling Sky Offset"))
                {
                    _jkl.CeilingSkyOffset = new Vector2(float.Parse(_args[0]), float.Parse(_args[1]));
                }
                else if (Match("MipMap Distances"))
                {
                    _jkl.MipMapDistances = new Vector4(float.Parse(_args[0]), float.Parse(_args[1]), float.Parse(_args[2]), float.Parse(_args[3]));
                }
                else if (Match("LOD Distances"))
                {
                    _jkl.LODDistances = new Vector4(float.Parse(_args[0]), float.Parse(_args[1]), float.Parse(_args[2]), float.Parse(_args[3]));
                }
                else if (Match("Perspective distance"))
                {
                    _jkl.PerspectiveDistance = float.Parse(_args[0]);
                }
                else if (Match("Gouraud distance"))
                {
                    _jkl.GouraudDistance = float.Parse(_args[0]);
                }
            }
        }
    }

    internal class WorldSurface
    {
        public Material Material { get; set; }
        public uint SurfaceFlags { get; set; }
        public uint FaceFlags { get; set; }
        public int Geo { get; set; }
        public int Light { get; set; }
        public int Tex { get; set; }
        public Adjoin Adjoin { get; set; }
        public float ExtraLight { get; set; }
        public SurfaceVertexGroup[] SurfaceVertexGroups { get; set; }
        public float[] Intensities { get; set; }
        public Vector3 SurfaceNormal { get; set; }
        public Sector Sector { get; set; }
    }

    internal class Sector
    {
        public uint Flags { get; set; }
        public int SurfaceStartIdx { get; set; }
        public int SurfaceCount { get; set; }
        public int[] VertexIndices { get; set; }
    }

    internal class SurfaceVertexGroup
    {
        public int VertexIdx { get; set; }
        public Vector2? TextureVertex { get; set; }
    }

    internal class Adjoin
    {
        public uint Flags { get; set; }
        public int Mirror { get; set; }
        public float Distance { get; set; }
        public WorldSurface Surface { get; set; }
    }
}
