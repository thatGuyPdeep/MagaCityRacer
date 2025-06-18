# MagaCityRacer - Mobile Optimization Strategy

**Document Version:** 1.0  
**Date:** January 2024  
**Team:** Mobile Development & Performance Engineering  
**Target Platforms:** iOS 12+ / Android API 24+  
**Performance Goal:** 60fps on 3-year-old mid-tier devices

---

## 🎯 **MOBILE OPTIMIZATION OVERVIEW**

### **Performance Targets**
```
Device Category      | Target FPS | Resolution | Memory Limit | Battery/Hour
Premium (2+ years)   | 60 FPS     | 1080p+    | 2GB         | <12%
Mid-tier (3+ years)  | 45+ FPS    | 720p+     | 1.5GB       | <15%
Budget (4+ years)    | 30+ FPS    | 720p      | 1GB         | <20%
```

### **Platform-Specific Targets**
**iOS Devices:**
- iPhone 12+ (A14): 60fps at 1080p
- iPhone X/XS (A11/A12): 45fps at 1080p
- iPhone 8 (A10): 30fps at 720p

**Android Devices:**
- Snapdragon 865+: 60fps at 1080p
- Snapdragon 730/Exynos 9820: 45fps at 720p
- Snapdragon 660/older: 30fps at 720p

---

## 🔧 **UNITY DOTS MOBILE OPTIMIZATIONS**

### **ECS Performance Strategy**
```csharp
// Burst-compiled systems for critical path
[BurstCompile(CompileSynchronously = true)]
public partial struct MobileOptimizedVehicleSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Mobile-specific batch processing
        var job = new VehicleBatchJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            BatchSize = GetOptimalBatchSize()
        };
        job.ScheduleParallel();
    }
    
    private int GetOptimalBatchSize()
    {
        // Adjust batch size based on device capability
        return SystemInfo.processorCount <= 4 ? 32 : 64;
    }
}
```

### **Memory Management Strategy**
```csharp
public class MobileMemoryManager : SystemBase
{
    private readonly int[] memoryBudgets = {
        1024,  // Low-end devices (1GB)
        1536,  // Mid-tier devices (1.5GB)
        2048   // High-end devices (2GB)
    };
    
    protected override void OnCreate()
    {
        var deviceTier = DetectDeviceTier();
        SetMemoryBudget(memoryBudgets[deviceTier]);
    }
    
    private void SetMemoryBudget(int budgetMB)
    {
        // Configure entity pooling based on memory budget
        var poolSize = budgetMB / 4; // 25% for entity pools
        ConfigureEntityPools(poolSize);
        
        // Configure texture streaming
        ConfigureTextureStreaming(budgetMB / 2); // 50% for textures
    }
}
```

---

## 📱 **PLATFORM-SPECIFIC OPTIMIZATIONS**

### **iOS Optimization**
```csharp
#if UNITY_IOS
public class iOSOptimizations : MonoBehaviour
{
    void Start()
    {
        // Metal rendering optimizations
        QualitySettings.renderPipeline = UniversalRenderPipelineAsset;
        
        // iOS-specific memory management
        Application.lowMemory += HandleLowMemory;
        
        // Thermal state monitoring
        StartCoroutine(MonitorThermalState());
    }
    
    private void HandleLowMemory()
    {
        // Aggressive memory cleanup
        Resources.UnloadUnusedAssets();
        GC.Collect();
        
        // Reduce quality settings
        var adaptiveQuality = FindObjectOfType<AdaptiveQualitySystem>();
        adaptiveQuality?.ReduceQuality();
    }
    
    private IEnumerator MonitorThermalState()
    {
        while (true)
        {
            var thermalState = GetThermalState();
            if (thermalState >= ThermalState.Serious)
            {
                ReducePerformanceForThermal();
            }
            yield return new WaitForSeconds(5f);
        }
    }
}
#endif
```

