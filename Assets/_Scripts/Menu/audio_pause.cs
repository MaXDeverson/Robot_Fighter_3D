using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audio_pause : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.GetComponent<AudioSource>().volume =PlayerPrefs.GetFloat("Music");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
