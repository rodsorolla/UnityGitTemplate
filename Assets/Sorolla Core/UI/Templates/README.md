# Sorolla.UI Prefab Templates

This folder contains template prefabs for common UI elements. These are starter templates that games can duplicate and customize.

## Creating the Template Prefabs

### BasePanel.prefab

Create a prefab with this hierarchy:

```
BasePanel (RectTransform, CanvasGroup)
├── Background (Image, Button) - Full screen raycast blocker, dark overlay
└── Window (RectTransform)
    ├── Header (RectTransform, HorizontalLayoutGroup)
    │   ├── Title (TextMeshProUGUI)
    │   └── CloseButton (Button, Image)
    └── Content (RectTransform) - Container for panel-specific content
```

Settings:
- CanvasGroup for fade transitions
- Background: Stretch anchors, semi-transparent black (#000000, alpha 0.5)
- Window: Center anchors, appropriate size
- CloseButton: Optional, can trigger panel close

### BaseButton.prefab

```
BaseButton (RectTransform, Button, Image)
├── Icon (Image) - Optional, left-aligned
└── Label (TextMeshProUGUI) - Center or right of icon
```

Settings:
- Button component with appropriate transition (Color Tint or Sprite Swap)
- HorizontalLayoutGroup if using icon + label
- ContentSizeFitter for auto-sizing

### Toast.prefab

Attach: `Sorolla.UI.Dialogs.ToastPanel`

```
Toast (RectTransform, ToastPanel)
└── Container (RectTransform, HorizontalLayoutGroup)
    ├── Icon (Image) - Optional
    └── Message (TextMeshProUGUI)
```

Settings:
- Anchor to bottom-center of screen
- Container slides up/down during animation
- Wire references in ToastPanel component

### ConfirmDialog.prefab

Attach: `Sorolla.UI.Dialogs.ConfirmDialog`

```
ConfirmDialog (RectTransform, ConfirmDialog, CanvasGroup)
├── Background (Image) - Blocker
└── Window (RectTransform)
    ├── Title (TextMeshProUGUI)
    ├── Message (TextMeshProUGUI)
    └── ButtonContainer (HorizontalLayoutGroup)
        ├── CancelButton (Button)
        │   └── Label (TextMeshProUGUI)
        └── ConfirmButton (Button)
            └── Label (TextMeshProUGUI)
```

Settings:
- Wire all references in ConfirmDialog component
- Window transform for scale animation
- ButtonContainer with spacing for button layout

### AlertDialog.prefab

Attach: `Sorolla.UI.Dialogs.AlertDialog`

Similar to ConfirmDialog but with only one button (OK).

## Usage

1. Duplicate these templates into your game's UI folder
2. Customize visuals (colors, fonts, sprites)
3. Add to your UIRegistry ScriptableObject
4. Open via UIManager:

```csharp
// Toast
ToastManager.Instance.ShowToast("Achievement unlocked!");

// Confirm dialog
await uiManager.OpenPanelAsync(UIPanelId.ConfirmDialog, new ConfirmDialog.Data
{
    Title = "Confirm",
    Message = "Are you sure?",
    OnResult = (confirmed) => Debug.Log($"Result: {confirmed}")
});
```

## Creating Transition Assets

Create ScriptableObject instances for transitions:

1. Right-click in Project > Create > Sorolla > UI > Transitions
2. Choose Fade, Scale, or Slide
3. Configure duration and easing
4. Use in OpenPanelAsync:

```csharp
var fadeIn = Resources.Load<FadeTransition>("Transitions/FadeIn");
await uiManager.OpenPanelAsync(panelId, args, fadeIn);
```
