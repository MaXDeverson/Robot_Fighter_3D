using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using UnityEngine.Scripting;

[RequireComponent(typeof(RectTransform))]
public class UIJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    public RectTransform handle;
    public float radius = 40f;
    public float autoReturnSpeed = 8f;
    private bool returnToStartPos;
    [SerializeField] private bool _withKeyboardVisualization;
    private RectTransform parentRect;
    private InputManager inputmanager;

    void OnEnable()
    {
        returnToStartPos = true;
        handle.transform.SetParent(transform);
        parentRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (inputmanager == null) inputmanager = GameObject.FindObjectOfType<InputManager>();

        //return to start position
        if (returnToStartPos)
        {
            if (handle.anchoredPosition.magnitude > Mathf.Epsilon)
            {
                handle.anchoredPosition -= new Vector2(handle.anchoredPosition.x * autoReturnSpeed, handle.anchoredPosition.y * autoReturnSpeed) * Time.deltaTime;
                inputmanager.OnTouchScreenJoystickEvent(Vector2.zero);
            }
            else
            {
                returnToStartPos = false;
            }
        }
        if (_withKeyboardVisualization)
        {
            Vector2 visualDirection = new Vector2();
            if (Input.GetKey(KeyCode.S))
            {
                visualDirection.y = -1;
            }
            if (Input.GetKey(KeyCode.W))
            {
                visualDirection.y = 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                visualDirection.x = 1;
            }
            if (Input.GetKey(KeyCode.A))
            {
                visualDirection.x = -1;
            }
            Visualizate(visualDirection);
        }
    }

    private void Visualizate(Vector2 direction)
    {
        handle.anchoredPosition = new Vector2(radius * direction.x, radius * direction.y);
    }

    //return coordinates
    public Vector2 Coordinates
    {
        get
        {
            if (handle.anchoredPosition.magnitude < radius)
            {
                return handle.anchoredPosition / radius;
            }
            return handle.anchoredPosition.normalized;
        }
    }

    //touch down
    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        returnToStartPos = false;
        var handleOffset = GetJoystickOffset(eventData);
        handle.anchoredPosition = handleOffset;
        if (inputmanager != null) inputmanager.OnTouchScreenJoystickEvent(handleOffset.normalized);
    }

    //touch drag
    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        var handleOffset = GetJoystickOffset(eventData);
        handle.anchoredPosition = handleOffset;
        if (inputmanager != null) inputmanager.OnTouchScreenJoystickEvent(handleOffset.normalized);
    }

    //touch up
    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        returnToStartPos = true;
    }

    //get offset
    private Vector2 GetJoystickOffset(PointerEventData eventData)
    {

        Vector3 globalHandle;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out globalHandle))
        {
            handle.position = globalHandle;
        }

        var handleOffset = handle.anchoredPosition;
        if (handleOffset.magnitude > radius)
        {
            handleOffset = handleOffset.normalized * radius;
            handle.anchoredPosition = handleOffset;
        }
        return handleOffset;
    }
}