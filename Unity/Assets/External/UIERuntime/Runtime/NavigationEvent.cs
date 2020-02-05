using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// Interface for all navigation events.
    /// </summary>
    public interface INavigationEvent
    {
    }

    /// <summary>
    /// Navigation events abstract base class.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NavigationEventBase<T> : EventBase<T>, INavigationEvent where T : NavigationEventBase<T>, new()
    {
    }

    /// <summary>
    /// Event typically sent when the user presses the D-pad, moves a joystick or presses the arrow keys.
    /// </summary>
    public class NavigationMoveEvent : NavigationEventBase<NavigationMoveEvent>
    {
        /// <summary>
        /// Move event direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// No specific direction.
            /// </summary>
            None,
            /// <summary>
            /// Lefto.
            /// </summary>
            Left,
            /// <summary>
            /// Upo.
            /// </summary>
            Up,
            /// <summary>
            /// Righto.
            /// </summary>
            Right,
            /// <summary>
            /// Downo.
            /// </summary>
            Down
        }

        internal static Direction DetermineMoveDirection(float x, float y, float deadZone = 0.6f)
        {
            // if vector is too small... just return
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return Direction.None;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                if (x > 0)
                    return Direction.Right;
                return Direction.Left;
            }
            else
            {
                if (y > 0)
                    return Direction.Up;
                return Direction.Down;
            }
        }

        /// <summary>
        /// The direction of the navigation.
        /// </summary>
        public Direction direction { get; private set; }
        
        /// <summary>
        /// The move vector.
        /// </summary>
        public Vector2 move { get; private set; }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values.
        /// Use this function instead of creating new events.
        /// Events obtained from this method should be released back to the pool using Dispose().
        /// </summary>
        /// <param name="moveVector">The move vector.</param>
        /// <returns>An initialized navigation event.</returns>
        public static NavigationMoveEvent GetPooled(Vector2 moveVector)
        {
            NavigationMoveEvent e = GetPooled();
            e.direction = DetermineMoveDirection(moveVector.x, moveVector.y);
            e.move = moveVector;
            return e;
        }

        /// <summary>
        /// Initialize the event members.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            direction = Direction.None;
            move = Vector2.zero;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NavigationMoveEvent()
        {
            Init();
        }
    }

    /// <summary>
    /// Event sent when the user presses the cancel button.
    /// </summary>
    public class NavigationCancelEvent : NavigationEventBase<NavigationCancelEvent>
    {
    }

    /// <summary>
    /// Event sent when the user presses the submit button.
    /// </summary>
    public class NavigationSubmitEvent : NavigationEventBase<NavigationSubmitEvent>
    {
    }
}
