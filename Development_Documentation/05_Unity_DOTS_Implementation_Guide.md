# MagaCityRacer - Unity DOTS Implementation Guide

**Document Version:** 1.0  
**Date:** January 2024  
**Technical Lead:** DOTS Architecture Team  
**Unity Version:** 2023.3 LTS  
**DOTS Packages:** Entities 1.0.16, Physics 1.0.16, Burst 1.8.8

---

## 🎯 **DOTS OVERVIEW FOR MAGACITYRACER**

### **Why DOTS for Mobile Racing?**
Unity's Data-Oriented Technology Stack (DOTS) provides significant performance advantages for mobile racing games:
- **CPU Performance:** 40-60% reduction in CPU usage vs. traditional MonoBehaviours
- **Memory Efficiency:** 50-70% reduction in memory allocations
- **Battery Life:** 20-30% improvement in power efficiency
- **Scalability:** Support for 100+ racing entities simultaneously

### **Current Codebase Analysis**
Based on the existing implementation:
```
Assets/Scripts/
├── Components/
│   ├── VehicleComponent.cs           - Core vehicle data structure
│   ├── PlayerInputComponent.cs       - Mobile input handling
│   ├── TrackComponent.cs            - Racing track definitions
│   └── CameraTargetComponent.cs     - Camera following system
├── Systems/
│   ├── VehicleMovementSystem.cs     - Physics and movement
│   ├── PlayerInputSystem.cs         - Input processing
│   └── CameraFollowSystem.cs        - Camera control
└── Bootstrap/
    └── GameBootstrap.cs             - ECS world initialization
```

### **DOTS Package Requirements**
```json
{
  "com.unity.entities": "1.0.16",
  "com.unity.entities.graphics": "1.0.16", 
  "com.unity.physics": "1.0.16",
  "com.unity.rendering.hybrid": "1.0.16",
  "com.unity.burst": "1.8.8",
  "com.unity.collections": "2.2.1",
  "com.unity.mathematics": "1.2.6",
  "com.unity.jobs": "0.70.0-preview.7"
}
```

---

## 🏗️ **ECS ARCHITECTURE IMPLEMENTATION**

### **Enhanced Component Definitions**

#### **Vehicle Components (Performance-Optimized)**
```csharp
using Unity.Entities;
using Unity.Mathematics;

// Hot data - frequently updated
[GenerateAuthoringComponent]
public struct VehicleComponent : IComponentData
{
    public float Speed;
    public float MaxSpeed;
    public float Acceleration;
    public float Handling;
    public float3 Velocity;
    public quaternion Rotation;
    public bool IsGrounded;
    public bool IsBoosting;
    public float BoostTimer;
    public VehicleState State;
}

// Cold data - configuration
[GenerateAuthoringComponent]
public struct VehicleConfigComponent : IComponentData
{
    public float MaxSpeedBase;
    public float AccelerationBase;
    public float HandlingBase;
    public float BoostPower;
    public float BoostDuration;
    public float Mass;
    public VehicleType Type;
}

public enum VehicleState : byte
{
    Idle = 0,
    Accelerating = 1,
    Braking = 2,
    Drifting = 3,
    Airborne = 4
}

public enum VehicleType : byte
{
    StreetRacer = 0,
    SpeedDemon = 1,
    DriftMaster = 2,
    AllTerrain = 3,
    Experimental = 4
}
```

#### **Enhanced Input Components**
```csharp
// Mobile-optimized input component
[GenerateAuthoringComponent]
public struct PlayerInputComponent : IComponentData
{
    public float SteeringInput;     // -1 to 1
    public float AccelerationInput; // 0 to 1
    public float BrakeInput;        // 0 to 1
    public bool BoostPressed;
    public InputMethod CurrentMethod;
    public float GyroSensitivity;
    public float TouchSensitivity;
    public bool AssistEnabled;
}

public enum InputMethod : byte
{
    Touch = 0,
    Gyroscope = 1,
    Gamepad = 2,
    Hybrid = 3
}
```

### **Mobile-Optimized System Implementation**

