using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections;
using Assets.Sith;

using Assets.Sith.Content;
using Assets.Sith.Game;
using Assets.Sith.Game.World;
using Assets.Sith.Vfs;


public class SithEditor : EditorWindow
{
    [MenuItem("Window/Sith")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SithEditor));
    }

    private string _gamePath = @"";
    private string _assetPath = "Extracted\\";
    private IEnumerable<GobFile> _gobFiles;
    private bool _gobFilesEnabled;
    private IEnumerable<string> _levelFiles;
    private Vector2 _scrollPos;

    void Start()
    {
    }

    void OnGUI()
    {
        _gamePath = EditorGUILayout.TextField("Game path", _gamePath);
        if (!Directory.Exists(_gamePath)) return;
        if (GUILayout.Button("Find GOBs"))
        {
            _gobFiles = Directory.EnumerateFiles(_gamePath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".gob") || s.ToLower().EndsWith(".goo"))
                .Select(x => new GobFile { Path = x.Replace(_gamePath + "\\", ""), Enabled = false }).ToArray();
        }

        if (_gobFiles != null)
        {
            _gobFilesEnabled = EditorGUILayout.BeginToggleGroup("GOB Files", _gobFilesEnabled);
            foreach (var gobFile in _gobFiles)
            {
                gobFile.Enabled = EditorGUILayout.Toggle(gobFile.Path, gobFile.Enabled);
            }
            EditorGUILayout.EndToggleGroup();
        }

        _assetPath = EditorGUILayout.TextField("Asset path", _assetPath);
        if (GUILayout.Button("Extract files"))
        {
            foreach (var gobFile in _gobFiles.Where(x => x.Enabled))
            {
                using (var stream = new GOBStream(Path.Combine(_gamePath, gobFile.Path)))
                {
                    stream.Extract(Path.Combine(_assetPath, gobFile.Path));
                }
            }
        }

        if (GUILayout.Button("Find levels"))
        {
            _levelFiles = Directory.EnumerateFiles(_gamePath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".jkl") || s.ToLower().EndsWith(".ndy"))
                .Select(x => x.Replace(_gamePath + "\\", ""))
                .ToArray();
        }

        if (_levelFiles != null)
        {
            GUILayout.Label("Level Files");
            EditorGUILayout.BeginHorizontal();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var file in _levelFiles)
            {
                //gobFile.Enabled = EditorGUILayout.Toggle(gobFile.Path, gobFile.Enabled);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(file);
                if (GUILayout.Button("Load", GUILayout.Width(100)))
                {
                    SithAssets.Instance.AddSystemPath(_gamePath);
                    SithAssets.Instance.AddSystemPath(Path.Combine(_gamePath, "Resource"));
                    SithAssets.Instance.AddSystemPath(Path.Combine(_gamePath, "Extracted"));

                    // JKDF2
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Resource\\Res1hi.gob"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Resource\\Res2.gob"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\JK1.GOB"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\JK1CTF.GOB"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\JK1MP.GOB"));

                    // MOTS
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Resource\\JKMRES.GOO"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Resource\\JKMsndLO.goo"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\JKM.GOO"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\JKM_KFY.GOO"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\JKM_MP.GOO"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Episode\\Jkm_saber.GOO"));

                    // IJIM
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "cd1.gob"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "cd2.gob"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Resource\\cd1.gob"));
                    SithAssets.Instance.AddGob(Path.Combine(_gamePath, "Resource\\cd2.gob"));

                    var go = new GameObject("SithWorld");
                    var world = go.AddComponent<SithWorld>();
                    world.Load(file);
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

    }

    class GobFile
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}