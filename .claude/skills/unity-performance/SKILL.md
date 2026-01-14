---
name: unity-performance
description: Unity performance optimization specialist for Hungry Snake game. Profiles and optimizes rendering, physics, memory usage, object pooling, DOTween animations, and Addressables loading. Use when addressing frame rate drops, memory leaks, loading times, GC spikes, or general performance issues in Unity C# projects.
---

# Unity Performance Optimization Expert

## Project-Specific Context

Hungry Snake uses performance-critical systems:
- **Multiple AI snakes** competing simultaneously
- **DOTween** for animations (growth, UI transitions)
- **Object pooling** via `FXPoolService` for VFX and particles
- **Addressables** for asset management
- **FlatKit shaders** for stylized rendering
- **Tutorial system** with real-time UI overlays

## Performance Profiling Approach

### Step 1: Identify Bottleneck
1. Use Unity Profiler (CPU, Rendering, Memory modules)
2. Check for GC spikes in Memory Profiler
3. Monitor frame time breakdown
4. Profile on target device (mobile if applicable)

### Step 2: Categorize Issue
- **CPU-bound**: Script execution, physics, AI
- **GPU-bound**: Rendering, shaders, overdraw
- **Memory**: GC pressure, allocations, leaks
- **I/O**: Asset loading, Addressables

### Step 3: Apply Targeted Optimizations
Use the strategies below based on profiling results.

## Optimization Strategies

### Object Pooling

**Current System**: `FXPoolService` at `Assets/_Game/Scripts/Services/FXPoolService.cs`

**Optimization Checklist**:
- Ensure all frequently spawned objects use pooling (food items, VFX, particles)
- Prewarm pools in `Start()` or level initialization
- Adjust pool sizes based on profiling (avoid over-allocation)
- Pool GameObjects, not just components

**Common Issues**:
```csharp
// BAD: Creates new instances every frame
Instantiate(foodPrefab, position, rotation);

// GOOD: Uses pooling
FXPoolService.Instance.GetPooledComponent<GameFood>(poolId);
```

**Check These Files**:
- `FXPoolService.cs`: Pool management
- `SnakeCollisionHandler.cs`: Food spawning
- `LevelFactory.cs`: Level object creation

### Component Caching

**Issue**: Repeated `GetComponent()` calls are expensive

**Solution**: Cache in `Awake()` or `Start()`

```csharp
// BAD: Every frame
void Update() {
    GetComponent<Rigidbody>().velocity = newVelocity;
}

// GOOD: Cached once
private Rigidbody _rigidbody;
void Awake() {
    _rigidbody = GetComponent<Rigidbody>();
}
void Update() {
    _rigidbody.velocity = newVelocity;
}
```

**Files to Review**:
- All controllers (`SnakeController`, `TutorialController`, etc.)
- Movement scripts (`SnakeMovement`, `AISnakeMovement`)
- Collision handlers

### DOTween Optimization

**Project Uses**: Extensive DOTween for growth animations and UI

**Best Practices**:
- Reuse tweens with `SetRecyclable(true)`
- Kill tweens when objects are destroyed
- Use `SetAutoKill(true)` for one-shot animations
- Avoid creating tweens every frame
- Use `DOTween.SetTweensCapacity()` for initial allocation

**Common Issues**:
```csharp
// BAD: Creates new tween constantly
void Update() {
    if (shouldGrow) {
        transform.DOScale(targetScale, duration);
    }
}

// GOOD: Create once, reuse or condition properly
private Tween _growTween;
void Grow() {
    _growTween?.Kill();
    _growTween = transform.DOScale(targetScale, duration).SetAutoKill(true);
}
```

**Check These Files**:
- `SnakeGrowth.cs`: Growth animations
- UI screens with transitions
- Any script using `DOTween` namespace

### Addressables Loading

**Optimization Strategies**:
- Preload frequently used assets during loading screens
- Use `AsyncOperationHandle` properly (release when done)
- Group related assets together
- Avoid loading same asset multiple times
- Use labels for batch loading

**Memory Management**:
```csharp
// Always release handles
var handle = Addressables.LoadAssetAsync<GameObject>(assetKey);
await handle.Task;
// ... use asset
Addressables.Release(handle);
```

**Files to Review**:
- `LevelFactory.cs`: Level asset loading
- Asset spawning services
- Any script using `Addressables` namespace

### Physics Optimization

**Snake System Considerations**:
- Multiple snakes with colliders and rigidbodies
- Continuous collision detection may be expensive

**Optimizations**:
- Use appropriate collision detection mode (Discrete vs Continuous)
- Minimize active rigidbodies (use kinematic when possible)
- Simplify collider shapes (capsules/spheres > mesh colliders)
- Use layer-based collision matrix (Physics settings)
- Consider trigger colliders for non-physical interactions

**Check These Files**:
- `SnakeCollisionHandler.cs`: Collision detection logic
- `BiteWidth.cs`: Swallow calculations
- Snake body segment prefabs

### UI Performance

**Current System**: UIManager with async panel transitions

**Optimizations**:
- Use `Canvas.renderMode = ScreenSpaceCamera` for 3D UI
- Separate static and dynamic UI into different canvases
- Disable raycast on non-interactive elements
- Use sprite atlases for UI textures
- Pool frequently shown/hidden panels

