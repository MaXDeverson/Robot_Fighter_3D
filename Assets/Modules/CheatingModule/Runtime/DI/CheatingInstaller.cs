using UnityEngine;
using Zenject;

namespace Game.Cheating.DI
{
    /// <summary>
    /// Binds <see cref="ICheatingService"/> and the cheat hotkeys. Belongs in the project context: the admin switch is
    /// flipped in the main menu and the cheats are used in the gameplay scene.
    /// <para>
    /// The device and the storage of the switch can be replaced by binding <see cref="IDeviceInfo"/> or
    /// <see cref="IAdminStorage"/> before this installer runs.
    /// </para>
    /// </summary>
    public sealed class CheatingInstaller : MonoInstaller
    {
        [Tooltip("Who may switch the admin mode on.")]
        [SerializeField] private CheatingConfig _config;

        public override void InstallBindings()
        {
            CheatingConfig config = _config;
            if (config == null)
            {
                // Zenject cannot take a null argument, and the game should still boot: nobody is allowed then.
                Debug.LogError($"[Cheating] {nameof(CheatingInstaller)} has no config.", this);
                config = ScriptableObject.CreateInstance<CheatingConfig>();
                config.Configure(false, false, false);
            }

            Container.Bind<IDeviceInfo>().To<UnityDeviceInfo>().AsSingle().IfNotBound();
            Container.Bind<IAdminStorage>().To<FileAdminStorage>().AsSingle().IfNotBound();

            // Not lazy: the device id goes to the log at start, which is how a new phone's id is found with adb.
            Container.BindInterfacesTo<CheatingService>().AsSingle().WithArguments(config).NonLazy();
            Container.BindInterfacesTo<CheatHotkeys>().AsSingle();
        }
    }
}
