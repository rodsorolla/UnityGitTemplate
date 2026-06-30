using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sorolla
{
    /// <summary>
    /// Global, zero-wiring haptic feedback for every uGUI <see cref="Button"/>.
    /// On each pointer-down it raycasts the EventSystem at the tap position and, if the
    /// press lands on an interactable Button, plays a light selection haptic.
    ///
    /// This catches buttons created at runtime (daily-reward cells, dynamic button prefabs,
    /// etc.) without any per-button or per-prefab setup. Spawned once by
    /// <see cref="HapticsService"/> on a DontDestroyOnLoad object.
    /// </summary>
    public class GlobalButtonHaptics : MonoBehaviour
    {
        private IHapticsService _haptics;
        private readonly List<RaycastResult> _results = new List<RaycastResult>();

        public void Initialize(IHapticsService haptics)
        {
            _haptics = haptics;
        }

        private void Update()
        {
            // Pointer-down only — once per press. Covers editor mouse and device touch
            // (legacy Input is available; project input handler = Both).
            if (!Input.GetMouseButtonDown(0)) return;
            if (_haptics == null || !_haptics.IsEnabled) return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;

            var pointerData = new PointerEventData(eventSystem) { position = Input.mousePosition };
            _results.Clear();
            eventSystem.RaycastAll(pointerData, _results);

            for (int i = 0; i < _results.Count; i++)
            {
                var button = _results[i].gameObject.GetComponentInParent<Button>();
                if (button != null && button.IsInteractable() && button.isActiveAndEnabled)
                {
                    _haptics.PlaySelection();
                    return;
                }
            }
        }
    }
}
