# MagaCityRacer - Technical Architecture Document

**Document Version:** 1.0  
**Date:** January 2024  
**Technical Team:** Lead Architect & Development Team  
**Unity Version:** 2023.3 LTS  
**DOTS Version:** Entities 1.0.16

---

## 🏗️ **TECHNICAL OVERVIEW**

### **Architecture Philosophy**
MagaCityRacer leverages Unity's DOTS (Data-Oriented Technology Stack) ECS (Entity-Component-System) architecture to achieve optimal performance on mobile devices. The system is designed for high entity counts, efficient memory usage, and consistent 60fps gameplay.

### **Core Technical Goals**
- **Performance Excellence:** 60fps on mid-tier mobile devices (3+ years old)
- **Memory Efficiency:** Minimal allocations during gameplay
- **Scalability:** Support for 100+ racing entities simultaneously
- **Mobile Optimization:** Battery life and thermal management
- **Maintainable Code:** Clean separation of concerns using ECS principles

### **Technology Stack**
```
Application Layer    | Unity 2023.3 LTS + DOTS 1.0
Architecture Layer   | Entity-Component-System (ECS)
Platform Layer      | iOS 12+ / Android API 24+
Rendering Pipeline   | Universal Render Pipeline (URP) 14.0.8
Physics Engine      | Unity Physics 1.0.16
Compilation         | Burst Compiler 1.8.8
Mathematics         | Unity.Mathematics 1.2.6
```

---

## 🎯 **ECS ARCHITECTURE DESIGN**

### **Entity Types & Archetypes**

#### **Primary Racing Entities**
```csharp
// Player Vehicle Entity
PlayerVehicleArchetype:
- VehicleComponent
- PlayerInputComponent  
- TransformComponent
- RenderMeshComponent
- PhysicsBodyComponent
- CameraTargetComponent

// AI Vehicle Entity
AIVehicleArchetype:
- VehicleComponent
- AIBehaviorComponent
- TransformComponent
- RenderMeshComponent
- PhysicsBodyComponent

// Track Entity
TrackArchetype:
- TrackComponent
- WaypointComponent
- ColliderComponent
- TransformComponent
- RenderMeshComponent

// Camera Entity
CameraArchetype:
- CameraTargetComponent
- RacingCameraComponent
- TransformComponent
```

#### **Supporting Entities**
```csharp
// Environment Entities
BuildingArchetype:
- TransformComponent
- RenderMeshComponent
- LODComponent

// Effect Entities
ParticleArchetype:
- ParticleComponent
- TransformComponent
- LifetimeComponent

// UI Entities
HUDArchetype:
- UIComponent
- TransformComponent
- CanvasComponent
```

### **Component Design Patterns**

#### **Core Components Structure**
```csharp
// High-frequency update components (hot data)
public struct VehicleComponent : IComponentData
{
    public float Speed;
    public float MaxSpeed;
    public float Acceleration;
    public float Handling;
    public float3 Velocity;
    public bool IsGrounded;
}

// Input data (frequent updates)
public struct PlayerInputComponent : IComponentData
{
    public float SteeringInput;
    public float AccelerationInput;
    public float BrakeInput;
    public bool BoostPressed;
    public InputMethod CurrentInputMethod;
}

// Configuration data (infrequent updates)
public struct VehicleConfigComponent : IComponentData
{
    public float MaxSpeedBase;
    public float AccelerationBase;
    public float HandlingBase;
    public float Mass;
    public VehicleType Type;
}
```

#### **Data Layout Optimization**
- **Hot/Cold Data Separation:** Frequently updated data in separate components
- **Memory Alignment:** Components aligned to cache line boundaries
- **Minimal Padding:** Struct layouts optimized for memory efficiency
- **Burst-Compatible:** All components compatible with Burst compilation

---

## ⚡ **SYSTEM ARCHITECTURE**

### **System Update Groups & Dependencies**

#### **System Execution Order**
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
- GameStateInitializationSystem
- VehicleSpawnSystem
- TrackInitializationSystem

