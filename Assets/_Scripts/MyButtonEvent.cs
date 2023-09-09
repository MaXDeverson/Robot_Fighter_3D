using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MyButtonEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Animation _animationClick;

    private void Start()
    {
        _animationClick = GetComponent<Animation>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        _animationClick.Play("click");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _animationClick.Play("unclick");
    }
}