using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

//[ExecuteAlways]
public class ImportMaterial : MonoBehaviour
{
    public bool import;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (import)
        {
            Renderer[] renderer = GetComponentsInChildren<Renderer>();
            var light = GetComponentsInChildren<Light>();

            foreach (Light l in light)
            {
                l.enabled = false;
            }

            foreach (Renderer rend in renderer)
            {
                Material[] mat = rend.materials;

                for (int m = 0; m < mat.Length; m++)
                {
                    string[] name = mat[m].name.Split("_mat");
                    mat[m] = Resources.Load<Material>("materials/" + name[0]);
                    if (File.Exists("Assets/Resources/materials/" + name[0] + ".mat"))
                    {
                        Debug.Log("File exist");
                    }
                }

                rend.materials = mat;
            }

            import = false;
            Destroy(this);
        }
    }
}
