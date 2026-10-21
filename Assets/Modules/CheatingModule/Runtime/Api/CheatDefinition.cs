using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Cheating
{
    /// <summary>
    /// One cheat — "win the level", "add coins". Everything about the cheat points at this asset: the
    /// <c>CheatButton</c>s that run it, its keyboard shortcut, and the <see cref="CheatHandler"/> in the scene that knows
    /// how it is done. So the button can sit in one prefab and the handler in another, and neither knows about the other.
    /// </summary>
    [CreateAssetMenu(fileName = "Cheat", menuName = "Game/Cheating/Cheat")]
    public sealed class CheatDefinition : ScriptableObject
    {
        [Tooltip("What a cheat button with no label of its own says, and what the log calls the cheat. Empty: the asset name.")]
        [SerializeField] private string _title;

        [Tooltip("Runs the cheat from a keyboard (editor, desktop builds) while the admin mode is on. None: no shortcut.")]
        [SerializeField] private Key _hotkey = Key.None;

        public string Title => string.IsNullOrWhiteSpace(_title) ? name : _title;

        public Key Hotkey => _hotkey;

        /// <summary>Test hook: fills the asset without going through the serializer.</summary>
        internal void Configure(string title, Key hotkey = Key.None)
        {
            _title = title;
            _hotkey = hotkey;
        }
    }
}
