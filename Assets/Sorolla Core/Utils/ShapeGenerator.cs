using UnityEngine;
using System.Collections.Generic;

public class ShapeGenerator : MonoBehaviour
{
    public enum ShapeType { Line, Grid, Square, Circle, Ring, Spiral, Pyramid, Cone, Cylinder, Sphere, Helix, Star }
    public enum AlignmentMode { None, FaceCenter, FaceOutward, AlongPath }

    public ShapeType shapeType = ShapeType.Pyramid;
    public GameObject[] prefabs = new GameObject[1];

    // Quick prefab selection folders (Editor only)
    [HideInInspector] public List<string> prefabFolders = new List<string>();
    [Range(0.1f, 10f)] public float spacing = 1.0f;
    [Range(0.1f, 10f)] public float verticalSpacing = 1.0f;

    // COMPLETENESS SETTING
    [Range(0f, 1f)] public float completeness = 1f;

    // ROTATION SETTINGS
    public bool randomRotation = false;
    public AlignmentMode alignmentMode = AlignmentMode.None;

    // SCALE SETTINGS
    public bool randomScale = false;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    public bool scaleGradient = false;
    [Range(0.1f, 3f)] public float gradientStartScale = 1.0f;
    [Range(0.1f, 3f)] public float gradientEndScale = 0.5f;

    // POSITION SETTINGS
    public bool positionJitter = false;
    [Range(0f, 2f)] public float jitterAmount = 0.2f;
    public float heightOffset = 0f;

    // LINE SETTINGS
    [Range(1f, 100f)] public float lineSize = 10f;
    public Vector3 lineDirection = Vector3.forward;
    public bool useWave = false;
    [Range(0.1f, 10f)] public float waveFrequency = 1f;
    [Range(0.1f, 10f)] public float waveAmplitude = 1f;


    // GRID SETTINGS
    [Range(1f, 50f)] public float gridSizeX = 5f;
    [Range(1f, 50f)] public float gridSizeZ = 5f;
    public bool fillGrid = true;

    // SQUARE SETTINGS
    [Range(1f, 50f)] public float squareSizeUnits = 5f;
    public bool fillSquare = true;

    // CIRCLE/RING SETTINGS
    [Range(0.5f, 20f)] public float radius = 3.0f;
    public bool fillCircle = false;
    [Range(0.1f, 19f)] public float innerRadius = 1.5f;

    // SPIRAL SETTINGS
    [Range(0.5f, 10f)] public float spiralRotations = 3f;
    [Range(0.1f, 5f)] public float spiralRadiusGrowth = 0.5f;
    [Range(0.2f, 5f)] public float spiralDecay = 1f; // 1 = linear, <1 = tighter center, >1 = tighter outer

    // PYRAMID SETTINGS
    [Range(1f, 30f)] public float pyramidSize = 4f;

    // CONE SETTINGS
    [Range(1f, 30f)] public float coneHeightUnits = 5f;
    [Range(0.5f, 20f)] public float coneRadius = 3f;
    public bool fillCone = false;

    // CYLINDER SETTINGS
    [Range(1f, 30f)] public float cylinderHeightUnits = 5f;
    [Range(0.5f, 20f)] public float cylinderRadius = 3f;
    public bool fillCylinder = false;

    // SPHERE SETTINGS
    [Range(3, 20)] public int sphereRings = 8;
    [Range(0.5f, 20f)] public float sphereRadius = 3f;
    public bool fillSphere = false;

    // HELIX SETTINGS
    [Range(1f, 50f)] public float helixHeight = 10f;
    [Range(0.5f, 10f)] public float helixRotations = 3f;
    [Range(5, 50)] public int helixPointsPerRotation = 20;
    [Range(0.5f, 20f)] public float helixRadius = 3f;

    // STAR SETTINGS
    [Range(3, 20)] public int starPoints = 5;
    [Range(0.5f, 20f)] public float starOuterRadius = 3f;
    [Range(0.1f, 19f)] public float starInnerRadius = 1.5f;

    // GIZMO SETTINGS
    // GIZMO SETTINGS (kept for backwards compatibility, no longer used)
    [HideInInspector] public Color gizmoColor = new Color(0, 1, 0, 0.3f);

    [HideInInspector] public GameObject previewRoot;
    [HideInInspector] public bool autoPreview = true;
    [HideInInspector] public bool usePrefabInstances = true; // Always use prefab instances to avoid (Clone) suffix

