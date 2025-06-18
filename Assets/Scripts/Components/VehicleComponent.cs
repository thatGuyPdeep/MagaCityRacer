using Unity.Entities;
using Unity.Mathematics;

namespace MagaCityRacer.Components
{
    /// <summary>
    /// Component that defines vehicle properties and current state
    /// </summary>
    public struct VehicleComponent : IComponentData
    {
        // Vehicle Properties
        public float MaxSpeed;
        public float Acceleration;
        public float Deceleration;
        public float TurnSpeed;
        public float DriftFactor;
        public float DownForce;
        
        // Current State
        public float CurrentSpeed;
        public float3 Velocity;
        public float3 Position;
        public quaternion Rotation;
        
        // Input State
        public float SteerInput;
        public float AccelerationInput;
        public float BrakeInput;
        
        // Mobile Specific
        public bool UseGyroSteering;
        public float GyroSensitivity;
        
        // Racing State
        public int CurrentLap;
        public float LapTime;
        public float BestLapTime;
        public int CurrentWaypoint;
        
        public static VehicleComponent CreateDefault()
        {
            return new VehicleComponent
            {
                MaxSpeed = 50f,
                Acceleration = 20f,
                Deceleration = 15f,
                TurnSpeed = 180f,
                DriftFactor = 0.95f,
                DownForce = 100f,
                CurrentSpeed = 0f,
                Velocity = float3.zero,
                Position = float3.zero,
                Rotation = quaternion.identity,
                SteerInput = 0f,
                AccelerationInput = 0f,
                BrakeInput = 0f,
                UseGyroSteering = false,
                GyroSensitivity = 1f,
                CurrentLap = 0,
                LapTime = 0f,
                BestLapTime = float.MaxValue,
                CurrentWaypoint = 0
            };
        }
    }
} 