using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

public class ExportMaterial : MonoBehaviour
{
    public bool export;
    //public Button m_HitMeButton;

    private void Start()
    {
        //Button btn = m_HitMeButton.GetComponent<Button>();
        //btn.onClick.AddListener(onExport);
    }

    void onExport()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (export)
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            var counters = new Dictionary<string, int>(10000); // init dict with 10000 capacity
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (Renderer r in rends)
                {
                    foreach (Material mat in r.materials)
                    {
                        string name = mat.name.Split(".")[0];
                        string path;
                        if (!counters.ContainsKey(name))
                        {
                            AssetDatabase.CreateAsset(mat.mainTexture, "Assets/textures/" + name + ".asset"); // Save mat texture
                            counters[name] = 1;
                            path = string.Join("", "Assets/materials/", name, ".mat (instance", ".mat");
                        }
                        else
                        {
                            path = string.Join("", "Assets/materials/", name, ".mat (instance)_", counters[name].ToString(), ".mat");
                            counters[name]++;
                        }
                        AssetDatabase.CreateAsset(mat, path);
                    }
                }
            }
            finally
            {
                // The StopAssetEditing should be enclosed in this finally scope
                // in case an exception occurs to keep asset database responsive.
                AssetDatabase.StopAssetEditing();
            }
            export = false;
        }
    }
}
