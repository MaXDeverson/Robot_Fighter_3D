namespace Game.GamepadSupport
{
    /// <summary>
    /// Which buttons a pad has printed on it, and so which glyphs it is drawn with. A pad that cannot be told apart —
    /// most Android pads come in under one common layout — is drawn as an Xbox one, the layout most of them copy.
    /// </summary>
    public enum GamepadFamily
    {
        Xbox,
        PlayStation,
        Switch,
    }
}
