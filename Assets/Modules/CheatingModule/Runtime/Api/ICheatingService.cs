using System;
using System.Collections.Generic;

namespace Game.Cheating
{
    /// <summary>
    /// The admin mode and the cheats behind it. Lives in the project context: the switch is flipped in the main menu's
    /// settings and the cheats it opens are used in the gameplay scene.
    /// <para>
    /// Two levels: the admin mode is <b>available</b> on the editor, desktop builds and the devices listed in
    /// <see cref="CheatingConfig"/>, and <b>enabled</b> only once the admin switch is turned on there. A saved switch
    /// means nothing on a device that is not allowed, so editing the saves opens nothing.
    /// </para>
    /// </summary>
    public interface ICheatingService
    {
        /// <summary>Why the admin mode is or is not available here.</summary>
        AdminAccess Access { get; }

        /// <summary>Whether this device may switch the admin mode on at all — the admin switch is shown only then.</summary>
        bool IsAdminAvailable { get; }

        /// <summary>Whether the admin mode is on: cheat buttons are shown and cheats run.</summary>
        bool IsAdminEnabled { get; }

        /// <summary>The admin mode went on (true) or off (false).</summary>
        event Action<bool> AdminEnabledChanged;

        /// <summary>
        /// Turns the admin mode on or off and keeps that for the next launches. Turning it on where it is not
        /// available is refused.
        /// </summary>
        void SetAdminEnabled(bool enabled);

        /// <summary>The cheats something in the loaded scenes can run right now.</summary>
        IReadOnlyCollection<CheatDefinition> RegisteredCheats { get; }

        /// <summary>
        /// Says how <paramref name="cheat"/> is done here. Dispose the result when the handler goes away (its scene is
        /// unloaded). With several handlers for one cheat the last one registered runs.
        /// </summary>
        IDisposable Register(CheatDefinition cheat, Action execute);

        /// <summary>Whether <see cref="Execute"/> would run <paramref name="cheat"/> now.</summary>
        bool CanExecute(CheatDefinition cheat);

        /// <summary>
        /// Runs <paramref name="cheat"/>. Refused, with a warning, while the admin mode is off or when nothing in the
        /// loaded scenes handles it.
        /// </summary>
        /// <returns>Whether the cheat ran.</returns>
        bool Execute(CheatDefinition cheat);
    }
}