### **Android Optimization**
```csharp
#if UNITY_ANDROID
public class AndroidOptimizations : MonoBehaviour
{
    void Start()
    {
        // Android-specific graphics API selection
        if (SystemInfo.supportsGraphicsFormat(GraphicsFormat.R8G8B8A8_UNorm, FormatUsage.Render))
        {
            // Use Vulkan if available
            ConfigureVulkanSettings();
        }
        else
        {
            // Fallback to OpenGL ES
            ConfigureOpenGLSettings();
        }
        
        // Android memory management
        ConfigureAndroidMemory();
    }
    
    private void ConfigureVulkanSettings()
    {
        // Vulkan-specific optimizations
        QualitySettings.vSyncCount = 0; // Disable VSync for Vulkan
        Application.targetFrameRate = 60;
    }
    
    private void ConfigureAndroidMemory()
    {
        // Get available memory
        var memInfo = new AndroidJavaClass("android.app.ActivityManager.MemoryInfo");
        var activityManager = new AndroidJavaClass("android.app.ActivityManager");
        
        // Adjust settings based on available memory
        AdjustQualityForMemory(GetAvailableMemoryMB());
    }
}
#endif
```

---

## ⚡ **PERFORMANCE OPTIMIZATION SYSTEMS**

### **Adaptive Quality Management**
```csharp
public class MobileAdaptiveQuality : MonoBehaviour
{
    [Header("Performance Monitoring")]
    public float targetFrameTime = 16.67f; // 60fps
    public float criticalFrameTime = 33.33f; // 30fps
    
    [Header("Quality Levels")]
    public QualityLevel[] qualityLevels;
    
    private int currentQualityIndex = 2; // Start at high quality
    private float frameTimeAccumulator = 0f;
    private int frameCount = 0;
    
    void Update()
    {
        MonitorPerformance();
        MonitorBattery();
        MonitorThermal();
    }
    
    private void MonitorPerformance()
    {
        frameTimeAccumulator += Time.unscaledDeltaTime * 1000f; // Convert to ms
        frameCount++;
        
        if (frameCount >= 60) // Check every second
        {
            float avgFrameTime = frameTimeAccumulator / frameCount;
            
            if (avgFrameTime > criticalFrameTime)
            {
                // Critical performance issue
                ReduceQuality(2); // Drop 2 levels
            }
            else if (avgFrameTime > targetFrameTime * 1.3f)
            {
                ReduceQuality(1);
            }
            else if (avgFrameTime < targetFrameTime * 0.8f)
            {
                IncreaseQuality(1);
            }
            
            frameTimeAccumulator = 0f;
            frameCount = 0;
        }
    }
    
    private void ReduceQuality(int levels = 1)
    {
        int newIndex = Mathf.Max(0, currentQualityIndex - levels);
        if (newIndex != currentQualityIndex)
        {
            currentQualityIndex = newIndex;
            ApplyQualityLevel(qualityLevels[currentQualityIndex]);
            Debug.Log($"Quality reduced to level {currentQualityIndex}");
        }
    }
}

[System.Serializable]
public class QualityLevel
{
    public string name;
    public int maxEntityCount;
    public float renderScale;
    public int textureQuality;
    public bool particlesEnabled;
    public bool shadowsEnabled;
    public int targetFrameRate;
}
```

### **Battery Optimization System**
```csharp
public class BatteryOptimizationSystem : MonoBehaviour
{
    [Header("Battery Thresholds")]
    public float lowBatteryThreshold = 0.2f;
    public float criticalBatteryThreshold = 0.1f;
    
    private BatteryStatus lastBatteryStatus;
    private float lastBatteryLevel = 1f;
    
    void Start()
    {
        InvokeRepeating(nameof(CheckBatteryStatus), 0f, 30f); // Check every 30 seconds
    }
    
    private void CheckBatteryStatus()
    {
        float currentLevel = SystemInfo.batteryLevel;
        BatteryStatus currentStatus = SystemInfo.batteryStatus;
        
        // React to battery level changes
        if (currentLevel < criticalBatteryThreshold)
        {
            ActivateCriticalBatterySaving();
        }
        else if (currentLevel < lowBatteryThreshold && currentStatus != BatteryStatus.Charging)
        {
            ActivateBatterySavingMode();
        }
        else if (currentLevel > 0.5f || currentStatus == BatteryStatus.Charging)
        {
            DeactivateBatterySavingMode();
        }
        
        lastBatteryLevel = currentLevel;
        lastBatteryStatus = currentStatus;
    }
    
    private void ActivateCriticalBatterySaving()
    {
        Application.targetFrameRate = 20;
        SetQualityLevel(0); // Lowest quality
        
        // Disable non-essential systems
        DisableParticleEffects();
        DisableAmbientAudio();
        
        Debug.Log("Critical battery saving activated");
    }
    
    private void ActivateBatterySavingMode()
    {
        Application.targetFrameRate = 30;
        SetQualityLevel(1); // Low quality
        
        // Reduce update frequencies
        ReduceSystemUpdateRates();
        
        Debug.Log("Battery saving mode activated");
    }
}
```

