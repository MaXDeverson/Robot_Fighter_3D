using System.Runtime.CompilerServices;

// Only IAdsManagerService and the types it speaks in are public: that is how "everything goes through the ads manager"
// is enforced by the compiler rather than just written down. The tests still need to reach the rest.
[assembly: InternalsVisibleTo("Game.Ads.Tests")]
