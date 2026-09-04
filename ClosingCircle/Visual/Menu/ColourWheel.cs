using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hue around the circumference, saturation with the radius, brightness on a slider beside it. A wheel rather
// than a triangle because the hit test is one conversion to polar, and a triangle's is not.
//
// The texture is generated the way GradientTexture already builds the wall's ramp, since the mod ships no
// images. Built once and shared: it never changes, only the marker moves.

namespace ClosingCircle.Visual.Menu
{
    public class ColourWheel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private const int Resolution = 192;

        private static Texture2D _face;
        private static Sprite _sprite;

        private RectTransform _rect;
        private RectTransform _marker;
        private Image _markerFill;

        private float _hue;
        private float _saturation = 1f;
        private float _value = 1f;

        // Raised on drag only. A change that came from the channel sliders must not echo back out, or the two
        // controls chase each other.
        public Action<Color> Picked;

        public static Texture2D Face()
        {
            if (_face != null) return _face;

            _face = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                name = GameFont.Mine + "Wheel",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float half = Resolution * 0.5f;

            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (distance > 1f)
                    {
                        _face.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float hue = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg / 360f, 1f);
                    Color colour = Color.HSVToRGB(hue, distance, 1f);

                    // Feathered at the rim so the circle has no staircase on it.
                    colour.a = Mathf.Clamp01((1f - distance) * Resolution * 0.25f);
                    _face.SetPixel(x, y, colour);
                }
            }

            _face.Apply();
            return _face;
        }

        // Cached alongside the texture: the panel rebuilds its categories on every push, and creating a sprite
        // per rebuild leaked one each time.
        public static Sprite Sprite()
        {
            if (_sprite != null) return _sprite;

            Texture2D face = Face();
            _sprite = UnityEngine.Sprite.Create(face, new Rect(0f, 0f, face.width, face.height),
                                                new Vector2(0.5f, 0.5f));
            return _sprite;
        }

        public void Bind(RectTransform rect, RectTransform marker, Image markerFill)
        {
            _rect = rect;
            _marker = marker;
            _markerFill = markerFill;
        }

        // Moves the marker to match a colour that came from somewhere else, without raising Picked.
        public void Show(Color colour)
        {
            Color.RGBToHSV(colour, out _hue, out _saturation, out _value);
            Place(colour);
        }

        public void SetValue(float value)
        {
            _value = Mathf.Clamp01(value);
            Emit();
        }

        public float Value => _value;

        public void OnPointerDown(PointerEventData eventData) => Take(eventData);

        public void OnDrag(PointerEventData eventData) => Take(eventData);

        private void Take(PointerEventData eventData)
        {
            if (_rect == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out local)) return;

            float radius = _rect.rect.width * 0.5f;
            if (radius <= 0f) return;

            Vector2 offset = local / radius;
            float distance = Mathf.Min(offset.magnitude, 1f);

            _hue = Mathf.Repeat(Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg / 360f, 1f);
            _saturation = distance;

            Emit();
        }

        private void Emit()
        {
            Color colour = Color.HSVToRGB(_hue, _saturation, _value);
            Place(colour);

            if (Picked != null) Picked(colour);
        }

        private void Place(Color colour)
        {
            if (_marker == null || _rect == null) return;

            float radius = _rect.rect.width * 0.5f;
            float angle = _hue * Mathf.PI * 2f;

            _marker.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (_saturation * radius);

            if (_markerFill != null) _markerFill.color = colour;
        }
    }
}
