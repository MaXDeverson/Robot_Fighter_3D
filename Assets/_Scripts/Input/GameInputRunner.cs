using UnityEngine;

// Created by GameInput on the persistent [GameInput] object.
// Applies input type changes before the other scripts update, outside of the Input System callbacks.
[DefaultExecutionOrder(-1000)]
public class GameInputRunner : MonoBehaviour
{
    private void Update()
    {
        GameInput.ApplyPendingInputType();
    }
}
