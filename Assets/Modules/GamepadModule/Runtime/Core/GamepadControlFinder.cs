using UnityEngine.InputSystem;

namespace Game.GamepadSupport
{
    /// <summary>
    /// Finds which control of a given pad an action is bound to, so a prompt shows the button that really does it.
    /// The binding is matched against the pad itself, which is what makes <c>*/{Submit}</c> come out as A on an Xbox
    /// pad but as the right-hand button on a Switch Pro controller, where Nintendo puts confirm.
    /// </summary>
    internal static class GamepadControlFinder
    {
        /// <summary>
        /// The path, relative to <paramref name="pad"/>, of the first control any binding of <paramref name="action"/>
        /// lands on — <c>buttonSouth</c>, <c>leftStick/left</c>. Null when no binding of the action fits the pad.
        /// </summary>
        public static string FindControlPath(InputAction action, InputDevice pad)
        {
            if (action == null || pad == null)
                return null;

            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];

                // A composite's own entry is a recipe ("2DVector"), not a control; its parts follow it in the list.
                if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath))
                    continue;

                InputControl control = InputControlPath.TryFindControl(pad, binding.effectivePath);
                if (control != null && control != pad)
                    return RelativePath(control, pad);
            }

            return null;
        }

        private static string RelativePath(InputControl control, InputDevice pad)
        {
            string path = control.path;
            string prefix = pad.path + "/";
            return path.StartsWith(prefix) ? path.Substring(prefix.Length) : control.name;
        }
    }
}