#### **Enhanced VehicleMovementSystem**
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct VehicleMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleComponent>();
        state.RequireForUpdate<PlayerInputComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        var vehicleJob = new VehiclePhysicsJob
        {
            DeltaTime = deltaTime
        };
        
        vehicleJob.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct VehiclePhysicsJob : IJobEntity
{
    public float DeltaTime;
    
    private void Execute(ref LocalTransform transform,
                        ref VehicleComponent vehicle,
                        ref PhysicsVelocity velocity,
                        in PlayerInputComponent input,
                        in VehicleConfigComponent config)
    {
        // Mobile-optimized physics calculations
        float targetSpeed = CalculateTargetSpeed(input, config);
        float speedDiff = targetSpeed - vehicle.Speed;
        float acceleration = CalculateAcceleration(speedDiff, input, config);
        
        vehicle.Speed = math.max(0, vehicle.Speed + acceleration * DeltaTime);
        
        // Steering with mobile optimization
        float steering = CalculateSteering(input, vehicle, config);
        quaternion deltaRotation = quaternion.RotateY(steering * DeltaTime);
        transform.Rotation = math.mul(transform.Rotation, deltaRotation);
        
        // Movement calculation
        float3 forward = math.forward(transform.Rotation);
        float3 movement = forward * vehicle.Speed * DeltaTime;
        transform.Position += movement;
        
        // Update physics for collision detection
        velocity.Linear = forward * vehicle.Speed;
        velocity.Angular = new float3(0, steering, 0);
        
        // Handle boost system
        ProcessBoost(ref vehicle, input, config);
        
        // Update vehicle state
        UpdateVehicleState(ref vehicle, input);
    }
    
    private float CalculateTargetSpeed(in PlayerInputComponent input, in VehicleConfigComponent config)
    {
        float baseSpeed = input.AccelerationInput * config.MaxSpeedBase;
        return input.IsBoosting ? baseSpeed * config.BoostPower : baseSpeed;
    }
    
    private float CalculateAcceleration(float speedDiff, in PlayerInputComponent input, in VehicleConfigComponent config)
    {
        float acceleration = speedDiff * config.AccelerationBase;
        
        // Apply braking
        if (input.BrakeInput > 0)
        {
            acceleration -= input.BrakeInput * config.AccelerationBase * 2f;
        }
        
        return acceleration;
    }
    
    private float CalculateSteering(in PlayerInputComponent input, in VehicleComponent vehicle, in VehicleConfigComponent config)
    {
        float steering = input.SteeringInput * config.HandlingBase;
        
        // Reduce steering at high speeds (mobile-friendly)
        float speedFactor = vehicle.Speed / config.MaxSpeedBase;
        steering *= math.lerp(1f, 0.3f, speedFactor);
        
        // Apply assistance for mobile controls
        if (input.AssistEnabled)
        {
            steering *= 0.8f; // Slight assistance
        }
        
        return steering;
    }
    
    private void ProcessBoost(ref VehicleComponent vehicle, in PlayerInputComponent input, in VehicleConfigComponent config)
    {
        if (input.BoostPressed && vehicle.BoostTimer > 0)
        {
            vehicle.IsBoosting = true;
            vehicle.BoostTimer -= DeltaTime;
        }
        else
        {
            vehicle.IsBoosting = false;
            // Regenerate boost slowly
            vehicle.BoostTimer = math.min(vehicle.BoostTimer + DeltaTime * 0.5f, config.BoostDuration);
        }
    }
    
    private void UpdateVehicleState(ref VehicleComponent vehicle, in PlayerInputComponent input)
    {
        if (!vehicle.IsGrounded)
        {
            vehicle.State = VehicleState.Airborne;
        }
        else if (input.BrakeInput > 0.5f)
        {
            vehicle.State = VehicleState.Braking;
        }
        else if (input.AccelerationInput > 0.1f)
        {
            vehicle.State = VehicleState.Accelerating;
        }
        else
        {
            vehicle.State = VehicleState.Idle;
        }
    }
}
```

#### **Mobile Input System Enhancement**
```csharp
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class PlayerInputSystem : SystemBase
{
    private InputAction steerAction;
    private InputAction accelerateAction;
    private InputAction brakeAction;
    private InputAction boostAction;
    
    private float gyroCalibration = 0f;
    private bool gyroEnabled = false;
    
    protected override void OnCreate()
    {
        RequireForUpdate<PlayerInputComponent>();
        InitializeInputActions();
        InitializeGyroscope();
    }
    
    private void InitializeInputActions()
    {
        var inputActions = new InputActionAsset();
        var racingMap = inputActions.AddActionMap("Racing");
        
        // Touch steering - using screen position delta
        steerAction = racingMap.AddAction("Steer", InputActionType.Value, 
            "<Touchscreen>/primaryTouch/delta/x");
        
        // Touch accelerate - using touch pressure or simple press
        accelerateAction = racingMap.AddAction("Accelerate", InputActionType.Button, 
            "<Touchscreen>/primaryTouch/press");
        
        // Brake using second finger
        brakeAction = racingMap.AddAction("Brake", InputActionType.Button, 
            "<Touchscreen>/touch1/press");
        
        // Boost using third finger or double tap
        boostAction = racingMap.AddAction("Boost", InputActionType.Button, 
            "<Touchscreen>/touch2/press");
        
        inputActions.Enable();
    }
    
    private void InitializeGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroEnabled = true;
            gyroCalibration = Input.gyro.attitude.z;
        }
    }
    
    protected override void OnUpdate()
    {
        ProcessInput();
    }
    
    private void ProcessInput()
    {
        // Get touch input
        float steerValue = ProcessTouchSteering();
        float accelerateValue = accelerateAction.ReadValue<float>();
        float brakeValue = brakeAction.ReadValue<float>();
        bool boostPressed = boostAction.WasPressedThisFrame();
        
        // Get gyroscope input
        float gyroInput = ProcessGyroscopeInput();
        
        // Combine inputs based on user preference
        float finalSteerValue = CombineSteeringInputs(steerValue, gyroInput);
        
        // Update all player input components
        Entities.ForEach((ref PlayerInputComponent input) =>
        {
            input.SteeringInput = finalSteerValue;
            input.AccelerationInput = accelerateValue;
            input.BrakeInput = brakeValue;
            input.BoostPressed = boostPressed;
            input.CurrentMethod = DetermineInputMethod(steerValue, gyroInput);
            
            // Apply input filtering for mobile
            ApplyMobileInputFiltering(ref input);
            
        }).Run();
    }
    
    private float ProcessTouchSteering()
    {
        float touchDelta = steerAction.ReadValue<float>();
        
        // Convert touch delta to steering value
        float sensitivity = 0.01f; // Adjust based on screen resolution
        float steerValue = touchDelta * sensitivity;
        
        // Apply dead zone
        if (math.abs(steerValue) < 0.05f)
            steerValue = 0f;
        
        return math.clamp(steerValue, -1f, 1f);
    }
    
    private float ProcessGyroscopeInput()
    {
        if (!gyroEnabled || !Input.gyro.enabled)
            return 0f;
        
        // Get current gyro attitude and subtract calibration
        float currentTilt = Input.gyro.attitude.z - gyroCalibration;
        
        // Apply sensitivity and clamping
        float gyroInput = currentTilt * 2f; // Sensitivity multiplier
        return math.clamp(gyroInput, -1f, 1f);
    }
    
    private float CombineSteeringInputs(float touchInput, float gyroInput)
    {
        // Determine which input is dominant
        float touchMagnitude = math.abs(touchInput);
        float gyroMagnitude = math.abs(gyroInput);
        
        if (touchMagnitude > 0.1f && gyroMagnitude > 0.1f)
        {
            // Hybrid mode - blend inputs
            return (touchInput + gyroInput) * 0.5f;
        }
        else if (touchMagnitude > gyroMagnitude)
        {
            return touchInput;
        }
        else
        {
            return gyroInput;
        }
    }
    
    private InputMethod DetermineInputMethod(float touchInput, float gyroInput)
    {
        float touchMagnitude = math.abs(touchInput);
        float gyroMagnitude = math.abs(gyroInput);
        
        if (touchMagnitude > 0.1f && gyroMagnitude > 0.1f)
            return InputMethod.Hybrid;
        else if (gyroMagnitude > 0.1f)
            return InputMethod.Gyroscope;
        else if (touchMagnitude > 0.1f)
            return InputMethod.Touch;
        else
            return InputMethod.Touch; // Default
    }
    
    private void ApplyMobileInputFiltering(ref PlayerInputComponent input)
    {
        // Apply low-pass filter to reduce jitter
        input.SteeringInput = ApplyLowPassFilter(input.SteeringInput, 0.1f);
        
        // Apply assistance if enabled
        if (input.AssistEnabled)
        {
            input.SteeringInput *= 0.9f; // Slight reduction for easier control
        }
    }
    
    private float ApplyLowPassFilter(float current, float filterStrength)
    {
        // Simple low-pass filter implementation
        // In a real implementation, you'd store previous values
        return current; // Placeholder - implement proper filtering
    }
    
    // Public method to recalibrate gyroscope
    public void CalibrateGyroscope()
    {
        if (gyroEnabled)
        {
            gyroCalibration = Input.gyro.attitude.z;
        }
    }
}
```

---

## 📱 **MOBILE OPTIMIZATION FRAMEWORK**

### **Adaptive Performance System**
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class AdaptivePerformanceSystem : SystemBase
{
    private float targetFrameTime = 1f / 60f; // 60 FPS
    private float frameTimeAccumulator = 0f;
    private int frameCount = 0;
    private int currentQualityLevel = 2; // 0=Low, 1=Medium, 2=High
    private float batteryLevel = 1f;
    private float thermalLevel = 0f;
    
    protected override void OnUpdate()
    {
        MonitorPerformance();
        MonitorDeviceStatus();
        AdjustQualitySettings();
    }
    
    private void MonitorPerformance()
    {
        frameTimeAccumulator += Time.unscaledDeltaTime;
        frameCount++;
        
        if (frameCount >= 60) // Check every second
        {
            float avgFrameTime = frameTimeAccumulator / frameCount;
            
            if (avgFrameTime > targetFrameTime * 1.5f) // Significantly slow
            {
                ReduceQuality();
            }
            else if (avgFrameTime < targetFrameTime * 0.7f) // Room for improvement
            {
                IncreaseQuality();
            }
            
            frameTimeAccumulator = 0f;
            frameCount = 0;
        }
    }
    
    private void MonitorDeviceStatus()
    {
        batteryLevel = SystemInfo.batteryLevel;
        
        // Monitor thermal state (iOS specific)
        #if UNITY_IOS && !UNITY_EDITOR
        thermalLevel = GetIOSThermalState();
        #endif
        
        // Reduce quality if battery is low or device is hot
        if (batteryLevel < 0.2f || thermalLevel > 0.7f)
        {
            if (currentQualityLevel > 0)
            {
                ReduceQuality();
            }
        }
    }
    
    private void ReduceQuality()
    {
        if (currentQualityLevel > 0)
        {
            currentQualityLevel--;
            ApplyQualityLevel(currentQualityLevel);
            Debug.Log($"Quality reduced to level {currentQualityLevel}");
        }
    }
    
    private void IncreaseQuality()
    {
        if (currentQualityLevel < 2 && batteryLevel > 0.5f && thermalLevel < 0.5f)
        {
            currentQualityLevel++;
            ApplyQualityLevel(currentQualityLevel);
            Debug.Log($"Quality increased to level {currentQualityLevel}");
        }
    }
    
    private void ApplyQualityLevel(int level)
    {
        switch (level)
        {
            case 0: // Low Quality
                Application.targetFrameRate = 30;
                SetMaxEntityCount(25);
                SetEffectsQuality(0.5f);
                SetTextureQuality(2);
                break;
                
            case 1: // Medium Quality
                Application.targetFrameRate = 45;
                SetMaxEntityCount(50);
                SetEffectsQuality(0.75f);
                SetTextureQuality(1);
                break;
                
            case 2: // High Quality
                Application.targetFrameRate = 60;
                SetMaxEntityCount(100);
                SetEffectsQuality(1.0f);
                SetTextureQuality(0);
                break;
        }
    }
    
    private void SetMaxEntityCount(int maxCount)
    {
        // Communicate with spawning systems
        var spawnerQuery = GetEntityQuery(typeof(VehicleSpawnerComponent));
        if (!spawnerQuery.IsEmpty)
        {
            var spawner = spawnerQuery.GetSingleton<VehicleSpawnerComponent>();
            spawner.MaxVehicles = maxCount;
            spawnerQuery.SetSingleton(spawner);
        }
    }
    
    private void SetEffectsQuality(float quality)
    {
        // Adjust particle system quality
        var effectsQuery = GetEntityQuery(typeof(EffectsQualityComponent));
        if (!effectsQuery.IsEmpty)
        {
            var effects = effectsQuery.GetSingleton<EffectsQualityComponent>();
            effects.QualityMultiplier = quality;
            effectsQuery.SetSingleton(effects);
        }
    }
    
    private void SetTextureQuality(int qualityLevel)
    {
        QualitySettings.masterTextureLimit = qualityLevel;
    }
    
    #if UNITY_IOS && !UNITY_EDITOR
    private float GetIOSThermalState()
    {
        // iOS thermal state detection
        // This would require iOS-specific native plugin
        return 0f; // Placeholder
    }
    #endif
}

// Supporting components for quality management
public struct VehicleSpawnerComponent : IComponentData
{
    public int MaxVehicles;
    public int CurrentVehicles;
}

public struct EffectsQualityComponent : IComponentData
{
    public float QualityMultiplier;
    public bool ParticlesEnabled;
}
```

