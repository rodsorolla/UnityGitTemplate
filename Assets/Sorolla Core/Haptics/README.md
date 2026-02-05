# Haptics Service

Cross-platform haptic feedback for iOS and Android.

## Setup

Add `HapticsService` component to a persistent GameObject. It auto-registers with ServiceLocator on Awake.

## API

```csharp
var haptics = ServiceLocator.Instance.Resolve<IHapticsService>();

// Properties
haptics.IsEnabled    // Get/set enabled state (persists across sessions)
haptics.IsSupported  // Check if device supports haptics

// Impact feedback
haptics.PlayImpact(HapticsIntensity.Light);
haptics.PlayImpact(HapticsIntensity.Medium);
haptics.PlayImpact(HapticsIntensity.Heavy);

// UI selection feedback
haptics.PlaySelection();

// Notification feedback
haptics.PlayNotification(HapticsType.Success);
haptics.PlayNotification(HapticsType.Warning);
haptics.PlayNotification(HapticsType.Error);
```

## Platform Support

| Platform | Implementation |
|----------|----------------|
| iOS 10+  | UIFeedbackGenerator |
| Android API 26+ | VibrationEffect with amplitude |
| Android < 26 | Basic vibration (no amplitude) |
| Editor | Debug.Log output |
