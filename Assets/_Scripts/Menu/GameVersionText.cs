using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writes the game version ("v0.8") into the <see cref="Text"/> it sits on.
/// </summary>
[RequireComponent(typeof(Text))]
public sealed class GameVersionText : MonoBehaviour
{
    [SerializeField] private string _prefix = "v";

    private void Awake()
    {
        GetComponent<Text>().text = _prefix + Application.version;
    }
}
