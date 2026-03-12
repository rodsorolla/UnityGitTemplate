using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPArcPopAnimator : MonoBehaviour
{
    [Header("Linking")]
    public bool isDriver = false;
    public int layerGroupId = 0;

    [Header("Arc")]
    public float arcRadius = 120f;  // Larger = gentler curve

    [Header("Pop")]
    public float initialDelay = 0f;
    public float popDuration = 0.35f;
    public float letterDelay = 0.05f;

    [Header("Ease")]
    public Ease popEase = Ease.OutBack;

    static Dictionary<int, float> sharedProgress = new();

    TextMeshProUGUI tmp;
    public TextMeshProUGUI Tmp => tmp != null ? tmp : (tmp = GetComponent<TextMeshProUGUI>());

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        tmp.ForceMeshUpdate();

        if (!sharedProgress.ContainsKey(layerGroupId))
            sharedProgress[layerGroupId] = 0f;

        if (isDriver)
            Play();
        
    }

    public void Play()
    {
        if (!isDriver)
            return;

        DOTween.Kill(this);
        sharedProgress[layerGroupId] = 0f;

        int charCount = tmp.textInfo.characterCount;
        float totalDuration = popDuration + charCount * letterDelay;

        DOTween.To(
            () => 0f,
            x => sharedProgress[layerGroupId] = x,
            1f,
            totalDuration
        ).SetEase(Ease.Linear)
         .SetDelay(initialDelay)
         .SetTarget(this);
    }

    void LateUpdate()
    {
        if (!sharedProgress.ContainsKey(layerGroupId))
            return;

        ApplyAnimation(sharedProgress[layerGroupId]);
    }

    public void ResetMesh()
    {
        if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();
        tmp.ForceMeshUpdate();
    }

    public void ApplyAnimation(float globalProgress)
    {
        tmp.ForceMeshUpdate();

        var textInfo = tmp.textInfo;
        var meshInfo = textInfo.meshInfo;

        int charCount = textInfo.characterCount;
        if (charCount == 0)
            return;

        float totalDuration = popDuration + charCount * letterDelay;
        float currentTime = globalProgress * totalDuration;

        // Calculate text center X
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < charCount; i++)
        {
            var ci = textInfo.characterInfo[i];
            if (!ci.isVisible) continue;
            float cx = (ci.bottomLeft.x + ci.bottomRight.x) * 0.5f;
            minX = Mathf.Min(minX, cx);
            maxX = Mathf.Max(maxX, cx);
        }
        float textCenterX = (minX + maxX) * 0.5f;

        for (int i = 0; i < charCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
                continue;

            float charTime = Mathf.Clamp01(
                (currentTime - i * letterDelay) / popDuration
            );

            // Calculate arc angle and offset (skip if radius is 0)
            float angle = 0f;
            Vector3 arcOffset = Vector3.zero;

            if (arcRadius > 0.01f)
            {
                float charCenterX = (charInfo.bottomLeft.x + charInfo.bottomRight.x) * 0.5f;
                float relativeX = charCenterX - textCenterX;
                float sinAngle = Mathf.Clamp(relativeX / arcRadius, -1f, 1f);
                angle = Mathf.Asin(sinAngle);
                float yOffset = Mathf.Cos(angle) * arcRadius - arcRadius;
                arcOffset = new Vector3(0f, yOffset, 0f);
            }

            float eased = DOVirtual.EasedValue(0f, 1f, charTime, popEase);
            float finalScale = eased;  // OutBack ease provides natural overshoot then settles at 1.0

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3 center = (
                meshInfo[materialIndex].vertices[vertexIndex] +
                meshInfo[materialIndex].vertices[vertexIndex + 2]
            ) * 0.5f;

            // Rotation to follow arc tangent (interpolated with easing)
            float rotAngle = -angle * eased;
            float cos = Mathf.Cos(rotAngle);
            float sin = Mathf.Sin(rotAngle);

            for (int v = 0; v < 4; v++)
            {
                Vector3 original = meshInfo[materialIndex].vertices[vertexIndex + v];
                Vector3 offset = (original - center) * finalScale;

                // Apply rotation around center
                float rotatedX = offset.x * cos - offset.y * sin;
                float rotatedY = offset.x * sin + offset.y * cos;
                Vector3 rotatedOffset = new Vector3(rotatedX, rotatedY, offset.z);

                meshInfo[materialIndex].vertices[vertexIndex + v] =
                    center + rotatedOffset + arcOffset * eased;
            }
        }

        for (int i = 0; i < meshInfo.Length; i++)
        {
            meshInfo[i].mesh.vertices = meshInfo[i].vertices;
        }
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}