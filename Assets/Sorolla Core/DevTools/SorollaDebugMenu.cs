using System;
using Sorolla.LevelFlow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sorolla.DevTools
{
    public class SorollaDebugMenu : MonoBehaviour
    {
        // Secret open gesture: 3 taps, then one 2-second hold, then a single tap.
        private const int TapsBeforeHold = 3;
        private const int TapsAfterHold = 1;
        private const float HoldSeconds = 2f;
        // A press longer than this counts as a hold attempt, never as a tap.
        private const float MaxTapSeconds = 0.4f;
        // Pausing longer than this between presses starts the sequence over. Comfortably
        // longer than the hold itself, so hesitating before the hold does not wipe the run.
        private const float SequenceTimeoutSeconds = 6f;
        // Set the first time the menu is opened: from then on a single tap is enough.
        private const string UnlockedKey = "Sorolla.DebugMenu.Unlocked";

        private enum GestureStage { BeforeHold, AfterHold }

        [Header("References")]
        [SerializeField] private GameObject _menu;
        [SerializeField] private Button _openMenuButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_InputField _setLevelInput;
        [SerializeField] private Button _setLevelButton;

        private GestureStage _stage;
        private int _tapCount;
        private float _pressStartTime;
        private float _lastInputTime;
        private EventTrigger _openTrigger;
        private EventTrigger.Entry _pressDownEntry;
        private EventTrigger.Entry _pressUpEntry;
        private ILevelFlowManager _levelFlow;

        private void Start()
        {
            _menu.SetActive(false);

            RegisterOpenGesture();
            _closeButton.onClick.AddListener(CloseMenu);
            _setLevelButton.onClick.AddListener(OnSetLevelClicked);
        }

        private void OnDestroy()
        {
            if (_openTrigger != null)
            {
                _openTrigger.triggers.Remove(_pressDownEntry);
                _openTrigger.triggers.Remove(_pressUpEntry);
            }
            _closeButton.onClick.RemoveListener(CloseMenu);
            _setLevelButton.onClick.RemoveListener(OnSetLevelClicked);
        }

        /// <summary>
        /// The gesture needs press DURATION, which onClick does not carry, so the open
        /// button is driven from pointer down/up instead. The EventTrigger is added at
        /// runtime so the prefab needs no extra wiring.
        /// </summary>
        private void RegisterOpenGesture()
        {
            if (_openMenuButton == null) return;

            _openTrigger = _openMenuButton.GetComponent<EventTrigger>();
            if (_openTrigger == null)
                _openTrigger = _openMenuButton.gameObject.AddComponent<EventTrigger>();

            _pressDownEntry = AddTriggerEntry(_openTrigger, EventTriggerType.PointerDown, OnOpenPressDown);
            _pressUpEntry = AddTriggerEntry(_openTrigger, EventTriggerType.PointerUp, OnOpenPressUp);
        }

        private static EventTrigger.Entry AddTriggerEntry(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
            return entry;
        }

        // Unscaled time throughout: the debug menu has to stay reachable while the game
        // is paused or running at a debug time scale.
        private void OnOpenPressDown()
        {
            if (Time.unscaledTime - _lastInputTime > SequenceTimeoutSeconds)
                ResetGesture();

            _pressStartTime = Time.unscaledTime;
        }

        private void OnOpenPressUp()
        {
            float held = Time.unscaledTime - _pressStartTime;
            _lastInputTime = Time.unscaledTime;

            // Already discovered on this device — the full sequence is a one-time gate.
            if (IsUnlocked)
            {
                OpenMenu();
                return;
            }

            // The HOLD is what advances the sequence, not a tap count hitting its target
            // exactly. Overshooting the tap count is the easiest thing in the world to do
            // on a hidden button, so extra taps are harmless — only the tally when the
            // hold lands has to be high enough.
            if (held >= HoldSeconds)
            {
                if (_stage == GestureStage.BeforeHold && _tapCount >= TapsBeforeHold)
                {
                    _stage = GestureStage.AfterHold;
                    _tapCount = 0;
                    LogGesture($"hold accepted ({held:0.00}s) — now tap {TapsAfterHold}x to open");
                }
                else
                {
                    LogGesture($"hold ({held:0.00}s) after only {_tapCount} tap(s) in {_stage} — sequence reset");
                    ResetGesture();
                }
                return;
            }

            // A press between "tap" and "hold" is too ambiguous to count as either.
            if (held > MaxTapSeconds)
            {
                LogGesture($"press of {held:0.00}s is neither a tap (<{MaxTapSeconds}s) nor a hold (>={HoldSeconds}s) — sequence reset");
                ResetGesture();
                return;
            }

            _tapCount++;

            if (_stage == GestureStage.BeforeHold)
            {
                LogGesture(_tapCount < TapsBeforeHold
                    ? $"tap {_tapCount}/{TapsBeforeHold} — keep tapping"
                    : $"tap {_tapCount} (>= {TapsBeforeHold}) — press and hold {HoldSeconds}s now");
                return;
            }

            if (_tapCount < TapsAfterHold)
            {
                LogGesture($"tap {_tapCount}/{TapsAfterHold} after hold");
                return;
            }

            OpenMenu();
        }

        /// <summary>
        /// Editor-only trace of the open gesture. Without it every failed attempt is a
        /// silent reset, which makes a mistimed press indistinguishable from a button
        /// that is not receiving pointer events at all.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogGesture(string message) => Debug.Log($"[SorollaDebugMenu] {message}");

        /// <summary>
        /// Persisted so the sequence only has to be performed once per device. Kept in
        /// PlayerPrefs rather than session state deliberately: this is a convenience
        /// gate, not a build-safety switch like the individual debug flags.
        /// </summary>
        private static bool IsUnlocked => PlayerPrefs.GetInt(UnlockedKey, 0) == 1;

        private void OpenMenu()
        {
            LogGesture("opening menu");
            ResetGesture();
            if (!IsUnlocked)
            {
                PlayerPrefs.SetInt(UnlockedKey, 1);
                // Flushed now, not left to app quit — a crash or an Editor stop would
                // otherwise lose the unlock and force the sequence again.
                PlayerPrefs.Save();
            }
            _menu.SetActive(true);
        }

        private void ResetGesture()
        {
            _stage = GestureStage.BeforeHold;
            _tapCount = 0;
        }

        private void CloseMenu()
        {
            ResetGesture();
            _menu.SetActive(false);
        }

        private void OnSetLevelClicked()
        {
            if (string.IsNullOrWhiteSpace(_setLevelInput.text))
                return;

            if (!int.TryParse(_setLevelInput.text, out int level))
                return;

            _levelFlow ??= ServiceLocator.Instance.Resolve<ILevelFlowManager>();
            if (_levelFlow == null)
            {
                Debug.LogWarning("[SorollaDebugMenu] ILevelFlowManager not found");
                return;
            }

            Debug.Log($"[SorollaDebugMenu] Starting level {level}");
            _levelFlow.StartLevel(level);
            CloseMenu();
        }
    }
}