using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace MyMotherToldMe.Game.Camera
{
    /// <summary>
    /// This component needs to be added as a child of the object it needs to follow.
    /// Also, if bounds are set, limits are correctly set to clamp  the viewport within given range.
    /// Best way to set its bounds is passing the extents of a desired <see cref="Area2D"/>.
    /// </summary>
    public partial class GameCamera : Camera2D
    {
        /// <summary>
        /// true when a focus is being processed
        /// </summary>
        private bool _tweening;
    
        /// <summary>
        /// this queue keeps track of all the focus requests arrived and process them with a FIFO policy
        /// </summary>
        private readonly Queue<QueuedZoomRequest> _zoomRequestsQueue = new();

        /// <summary>
        /// If set will update limits to clamp the viewport within it.
        /// </summary>
        public Rect2 Bounds
        {
            get => _bounds;
            set
            {
                _bounds = value;
                LimitLeft = (int) Bounds.Position.X;
                LimitRight = (int) (Bounds.Position.X + Bounds.Size.X);
                LimitTop = (int) Bounds.Position.Y;
                LimitBottom = (int) (Bounds.Position.Y + Bounds.Size.Y);
                ClampToBounds();
            }
        }

        private Rect2 _viewportRect;
        private Rect2 _bounds;

        public override void _Ready()
        {
            _viewportRect = GetViewportRect();
            Zoom = new Vector2(6, 6);
        }

        /// <summary>
        /// public API to request a camera focus (or de-focus) on a specific object
        /// </summary>
        public void EnqueueZoomRequest(Node2D target, float zoomValue, float duration)
        {
            GD.Print($"{nameof(EnqueueZoomRequest)} was called.");
            _zoomRequestsQueue.Enqueue(new QueuedZoomRequest
            {
                Node = target,
                ZoomValue = zoomValue,
                Duration = duration
            });
        }

        public async Task RunDequeueZooms()
        {
            GD.Print($"{nameof(RunDequeueZooms)} was called [{_zoomRequestsQueue.Count} requests].");
            _tweening = true;
            while (_zoomRequestsQueue.Any())
            {
                await DequeueZoomRequest();
                await Task.Delay(TimeSpan.FromSeconds(0.5f));
            }

            _tweening = false;
        }

        private async Task DequeueZoomRequest()
        {
            var dequeuedElement = _zoomRequestsQueue.Dequeue();
            await ZoomDetail(dequeuedElement.Node, dequeuedElement.ZoomValue, dequeuedElement.Duration);
        }

        private async Task ZoomDetail(Node2D target, float targetValue, float duration)
        {
            GetParent().RemoveChild(this);
            target.AddChild(this);

            var tweener = CreateTween();
            tweener.TweenProperty(this, "zoom", new Vector2(targetValue, targetValue), duration);
            tweener.Play();
            
            while (tweener.IsRunning())
            {
                var delay = GetProcessDeltaTime();
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        private bool ReachedMaxZoom()
        {
            return ReachedHorizontalLimit() || ReachedVerticalLimit();
        }

        /// <summary>
        /// Consider weather the viewport extents reached both left and right limits.
        /// </summary>
        private bool ReachedHorizontalLimit()
        {
            return ReachedLeftLimit() && ReachedRightLimit();
        }

        /// <summary>
        /// Consider weather the viewport extents reached both top and bottom limits.
        /// </summary>
        private bool ReachedVerticalLimit()
        {
            return ReachedTopLimit() && ReachedBottomLimit();
        }

        private bool ReachedLeftLimit() => _viewportRect.Position.X * Zoom.X <= LimitLeft;
        private bool ReachedRightLimit() => _viewportRect.End.X * Zoom.X >= LimitRight;
        private bool ReachedTopLimit() => _viewportRect.Position.Y * Zoom.Y <= LimitTop;
        private bool ReachedBottomLimit() => _viewportRect.End.Y * Zoom.Y >= LimitBottom;

        /// <summary>
        /// Used to know if the zoom will become 0 or negative after subtracting given amount.
        /// </summary>
        /// <param name="deduction">amount to be subtracted</param>
        private bool WillReachInnerLimit(float deduction)
        {
            return Zoom.X - deduction <= 0.05f;
        }

        private void UpdateZoom(float value)
        {
            if (value < 0 && WillReachInnerLimit(value)) return;
            if (value > 0 && ReachedMaxZoom()) return;
            
            Zoom += new Vector2(value, value);
        }

        private void ClampToBounds()
        {
            var viewportSize = _viewportRect.Size;
            
            // Calculate the minimum zoom value needed to fit the viewport within the bounds horizontally
            var minZoomX = Bounds.Size.X / viewportSize.X;

            // Calculate the minimum zoom value needed to fit the viewport within the bounds vertically
            var minZoomY = Bounds.Size.Y / viewportSize.Y;

            // Set the camera zoom to the minimum of the two values
            var zoomValue = Mathf.Min(minZoomX, minZoomY);
            Zoom = new Vector2(zoomValue, zoomValue);
        }

        private class QueuedZoomRequest
        {
            public Node2D Node;
            public float ZoomValue;
            public float Duration;
        }
    }
}