---

## 🔧 **PERFORMANCE PROFILING & DEBUGGING**

### **DOTS Performance Monitor**
```csharp
public partial class DOTSPerformanceMonitor : SystemBase
{
    private NativeArray<float> systemExecutionTimes;
    private NativeArray<int> entityCounts;
    private int frameIndex = 0;
    
    protected override void OnCreate()
    {
        systemExecutionTimes = new NativeArray<float>(10, Allocator.Persistent);
        entityCounts = new NativeArray<int>(10, Allocator.Persistent);
    }
    
    protected override void OnDestroy()
    {
        if (systemExecutionTimes.IsCreated)
            systemExecutionTimes.Dispose();
        if (entityCounts.IsCreated)
            entityCounts.Dispose();
    }
    
    protected override void OnUpdate()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        
        ProfileSystemPerformance();
        
        if (frameIndex % 300 == 0) // Every 5 seconds at 60fps
        {
            LogPerformanceData();
        }
        
        frameIndex++;
        
        #endif
    }
    
    private void ProfileSystemPerformance()
    {
        // Profile key systems
        var vehicleQuery = GetEntityQuery(typeof(VehicleComponent));
        var inputQuery = GetEntityQuery(typeof(PlayerInputComponent));
        var trackQuery = GetEntityQuery(typeof(TrackComponent));
        
        entityCounts[0] = vehicleQuery.CalculateEntityCount();
        entityCounts[1] = inputQuery.CalculateEntityCount();
        entityCounts[2] = trackQuery.CalculateEntityCount();
        
        // In a real implementation, you'd measure actual system execution times
        // This would require custom profiling integration
    }
    
    private void LogPerformanceData()
    {
        var performanceData = new System.Collections.Generic.Dictionary<string, object>
        {
            {"VehicleEntityCount", entityCounts[0]},
            {"InputEntityCount", entityCounts[1]},
            {"TrackEntityCount", entityCounts[2]},
            {"TotalEntityCount", EntityManager.GetAllEntities().Length},
            {"FrameRate", 1f / Time.unscaledDeltaTime},
            {"MemoryUsage", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false)},
            {"DeviceModel", SystemInfo.deviceModel}
        };
        
        Debug.Log($"DOTS Performance - Entities: {entityCounts[0]} vehicles, " +
                 $"FPS: {1f / Time.unscaledDeltaTime:F1}, " +
                 $"Memory: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / 1024 / 1024}MB");
    }
}
```

