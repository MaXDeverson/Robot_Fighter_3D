namespace Game.Cheating
{
    /// <summary>Where the admin switch is kept between launches.</summary>
    public interface IAdminStorage
    {
        bool IsAdminEnabled { get; set; }
    }
}
