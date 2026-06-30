using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Lightweight timer utility. No MonoBehaviour required per-timer.
    /// Timers are updated centrally via <see cref="SorollaTimerUpdater"/>, which auto-creates on first use.
    /// </summary>
    public class SorollaTimer
    {
        private static readonly List<SorollaTimer> _activeTimers = new List<SorollaTimer>();
        private static bool _updaterCreated;

        // Clear on play start so "Reload Domain = off" sessions don't keep ticking timers
        // (with callbacks into destroyed objects) from the previous play, and so the updater
        // is recreated fresh (_updaterCreated would otherwise stay true after its object is gone).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _activeTimers.Clear();
            _updaterCreated = false;
        }

        private float _duration;
        private float _elapsed;
        private bool _isRunning;
        private bool _isComplete;
        private bool _loop;
        private bool _useUnscaledTime;
        private Action _onComplete;
        private Action<float> _onTick;

        /// <summary>Time elapsed since the timer started.</summary>
        public float Elapsed => _elapsed;

        /// <summary>Time remaining until completion.</summary>
        public float Remaining => Mathf.Max(0f, _duration - _elapsed);

        /// <summary>Whether the timer is currently running.</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Whether the timer has completed.</summary>
        public bool IsComplete => _isComplete;

        /// <summary>Progress from 0 to 1.</summary>
        public float Progress => _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        private SorollaTimer() { }

        /// <summary>
        /// Starts a timer that fires onComplete after the specified duration.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="onComplete">Callback when timer finishes</param>
        /// <param name="loop">Whether to restart automatically</param>
        /// <param name="useUnscaledTime">If true, ignores Time.timeScale</param>
        public static SorollaTimer StartTimer(float duration, Action onComplete, bool loop = false, bool useUnscaledTime = false)
        {
            EnsureUpdater();
            var timer = new SorollaTimer
            {
                _duration = duration,
                _onComplete = onComplete,
                _loop = loop,
                _useUnscaledTime = useUnscaledTime,
                _isRunning = true
            };
            _activeTimers.Add(timer);
            return timer;
        }

        /// <summary>
        /// Starts a countdown timer that ticks with remaining time and fires onComplete when done.
        /// </summary>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="onTick">Called each frame with remaining time</param>
        /// <param name="onComplete">Called when countdown reaches zero</param>
        /// <param name="useUnscaledTime">If true, ignores Time.timeScale</param>
        public static SorollaTimer StartCountdown(float duration, Action<float> onTick, Action onComplete, bool useUnscaledTime = false)
        {
            EnsureUpdater();
            var timer = new SorollaTimer
            {
                _duration = duration,
                _onTick = onTick,
                _onComplete = onComplete,
                _useUnscaledTime = useUnscaledTime,
                _isRunning = true
            };
            _activeTimers.Add(timer);
            return timer;
        }

        /// <summary>Pauses the timer.</summary>
        public void Pause()
        {
            _isRunning = false;
        }

        /// <summary>Resumes the timer.</summary>
        public void Resume()
        {
            if (!_isComplete)
                _isRunning = true;
        }

        /// <summary>Cancels the timer and removes it from the active list.</summary>
        public void Cancel()
        {
            _isRunning = false;
            _isComplete = true;
            _activeTimers.Remove(this);
        }

        /// <summary>Restarts the timer from the beginning.</summary>
        public void Restart()
        {
            _elapsed = 0f;
            _isComplete = false;
            _isRunning = true;
            if (!_activeTimers.Contains(this))
                _activeTimers.Add(this);
        }

        /// <summary>
        /// Updates all active timers. Called by <see cref="SorollaTimerUpdater"/>.
        /// </summary>
        public static void UpdateAll(float deltaTime, float unscaledDeltaTime)
        {
            for (int i = _activeTimers.Count - 1; i >= 0; i--)
            {
                var timer = _activeTimers[i];
                if (!timer._isRunning) continue;

                var dt = timer._useUnscaledTime ? unscaledDeltaTime : deltaTime;
                timer._elapsed += dt;
                timer._onTick?.Invoke(timer.Remaining);

                if (timer._elapsed >= timer._duration)
                {
                    timer._onComplete?.Invoke();

                    if (timer._loop)
                    {
                        timer._elapsed -= timer._duration;
                    }
                    else
                    {
                        timer._isRunning = false;
                        timer._isComplete = true;
                        _activeTimers.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>Cancels all active timers.</summary>
        public static void CancelAll()
        {
            for (int i = _activeTimers.Count - 1; i >= 0; i--)
            {
                _activeTimers[i]._isRunning = false;
                _activeTimers[i]._isComplete = true;
            }
            _activeTimers.Clear();
        }

        private static void EnsureUpdater()
        {
            if (_updaterCreated) return;
            _updaterCreated = true;
            SorollaTimerUpdater.EnsureExists();
        }
    }
}
