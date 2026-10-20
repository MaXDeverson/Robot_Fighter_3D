using System.Runtime.CompilerServices;

// Everything stays internal: the window and the command line are the module's only doors. The tests reach the pure
// pieces (versions, file names, the ProjectSettings rewrite) without running a build.
[assembly: InternalsVisibleTo("Game.AutoBuilder.Editor.Tests")]
