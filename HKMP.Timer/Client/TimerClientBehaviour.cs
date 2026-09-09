using HkmpTimer;
using System;
using UnityEngine;

namespace HKMP.Timer
{
    public sealed class TimerClientBehaviour : MonoBehaviour
    {
        private const float ClockSyncInterval = 5f;
        private const float EditHoldTime = 0.25f;

        private const float MinimumWidth = 180f;
        private const float MinimumHeight = 80f;

        private const float ResizeBorder = 12f;
        private const float ResizeHandleSize = 12f;
        private const float EditBorderThickness = 2f;

        private TimerClientAddon _addon;

        private bool _windowVisible;
        private bool _editing;

        private bool _keyHeld;
        private bool _longPressTriggered;

        private float _keyDownTime;

        private bool _running;
        private bool _expired;

        private long _remainingMilliseconds;
        private long _startServerUtcTicks;

        private int _durationSeconds;

        private long _serverClockOffsetTicks;

        private float _nextClockSync;

        private GUIStyle _timerStyle;
        private GUIStyle _windowStyle;
        private GUIStyle _stateStyle;
        private GUIStyle _editStyle;

        private Color _currentTimerColor;

        private int _lastColorIndex = -1;

        private Font _hollowKnightFont;

        private Rect _windowRect;

        private bool _windowRectInitialized;

        private bool _dragging;

        private Vector2 _dragOffset;

        private bool _resizing;

        private ResizeMode _resizeMode;

        private Vector2 _resizeStartMouse;

        private Rect _resizeStartRect;

        private enum ResizeMode
        {
            None,

            Left,
            Right,
            Top,
            Bottom,

            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        public static TimerClientBehaviour Create(
            TimerClientAddon addon)
        {
            TimerClientBehaviour existing =
                FindObjectOfType<TimerClientBehaviour>();

            if (existing != null)
            {
                existing._addon =
                    addon;

                existing.InitializeWindowRect();

                return existing;
            }

            GameObject gameObject =
                new GameObject(
                    "HKMP.Timer.Client"
                );

            DontDestroyOnLoad(
                gameObject
            );

            TimerClientBehaviour behaviour =
                gameObject.AddComponent<
                    TimerClientBehaviour
                >();

            behaviour._addon =
                addon;

            return behaviour;
        }

        public static void NotifySettingsChanged()
        {
            TimerClientBehaviour behaviour =
                FindObjectOfType<TimerClientBehaviour>();

            if (behaviour == null)
            {
                return;
            }

            behaviour.UpdateColor();

            behaviour.InitializeWindowRect();

            behaviour.EnsureStyles();

            behaviour.ApplyFont();
        }

        private void Awake()
        {
            _nextClockSync =
                Time.unscaledTime +
                ClockSyncInterval;

            UpdateColor();

            InitializeWindowRect();
        }

        private void Update()
        {
            if (_addon == null)
            {
                return;
            }

            HandleKeybind();

            if (
                Time.unscaledTime >=
                _nextClockSync
            )
            {
                _nextClockSync =
                    Time.unscaledTime +
                    ClockSyncInterval;

                _addon.RequestClockSync();
            }

            if (
                _running &&
                GetRemainingMilliseconds() <= 0
            )
            {
                _running = false;

                _remainingMilliseconds = 0;

                _startServerUtcTicks = 0;

                _expired = true;
            }
        }

        private void HandleKeybind()
        {
            if (TimerInputActions.Timer == null)
            {
                return;
            }

            if (
                TimerInputActions.Timer.WasPressed
            )
            {
                _keyHeld = true;

                _longPressTriggered =
                    false;

                _keyDownTime =
                    Time.unscaledTime;
            }

            if (
                _keyHeld &&
                TimerInputActions.Timer.IsPressed
            )
            {
                float heldTime =
                    Time.unscaledTime -
                    _keyDownTime;

                if (
                    !_longPressTriggered &&
                    heldTime >= EditHoldTime
                )
                {
                    _longPressTriggered =
                        true;

                    EnterEditMode();
                }
            }

            if (
                _keyHeld &&
                !TimerInputActions.Timer.IsPressed
            )
            {
                bool wasLongPress =
                    _longPressTriggered;

                _keyHeld = false;

                _longPressTriggered =
                    false;

                if (wasLongPress)
                {
                    ExitEditMode();

                    _windowVisible =
                        true;
                }
                else
                {
                    _windowVisible =
                        !_windowVisible;
                }
            }
        }

