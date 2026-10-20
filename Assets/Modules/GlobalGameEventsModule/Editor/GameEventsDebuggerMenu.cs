using UnityEditor;

namespace Game.Events.EditorTools
{
    /// <summary>
    /// The one and only exception to this module's no-static rule.
    /// </summary>
    /// <remarks>
    /// Unity registers main menu entries exclusively through static methods; there is no instance based
    /// equivalent for <see cref="MenuItem"/>. The method below holds no state of any kind, reads nothing and
    /// writes nothing: it opens a window. Every other way into the module, including
    /// <see cref="GlobalGameEventsInstallerEditor"/>'s inspector button, goes through instance code, so deleting this
    /// file leaves a module with literally zero occurrences of the keyword.
    /// </remarks>
    internal sealed class GameEventsDebuggerMenu
    {
        [MenuItem("Window/Global Game Events Debugger")]
        private static void Open()
        {
            EditorWindow.GetWindow<GameEventsDebuggerWindow>().Show();
        }
    }
}
