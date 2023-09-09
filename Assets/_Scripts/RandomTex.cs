using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTex : MonoBehaviour
{
    public int texture_num;
    public Material m1, m2;
    public SkinnedMeshRenderer[] playerMesh;

    void Start()
    {
        if(texture_num == 1)
        {
            for (int i = 0; i < playerMesh.Length; i++)
                playerMesh[i].material = m1;
        }
        else if (texture_num == 2)
        {
            for (int i = 0; i < playerMesh.Length; i++)
                playerMesh[i].material = m2;
        }
    }

}
