using System.Collections.Generic;
using System.IO;
using System.Linq;
using jksharp.jklviewer;
using UnityEditor;
using UnityEngine;
using System.Collections;

public class SithEditor : EditorWindow {
    [MenuItem("Window/Sith")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SithEditor));
    }

    private string _jkPath = @"D:\Spel\Steam\steamapps\common\Star Wars Jedi Knight";
    private string _assetPath = "Extracted\\";
    private IEnumerable<GobFile> _gobFiles;
    private bool _gobFilesEnabled;

    void Start()
    {
    }

    void OnGUI()
    {
        _jkPath = EditorGUILayout.TextField("Jedi Knight path", _jkPath);
        if (GUILayout.Button("Find GOBs"))
        {
            _gobFiles = Directory.GetFiles(_jkPath, "*.gob", SearchOption.AllDirectories).Select(x => new GobFile{ Path = x.Replace(_jkPath + "\\", ""), Enabled = false}).ToArray();
        }

        if (_gobFiles != null)
        {
            _gobFilesEnabled = EditorGUILayout.BeginToggleGroup("GOB Files", _gobFilesEnabled);
            foreach(var gobFile in _gobFiles)
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
                using (GOBStream stream = new GOBStream(Path.Combine(_jkPath, gobFile.Path)))
                {
                    stream.Extract(Path.Combine(_assetPath, gobFile.Path));
                }
            }
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