    int currentGradientIndex = 0;
    int totalGradientObjects = 0;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!autoPreview) return;

        // Delay execution to avoid OnValidate issues
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject != null && !IsPartOfPrefabAsset())
            {
                GeneratePreview();
            }
        };
    }

    private bool IsPartOfPrefabAsset()
    {
        return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
    }
#endif

    public void GeneratePreview()
    {
#if UNITY_EDITOR
        if (IsPartOfPrefabAsset())
        {
            Debug.LogWarning("Cannot generate preview while editing prefab asset. Edit a scene instance instead.");
            return;
        }
#endif

        ClearPreview();

        if (prefabs == null || prefabs.Length == 0 || prefabs[0] == null)
        {
            Debug.LogWarning("Please assign at least one prefab before generating.");
            return;
        }

        previewRoot = new GameObject("Preview_" + shapeType);
        previewRoot.transform.SetParent(transform);
        previewRoot.transform.localPosition = Vector3.zero;

        currentGradientIndex = 0;
        totalGradientObjects = CalculateTotalObjects();

        switch (shapeType)
        {
            case ShapeType.Line:
                GenerateLine(previewRoot.transform);
                break;
            case ShapeType.Grid:
                GenerateGrid(previewRoot.transform);
                break;
            case ShapeType.Square:
                GenerateSquare(previewRoot.transform);
                break;
            case ShapeType.Circle:
                GenerateCircle(previewRoot.transform, Vector3.zero, radius, fillCircle);
                break;
            case ShapeType.Ring:
                GenerateRing(previewRoot.transform);
                break;
            case ShapeType.Spiral:
                GenerateSpiral(previewRoot.transform);
                break;
            case ShapeType.Pyramid:
                GeneratePyramid(previewRoot.transform);
                break;
            case ShapeType.Cone:
                GenerateCone(previewRoot.transform);
                break;
            case ShapeType.Cylinder:
                GenerateCylinder(previewRoot.transform);
                break;
            case ShapeType.Sphere:
                GenerateSphere(previewRoot.transform);
                break;
            case ShapeType.Helix:
                GenerateHelix(previewRoot.transform);
                break;
            case ShapeType.Star:
                GenerateStar(previewRoot.transform);
                break;
        }
    }

    public void AcceptShape()
    {
#if UNITY_EDITOR
        if (IsPartOfPrefabAsset())
        {
            Debug.LogWarning("Cannot accept shape while editing prefab asset.");
            return;
        }
#endif

        if (previewRoot == null)
        {
            Debug.LogWarning("No preview to accept. Generate a shape first.");
            return;
        }

        previewRoot.name = shapeType.ToString();
        previewRoot.transform.SetParent(null);
        previewRoot = null;
    }

    public void SaveAsPrefab()
    {
#if UNITY_EDITOR
        if (IsPartOfPrefabAsset())
        {
            Debug.LogWarning("Cannot save prefab while editing prefab asset.");
            return;
        }

        if (previewRoot == null)
        {
            Debug.LogWarning("No preview to save. Generate a shape first.");
            return;
        }

        string path = UnityEditor.EditorUtility.SaveFilePanelInProject(
            "Save Shape as Prefab",
            shapeType.ToString() + "_Shape",
            "prefab",
            "Choose where to save the prefab"
        );

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        GameObject tempObject = new GameObject(shapeType.ToString());
        Transform[] children = new Transform[previewRoot.transform.childCount];
        for (int i = 0; i < previewRoot.transform.childCount; i++)
        {
            children[i] = previewRoot.transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            child.SetParent(tempObject.transform);
        }

        GameObject prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(tempObject, path);

        foreach (Transform child in children)
        {
            child.SetParent(previewRoot.transform);
        }

        DestroyImmediate(tempObject);

        if (prefab != null)
        {
            UnityEditor.EditorUtility.DisplayDialog("Success", $"Prefab saved to:\n{path}", "OK");
            UnityEditor.EditorGUIUtility.PingObject(prefab);
        }
#endif
    }

    public void ClearPreview()
    {
#if UNITY_EDITOR
        if (IsPartOfPrefabAsset()) return;
#endif

        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }
    }

    int CalculateTotalObjects()
    {
        switch (shapeType)
        {
            case ShapeType.Line: 
                return Mathf.Max(2, Mathf.RoundToInt(lineSize / spacing) + 1);
            case ShapeType.Grid:
                int countX = Mathf.Max(2, Mathf.RoundToInt(gridSizeX / spacing) + 1);
                int countZ = Mathf.Max(2, Mathf.RoundToInt(gridSizeZ / spacing) + 1);
                return fillGrid ? countX * countZ : (countX * 2 + countZ * 2 - 4);
            case ShapeType.Square:
                int sqCount = Mathf.Max(2, Mathf.RoundToInt(squareSizeUnits / spacing) + 1);
                return fillSquare ? sqCount * sqCount : (sqCount * 4 - 4);
            case ShapeType.Spiral: return CalculateSpiralCount();
            case ShapeType.Helix: return Mathf.RoundToInt(helixRotations * helixPointsPerRotation);
            case ShapeType.Star: return starPoints * 2;
            case ShapeType.Circle:
                return fillCircle ? CalculateFilledCircleCount(radius) : CalculateCircleRingCount(radius);
            case ShapeType.Ring:
                return CalculateRingCount();
            case ShapeType.Pyramid:
                return CalculatePyramidCount();
            case ShapeType.Cone:
                return CalculateConeCount();
            case ShapeType.Cylinder:
                return CalculateCylinderCount();
            case ShapeType.Sphere:
                return CalculateSphereCount();
            default: return 100;
        }
    }

    int CalculateCircleRingCount(float r)
    {
        if (r <= 0) return 0;
        float circumference = 2f * Mathf.PI * r;
        return Mathf.Max(1, Mathf.RoundToInt(circumference / spacing));
    }

    int CalculateFilledCircleCount(float r)
    {
        if (r <= 0) return 0;
        int rings = Mathf.Max(1, Mathf.RoundToInt(r / spacing));
        int count = 1; // center point
        for (int i = 1; i <= rings; i++)
        {
            float currentR = (float)i / rings * r;
            count += CalculateCircleRingCount(currentR);
        }
        return count;
    }

    int CalculateRingCount()
    {
        int count = CalculateCircleRingCount(radius);
        if (innerRadius > 0 && innerRadius < radius)
        {
            int rings = Mathf.Max(1, Mathf.RoundToInt((radius - innerRadius) / spacing));
            for (int i = 1; i < rings; i++)
            {
                float currentR = Mathf.Lerp(innerRadius, radius, (float)i / rings);
                count += CalculateCircleRingCount(currentR);
            }
            count += CalculateCircleRingCount(innerRadius);
        }
        return count;
    }

    int CalculateSpiralCount()
    {
        // Matches the arc-length algorithm in GenerateSpiral
        float power = 1f / spiralDecay;
        float maxTheta = spiralRotations * 2f * Mathf.PI;
        float maxRadius = spiralRadiusGrowth * spiralRotations;
        float theta = 0f;
        int count = 0;
        int safetyLimit = 10000;
        
        while (theta <= maxTheta && count < safetyLimit)
        {
            count++;
            float t = theta / maxTheta;
            float r = maxRadius * Mathf.Pow(t, power);
            float drdt = (t > 0.0001f) 
                ? (maxRadius * power * Mathf.Pow(t, power - 1f) / maxTheta) 
                : (maxRadius * power / maxTheta);
            float arcFactor = Mathf.Sqrt(r * r + drdt * drdt);
            arcFactor = Mathf.Max(arcFactor, spacing * 0.1f);
            float dTheta = spacing / arcFactor;
            theta += dTheta;
        }
        return count;
    }

    int CalculatePyramidCount()
    {
        int baseCount = Mathf.Max(2, Mathf.RoundToInt(pyramidSize / spacing) + 1);
        int totalLayers = Mathf.Max(2, Mathf.RoundToInt(pyramidSize / verticalSpacing) + 1);
        int count = 0;
        for (int level = 0; level < totalLayers; level++)
        {
            float verticalProgress = (float)level / (totalLayers - 1);
            int currentCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * (1f - verticalProgress)));
            count += currentCount * currentCount;
        }
        return count;
    }

    int CalculateConeCount()
    {
        int layers = Mathf.Max(2, Mathf.RoundToInt(coneHeightUnits / verticalSpacing) + 1);
        int count = 0;
        for (int i = 0; i < layers; i++)
        {
            float t = (float)i / (layers - 1);
            float currentRadius = Mathf.Lerp(coneRadius, 0, t);
            if (currentRadius < spacing * 0.5f)
                count += 1;
            else
                count += fillCone ? CalculateFilledCircleCount(currentRadius) : CalculateCircleRingCount(currentRadius);
        }
        return count;
    }

    int CalculateCylinderCount()
    {
        int layers = Mathf.Max(2, Mathf.RoundToInt(cylinderHeightUnits / verticalSpacing) + 1);
        return layers * (fillCylinder ? CalculateFilledCircleCount(cylinderRadius) : CalculateCircleRingCount(cylinderRadius));
    }

    int CalculateSphereCount()
    {
        int count = 0;
        for (int ring = 0; ring < sphereRings; ring++)
        {
            float phi = Mathf.PI * (float)ring / (sphereRings - 1);
            float ringRadius = sphereRadius * Mathf.Sin(phi);
            if (fillSphere)
            {
                int subRings = Mathf.Max(1, Mathf.RoundToInt(ringRadius / spacing));
                for (int sr = 0; sr <= subRings; sr++)
                {
                    float currentR = (float)sr / Mathf.Max(1, subRings) * ringRadius;
                    count += CalculateCircleRingCount(currentR);
                }
            }
            else
            {
                count += CalculateCircleRingCount(ringRadius);
            }
        }
        return count;
    }

    void GenerateLine(Transform parent)
    {
        Vector3 dir = lineDirection.normalized;
        float effectiveLineSize = lineSize * completeness;
        int pointCount = Mathf.Max(2, Mathf.RoundToInt(effectiveLineSize / spacing) + 1);
        float actualSpacing = effectiveLineSize / (pointCount - 1);

        // Calculate perpendicular vector for wave offset
        Vector3 perpendicular = Vector3.Cross(dir, Vector3.up);
        if (perpendicular.magnitude < 0.001f)
        {
            perpendicular = Vector3.Cross(dir, Vector3.right);
        }
        perpendicular.Normalize();

        if (!useWave)
        {
            // Simple straight line
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 pos = dir * (i * actualSpacing);
                SpawnPrefab(pos, parent, Vector3.zero);
            }
        }
        else
        {
            // Wave line with arc-length parameterization for even spacing
            float omega = 2f * Mathf.PI * waveFrequency / lineSize;
            
            // Walk along the curve with constant arc-length steps
            float s = 0f;
            int count = 0;
            int safetyLimit = pointCount * 10;
            
            while (s <= effectiveLineSize + 0.001f && count < safetyLimit)
            {
                float wave = Mathf.Sin(omega * s) * waveAmplitude;
                Vector3 pos = dir * s + perpendicular * wave;
                SpawnPrefab(pos, parent, Vector3.zero);
                
                // Find next s such that arc length equals spacing
                float arcRemaining = spacing;
                float stepSize = spacing * 0.05f;
                while (arcRemaining > 0.0001f && s <= effectiveLineSize)
                {
                    float dydx = waveAmplitude * omega * Mathf.Cos(omega * s);
                    float arcFactor = Mathf.Sqrt(1f + dydx * dydx);
                    float ds = Mathf.Min(stepSize, arcRemaining / arcFactor);
                    s += ds;
                    arcRemaining -= ds * arcFactor;
                }
                count++;
            }
        }
    }

    void GenerateGrid(Transform parent)
    {
        int countX = Mathf.Max(2, Mathf.RoundToInt(gridSizeX / spacing) + 1);
        int countZ = Mathf.Max(2, Mathf.RoundToInt(gridSizeZ / spacing) + 1);
        float spacingX = gridSizeX / (countX - 1);
        float spacingZ = gridSizeZ / (countZ - 1);
        float centerOffsetX = gridSizeX * 0.5f;
        float centerOffsetZ = gridSizeZ * 0.5f;

        if (fillGrid)
        {
            int totalCells = countX * countZ;
            int cellsToFill = Mathf.Max(1, Mathf.RoundToInt(totalCells * completeness));
            int cellIndex = 0;
            
            for (int x = 0; x < countX && cellIndex < cellsToFill; x++)
            {
                float posX = x * spacingX - centerOffsetX;
                for (int z = 0; z < countZ && cellIndex < cellsToFill; z++)
                {
                    SpawnPrefab(new Vector3(posX, 0, z * spacingZ - centerOffsetZ), parent, Vector3.zero);
                    cellIndex++;
                }
            }
        }
        else
        {
            // Calculate total perimeter points for the grid
            int totalPerimeterPoints = (countX - 1) * 2 + (countZ - 1) * 2;
            int pointsToDraw = Mathf.Max(1, Mathf.RoundToInt(totalPerimeterPoints * completeness));
            int pointIndex = 0;
            
            float bottomZ = -centerOffsetZ;
            float topZ = centerOffsetZ;
            float leftX = -centerOffsetX;
            float rightX = centerOffsetX;
            
            // Bottom edge (left to right)
            for (int x = 0; x < countX - 1 && pointIndex < pointsToDraw; x++)
            {
                float posX = x * spacingX - centerOffsetX;
                SpawnPrefab(new Vector3(posX, 0, bottomZ), parent, Vector3.zero);
                pointIndex++;
            }
            // Right edge (bottom to top)
            for (int z = 0; z < countZ - 1 && pointIndex < pointsToDraw; z++)
            {
                float posZ = z * spacingZ - centerOffsetZ;
                SpawnPrefab(new Vector3(rightX, 0, posZ), parent, Vector3.zero);
                pointIndex++;
            }
            // Top edge (right to left)
            for (int x = countX - 1; x > 0 && pointIndex < pointsToDraw; x--)
            {
                float posX = x * spacingX - centerOffsetX;
                SpawnPrefab(new Vector3(posX, 0, topZ), parent, Vector3.zero);
                pointIndex++;
            }
            // Left edge (top to bottom)
            for (int z = countZ - 1; z > 0 && pointIndex < pointsToDraw; z--)
            {
                float posZ = z * spacingZ - centerOffsetZ;
                SpawnPrefab(new Vector3(leftX, 0, posZ), parent, Vector3.zero);
                pointIndex++;
            }
        }
    }

    void GenerateSquare(Transform parent)
    {
        int count = Mathf.Max(2, Mathf.RoundToInt(squareSizeUnits / spacing) + 1);
        float actualSpacing = squareSizeUnits / (count - 1);
        float centerOffset = squareSizeUnits * 0.5f;

        if (fillSquare)
        {
            // For filled square, completeness affects how much of the area is filled
            int totalCells = count * count;
            int cellsToFill = Mathf.Max(1, Mathf.RoundToInt(totalCells * completeness));
            int cellIndex = 0;
            
            for (int x = 0; x < count && cellIndex < cellsToFill; x++)
            {
                float posX = x * actualSpacing - centerOffset;
                for (int z = 0; z < count && cellIndex < cellsToFill; z++)
                {
                    SpawnPrefab(new Vector3(posX, 0, z * actualSpacing - centerOffset), parent, Vector3.zero);
                    cellIndex++;
                }
            }
        }
        else
        {
            // For outline square, completeness determines how much of the perimeter is drawn
            // Total perimeter points = 4 edges, we go around clockwise starting from bottom-left
            float minEdge = -centerOffset;
            float maxEdge = centerOffset;
            
            // Calculate total perimeter points
            int totalPerimeterPoints = (count - 1) * 4; // 4 sides, minus 4 corners counted once
            int pointsToDraw = Mathf.Max(1, Mathf.RoundToInt(totalPerimeterPoints * completeness));
            int pointIndex = 0;
            
            // Bottom edge (left to right)
            for (int i = 0; i < count - 1 && pointIndex < pointsToDraw; i++)
            {
                float pos = i * actualSpacing - centerOffset;
                SpawnPrefab(new Vector3(pos, 0, minEdge), parent, Vector3.zero);
                pointIndex++;
            }
            // Right edge (bottom to top)
            for (int i = 0; i < count - 1 && pointIndex < pointsToDraw; i++)
            {
                float pos = i * actualSpacing - centerOffset;
                SpawnPrefab(new Vector3(maxEdge, 0, pos), parent, Vector3.zero);
                pointIndex++;
            }
            // Top edge (right to left)
            for (int i = count - 1; i > 0 && pointIndex < pointsToDraw; i--)
            {
                float pos = i * actualSpacing - centerOffset;
                SpawnPrefab(new Vector3(pos, 0, maxEdge), parent, Vector3.zero);
                pointIndex++;
            }
            // Left edge (top to bottom)
            for (int i = count - 1; i > 0 && pointIndex < pointsToDraw; i--)
            {
                float pos = i * actualSpacing - centerOffset;
                SpawnPrefab(new Vector3(minEdge, 0, pos), parent, Vector3.zero);
                pointIndex++;
            }
        }
    }

    void GenerateCircle(Transform parent, Vector3 center, float r, bool filled)
    {
        if (r <= 0) return;

        int rings = Mathf.Max(1, Mathf.RoundToInt(r / spacing));

        if (filled)
        {
            SpawnPrefab(center, parent, Vector3.zero);
            for (int i = 1; i <= rings; i++)
            {
                float currentR = (float)i / rings * r;
                GenerateCircleRing(parent, center, currentR);
            }
        }
        else
        {
            GenerateCircleRing(parent, center, r);
        }
    }

    void GenerateCircleRing(Transform parent, Vector3 center, float r)
    {
        GenerateCircleRing(parent, center, r, completeness);
    }

    void GenerateCircleRing(Transform parent, Vector3 center, float r, float arcCompleteness)
    {
        float circumference = 2f * Mathf.PI * r;
        int fullCount = Mathf.Max(1, Mathf.RoundToInt(circumference / spacing));
        int count = Mathf.Max(1, Mathf.RoundToInt(fullCount * arcCompleteness));
        float totalArc = 2f * Mathf.PI * arcCompleteness;
        float radStep = count > 1 ? totalArc / count : 0f;
        bool needsLookDir = alignmentMode == AlignmentMode.FaceOutward;

        for (int i = 0; i < count; i++)
        {
            float rad = i * radStep;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            Vector3 offset = new Vector3(cos * r, 0f, sin * r);
            Vector3 pos = center + offset;
            Vector3 lookDir = needsLookDir ? new Vector3(cos, 0f, sin) : Vector3.zero;
            SpawnPrefab(pos, parent, lookDir);
        }
    }

    void GenerateRing(Transform parent)
    {
        GenerateCircleRing(parent, Vector3.zero, radius);

        if (innerRadius > 0 && innerRadius < radius)
        {
            int rings = Mathf.Max(1, Mathf.RoundToInt((radius - innerRadius) / spacing));
            for (int i = 1; i < rings; i++)
            {
                float currentR = Mathf.Lerp(innerRadius, radius, (float)i / rings);
                GenerateCircleRing(parent, Vector3.zero, currentR);
            }
            GenerateCircleRing(parent, Vector3.zero, innerRadius);
        }
    }

    void GenerateSpiral(Transform parent)
    {
        // Spiral with adjustable decay using inverse power for intuitive control
        // decay = 1: linear (Archimedean)
        // decay < 1: tighter rings at center, spread out toward edge  
        // decay > 1: spread out at center, tighter rings toward edge
        // We use power = 1/decay so that higher decay values compress outer rings
        float power = 1f / spiralDecay;
        float maxTheta = spiralRotations * 2f * Mathf.PI * completeness;
        float maxRadius = spiralRadiusGrowth * spiralRotations * completeness;
        float fullMaxTheta = spiralRotations * 2f * Mathf.PI;
        bool needsLookDir = alignmentMode == AlignmentMode.AlongPath;
        
        float theta = 0f;
        int safetyLimit = 10000;
        int count = 0;
        
        while (theta <= maxTheta && count < safetyLimit)
        {
            float t = theta / fullMaxTheta;
            float r = spiralRadiusGrowth * spiralRotations * Mathf.Pow(t, power);
            
            float cos = Mathf.Cos(theta);
            float sin = Mathf.Sin(theta);
            
            Vector3 pos = new Vector3(cos * r, 0f, sin * r);
            Vector3 lookDir = needsLookDir ? new Vector3(-sin, 0f, cos) : Vector3.zero;
            SpawnPrefab(pos, parent, lookDir);
            
            // Arc length step: ds/dtheta = sqrt(r^2 + (dr/dtheta)^2)
            // dr/dtheta = maxRadius * power * t^(power-1) / maxTheta
            float drdt;
            if (t > 0.0001f)
            {
                drdt = spiralRadiusGrowth * spiralRotations * power * Mathf.Pow(t, power - 1f) / fullMaxTheta;
            }
            else
            {
                // At t=0, use linear approximation to avoid numerical issues
                drdt = spiralRadiusGrowth * spiralRotations * power / fullMaxTheta;
            }
            
            float arcFactor = Mathf.Sqrt(r * r + drdt * drdt);
            // Ensure minimum step to avoid getting stuck at center
            float minArcFactor = spacing * 0.1f;
            arcFactor = Mathf.Max(arcFactor, minArcFactor);
            float dTheta = spacing / arcFactor;
            theta += dTheta;
            count++;
        }
    }

    void GeneratePyramid(Transform parent)
    {
        // Horizontal spacing determines base grid density
        int baseCount = Mathf.Max(2, Mathf.RoundToInt(pyramidSize / spacing) + 1);
        float horizontalSpacing = pyramidSize / (baseCount - 1);
        float baseExtent = pyramidSize * 0.5f;

        // Vertical spacing determines number of layers
        int totalLayers = Mathf.Max(2, Mathf.RoundToInt(pyramidSize / verticalSpacing) + 1);
        int layersToGenerate = Mathf.Max(1, Mathf.RoundToInt(totalLayers * completeness));
        float actualVerticalSpacing = pyramidSize / (totalLayers - 1);

        for (int level = 0; level < layersToGenerate; level++)
        {
            // Calculate how much the layer shrinks based on vertical progress
            float verticalProgress = (float)level / (totalLayers - 1);
            int currentCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * (1f - verticalProgress)));
            float levelSize = (currentCount - 1) * horizontalSpacing;
            float levelOffset = (pyramidSize - levelSize) * 0.5f;
            float y = level * actualVerticalSpacing;

            for (int x = 0; x < currentCount; x++)
            {
                for (int z = 0; z < currentCount; z++)
                {
                    Vector3 pos = new Vector3(
                        x * horizontalSpacing + levelOffset - baseExtent,
                        y,
                        z * horizontalSpacing + levelOffset - baseExtent
                    );
                    SpawnPrefab(pos, parent, Vector3.zero);
                }
            }
        }
    }

    void GenerateCone(Transform parent)
    {
        // Vertical spacing determines number of layers
        int fullLayers = Mathf.Max(2, Mathf.RoundToInt(coneHeightUnits / verticalSpacing) + 1);
        int layers = Mathf.Max(1, Mathf.RoundToInt(fullLayers * completeness));
        float effectiveHeight = coneHeightUnits * completeness;
        float layerSpacing = layers > 1 ? effectiveHeight / (layers - 1) : 0;

        for (int i = 0; i < layers; i++)
        {
            float y = i * layerSpacing;
            float t = layers > 1 ? (float)i / (layers - 1) : 0;

            float currentRadius = Mathf.Lerp(coneRadius, 0, t);

            // Horizontal spacing is used for circle density
            if (currentRadius < spacing * 0.5f)
            {
                SpawnPrefab(new Vector3(0, y, 0), parent, Vector3.zero);
            }
            else
            {
                GenerateCircle(parent, new Vector3(0, y, 0), currentRadius, fillCone);
            }
        }
    }

    void GenerateCylinder(Transform parent)
    {
        // Vertical spacing determines number of layers
        int fullLayers = Mathf.Max(2, Mathf.RoundToInt(cylinderHeightUnits / verticalSpacing) + 1);
        int layers = Mathf.Max(1, Mathf.RoundToInt(fullLayers * completeness));
        float effectiveHeight = cylinderHeightUnits * completeness;
        float layerSpacing = layers > 1 ? effectiveHeight / (layers - 1) : 0;

        for (int i = 0; i < layers; i++)
        {
            float y = i * layerSpacing;
            // Horizontal spacing is used by GenerateCircle for circle density
            GenerateCircle(parent, new Vector3(0, y, 0), cylinderRadius, fillCylinder);
        }
    }

    void GenerateSphere(Transform parent)
    {
        int ringsToGenerate = Mathf.Max(1, Mathf.RoundToInt(sphereRings * completeness));
        
        for (int ring = 0; ring < ringsToGenerate; ring++)
        {
            float phi = Mathf.PI * (float)ring / (sphereRings - 1);
            float y = sphereRadius * Mathf.Cos(phi);
            float ringRadius = sphereRadius * Mathf.Sin(phi);

            if (fillSphere)
            {
                int subRings = Mathf.Max(1, Mathf.RoundToInt(ringRadius / spacing));
                for (int sr = 0; sr <= subRings; sr++)
                {
                    float currentR = (float)sr / Mathf.Max(1, subRings) * ringRadius;
                    GenerateCircleRing(parent, new Vector3(0, y, 0), currentR);
                }
            }
            else
            {
                GenerateCircleRing(parent, new Vector3(0, y, 0), ringRadius);
            }
        }
    }

    void GenerateHelix(Transform parent)
    {
        int fullPoints = Mathf.RoundToInt(helixRotations * helixPointsPerRotation);
        int totalPoints = Mathf.Max(1, Mathf.RoundToInt(fullPoints * completeness));
        float radStep = (2f * Mathf.PI * helixRotations * completeness) / totalPoints;
        float heightStep = (helixHeight * completeness) / totalPoints;
        bool needsLookDir = alignmentMode == AlignmentMode.AlongPath;

        for (int i = 0; i <= totalPoints; i++)
        {
            float rad = i * radStep;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float y = i * heightStep;

            Vector3 pos = new Vector3(cos * helixRadius, y, sin * helixRadius);
            Vector3 lookDir = needsLookDir ? new Vector3(-sin, 0f, cos) : Vector3.zero;
            SpawnPrefab(pos, parent, lookDir);
        }
    }

    void GenerateStar(Transform parent)
    {
        int pointsToGenerate = Mathf.Max(1, Mathf.RoundToInt(starPoints * completeness));
        float radStep = (2f * Mathf.PI) / starPoints;
        float halfRadStep = radStep * 0.5f;
        bool needsLookDir = alignmentMode == AlignmentMode.FaceOutward;

        // Calculate how many full point pairs and partial points to render
        float totalStarSegments = starPoints * 2 * completeness;
        int fullSegments = Mathf.FloorToInt(totalStarSegments);

        for (int seg = 0; seg < fullSegments; seg++)
        {
            int pointIndex = seg / 2;
            bool isOuter = (seg % 2) == 0;

            if (isOuter)
            {
                float outerRad = pointIndex * radStep;
                float outerCos = Mathf.Cos(outerRad);
                float outerSin = Mathf.Sin(outerRad);
                Vector3 outerPos = new Vector3(outerCos * starOuterRadius, 0f, outerSin * starOuterRadius);
                Vector3 outerLookDir = needsLookDir ? new Vector3(outerCos, 0f, outerSin) : Vector3.zero;
                SpawnPrefab(outerPos, parent, outerLookDir);
            }
            else
            {
                float innerRad = pointIndex * radStep + halfRadStep;
                float innerCos = Mathf.Cos(innerRad);
                float innerSin = Mathf.Sin(innerRad);
                Vector3 innerPos = new Vector3(innerCos * starInnerRadius, 0f, innerSin * starInnerRadius);
                Vector3 innerLookDir = needsLookDir ? new Vector3(innerCos, 0f, innerSin) : Vector3.zero;
                SpawnPrefab(innerPos, parent, innerLookDir);
            }
        }
    }

    void SpawnPrefab(Vector3 position, Transform parent, Vector3 lookDirection)
    {
        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        if (prefab == null) return;

        if (positionJitter)
        {
            position += new Vector3(
                Random.Range(-jitterAmount, jitterAmount),
                Random.Range(-jitterAmount, jitterAmount),
                Random.Range(-jitterAmount, jitterAmount)
            );
        }

        position.y += heightOffset;

        GameObject obj;
#if UNITY_EDITOR
        // Always use InstantiatePrefab to maintain prefab link and avoid (Clone) suffix
        obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#else
        obj = Instantiate(prefab, parent);
#endif
        obj.transform.localPosition = position;

        Quaternion rotation = Quaternion.identity;

        if (alignmentMode == AlignmentMode.FaceCenter && position != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(-position.normalized);
        }
        else if (alignmentMode == AlignmentMode.FaceOutward && position != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(position.normalized);
        }
        else if (alignmentMode == AlignmentMode.AlongPath && lookDirection != Vector3.zero)
        {
            rotation = Quaternion.LookRotation(lookDirection);
        }

        if (randomRotation)
        {
            rotation *= Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        obj.transform.rotation = rotation;

        float scale = 1f;

        if (scaleGradient && totalGradientObjects > 1)
        {
            float t = (float)currentGradientIndex / (totalGradientObjects - 1);
            scale = Mathf.Lerp(gradientStartScale, gradientEndScale, t);
            currentGradientIndex++;
        }

        if (randomScale)
        {
            scale *= Random.Range(scaleRange.x, scaleRange.y);
        }

        obj.transform.localScale = obj.transform.localScale * scale;
    }


    void OnDestroy()
    {
        ClearPreview();
    }
}
