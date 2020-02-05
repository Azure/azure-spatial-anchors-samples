using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// Handles input and sending events to UIElements Panel.
    /// </summary>
    [AddComponentMenu("UIElements/Event System")]
    public class UIElementsEventSystem : MonoBehaviour
    {
        [FormerlySerializedAs("m_sendNavigationEvents")] 
        [SerializeField] private bool m_SendNavigationEvents = true;
        [SerializeField] private bool m_SendIMGUIEvents = true;
        [SerializeField] private bool m_SendInputEvents = true;

        /// <summary>
        /// True if the EventSystem allows navigation events (move / submit / cancel).
        /// </summary>
        public bool sendNavigationEvents
        {
            get { return m_SendNavigationEvents; }
            set { m_SendNavigationEvents = value; }
        }

        /// <summary>
        /// True if the EventSystem forwards IMGUI events (mouse and keyboard events).
        /// </summary>
        public bool sendIMGUIEvents
        {
            get { return m_SendIMGUIEvents; }
            set { m_SendIMGUIEvents = value; }
        }

        /// <summary>
        /// True if the EventSystem creates events from changes in the Input state (mouse and touch events).
        /// </summary>
        public bool sendInputEvents
        {
            get { return m_SendInputEvents; }
            set { m_SendInputEvents = value; }
        }

        [SerializeField] private string m_HorizontalAxis = "Horizontal";
        [SerializeField] private string m_VerticalAxis = "Vertical";
        [SerializeField] private string m_SubmitButton = "Submit";
        [SerializeField] private string m_CancelButton = "Cancel";
        [SerializeField] private float m_InputActionsPerSecond = 10;
        [SerializeField] private float m_RepeatDelay = 0.5f;

        /// <summary>
        /// True if the application has the focus. Events are sent oinly if this flag is true.
        /// </summary>
        public bool isAppFocused { get; private set; } = true;

        private Event m_Event = new Event();

        /// <summary>
        /// Used to override the default input, when sending InputEvents or NavigationEvents.
        /// </summary>
        /// <remarks>
        /// With this it is possible to bypass the Input system with your own. This can be used to feed
        /// fake input to the event system.
        /// </remarks>
        public InputWrapper inputOverride { get; set; }

        private InputWrapper m_DefaultInput;

        internal InputWrapper input
        {
            get
            {
                if (inputOverride != null)
                    return inputOverride;

                if (m_DefaultInput == null)
                {
                    var inputs = GetComponents<InputWrapper>();
                    foreach (var baseInput in inputs)
                    {
                        // We dont want to use any classes that derive from BaseInput for default.
                        if (baseInput != null && baseInput.GetType() == typeof(InputWrapper))
                        {
                            m_DefaultInput = baseInput;
                            break;
                        }
                    }

                    if (m_DefaultInput == null)
                        m_DefaultInput = gameObject.AddComponent<InputWrapper>();
                }

                return m_DefaultInput;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected UIElementsEventSystem()
        {
        }

        // We used to have a PanelManager,
        // It would register panels and return the last registered one as the focused panel,
        // We removed that manager but the focused panel notion needs some work
        PanelRenderer m_PanelRenderer = null;

        private PanelRenderer focusedPanelRenderer
        {
            get
            {
                if (m_PanelRenderer == null)
                    m_PanelRenderer = GetComponent<PanelRenderer>();
                return m_PanelRenderer;
            }
        }
        
        private IPanel focusedPanel { get { return focusedPanelRenderer?.panel; } }

        private bool ShouldIgnoreEventsOnAppNotFocused()
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
#if UNITY_EDITOR
                    if (UnityEditor.EditorApplication.isRemoteConnected)
                        return false;
#endif
                    return true;
                default:
                    return false;
            }
        }

        private Vector2 m_LastMousePosition;

        void Update()
        {
            if (focusedPanel == null)
                return;

            if (focusedPanel != null && !isAppFocused && ShouldIgnoreEventsOnAppNotFocused())
                return;

            if (sendIMGUIEvents)
            {
                while (Event.PopEvent(m_Event))
                {
                    if (m_Event.type == EventType.Repaint)
                        continue;

                    var panelPosition = Vector2.zero;
                    var panelDelta = Vector2.zero;

                    if (ScreenToPanel(focusedPanelRenderer, m_Event.mousePosition, m_Event.delta, out panelPosition, out panelDelta))
                    {
                        m_Event.mousePosition = panelPosition;
                        m_Event.delta = panelDelta;
                        EventBase evt = InternalBridge.CreateEvent(m_Event);
                        focusedPanel.visualTree.SendEvent(evt);
                    }
                }
            }

            if (sendInputEvents)
            {
                if (sendNavigationEvents)
                {
                    bool sendNavigationMove = ShouldSendMoveFromInput();

                    if (sendNavigationMove)
                    {
                        using (EventBase evt = NavigationMoveEvent.GetPooled(GetRawMoveVector()))
                        {
                            focusedPanel.visualTree.SendEvent(evt);
                        }
                    }

                    if (input.GetButtonDown(m_SubmitButton))
                    {
                        using (EventBase evt = NavigationSubmitEvent.GetPooled())
                        {
                            focusedPanel.visualTree.SendEvent(evt);
                        }
                    }

                    if (input.GetButtonDown(m_CancelButton))
                    {
                        using (EventBase evt = NavigationCancelEvent.GetPooled())
                        {
                            focusedPanel.visualTree.SendEvent(evt);
                        }
                    }
                }

                if (!ProcessTouchEvents() && input.mousePresent)
                {
                    ProcessMouseEvents();
                }
            }
        }

        private EventBase MakeTouchEvent(Touch touch, EventModifiers modifiers)
        {
            // Flip Y Coordinates.
            touch.position = new Vector2(touch.position.x, Screen.height - touch.position.y);
            touch.rawPosition = new Vector2(touch.rawPosition.x, Screen.height - touch.rawPosition.y);
            touch.deltaPosition = new Vector2(touch.deltaPosition.x, Screen.height - touch.deltaPosition.y);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    return PointerDownEvent.GetPooled(touch, modifiers);
                case TouchPhase.Moved:
                    return PointerMoveEvent.GetPooled(touch, modifiers);
                case TouchPhase.Stationary:
                    return PointerStationaryEvent.GetPooled(touch, modifiers);
                case TouchPhase.Ended:
                    return PointerUpEvent.GetPooled(touch, modifiers);
                case TouchPhase.Canceled:
                    return PointerCancelEvent.GetPooled(touch, modifiers);
                default:
                    return null;
            }
        }

        private bool ProcessTouchEvents()
        {
            for (int i = 0; i < input.touchCount; ++i)
            {
                Touch touch = input.GetTouch(i);

                if (touch.type == TouchType.Indirect)
                    continue;

                if (ScreenToPanel(focusedPanelRenderer, ref touch))
                {
                    using (EventBase evt = MakeTouchEvent(touch, EventModifiers.None))
                    {
                        focusedPanel.visualTree.SendEvent(evt);
                    }
                }                                                                                                   
            }

            return input.touchCount > 0;
        }

        private void ProcessMouseEvents()
        {
            Vector2 pos = new Vector2(input.mousePosition.x, Screen.height - input.mousePosition.y);
            if (pos != m_LastMousePosition)
            {
                m_LastMousePosition = pos;

                m_Event.type = EventType.MouseMove;

                if (ScreenToPanel(focusedPanelRenderer, ref m_Event, pos, m_LastMousePosition))
                {
                    m_Event.button = input.GetMouseButtonDown(0) ? 0 :
                        input.GetMouseButtonDown(1) ? 1 :
                        input.GetMouseButtonDown(2) ? 2 : 0;
                    m_Event.modifiers = EventModifiers.None;
                    m_Event.pressure = 0;
                    m_Event.clickCount = 0;
                    m_Event.character = default(char);
                    m_Event.keyCode = KeyCode.None;
                    /* FIXME use camera display id */
                    m_Event.displayIndex = 0;
                    m_Event.commandName = String.Empty;

                    EventBase evt = InternalBridge.CreateEvent(m_Event);
                    focusedPanel.visualTree.SendEvent(evt);
                }
            }

            for (var i = 0; i < 3; i++)
            {
                if (input.GetMouseButtonDown(i))
                {
                    m_Event.type = EventType.MouseDown;

                    if (ScreenToPanel(focusedPanelRenderer, ref m_Event, pos, m_LastMousePosition))
                    {
                        m_Event.button = i;
                        m_Event.modifiers = EventModifiers.None;
                        m_Event.pressure = 0;
                        m_Event.clickCount = 0;
                        m_Event.character = default(char);
                        m_Event.keyCode = KeyCode.None;
                        /* FIXME use camera display id */
                        m_Event.displayIndex = 0;
                        m_Event.commandName = String.Empty;

                        EventBase evt = InternalBridge.CreateEvent(m_Event);
                        focusedPanel.visualTree.SendEvent(evt);
                    }
                }
                
                if (input.GetMouseButtonUp(i))
                {
                    m_Event.type = EventType.MouseUp;

                    if (ScreenToPanel(focusedPanelRenderer, ref m_Event, pos, m_LastMousePosition))
                    {
                        m_Event.button = i;
                        m_Event.modifiers = EventModifiers.None;
                        m_Event.pressure = 0;
                        m_Event.clickCount = 0;
                        m_Event.character = default(char);
                        m_Event.keyCode = KeyCode.None;
                        /* FIXME use camera display id */
                        m_Event.displayIndex = 0;
                        m_Event.commandName = String.Empty;

                        EventBase evt = InternalBridge.CreateEvent(m_Event);
                        focusedPanel.visualTree.SendEvent(evt);
                    }
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            isAppFocused = hasFocus;
        }

        private Vector2 GetRawMoveVector()
        {
            Vector2 move = Vector2.zero;
            move.x = input.GetAxisRaw(m_HorizontalAxis);
            move.y = input.GetAxisRaw(m_VerticalAxis);

            if (input.GetButtonDown(m_HorizontalAxis))
            {
                if (move.x < 0)
                    move.x = -1f;
                if (move.x > 0)
                    move.x = 1f;
            }

            if (input.GetButtonDown(m_VerticalAxis))
            {
                if (move.y < 0)
                    move.y = -1f;
                if (move.y > 0)
                    move.y = 1f;
            }

            return move;
        }

        private int m_ConsecutiveMoveCount;
        private Vector2 m_LastMoveVector;
        private float m_PrevActionTime;

        private bool ShouldSendMoveFromInput()
        {
            float time = Time.unscaledTime;

            Vector2 movement = GetRawMoveVector();
            if (Mathf.Approximately(movement.x, 0f) && Mathf.Approximately(movement.y, 0f))
            {
                m_ConsecutiveMoveCount = 0;
                return false;
            }

            // If user pressed key again, always allow event
            bool allow = input.GetButtonDown(m_HorizontalAxis) || input.GetButtonDown(m_VerticalAxis);
            bool similarDir = (Vector2.Dot(movement, m_LastMoveVector) > 0);
            if (!allow)
            {
                // Otherwise, user held down key or axis.
                // If direction didn't change at least 90 degrees, wait for delay before allowing consecutive event.
                if (similarDir && m_ConsecutiveMoveCount == 1)
                    allow = (time > m_PrevActionTime + m_RepeatDelay);
                // If direction changed at least 90 degree, or we already had the delay, repeat at repeat rate.
                else
                    allow = (time > m_PrevActionTime + 1f / m_InputActionsPerSecond);
            }

            if (!allow)
                return false;

            // Debug.Log(m_ProcessingEvent.rawType + " axis:" + m_AllowAxisEvents + " value:" + "(" + x + "," + y + ")");
            var moveDirection = NavigationMoveEvent.DetermineMoveDirection(movement.x, movement.y);

            if (moveDirection != NavigationMoveEvent.Direction.None)
            {
                if (!similarDir)
                    m_ConsecutiveMoveCount = 0;
                m_ConsecutiveMoveCount++;
                m_PrevActionTime = time;
                m_LastMoveVector = movement;
            }
            else
            {
                m_ConsecutiveMoveCount = 0;
            }

            return moveDirection != NavigationMoveEvent.Direction.None;
        }
        
        static bool ScreenToPanel(PanelRenderer panelRenderer, ref Touch touch)
        {
            var panelPosition = Vector2.zero;
            var panelDelta = Vector2.zero;
            if (!ScreenToPanel(panelRenderer, touch.position, touch.deltaPosition,
                out panelPosition, out panelDelta))
                return false;
            touch.position = panelPosition;
            touch.deltaPosition = panelDelta;
            return true;
        }
        
        static bool ScreenToPanel(PanelRenderer panelRenderer, ref Event evt, Vector2 screenPosition, Vector2 lastScreenPosition)
        {
            var panelPosition = Vector2.zero;
            var panelDelta = Vector2.zero;
            if (!ScreenToPanel(panelRenderer, screenPosition, screenPosition - lastScreenPosition,
                out panelPosition, out panelDelta))
                return false;
            evt.mousePosition = panelPosition;
            evt.delta = panelDelta;
            return true;
        }

        static bool ScreenToPanel(PanelRenderer panelRenderer, Vector2 screenPosition, Vector2 screenDelta,
            out Vector2 panelPosition, out Vector2 panelDelta)
        {
            panelPosition = Vector2.zero;
            panelDelta = Vector2.zero;

            if (!panelRenderer.ScreenToPanel(screenPosition, out panelPosition))
                return false;
            var panelPrevPosition = Vector2.zero;
            if (panelRenderer.ScreenToPanel(screenPosition - screenDelta, out panelPrevPosition))
                panelDelta = panelPosition - panelPrevPosition;
            return true;
        }
    }
}