### **Entity Debug Visualization**
```csharp
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class EntityDebugSystem : SystemBase
{
    private bool debugEnabled = false;
    
    protected override void OnUpdate()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        
        // Toggle debug with F1 key
        if (Input.GetKeyDown(KeyCode.F1))
        {
            debugEnabled = !debugEnabled;
        }
        
        if (debugEnabled)
        {
            DrawVehicleDebugInfo();
            DrawTrackDebugInfo();
            DrawPerformanceOverlay();
        }
        
        #endif
    }
    
    private void DrawVehicleDebugInfo()
    {
        Entities
            .WithoutBurst()
            .ForEach((Entity entity,
                     in LocalTransform transform,
                     in VehicleComponent vehicle,
                     in PlayerInputComponent input) =>
        {
            var position = transform.Position;
            
            // Draw velocity vector
            var velocityEnd = position + vehicle.Velocity;
            Debug.DrawLine(position, velocityEnd, Color.red);
            
            // Draw forward direction
            var forward = math.forward(transform.Rotation);
            var forwardEnd = position + forward * 5f;
            Debug.DrawLine(position, forwardEnd, Color.blue);
            
            // Draw speed indicator (color-coded)
            var speedRatio = vehicle.Speed / vehicle.MaxSpeed;
            var speedColor = Color.Lerp(Color.green, Color.red, speedRatio);
            Debug.DrawRay(position + new float3(0, 2, 0), math.up() * speedRatio * 5f, speedColor);
            
        }).Run();
    }
    
    private void DrawTrackDebugInfo()
    {
        Entities
            .WithoutBurst()
            .ForEach((in TrackComponent track, in LocalTransform transform) =>
        {
            // Draw track boundaries
            Debug.DrawWireCube(transform.Position, new Vector3(track.TrackWidth, 1, 10));
            
        }).Run();
    }
    
    private void DrawPerformanceOverlay()
    {
        // This would typically use Unity's immediate mode GUI
        // to display performance metrics on screen
        var fps = 1f / Time.unscaledDeltaTime;
        var entityCount = EntityManager.GetAllEntities().Length;
        var memoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / 1024 / 1024;
        
        // In a real implementation, display this on screen using GUI
        Debug.Log($"Performance: {fps:F1} FPS, {entityCount} entities, {memoryMB}MB");
    }
}
```

