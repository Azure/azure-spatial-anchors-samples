using UnityEngine;

namespace Unity.UIElements.Runtime
{
    /// <summary>
    /// The Panel Scaler component is used for controlling the overall scale and pixel density of elements
    /// in the Panel.
    /// </summary>
    [AddComponentMenu("UIElements/Panel Scaler")]
    [DisallowMultipleComponent]
    public class PanelScaler : MonoBehaviour
    {
        // TODO we do not support sprites and have no notion of pixelsPerUnit at the moment

        /// <summary>
        /// Determines how elements in the Panel are scaled.
        /// </summary>
        public enum ScaleMode
        {
            /// <summary>
            /// Makes elements retain the same size in pixels regardless of screen size.
            /// </summary>
            ConstantPixelSize,
            /// <summary>
            /// Makes elements retain the same physical size (displayed size) regardless of screen size and resolution.
            /// </summary>
            ConstantPhysicalSize,
            /// <summary>
            /// Makes elements bigger the bigger the screen is.
            /// </summary>
            ScaleWithScreenSize
        }

        /// <summary>
        /// A mode used to scale the Panel area if the aspect ratio of the current resolution
        /// does not fit the reference resolution.
        /// </summary>
        public enum ScreenMatchMode
        {
            /// <summary>
            /// Scale the Panel area with the width as reference, the height as reference, or something in between.
            /// </summary>
            MatchWidthOrHeight,
            /// <summary>
            /// Crop the Panel area either horizontally or vertically, so the size of the Panel
            /// will never be larger than the reference.
            /// </summary>
            Shrink,
            /// <summary>
            /// Expand the Panel area either horizontally or vertically, so the size of the Panel
            /// will never be smaller than the reference.
            /// </summary>
            Expand
        }

        private interface IScaleModeImpl
        {
            float ComputeScalingFactor(Vector2 size);
        }

        [System.Serializable]
        internal struct ConstantPixelSizeScaler : IScaleModeImpl
        {
            [Delayed] public float scaleFactor;

            float IScaleModeImpl.ComputeScalingFactor(Vector2 size)
            {
                return scaleFactor == 0 ? 0 : 1.0f / scaleFactor;
            }
        }

        [System.Serializable]
        internal struct ConstantPhysicalSizeScaler : IScaleModeImpl
        {
            [Delayed] public float referenceDpi;
            [Delayed] public float fallbackDpi;

            internal float ComputeScalingFactor(Vector2 size, float currentDpi)
            {
                var dpi = currentDpi == 0 ? fallbackDpi : currentDpi;
                return dpi == 0 ? 0 : referenceDpi / dpi;
            }

            float IScaleModeImpl.ComputeScalingFactor(Vector2 size)
            {
                return ComputeScalingFactor(size, Screen.dpi);
            }
        }

        [System.Serializable]
        internal struct ScaleWithScreenSizeScaler : IScaleModeImpl
        {
            /// <summary>
            /// The resolution the UI is designed for. If the screen resolution is larger, the UI will be scaled up,
            /// and if it’s smaller, the UI will be scaled down.
            /// </summary>
            [Delayed] public Vector2Int referenceResolution;
            
            /// <summary>
            /// A mode used to scale the Panel area if the aspect ratio of the current resolution
            /// does not fit the reference resolution.
            /// </summary>
            public ScreenMatchMode screenMatchMode;
            
            /// <summary>
            /// Determines if the scaling is using the width or height as reference, or a mix in between.
            /// </summary>
            public float match;

            float IScaleModeImpl.ComputeScalingFactor(Vector2 size)
            {
                if (referenceResolution.x * referenceResolution.y == 0)
                    return 0;
                
                var refSize = (Vector2) referenceResolution;
                var sizeRatio = new Vector2(size.x / refSize.x, size.y / refSize.y);

                var denominator = 0f;
                switch (screenMatchMode)
                {
                    case ScreenMatchMode.Expand:
                        denominator = Mathf.Min(sizeRatio.x, sizeRatio.y);
                        break;
                    case ScreenMatchMode.Shrink:
                        denominator = Mathf.Max(sizeRatio.x, sizeRatio.y);
                        break;
                    default: // ScreenMatchMode.MatchWidthOrHeight:
                        var widthHeightRatio = Mathf.Clamp01(match);
                        denominator = Mathf.Lerp(sizeRatio.x, sizeRatio.y, widthHeightRatio);
                        break;
                }

                return denominator == 0 ? 0 : 1.0f / denominator;
            }
        }

        [SerializeField] internal ConstantPixelSizeScaler constantPixelSizeScaler = new ConstantPixelSizeScaler()
        {
            scaleFactor = 1
        };

        [SerializeField] internal ConstantPhysicalSizeScaler constantPhysicalSizeScaler = new ConstantPhysicalSizeScaler() 
        {
            referenceDpi = 96,
            fallbackDpi = 96
        };

        [SerializeField]
        internal ScaleWithScreenSizeScaler scaleWithScreenSizeScaler = new ScaleWithScreenSizeScaler()
        {
            referenceResolution = new Vector2Int(1200, 800),
            screenMatchMode = ScreenMatchMode.MatchWidthOrHeight,
            match = 0
        };

        /// <summary>
        /// Determines how elements in the Panel are scaled.
        /// </summary>
        public ScaleMode scaleMode = ScaleMode.ConstantPixelSize;

        IScaleModeImpl impl
        {
            get
            {
                switch (scaleMode)
                {
                    case ScaleMode.ConstantPixelSize: 
                        return constantPixelSizeScaler;
                    case ScaleMode.ConstantPhysicalSize: 
                        return constantPhysicalSizeScaler;
                    default: // ScaleMode.ScaleWithScreenSize:
                        return scaleWithScreenSizeScaler;
                }
            }
        }

        internal float ComputeScalingFactor(Vector2 size) { return impl.ComputeScalingFactor(size); }
    }
}