[UpdateInGroup(typeof(SimulationSystemGroup))]
- PlayerInputSystem (Order = 100)
- AIBehaviorSystem (Order = 200)
- VehicleMovementSystem (Order = 300)
- PhysicsSystem (Order = 400)
- CollisionDetectionSystem (Order = 500)
- TrackProgressSystem (Order = 600)
- GameStateSystem (Order = 700)

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
- CameraFollowSystem (Order = 100)
- AudioSystem (Order = 200)
- EffectsSystem (Order = 300)

[UpdateInGroup(typeof(PresentationSystemGroup))]
- UIUpdateSystem
- HUDSystem
- RenderingSystem
```

### **Critical Systems Implementation**

#### **VehicleMovementSystem (Core Racing Physics)**
```csharp
[BurstCompile]
public partial struct VehicleMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        // Parallel job for vehicle physics
        var vehicleJob = new VehiclePhysicsJob
        {
            DeltaTime = deltaTime,
            MaxSpeed = 100f,
            Acceleration = 50f
        };
        
        vehicleJob.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct VehiclePhysicsJob : IJobEntity
{
    public float DeltaTime;
    public float MaxSpeed;
    public float Acceleration;
    
    private void Execute(ref LocalTransform transform,
                        ref VehicleComponent vehicle,
                        in PlayerInputComponent input)
    {
        // Burst-compiled physics calculations
        var acceleration = input.AccelerationInput * Acceleration * DeltaTime;
        vehicle.Speed = math.clamp(vehicle.Speed + acceleration, 0, MaxSpeed);
        
        var steering = input.SteeringInput * vehicle.Handling * DeltaTime;
        var rotation = quaternion.RotateY(steering);
        
        transform.Rotation = math.mul(transform.Rotation, rotation);
        transform.Position += math.forward(transform.Rotation) * vehicle.Speed * DeltaTime;
    }
}
```

#### **PlayerInputSystem (Mobile Input Processing)**
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class PlayerInputSystem : SystemBase
{
    private TouchControls touchControls;
    private InputAction steerAction;
    private InputAction accelerateAction;
    
    protected override void OnCreate()
    {
        touchControls = new TouchControls();
        steerAction = touchControls.Racing.Steer;
        accelerateAction = touchControls.Racing.Accelerate;
    }
    
    protected override void OnUpdate()
    {
        var steerValue = steerAction.ReadValue<float>();
        var accelerateValue = accelerateAction.ReadValue<float>();
        
        // Process gyroscope input if enabled
        var gyroInput = ProcessGyroscopeInput();
        
        Entities.ForEach((ref PlayerInputComponent input) =>
        {
            input.SteeringInput = steerValue + gyroInput;
            input.AccelerationInput = accelerateValue;
            input.CurrentInputMethod = DetermineInputMethod();
        }).Run();
    }
    
    private float ProcessGyroscopeInput()
    {
        if (!SystemInfo.supportsGyroscope) return 0f;
        
        var gyro = Input.gyro;
        if (!gyro.enabled) return 0f;
        
        // Convert gyroscope rotation to steering input
        var rotation = gyro.attitude;
        return math.clamp(rotation.z * 2f, -1f, 1f);
    }
}
```

#### **CameraFollowSystem (Dynamic Racing Camera)**
```csharp
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{
    private Camera mainCamera;
    
    protected override void OnCreate()
    {
        mainCamera = Camera.main;
        RequireForUpdate<CameraTargetComponent>();
    }
    
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        Entities
            .WithoutBurst() // Camera operations not Burst compatible
            .ForEach((Entity entity,
                     in CameraTargetComponent target,
                     in LocalTransform transform,
                     in VehicleComponent vehicle) =>
        {
            if (mainCamera == null) return;
            
            // Calculate camera position with speed-based offset
            var speedFactor = vehicle.Speed / vehicle.MaxSpeed;
            var followDistance = math.lerp(8f, 12f, speedFactor);
            var followHeight = math.lerp(3f, 5f, speedFactor);
            
            var targetPosition = transform.Position - 
                                math.forward(transform.Rotation) * followDistance +
                                math.up() * followHeight;
            
            var lookPosition = transform.Position + 
                              math.forward(transform.Rotation) * speedFactor * 10f;
            
            // Smooth camera movement
            var currentPosition = mainCamera.transform.position;
            var newPosition = math.lerp(currentPosition, targetPosition, deltaTime * 5f);
            
            mainCamera.transform.position = newPosition;
            mainCamera.transform.LookAt(lookPosition);
            
            // Dynamic FOV based on speed
            var targetFOV = math.lerp(60f, 80f, speedFactor);
            mainCamera.fieldOfView = math.lerp(mainCamera.fieldOfView, targetFOV, deltaTime * 2f);
            
        }).Run();
    }
}
```

