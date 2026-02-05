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
        private AndroidJavaObject _vibrator;
        private bool _hasAmplitudeControl;
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
                return _vibrator != null;
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
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                // Check for amplitude control (API 26+)
                if (AndroidApiLevel >= 26)
                {
                    _hasAmplitudeControl = _vibrator.Call<bool>("hasAmplitudeControl");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsService] Failed to initialize Android vibrator: {e.Message}");
                _vibrator = null;
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static int AndroidApiLevel
        {
            get
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT");
            }
        }
#endif

        public void PlayImpact(HapticsIntensity intensity)
        {
            if (!IsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
            _HapticsPlayImpact((int)intensity);
#elif UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroidVibration(intensity);
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
            PlayAndroidVibration(HapticsIntensity.Light);
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
            // Map notification types to intensities
            var intensity = type switch
            {
                HapticsType.Success => HapticsIntensity.Medium,
                HapticsType.Warning => HapticsIntensity.Medium,
                HapticsType.Error => HapticsIntensity.Heavy,
                _ => HapticsIntensity.Light
            };
            PlayAndroidVibration(intensity);
#else
            Debug.Log($"[HapticsService] PlayNotification({type})");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void PlayAndroidVibration(HapticsIntensity intensity)
        {
            if (_vibrator == null) return;

            // Get duration and amplitude based on intensity
            var (duration, amplitude) = intensity switch
            {
                HapticsIntensity.Light => (20L, 50),
                HapticsIntensity.Medium => (30L, 128),
                HapticsIntensity.Heavy => (50L, 255),
                _ => (20L, 50)
            };

            try
            {
                if (AndroidApiLevel >= 26 && _hasAmplitudeControl)
                {
                    // Use VibrationEffect for API 26+
                    using var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", duration, amplitude);
                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    // Fallback for older devices
                    _vibrator.Call("vibrate", duration);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticsService] Android vibration failed: {e.Message}");
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
