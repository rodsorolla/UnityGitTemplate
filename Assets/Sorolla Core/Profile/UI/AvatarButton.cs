using UnityEngine;
using UnityEngine.UI;

namespace Sorolla.Profile.UI
{
    /// <summary>
    /// One avatar tile in the profile picker. Holds a palette of soft background colors and
    /// tints the BG image with one of them via <see cref="ApplyColor"/> (cycled by index) so the
    /// grid shows a pleasant spread of muted tones. The avatar art itself lives on a separate
    /// child image; this component only owns the background tint.
    /// </summary>
    public class AvatarButton : MonoBehaviour
    {
        [Tooltip("The BG image tinted from the palette below.")]
        [SerializeField] private Image _background;

        [Tooltip("Soft background colors. ApplyColor picks one (cycled by index). " +
                 "Kept muted on purpose — no pure/neon tones.")]
        [SerializeField] private Color[] _backgroundPalette =
        {
            new Color(0.486f, 0.647f, 0.769f), // dusty blue
            new Color(0.435f, 0.702f, 0.659f), // muted teal
            new Color(0.612f, 0.749f, 0.541f), // sage green
            new Color(0.659f, 0.608f, 0.788f), // soft lavender
            new Color(0.851f, 0.761f, 0.604f), // warm sand
            new Color(0.831f, 0.604f, 0.541f), // dusty coral
            new Color(0.522f, 0.576f, 0.722f), // slate periwinkle
            new Color(0.788f, 0.608f, 0.659f), // muted rose
        };

        /// <summary>
        /// Tints the BG image with a palette color chosen by index (wraps around the palette).
        /// No-ops safely if the background or palette isn't wired.
        /// </summary>
        public void ApplyColor(int index)
        {
            if (_background == null || _backgroundPalette == null || _backgroundPalette.Length == 0) return;
            int i = ((index % _backgroundPalette.Length) + _backgroundPalette.Length) % _backgroundPalette.Length;
            _background.color = _backgroundPalette[i];
        }
    }
}
