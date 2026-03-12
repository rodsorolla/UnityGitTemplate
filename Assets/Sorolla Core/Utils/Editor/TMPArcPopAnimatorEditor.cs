using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TMPArcPopAnimator))]
public class TMPArcPopAnimatorEditor : Editor
{
    bool _isPreviewing;
    double _startTime;
    TMPArcPopAnimator _target;
    TMPArcPopAnimator[] _group;

    void OnEnable()
    {
        _target = (TMPArcPopAnimator)target;
    }

    void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (!_isPreviewing)
        {
            if (GUILayout.Button("Preview Group"))
                StartPreview();
        }
        else
        {
            if (GUILayout.Button("Stop"))
                StopPreview();
        }
    }

    TMPArcPopAnimator[] FindGroup()
    {
        int groupId = _target.layerGroupId;

        // Search from scene roots to find all animators, including inactive objects
        var roots = _target.gameObject.scene.GetRootGameObjects();
        var list = new System.Collections.Generic.List<TMPArcPopAnimator>();
        foreach (var root in roots)
        {
            var animators = root.GetComponentsInChildren<TMPArcPopAnimator>(true);
            foreach (var a in animators)
                if (a.layerGroupId == groupId) list.Add(a);
        }

        // Guarantee target is included
        if (!list.Contains(_target))
            list.Add(_target);

        return list.ToArray();
    }

    void StartPreview()
    {
        if (_isPreviewing) return;
        if (_target == null) return;

        _group = FindGroup();
        foreach (var a in _group)
            a.Tmp.ForceMeshUpdate();

        _isPreviewing = true;
        _startTime = EditorApplication.timeSinceStartup + _target.initialDelay;
        EditorApplication.update += UpdatePreview;
    }

    void StopPreview()
    {
        if (!_isPreviewing) return;

        _isPreviewing = false;
        EditorApplication.update -= UpdatePreview;

        if (_group != null)
            foreach (var a in _group)
                if (a != null) a.ResetMesh();

        _group = null;
        SceneView.RepaintAll();
    }

    void UpdatePreview()
    {
        if (_target == null || _group == null)
        {
            StopPreview();
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - _startTime;
        if (elapsed < 0) elapsed = 0;

        float maxDuration = 0f;
        foreach (var a in _group)
        {
            a.Tmp.ForceMeshUpdate();
            int charCount = a.Tmp.textInfo.characterCount;
            float dur = a.popDuration + charCount * a.letterDelay;
            if (dur > maxDuration) maxDuration = dur;
        }

        if (maxDuration <= 0f) maxDuration = 0.01f;
        float progress = Mathf.Clamp01((float)(elapsed / maxDuration));

        foreach (var a in _group)
            a.ApplyAnimation(progress);

        SceneView.RepaintAll();
        Repaint();

        if (progress >= 1f)
            StopPreview();
    }
}