---

## 📱 **MOBILE OPTIMIZATION STRATEGIES**

### **Performance Optimization Framework**

#### **CPU Optimization**
```csharp
// Burst Compilation for Critical Systems
[BurstCompile(CompileSynchronously = true)]
public partial struct HighFrequencySystem : ISystem
{
    // Main thread burst compilation for immediate performance
}

// Job System for Parallel Processing
[BurstCompile]
public partial struct ParallelVehicleJob : IJobEntity
{
    public void Execute(/* parameters */)
    {
        // Parallel execution across multiple threads
    }
}

// Memory-Efficient Component Updates
public partial class OptimizedUpdateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Use ComponentLookup for random access
        var vehicleLookup = GetComponentLookup<VehicleComponent>();
        
        Entities
            .WithReadOnly(vehicleLookup)
            .ForEach((Entity entity, in SomeComponent component) =>
        {
            // Efficient component access
        }).ScheduleParallel();
    }
}
```

#### **Memory Management**
```csharp
// Native Collections for Performance
public class VehicleSystem : SystemBase
{
    private NativeArray<float3> velocityCache;
    private NativeArray<quaternion> rotationCache;
    
    protected override void OnCreate()
    {
        velocityCache = new NativeArray<float3>(1000, Allocator.Persistent);
        rotationCache = new NativeArray<quaternion>(1000, Allocator.Persistent);
    }
    
    protected override void OnDestroy()
    {
        if (velocityCache.IsCreated) velocityCache.Dispose();
        if (rotationCache.IsCreated) rotationCache.Dispose();
    }
}

// Memory Pool for Temporary Objects
public class EffectPoolSystem : SystemBase
{
    private NativeQueue<Entity> pooledEffects;
    
    public Entity GetPooledEffect()
    {
        if (pooledEffects.TryDequeue(out Entity entity))
            return entity;
        
        return EntityManager.CreateEntity(effectArchetype);
    }
    
    public void ReturnToPool(Entity entity)
    {
        pooledEffects.Enqueue(entity);
    }
}
```

#### **Rendering Optimization**
```csharp
// LOD System for Performance
public partial class LODSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var cameraPosition = Camera.main.transform.position;
        
        Entities.ForEach((Entity entity,
                         ref RenderMeshComponent renderMesh,
                         in LocalTransform transform,
                         in LODComponent lodComponent) =>
        {
            var distance = math.distance(cameraPosition, transform.Position);
            
            // Switch LOD based on distance
            if (distance > 100f)
                renderMesh.MeshID = lodComponent.LOD2;
            else if (distance > 50f)
                renderMesh.MeshID = lodComponent.LOD1;
            else
                renderMesh.MeshID = lodComponent.LOD0;
                
        }).ScheduleParallel();
    }
}

// Culling System for Off-Screen Objects
public partial class FrustumCullingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var cameraFrustum = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        
        Entities.ForEach((Entity entity,
                         ref RenderMeshComponent renderMesh,
                         in LocalTransform transform,
                         in BoundsComponent bounds) =>
        {
            var worldBounds = new Bounds(transform.Position, bounds.Size);
            var isVisible = GeometryUtility.TestPlanesAABB(cameraFrustum, worldBounds);
            
            renderMesh.Enabled = isVisible;
            
        }).ScheduleParallel();
    }
}
```