        private void EnterEditMode()
        {
            InitializeWindowRect();

            _editing = true;

            _windowVisible = true;

            _dragging = false;

            _resizing = false;

            _resizeMode =
                ResizeMode.None;
        }

        private void ExitEditMode()
        {
            _editing = false;

            _dragging = false;

            _resizing = false;

            _resizeMode =
                ResizeMode.None;

            SaveWindowSettings();
        }

        public void ApplyTimerState(
            TimerStatePacket packet)
        {
            if (packet == null)
            {
                return;
            }

            _running =
                packet.Running;

            _expired =
                packet.Expired;

            _durationSeconds =
                Mathf.Max(
                    0,
                    packet.DurationSeconds
                );

            _remainingMilliseconds =
                Math.Max(
                    0L,
                    packet.RemainingMilliseconds
                );

            _startServerUtcTicks =
                packet.StartUtcTicks;

            _serverClockOffsetTicks =
                packet.ServerUtcTicks -
                DateTime.UtcNow.Ticks;

            if (_running)
            {
                _expired = false;
            }
        }

        public void ApplyClockSyncResponse(
            ClockSyncResponsePacket packet)
        {
            if (packet == null)
            {
                return;
            }

            long localReceiveTicks =
                DateTime.UtcNow.Ticks;

            long roundTripTicks =
                localReceiveTicks -
                packet.ClientSendUtcTicks;

            if (roundTripTicks < 0)
            {
                roundTripTicks = 0;
            }

            long localMidpointTicks =
                packet.ClientSendUtcTicks +
                roundTripTicks / 2L;

            _serverClockOffsetTicks =
                packet.ServerUtcTicks -
                localMidpointTicks;
        }

        private long GetRemainingMilliseconds()
        {
            if (!_running)
            {
                return Math.Max(
                    0L,
                    _remainingMilliseconds
                );
            }

            long serverNowTicks =
                DateTime.UtcNow.Ticks +
                _serverClockOffsetTicks;

            long endTicks =
                _startServerUtcTicks +
                (long)_durationSeconds *
                TimeSpan.TicksPerSecond;

            long remainingTicks =
                endTicks -
                serverNowTicks;

            if (remainingTicks <= 0)
            {
                return 0;
            }

            return
                remainingTicks /
                TimeSpan.TicksPerMillisecond;
        }

        private void OnGUI()
        {
            if (!_windowVisible)
            {
                return;
            }

            InitializeWindowRect();

            EnsureStyles();

            UpdateColor();

            if (_editing)
            {
                HandleWindowEditor();
            }

            long remaining =
                GetRemainingMilliseconds();

            if (
                _running &&
                remaining <= 0
            )
            {
                _running = false;

                _remainingMilliseconds = 0;

                _startServerUtcTicks = 0;

                _expired = true;

                remaining = 0;
            }

            string timeText;

            if (_expired)
            {
                timeText =
                    "ВРЕМЯ ВЫШЛО";
            }
            else
            {
                timeText =
                    FormatMilliseconds(
                        remaining
                    );
            }

            if (_editing)
            {
                DrawEditorWindow(
                    timeText
                );
            }
            else
            {
                DrawNormalWindow(
                    timeText
                );
            }
        }

        private void DrawNormalWindow(
            string timeText)
        {
            GUI.Box(
                _windowRect,
                GUIContent.none,
                _windowStyle
            );

            GUI.Label(
                new Rect(
                    _windowRect.x + 10f,
                    _windowRect.y + 8f,
                    _windowRect.width - 20f,
                    _windowRect.height * 0.60f
                ),
                timeText,
                _timerStyle
            );

            string state;

            if (_expired)
            {
                state =
                    "ВРЕМЯ ВЫШЛО";
            }
            else
            {
                state =
                    _running
                        ? "ЗАПУЩЕН"
                        : "ОСТАНОВЛЕН";
            }

            GUI.Label(
                new Rect(
                    _windowRect.x + 10f,
                    _windowRect.y +
                    _windowRect.height -
                    28f,
                    _windowRect.width - 20f,
                    20f
                ),
                state,
                _stateStyle
            );
        }

