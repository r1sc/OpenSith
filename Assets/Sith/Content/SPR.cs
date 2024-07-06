using Assets.Sith.Utility;
using System;
using System.IO;
using UnityEngine;

namespace Assets.Sith.Content
{    
    public enum SPRType
    {
        FaceCamera     = 0,
        RandomPosition = 1, // shift to random position if sprite is animating
        Normal         = 2
    }
    class SPR : ISithAsset
    {
        public string Name { get; set; }
        public string MatFile { get; private set; }
        public SPRType Type { get; private set; }
        public Vector2 Size { get; private set; }
        public JKL.GeoMode GeoMode { get; private set; }
        public JKL.LightMode LightMode { get; private set; }
        public Color ExtraLight { get; private set; }
        public Vector3 Offset { get; private set; }

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
                _args = _line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                break;
            }
        }
        public void Load(string fileName, Stream dataStream)
        {
            Name = Path.GetFileNameWithoutExtension(fileName);
            using (sr = new StreamReader(dataStream))
            {
                ReadLine();
                if (_args.Length != 11 && _args.Length != 14)
                {
                    throw new Exception($"Unknown or corrupted SPR file: {fileName}");
                }

                int pos     = 0;
                MatFile     = _args[pos++];
                Type        = (SPRType)int.Parse(_args[pos++]);
                Size        = new Vector2(float.Parse(_args[pos++]), float.Parse(_args[pos++]));
                GeoMode     = (JKL.GeoMode)uint.Parse(_args[pos++]);
                LightMode   = (JKL.LightMode)uint.Parse(_args[pos++]);
                var texMode = int.Parse(_args[pos++]);

                if (_args.Length == 11)
                {
                    var intensity = Mathf.Clamp01(float.Parse(_args[pos++]));
                    ExtraLight = new Color(intensity, intensity, intensity);
                }
                else
                {
                    ExtraLight = new Color
                    (
                        Mathf.Clamp01(float.Parse(_args[pos++])),
                        Mathf.Clamp01(float.Parse(_args[pos++])),
                        Mathf.Clamp01(float.Parse(_args[pos++])),
                        Mathf.Clamp01(float.Parse(_args[pos++]))
                    );
                }

                Offset = new Vector3
                (
                    float.Parse(_args[pos++]),
                    float.Parse(_args[pos++]),
                    float.Parse(_args[pos++])
                );

                if (Type > SPRType.Normal || Size.x <= 0 || Size.y <= 0)
                {
                    throw new Exception($"Bad sprite parameter in SPR file: {fileName}");
                }
            }
        }
    }
}