### **Device-Specific Optimization**

#### **Adaptive Quality System**
```csharp
public class AdaptiveQualityManager : MonoBehaviour
{
    [Header("Performance Targets")]
    public float targetFrameRate = 60f;
    public float lowFrameThreshold = 45f;
    
    [Header("Quality Settings")]
    public QualityLevel[] qualityLevels;
    
    private int currentQualityIndex = 0;
    private float frameTimeAccumulator = 0f;
    private int frameCount = 0;
    
    void Update()
    {
        // Monitor frame rate performance
        frameTimeAccumulator += Time.unscaledDeltaTime;
        frameCount++;
        
        if (frameCount >= 60) // Check every 60 frames
        {
            float averageFrameRate = frameCount / frameTimeAccumulator;
            
            if (averageFrameRate < lowFrameThreshold)
            {
                ReduceQuality();
            }
            else if (averageFrameRate > targetFrameRate * 1.1f)
            {
                IncreaseQuality();
            }
            
            frameTimeAccumulator = 0f;
            frameCount = 0;
        }
    }
    
    void ReduceQuality()
    {
        if (currentQualityIndex < qualityLevels.Length - 1)
        {
            currentQualityIndex++;
            ApplyQualityLevel(qualityLevels[currentQualityIndex]);
        }
    }
    
    void IncreaseQuality()
    {
        if (currentQualityIndex > 0)
        {
            currentQualityIndex--;
            ApplyQualityLevel(qualityLevels[currentQualityIndex]);
        }
    }
    
    void ApplyQualityLevel(QualityLevel level)
    {
        // Adjust rendering settings
        QualitySettings.SetQualityLevel(level.unityQualityIndex);
        
        // Adjust ECS system performance
        World.DefaultGameObjectInjectionWorld
             .GetExistingSystemManaged<RenderingSystem>()
             .SetQualityLevel(level);
    }
}

[System.Serializable]
public class QualityLevel
{
    public string name;
    public int unityQualityIndex;
    public int maxEntityCount;
    public float renderScale;
    public bool enableEffects;
    public int textureQuality;
}
```

#### **Battery Optimization**
```csharp
public class BatteryOptimizationSystem : SystemBase
{
    private float batteryLevel;
    private BatteryStatus batteryStatus;
    
    protected override void OnUpdate()
    {
        batteryLevel = SystemInfo.batteryLevel;
        batteryStatus = SystemInfo.batteryStatus;
        
        // Reduce performance when battery is low
        if (batteryLevel < 0.2f && batteryStatus != BatteryStatus.Charging)
        {
            ActivatePowerSavingMode();
        }
        
        // Monitor thermal state (iOS specific)
        #if UNITY_IOS && !UNITY_EDITOR
        var thermalState = GetThermalState();
        if (thermalState >= ThermalState.Serious)
        {
            ReducePerformanceForThermal();
        }
        #endif
    }
    
    void ActivatePowerSavingMode()
    {
        // Reduce target frame rate
        Application.targetFrameRate = 30;
        
        // Reduce effect quality
        World.DefaultGameObjectInjectionWorld
             .GetExistingSystemManaged<EffectsSystem>()
             .SetPowerSavingMode(true);
        
        // Reduce update frequency for non-critical systems
        World.DefaultGameObjectInjectionWorld
             .GetExistingSystemManaged<AudioSystem>()
             .SetUpdateFrequency(0.5f);
    }
}
```

---

## 🎮 **INPUT SYSTEM ARCHITECTURE**