---

## 🎮 **MOBILE INPUT OPTIMIZATION**

### **Touch Input Performance**
```csharp
public class OptimizedTouchInput : MonoBehaviour
{
    [Header("Touch Settings")]
    public float touchSensitivity = 1f;
    public float deadZone = 0.1f;
    public bool useInputFiltering = true;
    
    private Vector2 filteredInput;
    private Queue<Vector2> inputHistory = new Queue<Vector2>();
    private const int FILTER_SAMPLES = 3;
    
    void Update()
    {
        ProcessTouchInput();
    }
    
    private void ProcessTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch primaryTouch = Input.GetTouch(0);
            Vector2 rawInput = primaryTouch.deltaPosition;
            
            // Apply dead zone
            if (rawInput.magnitude < deadZone * Screen.height)
                rawInput = Vector2.zero;
            
            // Apply filtering for smooth input
            if (useInputFiltering)
            {
                rawInput = ApplyInputFilter(rawInput);
            }
            
            // Send to vehicle input system
            UpdateVehicleInput(rawInput);
        }
    }
    
    private Vector2 ApplyInputFilter(Vector2 input)
    {
        inputHistory.Enqueue(input);
        
        if (inputHistory.Count > FILTER_SAMPLES)
            inputHistory.Dequeue();
        
        Vector2 averageInput = Vector2.zero;
        foreach (var sample in inputHistory)
        {
            averageInput += sample;
        }
        
        return averageInput / inputHistory.Count;
    }
}
```

### **Gyroscope Optimization**
```csharp
public class OptimizedGyroscopeInput : MonoBehaviour
{
    [Header("Gyroscope Settings")]
    public float sensitivity = 2f;
    public float calibrationOffset = 0f;
    public bool enableLowPassFilter = true;
    
    private float filteredInput = 0f;
    private const float FILTER_STRENGTH = 0.1f;
    
    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Input.gyro.updateInterval = 1f / 60f; // 60Hz update rate
            CalibrateGyroscope();
        }
    }
    
    void Update()
    {
        if (Input.gyro.enabled)
        {
            ProcessGyroscopeInput();
        }
    }
    
    private void ProcessGyroscopeInput()
    {
        // Get rotation rate instead of attitude for better performance
        Vector3 rotationRate = Input.gyro.rotationRate;
        float tiltInput = -rotationRate.z * sensitivity;
        
        // Apply calibration offset
        tiltInput -= calibrationOffset;
        
        // Apply low-pass filter
        if (enableLowPassFilter)
        {
            filteredInput = Mathf.Lerp(filteredInput, tiltInput, FILTER_STRENGTH);
            tiltInput = filteredInput;
        }
        
        // Clamp and send to vehicle system
        tiltInput = Mathf.Clamp(tiltInput, -1f, 1f);
        UpdateVehicleInput(tiltInput);
    }
    
    public void CalibrateGyroscope()
    {
        if (Input.gyro.enabled)
        {
            calibrationOffset = Input.gyro.attitude.z;
        }
    }
}
```

---

## 🎨 **RENDERING OPTIMIZATION**

