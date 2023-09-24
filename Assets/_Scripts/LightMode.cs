using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightMode : MonoBehaviour
{
    [SerializeField] private int _levelActiovatonIndex;
    [SerializeField] private GameObject _oldLight;
    [SerializeField] private GameObject _newLight;
    void Start()
    {
        if(General.CurrentLevel> _levelActiovatonIndex)
        {
            _oldLight.SetActive(false);
            _newLight.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