### **Mobile Input Framework**
```csharp
// Input Action Map for Racing Controls
[CreateAssetMenu(fileName = "RacingInputActions", menuName = "Input/Racing Actions")]
public class RacingInputActions : ScriptableObject, IInputActionCollection2
{
    public InputActionAsset asset;
    
    // Racing action map
    private InputActionMap m_Racing;
    private InputAction m_Racing_Steer;
    private InputAction m_Racing_Accelerate;
    private InputAction m_Racing_Brake;
    private InputAction m_Racing_Boost;
    private InputAction m_Racing_Pause;
    
    // Gyroscope actions
    private InputAction m_Gyro_Tilt;
    private InputAction m_Gyro_Calibrate;
    
    public struct RacingActions
    {
        private RacingInputActions m_Wrapper;
        public InputAction steer => m_Wrapper.m_Racing_Steer;
        public InputAction accelerate => m_Wrapper.m_Racing_Accelerate;
        public InputAction brake => m_Wrapper.m_Racing_Brake;
        public InputAction boost => m_Wrapper.m_Racing_Boost;
        public InputAction pause => m_Wrapper.m_Racing_Pause;
    }
}

// Touch Input Processor
public class TouchInputProcessor : InputProcessor<Vector2>
{
    [Tooltip("Sensitivity multiplier for touch input")]
    public float sensitivity = 1.0f;
    
    [Tooltip("Dead zone for touch input")]
    public float deadZone = 0.1f;
    
    public override Vector2 Process(Vector2 value, InputControl control)
    {
        // Apply dead zone
        if (value.magnitude < deadZone)
            return Vector2.zero;
        
        // Apply sensitivity
        return value * sensitivity;
    }
}

// Gyroscope Input Processor
public class GyroscopeInputProcessor : InputProcessor<Vector3>
{
    [Tooltip("Sensitivity for gyroscope steering")]
    public float steeringSensitivity = 2.0f;
    
    [Tooltip("Axis to use for steering (0=X, 1=Y, 2=Z)")]
    public int steeringAxis = 2;
    
    public override Vector3 Process(Vector3 value, InputControl control)
    {
        var steeringValue = value[steeringAxis] * steeringSensitivity;
        return new Vector3(steeringValue, 0, 0);
    }
}
```

### **Adaptive Input System**
```csharp
public class AdaptiveInputSystem : SystemBase
{
    private InputMethod currentInputMethod;
    private float lastInputTime;
    private float inputSwitchThreshold = 0.5f;
    
    protected override void OnUpdate()
    {
        var currentTime = Time.time;
        
        Entities.ForEach((ref PlayerInputComponent input) =>
        {
            // Detect input method based on input source
            var detectedMethod = DetectInputMethod();
            
            if (detectedMethod != currentInputMethod && 
                currentTime - lastInputTime > inputSwitchThreshold)
            {
                SwitchInputMethod(detectedMethod);
                lastInputTime = currentTime;
            }
            
            // Apply input method specific processing
            ApplyInputMethodProcessing(ref input);
            
        }).Run();
    }
    
    private InputMethod DetectInputMethod()
    {
        // Check for touch input
        if (Input.touchCount > 0)
            return InputMethod.Touch;
        
        // Check for gyroscope input
        if (Input.gyro.enabled && Input.gyro.attitude.z != 0)
            return InputMethod.Gyroscope;
        
        // Check for gamepad input
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
            return InputMethod.Gamepad;
        
        return currentInputMethod;
    }
    
    private void SwitchInputMethod(InputMethod newMethod)
    {
        currentInputMethod = newMethod;
        
        // Update UI to show appropriate input hints
        World.DefaultGameObjectInjectionWorld
             .GetExistingSystemManaged<UISystem>()
             .SetInputMethod(newMethod);
        
        // Adjust input sensitivity based on method
        AdjustInputSensitivity(newMethod);
    }
    
    private void ApplyInputMethodProcessing(ref PlayerInputComponent input)
    {
        switch (currentInputMethod)
        {
            case InputMethod.Touch:
                ApplyTouchAssist(ref input);
                break;
            case InputMethod.Gyroscope:
                ApplyGyroscopeFiltering(ref input);
                break;
            case InputMethod.Gamepad:
                ApplyGamepadProcessing(ref input);
                break;
        }
    }
    
    private void ApplyTouchAssist(ref PlayerInputComponent input)
    {
        // Subtle steering assist for touch controls
        input.SteeringInput *= 0.8f; // Reduce sensitivity
        
        // Apply dead zone
        if (math.abs(input.SteeringInput) < 0.1f)
            input.SteeringInput = 0f;
    }
    
    private void ApplyGyroscopeFiltering(ref PlayerInputComponent input)
    {
        // Low-pass filter for gyroscope to reduce noise
        var filteredSteering = math.lerp(input.SteeringInput, 
                                        GetGyroscopeInput(), 
                                        Time.deltaTime * 10f);
        input.SteeringInput = filteredSteering;
    }
}

public enum InputMethod
{
    Touch,
    Gyroscope,
    Gamepad,
    Hybrid
}
```

