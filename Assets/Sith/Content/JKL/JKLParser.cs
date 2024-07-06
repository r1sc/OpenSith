using Assets.Sith.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

#nullable enable 

namespace Assets.Sith.Content.JKL
{
    class JKLParser
    {
        private readonly JKL _jkl;
        private string _line;
        private List<string> _args;
        private StreamReader _streamReader;

        private static readonly Dictionary<int, Material> ndyStaticMats = new()
        {
            [32969] = new Material { Name = "kit_ptch_sde.mat", XTile = 1, YTile = 1 },
            [32970] = new Material { Name = "kit_ptch_top.mat", XTile = 1, YTile = 1 },
            [33092] = new Material { Name = "dflt.mat", XTile = 1, YTile = 1 }, // 33092-> (8144 & ~8000) 324: dflt.mat in jones3dstatic.ndy
            [33105] = new Material { Name = "vol_wall_logs_weathered.mat", XTile = 1, YTile = 1 }
        };

        private bool Match(string match)
        {
            if (_line.StartsWith(match, StringComparison.InvariantCultureIgnoreCase))
            {
                _line = _line.Remove(0, match.Length).Trim();
                _args = new List<string>();
                var buffer = "";
                foreach (var c in _line)
                {
                    if (c == ' ' || c == '\t')
                    {
                        if (buffer.Length != 0)
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
                _line = _streamReader.ReadLine()?.Trim() ?? null;
            } while (_line != null && (_line.StartsWith("#") || _line.StartsWith("//") || _line.Length == 0));
            if (_line != null)
                _args = new List<string>(_line.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries));
        }

        public JKLParser(JKL jkl, Stream jklStream)
        {
            _jkl = jkl;
            _streamReader = new StreamReader(jklStream);
        }

        public void Parse()
        {
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
                        //_streamReader.Dispose();
                        //return;
                        break;
                    case "SECTION: MODELS":
                        ParseModels();
                        break;
                    case "SECTION: TEMPLATES":
                        ParseTemplates();
                        break;
                    case "SECTION: THINGS":
                        ParseThings();
                        break;
                    default:
                        ReadLine();
                        break;
                }
            }
        }

        private void ParseThings()
        {
            ReadLine();
            if (Match("World things"))
            {
                _jkl.Things = new List<Thing>();
                while (true)
                {
                    ReadLine();
                    if (_line == "end")
                    {
                        break;
                    }

                    int pos = 1;
                    var thing = Thing.CreateBasedOn(_jkl.Templates[_args[pos++].ToLower()]);
                    thing.Name = _args[pos++];
                    var position = new Vector3
                    (
                        float.Parse(_args[pos++]),
                        float.Parse(_args[pos++]),
                        float.Parse(_args[pos++])
                    );
                    thing.Position    = Utils.GetUnityCoords(position);
                    var pitch         = float.Parse(_args[pos++]);
                    var yaw           = float.Parse(_args[pos++]);
                    var roll          = float.Parse(_args[pos++]);
                    thing.Orientation = Utils.FromThingOrientation(pitch, yaw, roll);
                    thing.SectorIdx   = int.Parse(_args[pos++]);
                    ParseThingParameters(_args.GetRange(pos, _args.Count - pos), ref thing);

                    _jkl.Things.Add(thing);
                }
            }
        }

        private void ParseTemplates()
        {
            ReadLine();
            if (Match("World templates"))
            {
                _jkl.Templates = new Dictionary<string, Thing>();
                while (true)
                {
                    ReadLine();
                    if (_line == "end")
                    {
                        break;
                    }

                    var name = _args[0];
                    if (_jkl.Templates.ContainsKey(name))
                    {
                        Debug.LogWarning($"JKLParser: template '{name}' already exists in the Templates list, skipping...");
                        continue;
                    }

                    var basedOn = _args[1].ToLower();
                    var numParams = _args.Count - 2;
                    var template = Thing.CreateBasedOn(basedOn == "none" ? null : _jkl.Templates[basedOn]);
                    ParseThingParameters(_args.GetRange(2, _args.Count - 2), ref template);
                    _jkl.Templates.Add(name.ToLower(), template);
                }
            }
        }

        private void ParseThingParameters(in List<string> rawParameters, ref Thing thing)
        {
            foreach (string value in rawParameters)
            {
                var argVal = value.Split('=');
                if (argVal.Length == 2) // MOTS has the first param archlight intens. idx and not thing param
                {
                    thing.Parameters[argVal[0].ToLower()] = argVal[1].ToLower();
                }
            }
        }