### **Mobile Rendering Pipeline**
```csharp
public class MobileRenderingOptimizer : MonoBehaviour
{
    [Header("Rendering Settings")]
    public bool useDynamicResolution = true;
    public float minRenderScale = 0.5f;
    public float maxRenderScale = 1.0f;
    
    [Header("LOD Settings")]
    public float[] lodDistances = { 25f, 50f, 100f };
    public float lodBias = 0.7f; // Aggressive LOD for mobile
    
    void Start()
    {
        ConfigureMobileRendering();
        SetupDynamicResolution();
    }
    
    private void ConfigureMobileRendering()
    {
        // Mobile-specific rendering settings
        QualitySettings.pixelLightCount = 1; // Single directional light
        QualitySettings.shadowCascades = 1; // Single shadow cascade
        QualitySettings.shadowDistance = 50f; // Limited shadow distance
        
        // LOD settings for mobile
        QualitySettings.lodBias = lodBias;
        QualitySettings.maximumLODLevel = 2; // Skip highest detail LODs
        
        // Texture settings
        QualitySettings.masterTextureLimit = GetOptimalTextureQuality();
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
    }
    
    private void SetupDynamicResolution()
    {
        if (useDynamicResolution && SystemInfo.supportsSetRenderTarget)
        {
            // Enable dynamic resolution scaling
            ScalableBufferManager.ResizeBuffers(maxRenderScale, maxRenderScale);
            
            StartCoroutine(MonitorAndAdjustResolution());
        }
    }
    
    private IEnumerator MonitorAndAdjustResolution()
    {
        while (true)
        {
            float currentFrameTime = Time.unscaledDeltaTime;
            float targetFrameTime = 1f / Application.targetFrameRate;
            
            if (currentFrameTime > targetFrameTime * 1.2f)
            {
                // Reduce resolution scale
                float newScale = Mathf.Max(minRenderScale, 
                    ScalableBufferManager.widthScaleFactor * 0.9f);
                ScalableBufferManager.ResizeBuffers(newScale, newScale);
            }
            else if (currentFrameTime < targetFrameTime * 0.8f)
            {
                // Increase resolution scale
                float newScale = Mathf.Min(maxRenderScale, 
                    ScalableBufferManager.widthScaleFactor * 1.05f);
                ScalableBufferManager.ResizeBuffers(newScale, newScale);
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
}
```

### **Particle System Optimization**
```csharp
public class MobileParticleOptimizer : MonoBehaviour
{
    [Header("Particle Settings")]
    public int maxParticles = 100;
    public bool enableDistanceCulling = true;
    public float cullingDistance = 50f;
    
    private ParticleSystem[] particleSystems;
    private Camera mainCamera;
    
    void Start()
    {
        particleSystems = FindObjectsOfType<ParticleSystem>();
        mainCamera = Camera.main;
        
        OptimizeParticleSystems();
    }
    
    private void OptimizeParticleSystems()
    {
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            
            // Limit particle count
            main.maxParticles = Mathf.Min(main.maxParticles, maxParticles);
            
            // Optimize for mobile
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            
            // Disable expensive features
            var collision = ps.collision;
            collision.enabled = false;
            
            var lights = ps.lights;
            lights.enabled = false;
            
            // Enable distance culling if requested
            if (enableDistanceCulling)
            {
                StartCoroutine(CullParticleSystem(ps));
            }
        }
    }
    
    private IEnumerator CullParticleSystem(ParticleSystem ps)
    {
        while (ps != null)
        {
            if (mainCamera != null)
            {
                float distance = Vector3.Distance(ps.transform.position, mainCamera.transform.position);
                bool shouldPlay = distance <= cullingDistance;
                
                if (shouldPlay && !ps.isPlaying)
                    ps.Play();
                else if (!shouldPlay && ps.isPlaying)
                    ps.Stop();
            }
            
            yield return new WaitForSeconds(0.5f); // Check every half second
        }
    }
}
```

---

## 📊 **PERFORMANCE MONITORING**

### **Real-Time Performance Dashboard**
```csharp
public class MobilePerformanceMonitor : MonoBehaviour
{
    [Header("Monitoring Settings")]
    public bool showDebugUI = true;
    public KeyCode toggleKey = KeyCode.F1;
    
    private float fps;
    private float memoryUsage;
    private int entityCount;
    private float batteryLevel;
    
    void Update()
    {
        UpdatePerformanceMetrics();
        
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugUI = !showDebugUI;
        }
    }
    
    private void UpdatePerformanceMetrics()
    {
        // FPS calculation
        fps = 1f / Time.unscaledDeltaTime;
        
        // Memory usage
        memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / 1024f / 1024f;
        
        // Entity count (if using DOTS)
        entityCount = GetEntityCount();
        
        // Battery level
        batteryLevel = SystemInfo.batteryLevel;
    }
    
    void OnGUI()
    {
        if (!showDebugUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"FPS: {fps:F1}");
        GUILayout.Label($"Memory: {memoryUsage:F1} MB");
        GUILayout.Label($"Entities: {entityCount}");
        GUILayout.Label($"Battery: {(batteryLevel >= 0 ? (batteryLevel * 100):F1 + "%" : "Unknown")}");
        GUILayout.Label($"Quality: {QualitySettings.GetQualityLevel()}");
        GUILayout.Label($"Resolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## 🧪 **TESTING & VALIDATION**

### **Automated Performance Testing**
```csharp
public class PerformanceTestSuite : MonoBehaviour
{
    [Header("Test Configuration")]
    public float testDuration = 60f; // 1 minute tests
    public string[] testScenarios;
    