---

## 🔧 **BUILD & DEPLOYMENT PIPELINE**

### **Platform Configuration**

#### **iOS Build Settings**
```csharp
// iOS Build Configuration
public static class iOSBuildConfiguration
{
    public static void ConfigureiOSBuild()
    {
        // Player settings for iOS
        PlayerSettings.iOS.applicationDisplayName = "MagaCityRacer";
        PlayerSettings.iOS.bundleIdentifier = "com.studio.magacityracer";
        PlayerSettings.iOS.buildNumber = GetBuildNumber();
        
        // Performance settings
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        PlayerSettings.iOS.targetOSVersionString = "12.0";
        PlayerSettings.iOS.architecture = iOSArchitecture.ARM64;
        
        // Graphics settings
        PlayerSettings.iOS.scriptCallOptimization = ScriptCallOptimizationLevel.SlowAndSafe;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new GraphicsDeviceType[] { GraphicsDeviceType.Metal });
        
        // DOTS specific settings
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_Standard_2_0);
        
        // Memory and performance
        PlayerSettings.iOS.hideHomeButton = false;
        PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.Default;
        PlayerSettings.iOS.requiresPersistentWiFi = false;
    }
}
```

#### **Android Build Settings**
```csharp
// Android Build Configuration
public static class AndroidBuildConfiguration
{
    public static void ConfigureAndroidBuild()
    {
        // Player settings for Android
        PlayerSettings.Android.applicationDisplayName = "MagaCityRacer";
        PlayerSettings.Android.bundleIdentifier = "com.studio.magacityracer";
        PlayerSettings.Android.bundleVersionCode = GetVersionCode();
        
        // Performance settings
        PlayerSettings.Android.targetArchitecture = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        
        // Graphics settings
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, 
            new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
        
        // DOTS specific settings
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        // Memory optimization
        PlayerSettings.Android.memoryModel = AndroidMemoryModel.MemoryMapped;
        PlayerSettings.Android.chromeosInputEmulation = false;
    }
}
```

### **Automated Build Pipeline**
```yaml
# Unity Cloud Build Configuration
version: 1.0

builds:
  ios-development:
    platform: ios
    xcodeVersion: xcode14
    buildTargetGroup: ios
    sceneList:
      - Assets/Scenes/Bootstrap.unity
      - Assets/Scenes/MainMenu.unity
      - Assets/Scenes/Racing.unity
    preBuildScript: Scripts/PreBuildProcessing.cs
    postBuildScript: Scripts/PostBuildProcessing.cs
    
  android-development:
    platform: android
    buildTargetGroup: android
    sceneList:
      - Assets/Scenes/Bootstrap.unity
      - Assets/Scenes/MainMenu.unity
      - Assets/Scenes/Racing.unity
    preBuildScript: Scripts/PreBuildProcessing.cs
    postBuildScript: Scripts/PostBuildProcessing.cs

settings:
  scripting_backend: il2cpp
  api_compatibility_level: 4.6
  managed_stripping_level: Medium
  
advanced_settings:
  unity_version: 2023.3.f1
  buildNumber: ${BUILD_NUMBER}
  cloudBuildTargetName: ${CLOUD_BUILD_TARGET_NAME}
```

---

## 📊 **PERFORMANCE MONITORING & ANALYTICS**