        private void DrawEditorWindow(
            string timeText)
        {
            GUI.Box(
                _windowRect,
                GUIContent.none,
                _windowStyle
            );

            GUI.Label(
                new Rect(
                    _windowRect.x + 10f,
                    _windowRect.y + 8f,
                    _windowRect.width - 20f,
                    _windowRect.height * 0.60f
                ),
                timeText,
                _timerStyle
            );

            GUI.Label(
                new Rect(
                    _windowRect.x + 10f,
                    _windowRect.y +
                    _windowRect.height -
                    28f,
                    _windowRect.width - 20f,
                    20f
                ),
                "РЕЖИМ РЕДАКТИРОВАНИЯ",
                _editStyle
            );

            DrawEditorBorder();

            DrawResizeHandle(
                _windowRect.x,
                _windowRect.y
            );

            DrawResizeHandle(
                _windowRect.x +
                _windowRect.width,
                _windowRect.y
            );

            DrawResizeHandle(
                _windowRect.x,
                _windowRect.y +
                _windowRect.height
            );

            DrawResizeHandle(
                _windowRect.x +
                _windowRect.width,
                _windowRect.y +
                _windowRect.height
            );
        }

        private void DrawEditorBorder()
        {
            Color previousColor =
                GUI.color;

            GUI.color =
                UnityEngine.Color.white;

            GUI.DrawTexture(
                new Rect(
                    _windowRect.x,
                    _windowRect.y,
                    _windowRect.width,
                    EditBorderThickness
                ),
                Texture2D.whiteTexture
            );

            GUI.DrawTexture(
                new Rect(
                    _windowRect.x,
                    _windowRect.y +
                    _windowRect.height -
                    EditBorderThickness,
                    _windowRect.width,
                    EditBorderThickness
                ),
                Texture2D.whiteTexture
            );

            GUI.DrawTexture(
                new Rect(
                    _windowRect.x,
                    _windowRect.y,
                    EditBorderThickness,
                    _windowRect.height
                ),
                Texture2D.whiteTexture
            );

            GUI.DrawTexture(
                new Rect(
                    _windowRect.x +
                    _windowRect.width -
                    EditBorderThickness,
                    _windowRect.y,
                    EditBorderThickness,
                    _windowRect.height
                ),
                Texture2D.whiteTexture
            );

            GUI.color =
                previousColor;
        }

        private void DrawResizeHandle(
            float x,
            float y)
        {
            Color previousColor =
                GUI.color;

            GUI.color =
                UnityEngine.Color.white;

            float halfSize =
                ResizeHandleSize / 2f;

            GUI.DrawTexture(
                new Rect(
                    x - halfSize,
                    y - halfSize,
                    ResizeHandleSize,
                    ResizeHandleSize
                ),
                Texture2D.whiteTexture
            );

            GUI.color =
                previousColor;
        }

        private void HandleWindowEditor()
        {
            Event currentEvent =
                Event.current;

            Vector2 mousePosition =
                currentEvent.mousePosition;

            if (
                currentEvent.type ==
                    EventType.MouseDown &&
                currentEvent.button == 0
            )
            {
                ResizeMode resizeMode =
                    GetResizeMode(
                        mousePosition
                    );

                if (
                    resizeMode !=
                    ResizeMode.None
                )
                {
                    BeginResize(
                        resizeMode,
                        mousePosition
                    );

                    currentEvent.Use();

                    return;
                }

                if (
                    _windowRect.Contains(
                        mousePosition
                    )
                )
                {
                    BeginDrag(
                        mousePosition
                    );

                    currentEvent.Use();

                    return;
                }
            }

            if (
                currentEvent.type ==
                    EventType.MouseDrag &&
                currentEvent.button == 0
            )
            {
                if (_dragging)
                {
                    UpdateDrag(
                        mousePosition
                    );

                    currentEvent.Use();

                    return;
                }

                if (_resizing)
                {
                    UpdateResize(
                        mousePosition
                    );

                    currentEvent.Use();

                    return;
                }
            }

            if (
                currentEvent.type ==
                    EventType.MouseUp &&
                currentEvent.button == 0
            )
            {
                if (
                    _dragging ||
                    _resizing
                )
                {
                    _dragging = false;

                    _resizing = false;

                    _resizeMode =
                        ResizeMode.None;

                    SaveWindowSettings();

                    currentEvent.Use();
                }
            }
        }

