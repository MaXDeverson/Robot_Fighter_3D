using UnityEngine;
using Zenject;

namespace Game.GamepadSupport.DI
{
    /// <summary>
    /// Binds <see cref="IGamepadService"/>, <see cref="GamepadNavigation"/> and the config. Belongs in the project
    /// context: a pad is plugged in once for the whole game, and the menus it walks are in every scene.
    /// </summary>
    public sealed class GamepadInstaller : MonoInstaller
    {
        [SerializeField] private GamepadConfig _config;

        public override void InstallBindings()
        {
            GamepadConfig config = _config;
            if (config == null)
            {
                // Zenject cannot take a null argument, and the game should still boot: no glyphs, no prompts then.
                Debug.LogError($"[Gamepad] {nameof(GamepadInstaller)} has no config.", this);
                config = ScriptableObject.CreateInstance<GamepadConfig>();
            }

            Container.BindInstance(config);
            Container.BindInterfacesTo<GamepadService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GamepadNavigation>().AsSingle();
        }
    }
}
