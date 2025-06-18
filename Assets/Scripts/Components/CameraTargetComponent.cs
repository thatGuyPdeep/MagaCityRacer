using Unity.Entities;
using Unity.Mathematics;

namespace MagaCityRacer.Components
{
    /// <summary>
    /// Component for camera target tracking in racing games
    /// </summary>
    public struct CameraTargetComponent : IComponentData
    {
        public Entity TargetEntity;
        public float3 Offset;
        public float3 LookAtOffset;
        public float FollowSpeed;
        public float LookSpeed;
        public bool IsActive;
    }
    
    /// <summary>
    /// Component for dynamic racing camera behavior
    /// </summary>
    public struct RacingCameraComponent : IComponentData
    {
        // Camera Modes
        public CameraMode CurrentMode;
        public CameraMode PreviousMode;
        
        // Follow Camera Settings
        public float3 FollowOffset;
        public float FollowDistance;
        public float FollowHeight;
        public float FollowDamping;
        
        // Look Settings
        public float3 LookOffset;
        public float LookDamping;
        public float LookAheadDistance;
        
        // Dynamic Behavior
        public float SpeedInfluence;
        public float TurnInfluence;
        public float FOVBase;
        public float FOVSpeedMultiplier;
        public float MaxFOV;
        
        // Shake and Effects
        public float ShakeIntensity;
        public float ShakeDuration;
        public float ShakeTimer;
        
        // Mobile Specific
        public bool AutoSwitchModes;
        public float TouchSensitivity;
        public bool AllowManualRotation;
        
        public static RacingCameraComponent CreateDefault()
        {
            return new RacingCameraComponent
            {
                CurrentMode = CameraMode.Follow,
                PreviousMode = CameraMode.Follow,
                FollowOffset = new float3(0, 2, -6),
                FollowDistance = 6f,
                FollowHeight = 2f,
                FollowDamping = 5f,
                LookOffset = new float3(0, 0, 10),
                LookDamping = 3f,
                LookAheadDistance = 10f,
                SpeedInfluence = 0.5f,
                TurnInfluence = 0.3f,
                FOVBase = 60f,
                FOVSpeedMultiplier = 0.5f,
                MaxFOV = 80f,
                ShakeIntensity = 0f,
                ShakeDuration = 0f,
                ShakeTimer = 0f,
                AutoSwitchModes = true,
                TouchSensitivity = 1f,
                AllowManualRotation = false
            };
        }
    }
    
    public enum CameraMode
    {
        Follow,      // Behind the car
        Cockpit,     // First person view
        Hood,        // Hood view
        Cinematic,   // Dynamic cinematic angles
        Free         // Free camera for replays
    }
} 