---

## 📚 **DEVELOPMENT BEST PRACTICES**

### **Component Design Rules**
1. **Separate Hot and Cold Data:** Keep frequently updated data in separate components
2. **Use Burst-Compatible Types:** Prefer Unity.Mathematics types over UnityEngine types
3. **Minimize Component Size:** Smaller components = better cache performance
4. **Avoid Managed References:** Components should only contain unmanaged data

### **System Architecture Guidelines**
1. **Use Job System:** Implement IJobEntity for parallel processing
2. **Proper Update Groups:** Place systems in correct update groups
3. **Minimize Dependencies:** Reduce system dependencies for better parallelization
4. **Use ComponentLookup Sparingly:** Only for random access patterns

### **Mobile-Specific Optimizations**
1. **Adaptive Quality:** Implement quality scaling based on device performance
2. **Battery Awareness:** Monitor battery level and adjust performance accordingly
3. **Thermal Management:** Reduce quality when device overheats
4. **Memory Management:** Use object pooling and avoid frequent allocations

### **Testing and Validation**
1. **Profile on Target Devices:** Always test on actual mobile hardware
2. **Automated Performance Testing:** Set up automated benchmarks
3. **Memory Leak Detection:** Regular memory profiling sessions
4. **Battery Usage Testing:** Monitor power consumption during gameplay

---

**Document Control:**  
**Created by:** DOTS Architecture Team  
**Reviewed by:** Technical Lead, Mobile Development Team  
**Approved by:** Technical Director  
**Next Review Date:** [Monthly DOTS architecture review]