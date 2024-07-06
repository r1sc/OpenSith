using Assets.Sith.Content.JKL;
using Assets.Sith.Utility;
using Assets.Sith.Vfs;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Material = UnityEngine.Material;

namespace Assets.Sith.Content
{
    public class AssetStore : VirtualFileSystem
    {
        public LightMode DefaultLightMode = LightMode.Gouraud;

        public AssetStore() 
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;//CultureInfo.GetCultureInfo("en-us");
        }

        public T Load<T>(string filename) where T: ISithAsset
        {
            filename = filename.ToLower();
            if (typeof(T) == typeof(SPR))
                return (T)(object)LoadSprite(filename);
            else if (typeof(T) == typeof(Model3DO))
                return (T)(object)LoadModel3DO(filename);
            else if (typeof(T) == typeof(Puppet))
                return (T)(object)LoadPuppet(filename);
            else if (typeof(T) == typeof(CMP))
                return (T)(object)LoadCMP(filename);
            throw new Exception("Unsupported Asset");
        }

        public Material Load(string filename, LightMode lightMode, FaceFlag flags, CMP cmp)
        {
            return Load(filename, lightMode, flags, cmp, false);
        }

        public Material Load(string filename, LightMode lightMode, FaceFlag flags, CMP cmp, bool transparentShader)
        {
            filename = filename.ToLower();
            //if (_materialCache.ContainsKey(filename)
            // && _materialCache[filename].ContainsKey(lightMode))
            //{
            //    return _materialCache[filename][lightMode];
            //}

            var mat = new MAT();
            if (_matCache.ContainsKey(filename))
            {
                mat = _matCache[filename];
            }
            else
            {
                var matPath = @"mat\" + filename;
                if (!Exists(matPath))
                {
                    matPath = @"3do\mat\" + filename;
                }

                mat.Load(filename, GetStream(matPath), cmp);
                _matCache.Add(filename, mat);
            }

            bool hasAlpha = /*(mat.colorMode == ColorMode.RGBA) && */(flags & FaceFlag.Textranslucent) != 0;
            var m = new Material(GetShader(lightMode, transparentShader))
            {
                name = filename
            };

            if (mat.Textures.Length > 0) {
                m.mainTexture = mat.Textures[0];
            }

            if (mat.ColorMode == ColorMode.RGBA || (flags & FaceFlag.Textranslucent) != 0)
            {
                m.renderQueue += 1000;
            }

            //if ((flags & FaceFlag.Textranslucent) != 0)
            //{
            //    m.renderQueue += 200;
            //}
            //m.EnableZWrite((mat.ColorMode != ColorMode.RGBA) || (flags & (FaceFlag.Textranslucent | FaceFlag.ZWriteOff)) != 0);
            m.EnableZWrite((flags & (FaceFlag.Textranslucent | FaceFlag.ZWriteOff)) == 0);
            //m.SetInt("_ZWrite", (mat.ColorMode != ColorMode.RGBA) || !hasAlpha ? 1 : 0);

            //if (!_materialCache.ContainsKey(filename))
            //{
            //    _materialCache.Add(filename, new Dictionary<LightMode, UnityEngine.Material>());
            //}

            //_materialCache[filename].Add(lightMode, m);
            return m;
        }

        private Shader GetShader(LightMode lm, bool hasAlpha)
        {
            switch (lm)
            {
                case LightMode.Unlit:
                    //return hasAlpha ? Shader.Find("Unlit/Transparent") : Shader.Find("Unlit/Texture");
                case LightMode.Lit:
                case LightMode.Diffuse:
                case LightMode.Gouraud:
                    return hasAlpha ? Shader.Find("Sith/Transparent") : Shader.Find("Sith/Texture");
            }
            Debug.LogWarning($"GetShader: Unknown light mode \"{lm}\", using global light mode!");
            return GetShader(DefaultLightMode, hasAlpha);
        }

        private CMP LoadCMP(string filename)
        {
            if (_cmpCache.ContainsKey(filename))
                return _cmpCache[filename];

            CMP cmp = new CMP();
            cmp.Load(filename, GetStream(@"misc\cmp\" + filename));
            _cmpCache[filename] = cmp;
            return cmp;
        }
        private SPR LoadSprite(string filename)
        {
            filename = filename.ToLower();
            var spr = new Assets.Sith.Content.SPR();
            if (_sprCache.ContainsKey(filename))
            {
                spr = _sprCache[filename];
            }
            else
            {
                spr.Load(filename, GetStream($"misc\\spr\\{filename}"));
                _sprCache.Add(filename, spr);
            }
            return spr;
        }

        private Model3DO LoadModel3DO(string filename)
        {
            if (_modelCache.ContainsKey(filename))
               return _modelCache[filename];

            var model = new Model3DO();
            model.Load(filename, GetStream(@"3do\" + filename));
            _modelCache[filename] = model;
            return model;
        }

        private Puppet LoadPuppet(string filename)
        {
            if (_puppetCache.ContainsKey(filename))
                return _puppetCache[filename];

            var puppet = PUPPETParser.Parse(filename, GetStream(@"misc\pup\" + filename));
            _puppetCache.Add(filename, puppet);
            return puppet;
        }

        private static readonly Dictionary<string, Dictionary<LightMode, Material>> _materialCache = new Dictionary<string, Dictionary<LightMode, Material>>();
        private static readonly Dictionary<string, CMP> _cmpCache = new Dictionary<string, CMP>();
        private static readonly Dictionary<string, MAT> _matCache = new Dictionary<string, MAT>();
        private static readonly Dictionary<string, SPR> _sprCache = new Dictionary<string, SPR>();
        private static readonly Dictionary<string, Model3DO> _modelCache = new Dictionary<string, Model3DO>();
        private static readonly Dictionary<string, Puppet> _puppetCache = new Dictionary<string, Puppet>();
    }
}
