using Unity.Entities;
using Unity.Mathematics;

namespace MagaCityRacer.Components
{
    /// <summary>
    /// Component for handling player input on mobile devices
    /// </summary>
    public struct PlayerInputComponent : IComponentData
    {
        // Touch Input
        public float2 TouchPosition;
        public float2 TouchDelta;
        public bool IsTouching;
        public bool TouchStarted;
        public bool TouchEnded;
        
        // Steering Input
        public float SteerInput;      // -1 to 1
        public float AccelInput;      // 0 to 1
        public float BrakeInput;      // 0 to 1
        
        // Gyroscope Input
        public float3 GyroRotationRate;
        public float3 DeviceAcceleration;
        public bool GyroEnabled;
        
        // UI Button States
        public bool AccelButtonPressed;
        public bool BrakeButtonPressed;
        public bool BoostButtonPressed;
        public bool HandbrakePressed;
        
        // Input Settings
        public float SteeringSensitivity;
        public float GyroSensitivity;
        public float DeadZone;
        public bool InvertSteering;
        
        // Mobile Specific
        public bool UseTiltSteering;
        public bool UseButtonAcceleration;
        public float TouchSteering;
        
        public static PlayerInputComponent CreateDefault()
        {
            return new PlayerInputComponent
            {
                TouchPosition = float2.zero,
                TouchDelta = float2.zero,
                IsTouching = false,
                TouchStarted = false,
                TouchEnded = false,
                SteerInput = 0f,
                AccelInput = 0f,
                BrakeInput = 0f,
                GyroRotationRate = float3.zero,
                DeviceAcceleration = float3.zero,
                GyroEnabled = false,
                AccelButtonPressed = false,
                BrakeButtonPressed = false,
                BoostButtonPressed = false,
                HandbrakePressed = false,
                SteeringSensitivity = 1f,
                GyroSensitivity = 1f,
                DeadZone = 0.1f,
                InvertSteering = false,
                UseTiltSteering = true,
                UseButtonAcceleration = false,
                TouchSteering = 0f
            };
        }
    }
} 