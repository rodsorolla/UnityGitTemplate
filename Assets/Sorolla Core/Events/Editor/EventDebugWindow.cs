#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sorolla.Events.Editor
{
    /// <summary>
    /// Dev tooling for the events module. Tools → Sorolla → Events → Debug Window.
    /// </summary>
    public class EventDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Sorolla Core/Events/Debug Window")]
        public static void Open() => GetWindow<EventDebugWindow>("Events Debug");

        private int _grantAmount = 10;

        private void OnGUI()
        {
            GUILayout.Label("Sorolla.Events Debug", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this window.", MessageType.Info);
                return;
            }

            var svc = Sorolla.ServiceLocator.Instance.TryResolve<IEventService>();
            if (svc == null)
            {
                EditorGUILayout.HelpBox("IEventService not registered yet.", MessageType.Warning);
                return;
            }

            var active = svc.ActiveEvent;
            EditorGUILayout.LabelField("Active event", active?.EventId ?? "(none)");
            if (active != null)
            {
                EditorGUILayout.LabelField("Type", active.EventType);
                EditorGUILayout.LabelField("Ends in", svc.TimeUntilActiveEnds.ToString());
                var p = svc.GetProgress(active.EventId);
                EditorGUILayout.LabelField("Progress", p?.progress.ToString() ?? "0");
                EditorGUILayout.LabelField("Steps claimed mask", p == null ? "0" : p.claimedStepBitset.ToString());
                EditorGUILayout.LabelField("Grand prize", (p?.grandPrizeClaimed ?? false).ToString());
            }
            EditorGUILayout.LabelField("Time to next start", svc.TimeUntilNextEventStarts.ToString());
            EditorGUILayout.LabelField("Rollback flag", svc.LastClockRollbackDetected.ToString());

            EditorGUILayout.Space();
            GUILayout.Label("Grant collectibles to active event", EditorStyles.boldLabel);
            _grantAmount = EditorGUILayout.IntField("Amount", _grantAmount);
            using (new EditorGUI.DisabledScope(active == null || _grantAmount <= 0))
            {
                if (GUILayout.Button("Commit Run Now"))
                {
                    var c = svc.BeginRunCollector();
                    c?.Add(_grantAmount);
                    svc.CommitRun(c);
                }
            }
        }
    }
}
#endif
