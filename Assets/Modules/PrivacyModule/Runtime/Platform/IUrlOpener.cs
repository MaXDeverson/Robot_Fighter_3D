namespace Game.Privacy
{
    /// <summary>Opens a page in the browser, so the tests can see what would be opened.</summary>
    internal interface IUrlOpener
    {
        void Open(string url);
    }
}
