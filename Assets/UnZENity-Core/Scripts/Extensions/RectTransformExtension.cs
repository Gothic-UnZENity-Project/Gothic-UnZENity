using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace GUZ.Core.Extensions
{
    public static class RectTransformExtension
    {
        public enum AnchorPosition
        {
            DoubleStretch,
            TopRight,
            TopStretch,
            MiddleStretch,
            BottomRight
            // TODO - Add more once needed.
        }

        public static void SetAnchor(this RectTransform rt, AnchorPosition pos)
        {
            var anchorMin = pos switch
            {
                AnchorPosition.DoubleStretch => new Vector2(0, 0),
                AnchorPosition.TopRight => new Vector2(1, 1),
                AnchorPosition.TopStretch => new Vector2(0, 1),
                AnchorPosition.MiddleStretch => new Vector2(0.5f, 0.5f),
                AnchorPosition.BottomRight => new Vector2(1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(pos), pos, null)
            };

            var anchorMax = pos switch
            {
                AnchorPosition.DoubleStretch => new Vector2(1, 1),
                AnchorPosition.TopRight => new Vector2(1, 1),
                AnchorPosition.MiddleStretch => new Vector2(0.5f, 0.5f),
                AnchorPosition.TopStretch => new Vector2(1, 1),
                AnchorPosition.BottomRight => new Vector2(1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(pos), pos, null)
            };

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
        }
    }
}