        private ResizeMode GetResizeMode(
            Vector2 mousePosition)
        {
            bool nearLeft =
                Mathf.Abs(
                    mousePosition.x -
                    _windowRect.x
                ) <= ResizeBorder;

            bool nearRight =
                Mathf.Abs(
                    mousePosition.x -
                    (
                        _windowRect.x +
                        _windowRect.width
                    )
                ) <= ResizeBorder;

            bool nearTop =
                Mathf.Abs(
                    mousePosition.y -
                    _windowRect.y
                ) <= ResizeBorder;

            bool nearBottom =
                Mathf.Abs(
                    mousePosition.y -
                    (
                        _windowRect.y +
                        _windowRect.height
                    )
                ) <= ResizeBorder;

            if (nearLeft && nearTop)
            {
                return ResizeMode.TopLeft;
            }

            if (nearRight && nearTop)
            {
                return ResizeMode.TopRight;
            }

            if (nearLeft && nearBottom)
            {
                return ResizeMode.BottomLeft;
            }

            if (nearRight && nearBottom)
            {
                return ResizeMode.BottomRight;
            }

            if (nearLeft)
            {
                return ResizeMode.Left;
            }

            if (nearRight)
            {
                return ResizeMode.Right;
            }

            if (nearTop)
            {
                return ResizeMode.Top;
            }

            if (nearBottom)
            {
                return ResizeMode.Bottom;
            }

            return ResizeMode.None;
        }

        private void BeginDrag(
            Vector2 mousePosition)
        {
            _dragging = true;

            _dragOffset =
                mousePosition -
                new Vector2(
                    _windowRect.x,
                    _windowRect.y
                );
        }

        private void UpdateDrag(
            Vector2 mousePosition)
        {
            float newX =
                mousePosition.x -
                _dragOffset.x;

            float newY =
                mousePosition.y -
                _dragOffset.y;

            float minimumVisible =
                40f;

            newX =
                Mathf.Clamp(
                    newX,
                    -_windowRect.width +
                    minimumVisible,
                    Screen.width -
                    minimumVisible
                );

            newY =
                Mathf.Clamp(
                    newY,
                    -_windowRect.height +
                    minimumVisible,
                    Screen.height -
                    minimumVisible
                );

            _windowRect.x =
                newX;

            _windowRect.y =
                newY;
        }

        private void BeginResize(
            ResizeMode resizeMode,
            Vector2 mousePosition)
        {
            _resizing = true;

            _resizeMode =
                resizeMode;

            _resizeStartMouse =
                mousePosition;

            _resizeStartRect =
                _windowRect;
        }

