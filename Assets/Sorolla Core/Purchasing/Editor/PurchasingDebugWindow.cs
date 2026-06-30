using Sorolla;
using Sorolla.Purchasing;
using UnityEditor;
using UnityEngine;

namespace Sorolla.Purchasing.EditorTools
{
    public class PurchasingDebugWindow : EditorWindow
    {
        [MenuItem("Tools/Sorolla Core/Purchasing Debug Window")]
        public static void Open() => GetWindow<PurchasingDebugWindow>("Purchasing Debug");

        private string _productIdInput = "com.sorolla.template.noads";
        private string _entitlementInput = "noads";
        private PurchaseFailureReason _selectedReason = PurchaseFailureReason.PaymentDeclined;

        private void OnGUI()
        {
            GUILayout.Label("Purchasing Debug", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to drive the purchasing service.", MessageType.Info);
                return;
            }

            var svc = ServiceLocator.Instance?.TryResolve<PurchasingService>();
            var ent = ServiceLocator.Instance?.TryResolve<EntitlementService>();
            if (svc == null) { EditorGUILayout.HelpBox("PurchasingService not registered.", MessageType.Warning); return; }

            var mock = svc.BackendForTests as MockPurchasingBackend;
            if (mock == null)
            {
                EditorGUILayout.HelpBox("Active backend is not the mock — dev hooks are inert.", MessageType.Info);
            }

            EditorGUILayout.Space();
            _productIdInput = EditorGUILayout.TextField("Product ID", _productIdInput);

            using (new EditorGUI.DisabledScope(mock == null))
            {
                if (GUILayout.Button("Force next purchase: SUCCESS"))
                    mock.ForceNextSuccessOnly();

                _selectedReason = (PurchaseFailureReason)EditorGUILayout.EnumPopup("Failure reason", _selectedReason);
                if (GUILayout.Button("Force next purchase: FAIL"))
                    mock.ForceNextFailureWithReason(_selectedReason);

                EditorGUILayout.Space();
                if (GUILayout.Button("Simulate previously-owned (this product ID)"))
                    mock.SimulatePreviouslyOwned(_productIdInput);
                if (GUILayout.Button("Clear simulated previously-owned"))
                    mock.ClearPreviouslyOwned();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Init delay (ms)", GUILayout.Width(120));
                mock.InitDelayMs = EditorGUILayout.IntField(mock.InitDelayMs);
                if (GUILayout.Button("Simulate init failure"))
                    mock.SimulateInitFailure();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Trigger purchase NOW"))
                svc.InitiatePurchase(_productIdInput);
            if (GUILayout.Button("Trigger restore NOW"))
                svc.Restore();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            if (GUILayout.Button("Reset processed-products store"))
                new ProcessedProductsStore().Reset();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Entitlements", EditorStyles.boldLabel);
            _entitlementInput = EditorGUILayout.TextField("Key", _entitlementInput);
            using (new EditorGUI.DisabledScope(ent == null))
            {
                if (GUILayout.Button("Grant"))   ent.Grant(_entitlementInput);
                if (GUILayout.Button("Revoke"))  ent.Revoke(_entitlementInput);
                EditorGUILayout.LabelField("Currently granted:");
                if (ent != null)
                    foreach (var k in ent.AllGranted)
                        EditorGUILayout.LabelField($"  • {k}");
            }
        }
    }
}
