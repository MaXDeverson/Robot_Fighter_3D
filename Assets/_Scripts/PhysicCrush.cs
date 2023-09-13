using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicCrush : MonoBehaviour
{
    [SerializeField] private List<Rigidbody> _parts;
    void Start()
    {
        Crush();
    }

    public void Crush()
    {
        _parts.ForEach(x => x.AddForce(new Vector3(Random.Range(-3, 3),4, Random.Range(1, 3))));
    }
}
