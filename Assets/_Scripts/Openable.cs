using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class Openable : MonoBehaviour
{
    public Action OnOpen;
    public GameObject PhysicRestriction;
    public Action<GameObject> OnEntered;
    public abstract void Open();
}
