using UnityEngine;

namespace Game.Privacy
{
    /// <summary><see cref="Application.OpenURL"/>.</summary>
    internal sealed class UnityUrlOpener : IUrlOpener
    {
        public void Open(string url)
        {
            Application.OpenURL(url);
        }
    }
}
