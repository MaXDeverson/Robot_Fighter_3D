using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace Game.GamepadSupport.Tests
{
    /// <summary>A pad that is plugged in or not, as the test says.</summary>
    internal sealed class FakeGamepadService : IGamepadService
    {
        public bool IsConnected { get; set; } = true;

        public Gamepad Current => null;

        public GamepadFamily Family => GamepadFamily.Xbox;

        /// <summary>What <see cref="GetGlyph"/> answers for any action while a pad is connected.</summary>
        public Sprite Glyph { get; set; }

        public event Action Changed;

        public Sprite GetGlyph(InputAction action) => IsConnected ? Glyph : null;

        public void RaiseChanged() => Changed?.Invoke();
    }

    /// <summary>
    /// Shared setup on top of the Input System's own test fixture, which swaps in an empty input system for the test
    /// and puts the real one back after it: assets and objects created for the test and destroyed after it.
    /// </summary>
    internal abstract class GamepadTestFixture : InputTestFixture
    {
        private readonly List<Object> _spawned = new List<Object>();

        public override void TearDown()
        {
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                    Object.DestroyImmediate(spawned);
            }

            _spawned.Clear();
            base.TearDown();
        }

        protected T Track<T>(T spawned) where T : Object
        {
            _spawned.Add(spawned);
            return spawned;
        }

        protected GameObject Spawn(string name, Transform parent = null)
        {
            var go = Track(new GameObject(name, typeof(RectTransform)));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        protected Sprite Glyph(string name)
        {
            Texture2D texture = Track(new Texture2D(4, 4));
            Sprite sprite = Track(Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero));
            sprite.name = name;
            return sprite;
        }

        protected GamepadGlyphSet GlyphSet(GamepadFamily family, params (string control, Sprite sprite)[] glyphs)
        {
            var set = Track(ScriptableObject.CreateInstance<GamepadGlyphSet>());
            var list = new List<GamepadGlyphSet.Glyph>();
            foreach ((string control, Sprite sprite) in glyphs)
                list.Add(new GamepadGlyphSet.Glyph(control, sprite));
            set.Configure(family, list);
            return set;
        }

        protected GamepadConfig Config(InputActionReference submit, InputActionReference cancel, params GamepadGlyphSet[] sets)
        {
            var config = Track(ScriptableObject.CreateInstance<GamepadConfig>());
            config.Configure(sets, submit, cancel);
            return config;
        }

        /// <summary>An actions asset of the test's own, with a UI map shaped like the game's.</summary>
        protected InputActionAsset Actions()
        {
            var asset = Track(ScriptableObject.CreateInstance<InputActionAsset>());
            InputActionMap ui = asset.AddActionMap("UI");
            ui.AddAction("Submit", InputActionType.Button, "*/{Submit}");
            ui.AddAction("Cancel", InputActionType.Button, "*/{Cancel}");
            InputActionMap player = asset.AddActionMap("Player");
            player.AddAction("Jump", InputActionType.Button, "<Keyboard>/space").AddBinding("<Gamepad>/buttonSouth");
            player.AddAction("MoveLeft", InputActionType.Button, "<Keyboard>/a").AddBinding("<Gamepad>/leftStick/left");
            player.AddAction("Pause", InputActionType.Button, "<Gamepad>/start");
            player.AddAction("Fly", InputActionType.Button, "<Keyboard>/f");
            return asset;
        }

        protected InputActionReference Reference(InputActionAsset asset, string action)
        {
            return Track(InputActionReference.Create(asset.FindAction(action, throwIfNotFound: true)));
        }
    }
}
