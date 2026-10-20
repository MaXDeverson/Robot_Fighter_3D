using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Plays the click / unclick animation of a touch screen button when it is touched
// or when its action is pressed on the keyboard or gamepad.
public class MyButtonEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Input action that animates this button (Punch, Kick, Jump, Defend...). Empty = the Action Down of the UIButton on this object.")]
    [SerializeField] private string _action = "";
    private Animation _animationClick;

    private void Awake()
    {
        _animationClick = GetComponent<Animation>();
        if (string.IsNullOrEmpty(_action) && TryGetComponent(out UIButton uiButton)) _action = uiButton.actionDown;
    }

    private void OnEnable()
    {
        InputManager.onInputEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputManager.onInputEvent -= OnInputEvent;
    }

    private void OnInputEvent(string action, BUTTONSTATE buttonState)
    {
        if (action != _action) return;
        if (buttonState == BUTTONSTATE.PRESS) _animationClick.Play("click");
        else if (buttonState == BUTTONSTATE.RELEASE) _animationClick.Play("unclick");
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
