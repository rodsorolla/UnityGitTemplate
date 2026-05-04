using DG.DOTweenEditor;
using DG.Tweening;
using Sorolla.UI.Effects;
using UnityEditor;
using UnityEngine;

namespace Sorolla.UI.Effects.EditorTools
{
    /// <summary>
    /// Custom inspector for <see cref="TextChangeEffect"/> — adds a Preview Animation button
    /// that works in both Play mode (runtime tween) and Edit mode (DOTweenEditorPreview).
    /// </summary>
    [CustomEditor(typeof(TextChangeEffect))]
    public class TextChangeEffectEditor : UnityEditor.Editor
    {
        private static Sequence _previewSequence;
        private static TextChangeEffect _previewTarget;
        private static Vector3 _previewBaseScale;
        private static Color _previewBaseColor;
        private static bool _previewActive;

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StopPreview();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var component = (TextChangeEffect)target;

            using (new EditorGUI.DisabledScope(_previewActive))
            {
                if (GUILayout.Button("Preview Animation", GUILayout.Height(24f)))
                {
                    if (Application.isPlaying)
                    {
                        component.PlayNow();
                    }
                    else
                    {
                        StartPreview(component);
                    }
                }
            }

            if (_previewActive && GUILayout.Button("Stop Preview"))
            {
                StopPreview();
            }
        }

        private static void StartPreview(TextChangeEffect component)
        {
            StopPreview();

            _previewTarget = component;
            _previewSequence = component.BuildPreviewSequence(out _previewBaseScale, out _previewBaseColor);
            _previewActive = true;

            _previewSequence.OnKill(() =>
            {
                if (_previewTarget != null)
                {
                    _previewTarget.RestorePreviewState(_previewBaseScale, _previewBaseColor);
                }
                _previewSequence = null;
                _previewTarget = null;
                _previewActive = false;
            });

            DOTweenEditorPreview.PrepareTweenForPreview(_previewSequence);
            DOTweenEditorPreview.Start();
        }

        private static void StopPreview()
        {
            if (_previewSequence != null)
            {
                _previewSequence.Kill();
                _previewSequence = null;
            }
            DOTweenEditorPreview.Stop();
            if (_previewTarget != null)
            {
                _previewTarget.RestorePreviewState(_previewBaseScale, _previewBaseColor);
                _previewTarget = null;
            }
            _previewActive = false;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state) => StopPreview();
    }
}
