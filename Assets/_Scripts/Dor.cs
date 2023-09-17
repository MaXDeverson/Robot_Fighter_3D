using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dor : Openable
{
    [SerializeField] private Animation _openAnimation;
    [SerializeField] private GameObject _physicRestriction;
    [SerializeField] private bool _autoOpen;
    public override void Open()
    {
        _openAnimation.Play();
        StartCoroutine(OffPhysic());
    }

    private IEnumerator OffPhysic()
    {
        yield return new WaitForSeconds(3);
        _physicRestriction.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(_autoOpen)
        {
            _openAnimation.Play();
        }
        if (other.CompareTag("Player"))
        {
            OnEntered?.Invoke(other.gameObject);
        }
    }
}
