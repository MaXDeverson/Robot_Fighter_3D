using System.Runtime.CompilerServices;

// The tests fill the config and the cheats through internal hooks instead of SerializedObject, and drive the hotkeys
// with a fake keyboard.
[assembly: InternalsVisibleTo("Game.Cheating.Tests")]
