using System;
using TMPro;
using UnityEngine;

namespace Sorolla.Lives.UI
{
    /// <summary>
    /// Drop-in lives counter. Subscribes to ILivesService and renders one of three states:
    /// normal (count), at-zero (regen countdown), booster-active (infinity icon, no text).
    ///
    /// Label wiring:
    /// - If <see cref="_countLabel"/> and <see cref="_countdownLabel"/> are the same TMP
    ///   component, the widget displays count when lives > 0 and regen mm:ss when lives == 0.
    /// - If wired to separate TMPs, count and countdown render independently.
    ///
    /// The GameObject stays active so Update can recover when services or level data
    /// initialize after this widget's OnEnable (common during scene load).
    /// </summary>
    public class LivesTopBarWidget : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private TMP_Text _countLabel;
        [SerializeField] private TMP_Text _countdownLabel;
        [SerializeField] private GameObject _heartIcon;
        [SerializeField] private GameObject _infinityIcon;

        [Header("Behavior")]
        [Tooltip("If set, overrides LevelFlowManager.CurrentLevelIndex used for the gate visibility check.")]
        [SerializeField] private int _forcedLevelIndex = -1;

        private ILivesService _lives;
        private Sorolla.LevelFlow.ILevelFlowManager _flow;
        private bool _subscribed;
        private int _lastIdx = int.MinValue;
        private float _nextSecondTick;

        private void OnEnable()
        {
            TryResolveAndSubscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_lives == null || !_subscribed)
            {
                TryResolveAndSubscribe();
                if (_lives == null) return;
            }
            int idx = _forcedLevelIndex >= 0
                ? _forcedLevelIndex
                : (_flow != null ? _flow.CurrentLevelIndex : int.MaxValue);
            bool idxChanged = idx != _lastIdx;
            if (idxChanged) _lastIdx = idx;

            // Per-second refresh keeps the regen countdown live without hitting the
            // service every frame. Refresh() is also fired on lives-change events.
            if (idxChanged || Time.unscaledTime >= _nextSecondTick)
            {
                _nextSecondTick = Time.unscaledTime + 1f;
                Refresh();
            }
        }

        private void TryResolveAndSubscribe()
        {
            if (_lives == null) _lives = ServiceLocator.Instance.TryResolve<ILivesService>();
            if (_flow == null) _flow = ServiceLocator.Instance.TryResolve<Sorolla.LevelFlow.ILevelFlowManager>();
            if (_lives != null && !_subscribed)
            {
                _lives.OnCurrentChanged += HandleCurrentChanged;
                _lives.OnBoosterActivated += HandleBoosterChanged;
                _lives.OnBoosterExpired += HandleBoosterExpired;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_lives != null && _subscribed)
            {
                _lives.OnCurrentChanged -= HandleCurrentChanged;
                _lives.OnBoosterActivated -= HandleBoosterChanged;
                _lives.OnBoosterExpired -= HandleBoosterExpired;
            }
            _subscribed = false;
        }

        private void Refresh()
        {
            int idx = _forcedLevelIndex >= 0
                ? _forcedLevelIndex
                : (_flow != null ? _flow.CurrentLevelIndex : int.MaxValue);
            bool active = _lives != null && _lives.IsActiveForLevel(idx);
            if (!active)
            {
                SetIconsActive(false, false);
                ApplyText(string.Empty, string.Empty);
                return;
            }

            bool booster = _lives.IsBoosterActive;
            SetIconsActive(true, booster);

            string countText;
            string countdownText;
            if (booster)
            {
                countText = string.Empty;
                countdownText = FormatTime(_lives.BoosterTimeRemaining);
            }
            else if (_lives.Current > 0)
            {
                countText = _lives.Current.ToString();
                countdownText = string.Empty;
            }
            else
            {
                // Out of lives — show regen countdown.
                countText = string.Empty;
                TimeSpan t = _lives.TimeUntilNextLife;
                countdownText = t > TimeSpan.Zero ? FormatTime(t) : "0";
            }
            ApplyText(countText, countdownText);
        }

        private void ApplyText(string countText, string countdownText)
        {
            if (_countLabel != null && _countLabel == _countdownLabel)
            {
                // Shared TMP: display whichever is non-empty (count wins when both).
                _countLabel.text = !string.IsNullOrEmpty(countText) ? countText : countdownText;
                return;
            }
            if (_countLabel) _countLabel.text = countText;
            if (_countdownLabel) _countdownLabel.text = countdownText;
        }

        private void SetIconsActive(bool active, bool boosterActive)
        {
            if (_heartIcon) _heartIcon.SetActive(active && !boosterActive);
            if (_infinityIcon) _infinityIcon.SetActive(active && boosterActive);
            if (active)
            {
                if (_countLabel && !_countLabel.gameObject.activeSelf) _countLabel.gameObject.SetActive(true);
                if (_countdownLabel && !_countdownLabel.gameObject.activeSelf) _countdownLabel.gameObject.SetActive(true);
            }
        }

        private static string FormatTime(TimeSpan t) => $"{t:mm\\:ss}";

        private void HandleCurrentChanged(int _) => Refresh();
        private void HandleBoosterChanged(TimeSpan _) => Refresh();
        private void HandleBoosterExpired() => Refresh();
    }
}
