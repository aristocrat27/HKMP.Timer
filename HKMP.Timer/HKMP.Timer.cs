using Hkmp.Api.Client;
using Hkmp.Api.Server;
using Modding;
using Satchel.BetterMenus;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.Timer
{
    public class TimerMod :
        Mod,
        IGlobalSettings<TimerGlobalSettings>,
        ICustomMenuMod
    {
        internal static TimerMod Instance;

        public static TimerGlobalSettings GlobalSettings;

        private Menu _menu;

        private static Sprite _circleSprite;

        public TimerMod()
            : base("HKMP.Timer")
        {
            Instance = this;
        }

        public override string GetVersion()
        {
            return "1.0.0.0";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>();
        }

        public void OnLoadGlobal(
            TimerGlobalSettings settings)
        {
            GlobalSettings =
                settings ??
                new TimerGlobalSettings();

            if (GlobalSettings.KeyBinds == null)
            {
                GlobalSettings.KeyBinds =
                    new TimerKeybinds();
            }

            TimerInputActions.Initialize();
        }

        public TimerGlobalSettings OnSaveGlobal()
        {
            if (GlobalSettings == null)
            {
                GlobalSettings =
                    new TimerGlobalSettings();
            }

            return GlobalSettings;
        }

        public bool ToggleButtonInsideMenu
        {
            get
            {
                return false;
            }
        }

        public MenuScreen GetMenuScreen(
            MenuScreen modListMenu,
            ModToggleDelegates? toggleDelegates)
        {
            TimerInputActions.Initialize();

            if (_menu == null)
            {
                _menu =
                    new Menu(
                        "HKMP.Timer",
                        new Element[]
                        {
                            new TextPanel(
                                "Настройки таймера",
                                fontSize: 32
                            ),

                            new KeyBind(
                                "Клавиша таймера",
                                TimerInputActions.Timer
                            ),

                            new TextPanel(
                                "Цвет таймера",
                                fontSize: 28
                            ),

                            new StaticPanel(
                                "Палитра",
                                CreateColorPalette
                            ),

                            new TextPanel(
                                "Для изменения положения и размера " +
                                "удерживайте клавишу таймера в игре.",
                                fontSize: 18
                            )
                        }
                    );
            }

            return _menu.GetMenuScreen(
                modListMenu
            );
        }

        private void CreateColorPalette(
            GameObject parent)
        {
            if (parent == null)
            {
                return;
            }

            RectTransform parentRect =
                parent.GetComponent<RectTransform>();

            if (parentRect == null)
            {
                return;
            }

            GameObject palette =
                new GameObject(
                    "TimerColorPalette"
                );

            RectTransform paletteRect =
                palette.AddComponent<RectTransform>();

            paletteRect.SetParent(
                parentRect,
                false
            );

            paletteRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            paletteRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            paletteRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            paletteRect.sizeDelta =
                new Vector2(
                    600f,
                    80f
                );

            HorizontalLayoutGroup layout =
                palette.AddComponent<
                    HorizontalLayoutGroup
                >();

            layout.childAlignment =
                TextAnchor.MiddleCenter;

            layout.childControlWidth =
                false;

            layout.childControlHeight =
                false;

            layout.childForceExpandWidth =
                false;

            layout.childForceExpandHeight =
                false;

            layout.spacing =
                16f;

            for (int i = 0; i < 8; i++)
            {
                CreateColorButton(
                    palette.transform,
                    i
                );
            }
        }

        private void CreateColorButton(
            Transform parent,
            int colorIndex)
        {
            GameObject buttonObject =
                new GameObject(
                    "TimerColor" +
                    colorIndex
                );

            buttonObject.transform.SetParent(
                parent,
                false
            );

            RectTransform buttonRect =
                buttonObject.AddComponent<
                    RectTransform
                >();

            buttonRect.sizeDelta =
                new Vector2(
                    52f,
                    52f
                );

            /*
             * Внешний круг.
             *
             * Он отвечает только за толстую обводку
             * выбранного цвета.
             */
            GameObject borderObject =
                new GameObject(
                    "Border"
                );

            borderObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            RectTransform borderRect =
                borderObject.AddComponent<
                    RectTransform
                >();

            borderRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            borderRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            borderRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            borderRect.sizeDelta =
                new Vector2(
                    52f,
                    52f
                );

            Image borderImage =
                borderObject.AddComponent<
                    Image
                >();

            borderImage.sprite =
                GetCircleSprite();

            borderImage.color =
                Color.white;

            borderImage.raycastTarget =
                false;

            /*
             * Сам цветной круг находится поверх
             * белой обводки и чуть меньше неё.
             *
             * Благодаря этому получается настоящий
             * толстый контур.
             */
            GameObject colorObject =
                new GameObject(
                    "Color"
                );

            colorObject.transform.SetParent(
                buttonObject.transform,
                false
            );

            RectTransform colorRect =
                colorObject.AddComponent<
                    RectTransform
                >();

            colorRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            colorRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            colorRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            colorRect.sizeDelta =
                new Vector2(
                    42f,
                    42f
                );

            Image colorImage =
                colorObject.AddComponent<
                    Image
                >();

            colorImage.sprite =
                GetCircleSprite();

            colorImage.color =
                TimerClientBehaviour.GetTimerColor(
                    colorIndex
                );

            colorImage.raycastTarget =
                true;

            /*
             * Кнопка располагается поверх визуальных
             * элементов, но сама прозрачная.
             */
            Button button =
                buttonObject.AddComponent<Button>();

            button.targetGraphic =
                colorImage;

            ColorBlock colors =
                button.colors;

            colors.normalColor =
                Color.white;

            colors.highlightedColor =
                Color.white;

            colors.pressedColor =
                Color.white;

            colors.selectedColor =
                Color.white;

            colors.disabledColor =
                Color.white;

            colors.colorMultiplier =
                1f;

            button.colors =
                colors;

            button.transition =
                Selectable.Transition.None;

            button.onClick.AddListener(
                delegate
                {
                    SetTimerColor(
                        colorIndex
                    );
                }
            );

            UpdateColorButtonSelection(
                buttonObject,
                colorIndex
            );
        }

        private void SetTimerColor(
            int colorIndex)
        {
            GlobalSettings.TimerColor =
                Mathf.Clamp(
                    colorIndex,
                    0,
                    7
                );

            UpdateAllColorButtons();

            TimerClientBehaviour
                .NotifySettingsChanged();
        }

        private void UpdateAllColorButtons()
        {
            if (_menu == null ||
                _menu.menuScreen == null)
            {
                return;
            }

            Transform[] transforms =
                _menu.menuScreen
                    .GetComponentsInChildren<
                        Transform
                    >(
                        true
                    );

            foreach (
                Transform transform
                in transforms)
            {
                if (transform == null)
                {
                    continue;
                }

                if (
                    !transform.name.StartsWith(
                        "TimerColor"
                    )
                )
                {
                    continue;
                }

                string number =
                    transform.name.Substring(
                        "TimerColor".Length
                    );

                int index;

                if (
                    !int.TryParse(
                        number,
                        out index
                    )
                )
                {
                    continue;
                }

                UpdateColorButtonSelection(
                    transform.gameObject,
                    index
                );
            }
        }

        private void UpdateColorButtonSelection(
            GameObject buttonObject,
            int colorIndex)
        {
            if (buttonObject == null)
            {
                return;
            }

            Transform border =
                buttonObject.transform.Find(
                    "Border"
                );

            if (border == null)
            {
                return;
            }

            /*
             * Выбранный цвет получает толстую белую
             * обводку.
             *
             * Невыбранный цвет полностью убирает её.
             */
            border.gameObject.SetActive(
                GlobalSettings.TimerColor ==
                colorIndex
            );
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
            {
                return _circleSprite;
            }

            const int size = 64;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name =
                "HKMP.Timer.Circle";

            Vector2 center =
                new Vector2(
                    size / 2f,
                    size / 2f
                );

            float radius =
                size / 2f - 1f;

            Color[] pixels =
                new Color[
                    size *
                    size
                ];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance =
                        Vector2.Distance(
                            new Vector2(
                                x + 0.5f,
                                y + 0.5f
                            ),
                            center
                        );

                    pixels[
                        y * size + x
                    ] =
                        distance <= radius
                            ? Color.white
                            : Color.clear;
                }
            }

            texture.SetPixels(
                pixels
            );

            texture.Apply();

            _circleSprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0,
                        0,
                        size,
                        size
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    ),
                    100f
                );

            _circleSprite.name =
                "HKMP.Timer.CircleSprite";

            return _circleSprite;
        }

        public override void Initialize(
            Dictionary<string, Dictionary<string, GameObject>>
                preloadedObjects)
        {
            Log("Initializing");

            Instance = this;

            TimerInputActions.Initialize();

            try
            {
                ClientAddon.RegisterAddon(
                    new TimerClientAddon()
                );

                Log(
                    "HKMP.Timer client addon registered."
                );
            }
            catch (Exception ex)
            {
                Log(
                    "Failed to register client addon: " +
                    ex
                );
            }

            try
            {
                TimerServerAddon serverAddon =
                    new TimerServerAddon();

                ServerAddon.RegisterAddon(
                    serverAddon
                );

                Log(
                    "HKMP.Timer server addon registered."
                );
            }
            catch (Exception ex)
            {
                Log(
                    "Failed to register server addon: " +
                    ex
                );
            }

            Log("Initialized");
        }
    }
}