        private void UpdateResize(
            Vector2 mousePosition)
        {
            Vector2 delta =
                mousePosition -
                _resizeStartMouse;

            float left =
                _resizeStartRect.x;

            float right =
                _resizeStartRect.x +
                _resizeStartRect.width;

            float top =
                _resizeStartRect.y;

            float bottom =
                _resizeStartRect.y +
                _resizeStartRect.height;

            switch (_resizeMode)
            {
                case ResizeMode.Left:
                    left =
                        _resizeStartRect.x +
                        delta.x;
                    break;

                case ResizeMode.Right:
                    right =
                        _resizeStartRect.x +
                        _resizeStartRect.width +
                        delta.x;
                    break;

                case ResizeMode.Top:
                    top =
                        _resizeStartRect.y +
                        delta.y;
                    break;

                case ResizeMode.Bottom:
                    bottom =
                        _resizeStartRect.y +
                        _resizeStartRect.height +
                        delta.y;
                    break;

                case ResizeMode.TopLeft:
                    left =
                        _resizeStartRect.x +
                        delta.x;

                    top =
                        _resizeStartRect.y +
                        delta.y;
                    break;

                case ResizeMode.TopRight:
                    right =
                        _resizeStartRect.x +
                        _resizeStartRect.width +
                        delta.x;

                    top =
                        _resizeStartRect.y +
                        delta.y;
                    break;

                case ResizeMode.BottomLeft:
                    left =
                        _resizeStartRect.x +
                        delta.x;

                    bottom =
                        _resizeStartRect.y +
                        _resizeStartRect.height +
                        delta.y;
                    break;

                case ResizeMode.BottomRight:
                    right =
                        _resizeStartRect.x +
                        _resizeStartRect.width +
                        delta.x;

                    bottom =
                        _resizeStartRect.y +
                        _resizeStartRect.height +
                        delta.y;
                    break;
            }

            if (
                right - left <
                MinimumWidth
            )
            {
                if (
                    _resizeMode ==
                        ResizeMode.Left ||
                    _resizeMode ==
                        ResizeMode.TopLeft ||
                    _resizeMode ==
                        ResizeMode.BottomLeft
                )
                {
                    left =
                        right -
                        MinimumWidth;
                }
                else
                {
                    right =
                        left +
                        MinimumWidth;
                }
            }

            if (
                bottom - top <
                MinimumHeight
            )
            {
                if (
                    _resizeMode ==
                        ResizeMode.Top ||
                    _resizeMode ==
                        ResizeMode.TopLeft ||
                    _resizeMode ==
                        ResizeMode.TopRight
                )
                {
                    top =
                        bottom -
                        MinimumHeight;
                }
                else
                {
                    bottom =
                        top +
                        MinimumHeight;
                }
            }

            _windowRect =
                Rect.MinMaxRect(
                    left,
                    top,
                    right,
                    bottom
                );

            float maximumWidth =
                Mathf.Max(
                    MinimumWidth,
                    Screen.width
                );

            float maximumHeight =
                Mathf.Max(
                    MinimumHeight,
                    Screen.height
                );

            if (
                _windowRect.width >
                maximumWidth
            )
            {
                _windowRect.width =
                    maximumWidth;
            }

            if (
                _windowRect.height >
                maximumHeight
            )
            {
                _windowRect.height =
                    maximumHeight;
            }
        }

        private void InitializeWindowRect()
        {
            if (_windowRectInitialized)
            {
                return;
            }

            if (TimerMod.GlobalSettings == null)
            {
                TimerMod.GlobalSettings =
                    new TimerGlobalSettings();
            }

            float width =
                Mathf.Max(
                    MinimumWidth,
                    TimerMod.GlobalSettings.TimerWidth
                );

            float height =
                Mathf.Max(
                    MinimumHeight,
                    TimerMod.GlobalSettings.TimerHeight
                );

            float x =
                TimerMod.GlobalSettings.TimerX;

            float y =
                TimerMod.GlobalSettings.TimerY;

            if (x < 0f)
            {
                x =
                    Screen.width / 2f -
                    width / 2f;
            }

            if (y < 0f)
            {
                y = 30f;
            }

            _windowRect =
                new Rect(
                    x,
                    y,
                    width,
                    height
                );

            ClampWindowToScreen();

            _windowRectInitialized = true;
        }

        private void ClampWindowToScreen()
        {
            float minimumVisible =
                40f;

            _windowRect.x =
                Mathf.Clamp(
                    _windowRect.x,
                    -_windowRect.width +
                    minimumVisible,
                    Screen.width -
                    minimumVisible
                );

            _windowRect.y =
                Mathf.Clamp(
                    _windowRect.y,
                    -_windowRect.height +
                    minimumVisible,
                    Screen.height -
                    minimumVisible
                );

            _windowRect.width =
                Mathf.Clamp(
                    _windowRect.width,
                    MinimumWidth,
                    Mathf.Max(
                        MinimumWidth,
                        Screen.width
                    )
                );

            _windowRect.height =
                Mathf.Clamp(
                    _windowRect.height,
                    MinimumHeight,
                    Mathf.Max(
                        MinimumHeight,
                        Screen.height
                    )
                );
        }

        private void SaveWindowSettings()
        {
            if (TimerMod.GlobalSettings == null)
            {
                return;
            }

            TimerMod.GlobalSettings.TimerX =
                _windowRect.x;

            TimerMod.GlobalSettings.TimerY =
                _windowRect.y;

            TimerMod.GlobalSettings.TimerWidth =
                _windowRect.width;

            TimerMod.GlobalSettings.TimerHeight =
                _windowRect.height;
        }

