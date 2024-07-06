using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DeactivateLights : MonoBehaviour
{
    public bool deacivateLights;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (deacivateLights)
        {
            Light[] lights = GetComponentsInChildren<Light>();

            foreach (Light light in lights)
            {
                light.enabled = false;
            }
            deacivateLights = false;
            DestroyImmediate(this);
        }
    }
}
