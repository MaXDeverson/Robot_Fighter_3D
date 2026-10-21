namespace Game.Cheating
{
    /// <summary>Keeps the admin switch for one session only. What the tests use.</summary>
    public sealed class MemoryAdminStorage : IAdminStorage
    {
        public bool IsAdminEnabled { get; set; }
    }
}