        private void EnsureStyles()
        {
            if (_timerStyle != null)
            {
                ApplyFont();
                return;
            }

            _timerStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            _timerStyle.fontSize =
                36;

            _timerStyle.fontStyle =
                UnityEngine.FontStyle.Bold;

            _timerStyle.alignment =
                TextAnchor.MiddleCenter;

            _windowStyle =
                new GUIStyle(
                    GUI.skin.box
                );

            _stateStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            _stateStyle.fontSize =
                14;

            _stateStyle.alignment =
                TextAnchor.MiddleCenter;

            _editStyle =
                new GUIStyle(
                    GUI.skin.label
                );

            _editStyle.fontSize =
                12;

            _editStyle.alignment =
                TextAnchor.MiddleCenter;

            ApplyFont();

            UpdateColor();
        }

        private void ApplyFont()
        {
            if (_timerStyle == null)
            {
                return;
            }

            if (_hollowKnightFont == null)
            {
                _hollowKnightFont =
                    FindHollowKnightFont();
            }

            if (_hollowKnightFont == null)
            {
                return;
            }

            _timerStyle.font =
                _hollowKnightFont;

            _stateStyle.font =
                _hollowKnightFont;

            _editStyle.font =
                _hollowKnightFont;
        }

        private static Font FindHollowKnightFont()
        {
            Font regular =
                null;

            Font bold =
                null;

            Font[] fonts =
                Resources.FindObjectsOfTypeAll<Font>();

            foreach (Font font in fonts)
            {
                if (font == null)
                {
                    continue;
                }

                if (
                    font.name ==
                    "TrajanPro-Regular"
                )
                {
                    regular = font;
                }

                if (
                    font.name ==
                    "TrajanPro-Bold"
                )
                {
                    bold = font;
                }
            }

            return
                regular ??
                bold ??
                GUI.skin.font;
        }

        private void UpdateColor()
        {
            int index =
                TimerMod.GlobalSettings != null
                    ? TimerMod.GlobalSettings.TimerColor
                    : 0;

            index =
                Mathf.Clamp(
                    index,
                    0,
                    7
                );

            if (_lastColorIndex == index)
            {
                return;
            }

            _lastColorIndex =
                index;

            _currentTimerColor =
                GetTimerColor(index);

            if (_timerStyle != null)
            {
                _timerStyle.normal.textColor =
                    _currentTimerColor;
            }

            if (_stateStyle != null)
            {
                _stateStyle.normal.textColor =
                    _currentTimerColor;
            }

            if (_editStyle != null)
            {
                _editStyle.normal.textColor =
                    _currentTimerColor;
            }
        }

        public static Color GetTimerColor(
            int index)
        {
            switch (index)
            {
                case 0:
                    return new Color(
                        0.78f,
                        0.78f,
                        0.76f
                    );

                case 1:
                    return new Color(
                        0.48f,
                        0.56f,
                        0.65f
                    );

                case 2:
                    return new Color(
                        0.49f,
                        0.58f,
                        0.50f
                    );

                case 3:
                    return new Color(
                        0.46f,
                        0.58f,
                        0.62f
                    );

                case 4:
                    return new Color(
                        0.55f,
                        0.49f,
                        0.62f
                    );

                case 5:
                    return new Color(
                        0.67f,
                        0.59f,
                        0.43f
                    );

                case 6:
                    return new Color(
                        0.64f,
                        0.49f,
                        0.52f
                    );

                case 7:
                    return new Color(
                        0.50f,
                        0.62f,
                        0.59f
                    );

                default:
                    return new Color(
                        0.78f,
                        0.78f,
                        0.76f
                    );
            }
        }

        private static string FormatMilliseconds(
            long milliseconds)
        {
            milliseconds =
                Math.Max(
                    0L,
                    milliseconds
                );

            long totalSeconds =
                milliseconds / 1000L;

            long hours =
                totalSeconds / 3600L;

            long minutes =
                (totalSeconds % 3600L) /
                60L;

            long seconds =
                totalSeconds % 60L;

            if (hours > 0)
            {
                return string.Format(
                    "{0:00}:{1:00}:{2:00}",
                    hours,
                    minutes,
                    seconds
                );
            }

            return string.Format(
                "{0:00}:{1:00}",
                minutes,
                seconds
            );
        }
    }
}