**Common Issues**:
```csharp
// BAD: Rebuilds canvas every frame
void Update() {
    scoreText.text = score.ToString();
}

// GOOD: Update only when changed
private int _lastScore;
void Update() {
    if (score != _lastScore) {
        scoreText.text = score.ToString();
        _lastScore = score;
    }
}
```

**Files to Review**:
- `UIManager.cs`: Panel management
- All UI screens (`UIScreen` subclasses)
- `UITutorialMessagePanel.cs`: Tutorial overlay

### Rendering Optimization

**FlatKit Shader Considerations**:
- Stylized shaders can be expensive
- Multiple materials = multiple draw calls

**Optimizations**:
- Use GPU instancing when possible
- Batch materials (shared materials for similar objects)
- Reduce overdraw (UI, transparent objects)
- Use occlusion culling for complex scenes
- LOD system for distant snakes (if applicable)

**Check These Files**:
- Material assignments in prefabs
- Shader settings in scene
- Camera settings (culling mask, render distance)

### Memory Management

**GC Optimization**:
- Avoid allocations in `Update()` or frequent methods
- Use object pooling for temporary objects
- Cache collections instead of creating new ones
- Use `StringBuilder` for string concatenation

**Common Allocations to Avoid**:
```csharp
// BAD: Allocates every frame
void Update() {
    var enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Allocates array
    Debug.Log("Enemies: " + enemyCount); // Allocates string
}

// GOOD: Cached and conditional
private List<GameObject> _enemyCache = new List<GameObject>();
void OnEnemySpawned(GameObject enemy) {
    _enemyCache.Add(enemy);
}
```

**Files to Review**:
- Any script with `Update()`, `FixedUpdate()`, or `LateUpdate()`
- Event handlers called frequently
- Collection usage (List, Dictionary, Arrays)

### AI Snake Optimization

**Multiple AI Snakes = Multiple Pathfinding/Decision Systems**

**Optimizations**:
- Stagger AI updates (don't update all on same frame)
- Use simpler AI for distant snakes
- Reduce update frequency based on distance to player
- Cache expensive calculations

**Example**:
```csharp
// Stagger updates across frames
private float _updateInterval = 0.1f;
private float _updateOffset;

void Start() {
    _updateOffset = Random.Range(0f, _updateInterval);
}

void Update() {
    if (Time.time >= _lastUpdate + _updateInterval + _updateOffset) {
        PerformExpensiveAIUpdate();
        _lastUpdate = Time.time;
    }
}
```

**Files to Review**:
- `AISnakeMovement.cs`: AI movement logic
- Any AI decision-making scripts
- Snake spawning and management

## Profiling Checklist

### CPU Profiling
- [ ] Identify most expensive scripts in Profiler
- [ ] Check for `GetComponent()` in hot paths
- [ ] Look for allocations in frequently called methods
- [ ] Verify object pooling is used for spawned objects
- [ ] Check AI update frequency

### GPU Profiling
- [ ] Count draw calls and batches
- [ ] Check for shader complexity
- [ ] Verify GPU instancing where applicable
- [ ] Look for overdraw issues
- [ ] Check texture sizes and compression

### Memory Profiling
- [ ] Monitor GC allocations and spikes
- [ ] Check for memory leaks (growing memory over time)
- [ ] Verify Addressables are properly released
- [ ] Look for unused loaded assets
- [ ] Check texture and mesh memory usage

## Performance Targets

Based on typical mobile/PC game standards:
- **Frame Rate**: 60 FPS (16.67ms per frame)
- **GC**: < 5ms spikes, < 100 allocations/frame
- **Draw Calls**: < 100 for mobile, < 500 for PC
- **Memory**: Stable over time (no growing trend)

## Common Performance Issues in This Project

### Issue: Frame drops when multiple snakes eat food
**Cause**: Instantiating food particles without pooling
**Solution**: Ensure `FXPoolService` is used for all VFX
**Files**: `SnakeCollisionHandler.cs`

### Issue: GC spikes during tutorial
**Cause**: String allocations in UI updates
**Solution**: Cache strings, update only when changed
**Files**: `UITutorialMessagePanel.cs`, tutorial steps

### Issue: Slow level transitions
**Cause**: Synchronous Addressables loading
**Solution**: Preload assets, use async loading with loading screen
**Files**: `LevelFactory.cs`, `LevelFlowService.cs`

### Issue: UI rebuild lag
**Cause**: Multiple canvases rebuilding simultaneously
**Solution**: Separate static/dynamic UI, disable when not visible
**Files**: UI panel prefabs, `UIManager.cs`

## Instructions

When optimizing performance:

1. **Profile first**: Use Unity Profiler to identify actual bottlenecks
2. **Measure impact**: Before/after comparison with concrete metrics
3. **Target worst offenders**: Focus on biggest performance gains first
4. **Preserve functionality**: Don't break existing features
5. **Test on target platform**: Mobile performance differs from editor
6. **Document changes**: Explain optimization rationale in code comments

## Examples

Use this skill when the user says:
- "The game is lagging when multiple snakes are on screen"
- "Frame rate drops during level transitions"
- "Memory keeps growing over time"
- "GC spikes are causing stuttering"
- "Loading times are too long"
- "UI animations are choppy"
- "Optimize the snake collision system"
- "Profile and improve performance"
