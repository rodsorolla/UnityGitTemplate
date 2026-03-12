using System.Runtime.InteropServices;
using Sorolla.PersistentData;
using UnityEngine;

namespace Sorolla
{
    /// <summary>
    /// Cross-platform haptic feedback service.
    /// Supports iOS (UIFeedbackGenerator) and Android (Vibrator API).
    /// Extends SorollaManager for proper initialization order with other Sorolla services.
    /// </summary>
    public class HapticsService : SorollaManager, IHapticsService
    {
        private const string SaveFileName = "haptics";

        private HapticsData _data;
        private bool _isDirty;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _HapticsPlayImpact(int intensity);

        [DllImport("__Internal")]
        private static extern void _HapticsPlaySelection();

        [DllImport("__Internal")]
        private static extern void _HapticsPlayNotification(int type);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _unityView;
#endif

        public bool IsEnabled
        {
            get => _data.isEnabled;
            set
            {
                if (_data.isEnabled == value) return;
                _data.isEnabled = value;
                _isDirty = true;
            }
        }

        public bool IsSupported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true; // iOS 10+ always supports haptics
#elif UNITY_ANDROID && !UNITY_EDITOR
                return _unityView != null;
#else
                return false;
#endif
            }
        }

        protected override void Initialize()
        {
            Load();
            InitializePlatform();
            ServiceLocator.Instance.Register<IHapticsService>(this);
            Debug.Log("[HapticsService] Initialized");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isDirty)
            {
                Save();
            }
        }

        private void InitializePlatform()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var window = activity.Call<AndroidJavaObject>("getWindow");
                _unityView = window.Call<AndroidJavaObject>("getDecorView");
                _unityView.Call("setHapticFeedbackEnabled", true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsService] Failed to initialize Android haptics: {e.Message}");
                _unityView = null;
            }
#endif
        }

        public void PlayImpact(HapticsIntensity intensity)
        {
            if (!IsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            _HapticsPlayImpact((int)intensity);
#elif UNITY_ANDROID && !UNITY_EDITOR
            int constant = intensity switch
            {
                HapticsIntensity.Light => KEYBOARD_TAP,
                HapticsIntensity.Medium => VIRTUAL_KEY,
                HapticsIntensity.Heavy => LONG_PRESS,
                _ => KEYBOARD_TAP
            };
            PlayAndroidHaptic(constant);
#else
            Debug.Log($"[HapticsService] PlayImpact({intensity})");
#endif
        }

        public void PlaySelection()
        {
            if (!IsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            _HapticsPlaySelection();
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroidHaptic(KEYBOARD_TAP);
#else
            Debug.Log("[HapticsService] PlaySelection()");
#endif
        }

        public void PlayNotification(HapticsType type)
        {
            if (!IsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            // Map HapticsType to iOS notification types (0=success, 1=warning, 2=error)
            int iosType = type switch
            {
                HapticsType.Success => 0,
                HapticsType.Warning => 1,
                HapticsType.Error => 2,
                _ => 0
            };
            _HapticsPlayNotification(iosType);
#elif UNITY_ANDROID && !UNITY_EDITOR
            int notifConstant = type switch
            {
                HapticsType.Success => VIRTUAL_KEY,
                HapticsType.Warning => VIRTUAL_KEY,
                HapticsType.Error => LONG_PRESS,
                _ => KEYBOARD_TAP
            };
            PlayAndroidHaptic(notifConstant);
#else
            Debug.Log($"[HapticsService] PlayNotification({type})");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // HapticFeedbackConstants
        private const int LONG_PRESS = 0;
        private const int VIRTUAL_KEY = 1;
        private const int KEYBOARD_TAP = 3;

        private void PlayAndroidHaptic(int feedbackConstant)
        {
            if (_unityView == null) return;

            try
            {
                _unityView.Call<bool>("performHapticFeedback", feedbackConstant);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsService] Android haptic feedback failed: {e.Message}");
            }
        }
#endif

        private void Load()
        {
            _data = SaveSystem.Load<HapticsData>(SaveFileName);
            _isDirty = false;
        }

        private void Save()
        {
            var result = SaveSystem.Save(_data, SaveFileName);
            if (result.Success)
            {
                _isDirty = false;
            }
            else
            {
                Debug.LogError($"[HapticsService] Save failed: {result.ErrorMessage}");
            }
        }
    }
}
