using System.Runtime.CompilerServices;

// The tests drive the service through fakes of the internal provider, clock and URL opener, and fill the config, the
// stub and the installer through internal hooks instead of SerializedObject.
[assembly: InternalsVisibleTo("Game.Privacy.Tests")]
