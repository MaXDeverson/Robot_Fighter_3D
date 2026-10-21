using System.Runtime.CompilerServices;

// The tests fill the config and the glyph sets through internal hooks instead of SerializedObject, and tick the
// navigation by hand.
[assembly: InternalsVisibleTo("Game.GamepadSupport.Tests")]
