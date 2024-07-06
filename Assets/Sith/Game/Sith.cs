using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Assets.Sith.Content;
using Assets.Sith.Content.JKL;
using Assets.Sith.Game.World;
using Assets.Sith.Utility;
using Assets.Sith.Vfs;
using UnityEngine;
using Material = UnityEngine.Material;


namespace Assets.Sith.Game
{
    public sealed class SithAssets : Singleton<AssetStore> { }

    public class Sith : MonoBehaviour
    {
        //public Texture2D Atlas;
        //public Material StandardMaterial;

        [Tooltip("JKL or NDY level to load.")]
        public string Level;
        public string GamePath;

        void Start()
        {
            //RenderSettings.skybox = null;
            //RenderSettings.sun = null;
            //RenderSettings.ambientLight = Color.white;

            SithAssets.Instance.AddSystemPath(GamePath);
            SithAssets.Instance.AddSystemPath(Path.Combine(GamePath, "Resource"));
            SithAssets.Instance.AddSystemPath(Path.Combine(GamePath, "Extracted"));

            // JKDF2
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Resource\\Res1hi.gob"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Resource\\Res2.gob"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\JK1.GOB"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\JK1CTF.GOB"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\JK1MP.GOB"));

            // MOTS
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Resource\\JKMRES.GOO"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Resource\\JKMsndLO.goo"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\JKM.GOO"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\JKM_KFY.GOO"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\JKM_MP.GOO"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Episode\\Jkm_saber.GOO"));

            // IJIM
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Resource\\cd1.gob"));
            SithAssets.Instance.AddGob(Path.Combine(GamePath, "Resource\\cd2.gob"));

            if (!string.IsNullOrEmpty(Level))
            {
                var go = new GameObject("SithWorld");
                var world = go.AddComponent<SithWorld>();

                bool loadNdy = Path.GetExtension(Level).ToLower() == ".ndy";
                world.Load((loadNdy ? @"ndy\" : @"jkl\") + Level);
            }
        }
    }
}
