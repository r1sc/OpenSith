using Assets.Sith.Content;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Sith.Utility
{
    public static class MaterialExtensions
    {
        public static void SetCullMode(this Material mat, UnityEngine.Rendering.CullMode mode)
        {
            mat.SetInt("_Culling", (int)mode);
        }

        // [Applies to Sith shader] Sets geometry mode
        public static void SetGeoMode(this Material mat, Content.JKL.GeoMode geoMode)
        {
            mat.SetInt("_GeoMode", (int)geoMode);
        }

        // [Applies to Sith shader] Sets lighting mode
        public static void SetLightMode(this Material mat, Content.JKL.LightMode lightMode)
        {
            mat.SetInt("_LightMode", (int)lightMode);
        }

        public static Content.JKL.LightMode GetLightMode(this Material mat)
        {
            return (Content.JKL.LightMode) mat.GetInt("_LightMode");
        }

        // [Applies to Sith shader] Sector ambient light
        public static void SetAmbientLight(this Material mat, Color ambientLight)
        {
            mat.SetColor("_AmbientLight", ambientLight);
        }

        // [Applies to Sith shader] Sector extra light
        public static void SetExtraLight(this Material mat, Color extraLight)
        {
            mat.SetColor("_ExtraLight", extraLight);
        }

        // [Applies to Sith shader] Mesh face extra light
        public static void SetFaceExtraLight(this Material mat, Color extraLight)
        {
            mat.SetColor("_FaceExtraLight", extraLight);
        }

        // [Applies to Sith shader] Sector tint color
        public static void SetTint(this Material mat, Color tint)
        {
            mat.SetColor("_Tint", tint);
        }

        public static void EnableIjimShading(this Material mat, bool enable)
        {
            mat.SetInt("_Ijim", enable ? 1 : 0);
        }

        public static void EnableZWrite(this Material mat, bool enable)
        {
            mat.SetInt("_ZWrite", enable ? 1 : 0);
        }
    }

    public static class GameObjectExtensions
    {
        public static void SetSectorLight(this GameObject obj, Color ambientLight, Color extraLight)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var matBlock = new MaterialPropertyBlock();
                matBlock.SetColor("_AmbientLight", ambientLight);
                matBlock.SetColor("_ExtraLight", extraLight);
                r.SetPropertyBlock(matBlock);
                //var mats = new Material[rend.materials.Length];
                //for (var j = 0; j < r.materials.Length; j++)
                //{
                //    //var tempMaterial = new Material(r.sharedMaterials[j]);
                //    //tempMaterial.SetAmbientLight(ambientLight);
                //    //tempMaterial.SetExtraLight(extraLight);
                //    //r.sharedMaterials[j] = tempMaterial;
                //    //mats[j] = newMat;
                //    r.materials[j].SetAmbientLight(ambientLight);
                //    r.materials[j].SetExtraLight(extraLight);
                //}
                //rend.materials = mats;
            }
        }

        public static void SetSectorTint(this GameObject obj, Color tint)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                //var mats = new Material[rend.materials.Length];
                for (var j = 0; j < r.materials.Length; j++)
                {
                    r.materials[j].SetTint(tint);
                }
            }
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue ValueOr<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static int IntOr<TKey>(this Dictionary<TKey, string> dictionary, TKey key, int defaultValue = 0)
        {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            

            return dictionary.TryGetValue(key, out string value) 
                ? int.Parse(value.Replace("0x", ""), NumberStyles.AllowHexSpecifier)
                : defaultValue;
        }

        public static float FloatOr<TKey>(this Dictionary<TKey, string> dictionary, TKey key, float defaultValue = 0.0f)
        {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            return dictionary.TryGetValue(key, out string value) ? float.Parse(value) : defaultValue;
        }
    }
}