### **Real-Time Performance Monitoring**
```csharp
public class PerformanceMonitorSystem : SystemBase
{
    private NativeArray<float> frameTimeHistory;
    private NativeArray<float> memoryUsageHistory;
    private int currentFrameIndex;
    
    protected override void OnCreate()
    {
        frameTimeHistory = new NativeArray<float>(300, Allocator.Persistent); // 5 seconds at 60fps
        memoryUsageHistory = new NativeArray<float>(60, Allocator.Persistent); // 1 second
    }
    
    protected override void OnUpdate()
    {
        // Record frame time
        frameTimeHistory[currentFrameIndex % 300] = Time.unscaledDeltaTime;
        
        // Record memory usage (every 60 frames)
        if (currentFrameIndex % 60 == 0)
        {
            var memoryUsage = Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f); // MB
            memoryUsageHistory[(currentFrameIndex / 60) % 60] = memoryUsage;
        }
        
        currentFrameIndex++;
        
        // Analyze performance every 5 seconds
        if (currentFrameIndex % 300 == 0)
        {
            AnalyzePerformance();
        }
    }
    
    private void AnalyzePerformance()
    {
        var averageFrameTime = CalculateAverage(frameTimeHistory);
        var averageFPS = 1f / averageFrameTime;
        var averageMemory = CalculateAverage(memoryUsageHistory);
        
        // Send analytics data
        Analytics.CustomEvent("PerformanceSnapshot", new Dictionary<string, object>
        {
            {"averageFPS", averageFPS},
            {"averageMemoryMB", averageMemory},
            {"deviceModel", SystemInfo.deviceModel},
            {"graphicsDeviceName", SystemInfo.graphicsDeviceName},
            {"systemMemorySize", SystemInfo.systemMemorySize}
        });
        
        // Trigger quality adjustment if needed
        if (averageFPS < 45f)
        {
            World.DefaultGameObjectInjectionWorld
                 .GetExistingSystemManaged<AdaptiveQualitySystem>()
                 .ReduceQuality();
        }
    }
    
    private float CalculateAverage(NativeArray<float> array)
    {
        float sum = 0f;
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }
        return sum / array.Length;
    }
}
```

### **Crash Reporting & Error Handling**
```csharp
public class ErrorReportingSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Initialize crash reporting
        #if !UNITY_EDITOR
        CrashReportingService.Initialize();
        #endif
        
        // Set up exception handling
        Application.logMessageReceived += HandleLogMessage;
    }
    
    private void HandleLogMessage(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            // Report critical errors
            var errorData = new Dictionary<string, object>
            {
                {"message", logString},
                {"stackTrace", stackTrace},
                {"type", type.ToString()},
                {"deviceInfo", GetDeviceInfo()},
                {"gameplayContext", GetGameplayContext()}
            };
            
            #if !UNITY_EDITOR
            Analytics.CustomEvent("GameError", errorData);
            #endif
            
            Debug.LogError($"Critical Error Reported: {logString}");
        }
    }
    
    private Dictionary<string, object> GetDeviceInfo()
    {
        return new Dictionary<string, object>
        {
            {"deviceModel", SystemInfo.deviceModel},
            {"operatingSystem", SystemInfo.operatingSystem},
            {"graphicsDeviceName", SystemInfo.graphicsDeviceName},
            {"processorType", SystemInfo.processorType},
            {"systemMemorySize", SystemInfo.systemMemorySize}
        };
    }
    
    private Dictionary<string, object> GetGameplayContext()
    {
        var gameState = World.DefaultGameObjectInjectionWorld
                             .GetExistingSystemManaged<GameStateSystem>();
        
        return new Dictionary<string, object>
        {
            {"currentTrack", gameState?.CurrentTrack ?? "Unknown"},
            {"raceTime", gameState?.RaceTime ?? 0f},
            {"playerPosition", gameState?.PlayerPosition ?? 0},
            {"vehicleCount", gameState?.VehicleCount ?? 0}
        };
    }
}
```

---

**Document Control:**  
**Created by:** Technical Architecture Team  
**Reviewed by:** Development Team, Performance Engineering  
**Approved by:** Technical Director  
**Next Review Date:** [Monthly technical architecture review] 