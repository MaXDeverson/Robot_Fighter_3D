using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class RandomLight : MonoBehaviour
{
    void Start()
    {
        Light _light = GetComponent<Light>();
        _light.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
