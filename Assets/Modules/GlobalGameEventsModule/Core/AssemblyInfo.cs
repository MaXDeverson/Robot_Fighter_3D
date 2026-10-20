using System.Runtime.CompilerServices;

// The service keeps its moving parts (channels, subscription records, queue channels) internal so that the
// public surface stays small. The sibling assemblies of the module still need to reach them.
[assembly: InternalsVisibleTo("GlobalGameEvents.Zenject")]
[assembly: InternalsVisibleTo("GlobalGameEvents.Editor")]
[assembly: InternalsVisibleTo("GlobalGameEvents.Tests")]
