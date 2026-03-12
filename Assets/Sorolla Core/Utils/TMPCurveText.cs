using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Curves TMP_Text (UGUI) along an arc. Works at runtime and in editor.
    /// Applies the curve to this component's TMP_Text and all child TMP_Text components.
    /// </summary>
    [ExecuteAlways]
    public class TMPCurveText : MonoBehaviour
    {
        [Header("Curve Settings")]
        [SerializeField] private float _curveAngle = 30f;

        private readonly List<TMP_Text> _textComponents = new();

        private void OnEnable()
        {
            CollectTextComponents();
            SubscribeAll();
            ApplyCurveAll();
        }

        private void OnDisable()
        {
            UnsubscribeAll();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            CollectTextComponents();
            SubscribeAll();
            ApplyCurveAll();
        }

        private void OnTransformChildrenChanged()
        {
            CollectTextComponents();
            SubscribeAll();
            ApplyCurveAll();
        }

        /// <summary>
        /// The arc angle in degrees. Positive curves upward, negative curves downward.
        /// </summary>
        public float CurveAngle
        {
            get => _curveAngle;
            set
            {
                _curveAngle = value;
                ApplyCurveAll();
            }
        }

        private void CollectTextComponents()
        {
            UnsubscribeAll();
            _textComponents.Clear();
            GetComponentsInChildren(true, _textComponents);
        }

        private void SubscribeAll()
        {
            foreach (var tmp in _textComponents)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            }
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void UnsubscribeAll()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        private void OnTextChanged(Object obj)
        {
            if (obj is TMP_Text tmp && _textComponents.Contains(tmp))
            {
                ApplyCurve(tmp);
            }
        }

        private void ApplyCurveAll()
        {
            foreach (var tmp in _textComponents)
            {
                if (tmp != null)
                {
                    tmp.ForceMeshUpdate();
                    ApplyCurve(tmp);
                }
            }
        }

        private void ApplyCurve(TMP_Text tmp)
        {
            if (tmp == null) return;

            var textInfo = tmp.textInfo;
            if (textInfo == null || textInfo.characterCount == 0) return;

            // Find the horizontal bounds of the text
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;
                var charInfo = textInfo.characterInfo[i];
                float charMidX = (charInfo.bottomLeft.x + charInfo.bottomRight.x) * 0.5f;
                if (charMidX < minX) minX = charMidX;
                if (charMidX > maxX) maxX = charMidX;
            }

            if (minX >= maxX) return;

            float textWidth = maxX - minX;
            float centerX = (minX + maxX) * 0.5f;

            // Convert angle to radians (half angle for each side)
            float halfAngleRad = _curveAngle * 0.5f * Mathf.Deg2Rad;

            // Calculate radius from arc length and angle
            // arcLength ≈ textWidth, angle = curveAngle
            // radius = arcLength / angle (in radians)
            float absAngleRad = Mathf.Abs(_curveAngle) * Mathf.Deg2Rad;
            if (absAngleRad < 0.001f) return; // Too small, skip

            float radius = textWidth / absAngleRad;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                var charInfo = textInfo.characterInfo[i];
                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                var vertices = textInfo.meshInfo[materialIndex].vertices;

                // Character center X position normalized to [-0.5, 0.5]
                float charMidX = (charInfo.bottomLeft.x + charInfo.bottomRight.x) * 0.5f;
                float normalizedX = (charMidX - centerX) / textWidth;

                // Angle for this character along the arc
                float charAngle = normalizedX * _curveAngle * Mathf.Deg2Rad;

                // Vertical offset only: y = radius - radius * cos(charAngle)
                // No horizontal shift — keep original letter spacing
                float yOffset = -radius * (1f - Mathf.Cos(charAngle));

                // Apply offset and rotation to each vertex
                Vector3 charCenter = new Vector3(charMidX, (charInfo.bottomLeft.y + charInfo.topLeft.y) * 0.5f, 0f);

                for (int v = 0; v < 4; v++)
                {
                    // Translate to origin, rotate, translate back
                    Vector3 vert = vertices[vertexIndex + v];
                    vert -= charCenter;

                    // Rotate around Z axis
                    float cos = Mathf.Cos(-charAngle);
                    float sin = Mathf.Sin(-charAngle);
                    float rotX = vert.x * cos - vert.y * sin;
                    float rotY = vert.x * sin + vert.y * cos;

                    vert = new Vector3(rotX, rotY, vert.z) + charCenter;

                    // Apply vertical arc offset only
                    vert.y += yOffset;

                    vertices[vertexIndex + v] = vert;
                }
            }

            // Push modified vertices back
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}
