using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MyButtonEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] KeyCode _keyCode = KeyCode.KeypadEnter;
    private Animation _animationClick;
    private bool _isPressed;
    private void Start()
    {
        _animationClick = GetComponent<Animation>();
    }

    private void FixedUpdate()
    {
        if( Input.GetKeyDown(_keyCode))
        {
            _animationClick.Play("click");
            // _isPressed = true
        }
        if(Input.GetKeyDown(_keyCode))
        {
            _animationClick.Play("unclick");
        }
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