    private List<PerformanceResult> testResults = new List<PerformanceResult>();
    
    [System.Serializable]
    public class PerformanceResult
    {
        public string scenario;
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float averageMemory;
        public float peakMemory;
        public bool passedTest;
    }
    
    public void RunPerformanceTests()
    {
        StartCoroutine(ExecuteTestSuite());
    }
    
    private IEnumerator ExecuteTestSuite()
    {
        testResults.Clear();
        
        foreach (string scenario in testScenarios)
        {
            yield return StartCoroutine(RunSingleTest(scenario));
        }
        
        GenerateTestReport();
    }
    
    private IEnumerator RunSingleTest(string scenario)
    {
        Debug.Log($"Running performance test: {scenario}");
        
        var result = new PerformanceResult { scenario = scenario };
        var fpsData = new List<float>();
        var memoryData = new List<float>();
        
        float startTime = Time.time;
        
        while (Time.time - startTime < testDuration)
        {
            fpsData.Add(1f / Time.unscaledDeltaTime);
            memoryData.Add(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / 1024f / 1024f);
            
            yield return new WaitForSeconds(0.1f); // Sample every 100ms
        }
        
        // Calculate results
        result.averageFPS = fpsData.Average();
        result.minFPS = fpsData.Min();
        result.maxFPS = fpsData.Max();
        result.averageMemory = memoryData.Average();
        result.peakMemory = memoryData.Max();
        
        // Determine pass/fail
        result.passedTest = result.averageFPS >= 30f && result.peakMemory <= 2048f;
        
        testResults.Add(result);
        
        Debug.Log($"Test completed: {scenario} - Average FPS: {result.averageFPS:F1}, Peak Memory: {result.peakMemory:F1}MB");
    }
    
    private void GenerateTestReport()
    {
        string report = "Performance Test Report\n";
        report += "========================\n\n";
        
        foreach (var result in testResults)
        {
            report += $"Scenario: {result.scenario}\n";
            report += $"  Average FPS: {result.averageFPS:F1}\n";
            report += $"  Min FPS: {result.minFPS:F1}\n";
            report += $"  Max FPS: {result.maxFPS:F1}\n";
            report += $"  Average Memory: {result.averageMemory:F1} MB\n";
            report += $"  Peak Memory: {result.peakMemory:F1} MB\n";
            report += $"  Result: {(result.passedTest ? "PASS" : "FAIL")}\n\n";
        }
        
        Debug.Log(report);
        
        // Save report to file
        System.IO.File.WriteAllText(Application.persistentDataPath + "/performance_report.txt", report);
    }
}
```

---

## 📋 **MOBILE OPTIMIZATION CHECKLIST**

### **Pre-Development Setup**
- [ ] Target device specifications documented
- [ ] Performance budgets established
- [ ] Testing device lab prepared
- [ ] Profiling tools configured

### **Code Optimization**
- [ ] DOTS systems Burst-compiled
- [ ] Memory pooling implemented
- [ ] Garbage collection minimized
- [ ] Platform-specific optimizations applied

### **Rendering Optimization**
- [ ] Dynamic resolution scaling implemented
- [ ] LOD system configured for mobile
- [ ] Particle systems optimized
- [ ] Texture streaming configured

### **Input Optimization**
- [ ] Touch input filtering implemented
- [ ] Gyroscope optimization applied
- [ ] Input dead zones configured
- [ ] Haptic feedback optimized

### **Performance Monitoring**
- [ ] Real-time performance dashboard
- [ ] Automated testing suite
- [ ] Battery monitoring system
- [ ] Thermal state monitoring

### **Quality Assurance**
- [ ] Performance testing on target devices
- [ ] Battery life validation
- [ ] Memory leak detection
- [ ] Thermal throttling testing

---

**Document Control:**  
**Created by:** Mobile Optimization Team  
**Reviewed by:** Technical Lead, Performance Engineering  
**Approved by:** Technical Director  
**Next Review Date:** [Monthly mobile optimization review]