        private void ParseModels()
        {
            ReadLine();
            if (Match("World models"))
            {
                _jkl.Models = new List<string>();
                
                while (true)
                {
                    ReadLine();
                    if (_line == "end")
                    {
                        break;
                    }

                    _jkl.Models.Add(_args[_args.Count - 1]);
                }
            }
        }

        private void ParseSectors()
        {
            Sector? currentSector = null;
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
                    currentSector.num = currentSectorIdx;
                }
                else if (Match("FLAGS"))
                {
                    currentSector.Flags = (uint)ParseHex(_args[0]);
                }
                else if (Match("AMBIENT LIGHT"))
                {
                    if (_jkl.Version == 1)
                    {
                        var intensity = Mathf.Clamp01(float.Parse(_args[0]));
                        currentSector.AmbientLight = new Color(intensity, intensity, intensity);
                    }
                    else
                    {
                        currentSector.AmbientLight = new Color
                        (
                            Mathf.Clamp01(float.Parse(_args[0])),
                            Mathf.Clamp01(float.Parse(_args[1])),
                            Mathf.Clamp01(float.Parse(_args[2]))
                        );
                    }
                }
                else if (Match("EXTRA LIGHT"))
                {
                    if (_jkl.Version == 1)
                    {
                        var intensity = Mathf.Clamp01(float.Parse(_args[0]));
                        currentSector.ExtraLight = new Color(intensity, intensity, intensity);
                    }
                    else
                    {
                        currentSector.ExtraLight = new Color
                        (
                            Mathf.Clamp01(float.Parse(_args[0])),
                            Mathf.Clamp01(float.Parse(_args[1])),
                            Mathf.Clamp01(float.Parse(_args[2]))
                        );
                    }
                }
                else if (Match("TINT"))
                {
                    currentSector.Tint = new Color
                    (
                        Mathf.Clamp01(float.Parse(_args[0])),
                        Mathf.Clamp01(float.Parse(_args[1])),
                        Mathf.Clamp01(float.Parse(_args[2]))
                    );
                }
                else if (Match("AVERAGE LIGHT INTENSITY"))
                {
                    currentSector.AvgLightIntensity = new Color
                    (
                        Mathf.Clamp01(float.Parse(_args[0])),
                        Mathf.Clamp01(float.Parse(_args[1])),
                        Mathf.Clamp01(float.Parse(_args[2]))
                    );
                }
                else if (Match("AVERAGE LIGHT POSITION"))
                {
                    currentSector.AvgLightPosition = new Vector3
                    (
                        float.Parse(_args[0]),
                        float.Parse(_args[1]),
                        float.Parse(_args[2])
                    );
                }
                else if (Match("AVERAGE LIGHT FALLOFF"))
                {
                    currentSector.AvgLightFalloff = new Vector2
                    (
                        float.Parse(_args[0]),
                        float.Parse(_args[1])
                    );
                }
                else if (Match("BOUNDBOX"))
                {
                    var min = new Vector3
                    (
                        float.Parse(_args[0]),
                        float.Parse(_args[1]),
                        float.Parse(_args[2])
                    );
                    var max = new Vector3
                    (
                        float.Parse(_args[3]),
                        float.Parse(_args[4]),
                        float.Parse(_args[5])
                    );

                    if (currentSector.Bounds == null)
                    {
                        currentSector.Bounds = new Bounds(new Vector3(), new Vector3());
                    }
                    currentSector.Bounds.min = min;
                    currentSector.Bounds.max = max;
                    currentSector.Bounds.size = max - min;
                }
                else if (Match("COLLIDEBOX"))
                {
                    var min = new Vector3
                    (
                        float.Parse(_args[0]),
                        float.Parse(_args[1]),
                        float.Parse(_args[2])
                    );
                    var max = new Vector3
                    (
                        float.Parse(_args[3]),
                        float.Parse(_args[4]),
                        float.Parse(_args[5])
                    );

                    currentSector.CollideBounds = new Bounds(new Vector3(), max - min);
                    if (currentSector.Bounds != null)
                    {
                        currentSector.CollideBounds.center = currentSector.Bounds.center;
                    }
                }
                else if (Match("SOUND"))
                {
                    currentSector.Sound = new Sector.AmbientSound();
                    currentSector.Sound.File = _args[0];
                    currentSector.Sound.Volume = Mathf.Clamp01(float.Parse(_args[1]));
                }
                else if (Match("THRUST"))
                {
                    currentSector.Thrust = new Vector3
                    (
                        float.Parse(_args[0]),
                        float.Parse(_args[1]),
                        float.Parse(_args[2])
                    );
                }
                else if (Match("CENTER"))
                {
                    var center = new Vector3
                    (
                        float.Parse(_args[0]),
                        float.Parse(_args[1]),
                        float.Parse(_args[2])
                    );

                    if (currentSector.Bounds == null)
                    {
                        currentSector.Bounds = new Bounds(new Vector3(), new Vector3());
                    }
                    currentSector.Bounds.center = center;

                    if (currentSector.CollideBounds != null)
                    {
                        currentSector.CollideBounds.center = center;
                    }
                }
                else if (Match("RADIUS"))
                {
                    currentSector.Radius = float.Parse(_args[0]);
                }
                else if (Match("VERTICES"))
                {
                    var numVertices = int.Parse(_args[0]);
                    currentSector.VertexIndices = new int[numVertices];
                    for (int i = 0; i < numVertices; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

        private Material? GetMaterialByIdx(int idx)
        {
            Material? mat = null;
            if (idx == -1 || idx >= _jkl.Materials.Length)
            {
                if (_jkl.Version != 1 && ndyStaticMats.ContainsKey(idx))
                {
                    // IJIM
                    mat = ndyStaticMats[idx];
                }
            }
            else
            {
                mat = _jkl.Materials[idx];
            }
            return mat;
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
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                    _jkl.WorldSurfaces = new Surface[int.Parse(_args[0])];
                    for (int i = 0; i < _jkl.WorldSurfaces.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        uint argsPos  = 0;
                        var idx       = int.Parse(args[argsPos++].Replace(":", ""));
                        var matIdx    = int.Parse(args[argsPos++]);
                        var surfflags = Convert.ToUInt32(args[argsPos++], 16);
                        var faceflags = (FaceFlag)Convert.ToUInt32(args[argsPos++], 16);
                        var geoMode   = (GeoMode)uint.Parse(args[argsPos++]);
                        var lightMode = (LightMode)uint.Parse(args[argsPos++]);
                        var texMode   = int.Parse(args[argsPos++]); // not used
                        var adjoinIdx = int.Parse(args[argsPos++]);

                        if (geoMode > GeoMode.Textured)
                        {
                            Debug.LogWarning($"JKLParser: Invalid GeoMode '{geoMode}' for surface '{idx}'");
                            geoMode = GeoMode.Textured;
                        }
                        else if( lightMode > LightMode.Gouraud)
                        {
                            Debug.LogWarning($"JKLParser: Invalid LightMode '{geoMode}' for surface '{idx}'");
                            lightMode = LightMode.Gouraud;
                        }

                        var material = GetMaterialByIdx(matIdx);
                        if (material != null && !material.Name.EndsWith(".mat", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Debug.LogError($"JKLParser: Invalid material name '{material.Name}' in the list of materials");
                            material = null;
                        }
                        else if (material == null && matIdx > -1)
                        {
                            Debug.LogWarning($"JKLParser: Material with idx={matIdx} was not found in the list of materials");
                        }

                        if (material == null && geoMode > GeoMode.Solid)
                        {
                            geoMode = GeoMode.Solid;
                        }

                        Adjoin? adjoin = adjoinIdx == -1 ? null : _jkl.WorldAdjoins[adjoinIdx];
                        var surface = new Surface
                        {
                            Material = material,
                            SurfaceFlags = surfflags,// (uint)ParseHex(args[2]),
                            FaceFlags = faceflags, //(uint)ParseHex(args[3]),
                            GeoMode   = geoMode,//int.Parse(args[4]),
                            LightMode = lightMode, //int.Parse(args[5]),
                            //Tex = int.Parse(args[6]),
                            Adjoin = adjoin,
                            //ExtraLight = float.Parse(args[8]),
                            //SurfaceVertexGroups = new SurfaceVertexGroup[int.Parse(_jkl.Version == 1 ? args[9] : args[12])],
                            Sector = null
                        };

                        if(adjoin != null)
                            adjoin.Surface = surface;

                        if (_jkl.Version == 1)
                        {
                            var intensity = Mathf.Clamp01(float.Parse(args[argsPos++]));
                            surface.ExtraLight = new Color(intensity, intensity, intensity);
                        }
                        else
                        {
                            surface.ExtraLight = new Color
                            (
                                Mathf.Clamp01(float.Parse(args[argsPos++])),
                                Mathf.Clamp01(float.Parse(args[argsPos++])),
                                Mathf.Clamp01(float.Parse(args[argsPos++])),
                                Mathf.Clamp01(float.Parse(args[argsPos++]))
                            );
                        }

                        surface.SurfaceVertexGroups = new SurfaceVertexGroup[int.Parse(args[argsPos++])];
                        _jkl.WorldSurfaces[idx] = surface;

                        // Parse polygon vertex & UV indices
                        var numVertexGroups = _jkl.WorldSurfaces[idx].SurfaceVertexGroups.Length;
                        for (int k = 0; k < _jkl.WorldSurfaces[idx].SurfaceVertexGroups.Length; k++)
                        {
                            var surfaceVertexGroup = new SurfaceVertexGroup();
                            var group = args[argsPos++].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            var vIdx = int.Parse(group[0]);
                            if (vIdx == -1)
                                throw new Exception("Null vertex??");

                            int tvIdx;
                            if (group.Length > 1)
                                tvIdx = int.Parse(group[1]);
                            else // there was and additional space(s) separating the two indices
                                tvIdx = int.Parse(args[argsPos++]);

                            surfaceVertexGroup.VertexIdx = vIdx;
                            surfaceVertexGroup.TextureVertex = tvIdx == -1 ? null : (Vector2?)_jkl.WorldTextureVertices[tvIdx];

                            _jkl.WorldSurfaces[idx].SurfaceVertexGroups[k] = surfaceVertexGroup;
                        }

                        // Parse vertex colors
                        _jkl.WorldSurfaces[idx].VertColors = new Color[numVertexGroups];
                        bool rgbColors = (args.Length - argsPos) != numVertexGroups;
                        if (rgbColors && _jkl.Version == 1)
                        {
                            _jkl.type = WorldType.MOTS;
                        }
                        
                        for (int k = 0; k < numVertexGroups; k++)                        {
                            Color vcolor;
                            
                            if (rgbColors)
                            {
                                var alpha = surface.ExtraLight.a; // IJIM
                                if (_jkl.type == WorldType.MOTS)
                                    alpha = Mathf.Clamp01(float.Parse(args[argsPos++]));
                                vcolor = new Color
                                (
                                    Mathf.Clamp01(float.Parse(args[argsPos++])),
                                    Mathf.Clamp01(float.Parse(args[argsPos++])),
                                    Mathf.Clamp01(float.Parse(args[argsPos++])),
                                    alpha
                                );
                            }
                            else // JKDF2
                            {
                                var intensity = Mathf.Clamp01(float.Parse(args[argsPos++]));
                                vcolor = new Color(intensity, intensity, intensity);
                            }
                            _jkl.WorldSurfaces[idx].VertColors[k] = vcolor;
                        }
                    }

                    // Surface normals
                    for (int i = 0; i < _jkl.WorldSurfaces.Length; i++)
                    {
                        ReadLine();
                        string[] args = _line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

                    var args = _line.Split(new[] { ':'}, StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length == 0)
                    {
                        throw new Exception("Invalid entry in the Materials section");
                    }

                    int pos = 0;
                    var idx = i;                    
                    if (args.Length > 1)
                    {
                        idx = int.Parse(args[pos++]);
                    }

                    int matNameEndIdx = args[pos].IndexOf(".mat", StringComparison.InvariantCultureIgnoreCase);
                    if (matNameEndIdx == -1)
                    {
                        throw new Exception("Invalid material file name in the Materials section");
                    }

                    matNameEndIdx += 4;
                    var filename = args[pos].Substring(0, matNameEndIdx).Trim();
                    float xTile = 1.0f;
                    float yTile = 1.0f;
                    if (_jkl.Version == 1)
                    {
                        args = args[pos].Substring(matNameEndIdx).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        xTile = float.Parse(args[0]);
                        yTile = float.Parse(args[1]);
                    }

                    _jkl.Materials[idx] = new Material
                    {
                        Name  = filename,
                        XTile = xTile,
                        YTile = yTile
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
                    _jkl.type = _jkl.Version == 1 ? WorldType.JKDF2 : WorldType.IJIM;
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
                else if (Match("Fog"))
                {
                    _jkl.Fog = new Fog();
                    _jkl.Fog.Enabled = Convert.ToBoolean(int.Parse(_args[0]));
                    _jkl.Fog.Color = new Color(float.Parse(_args[1]), float.Parse(_args[2]), float.Parse(_args[3]), float.Parse(_args[4]));
                    _jkl.Fog.Start = float.Parse(_args[5]);
                    _jkl.Fog.End = float.Parse(_args[6]);
                }
            }
        }
    }

    internal class Thing : ICloneable
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public int SectorIdx { get; set; }
        public Dictionary<string, string> Parameters { get; private set; }
        public Thing Template;

        public Thing()
        {
            Parameters = new Dictionary<string, string>();
        }

        public static Thing CreateBasedOn(Thing template)
        {
            var thing = new Thing();
            if (template != null)
            {
                thing = (Thing)template.Clone();
                thing.Template = template;
            }
            return thing;
        }

        /*DeepCopy clone*/
        public object Clone()
        {
            Thing clone = (Thing)this.MemberwiseClone();
            clone.Parameters = new Dictionary<string, string>(this.Parameters);
            return clone;
        }
    }

    public enum WorldType : uint
    {
        JKDF2 = 0,
        MOTS  = 1,
        IJIM  = 2,
    };

    [Flags]
    public enum FaceFlag : uint
    {
        Normal         = 0x00,
        DoubleSided    = 0x01, // Disables face backface culling.
        Textranslucent = 0x02, // Polygon has translucent texture (adjoin surface & 3DO model polygon)
        TexClamp_x     = 0x04, // Mapped texture is clamped in x instead of being repeated (IJIM & maybe MOTS)
        TexClamp_y     = 0x08, // Mapped texture is clamped in y instead of being repeated (IJIM & maybe MOTS)
        TexFilterNone  = 0x10, // Disables bilinear texture filtering for the polygon texture. (Sets to point filter aka nearest)
        ZWriteOff      = 0x20, // Disables ZWrite for face
        IjimFogEnabled = 0x100 // (IJIM specific) Enables fog rendering for the face polygon.
                               //   Note: This flag is set by default for all surfaces but sky surfaces.
    };

    public enum GeoMode : uint
    {
        NotDrawn    = 0,
        Vertex      = 1,
        Wireframe   = 2,
        Solid       = 3,
        Textured    = 4
    };

    public enum LightMode : uint
    {
        Unlit   = 0,
        Lit     = 1,
        Diffuse = 2,
        Gouraud = 3
    }

    public class Surface
    {
        public Material? Material { get; set; }
        public uint SurfaceFlags { get; set; }
        public FaceFlag FaceFlags { get; set; }
        public GeoMode GeoMode { get; set; }
        public LightMode LightMode { get; set; }
        public Adjoin? Adjoin { get; set; }
        public Color ExtraLight { get; set; }
        public SurfaceVertexGroup[] SurfaceVertexGroups { get; set; }
        public Color[] VertColors { get; set; }
        public Vector3 SurfaceNormal { get; set; }
        public Sector Sector { get; set; }
    }

    public class Sector
    {
        [Serializable]
        public class AmbientSound
        {
            [SerializeField]
            public string File;
            [SerializeField]
            public float Volume;
        }

        public int num;

        public uint Flags { get; set; }
        public int SurfaceStartIdx { get; set; }
        public int SurfaceCount { get; set; }
        public int[] VertexIndices { get; set; }
        public Color AmbientLight;
        public Color ExtraLight;
        public Color? Tint;
        public Color? AvgLightIntensity;
        public Vector3? AvgLightPosition;
        public Vector2? AvgLightFalloff;
        public Bounds Bounds;
        public Bounds CollideBounds; // optional
        public float Radius;
        public AmbientSound? Sound; // optional
        public Vector3? Thrust;
    }

    public class SurfaceVertexGroup
    {
        public int VertexIdx { get; set; }
        public Vector2? TextureVertex { get; set; }
    }

    public class Adjoin
    {
        public uint Flags { get; set; }
        public int Mirror { get; set; }
        public float Distance { get; set; }
        public Surface Surface { get; set; }
    }
}
