#if UNITY_EDITOR
using UnityEditor;
using NaughtyAttributes.Editor;

namespace Sorolla.Tutorial
{
    [CustomEditor(typeof(TutorialStepBase), true)]
    public class TutorialStepEditor : NaughtyInspector
    {
        // That's it! NaughtyInspector handles everything
    }
}
#endif