using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MagaCityRacer.Components;

namespace MagaCityRacer.Systems
{
    /// <summary>
    /// System for handling mobile input including touch, gyroscope, and UI buttons
    /// Processes input and updates vehicle components accordingly
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        private bool gyroEnabled;
        private Vector2 screenCenter;
        
        protected override void OnCreate()
        {
            RequireForUpdate<PlayerInputComponent>();
            RequireForUpdate<VehicleComponent>();
            
            // Initialize gyroscope if available
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                gyroEnabled = true;
            }
            
            screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            Vector2 currentScreenCenter = screenCenter;
            bool gyroSupported = gyroEnabled;
            
            // Get gyroscope data
            Vector3 gyroRotationRate = gyroSupported ? Input.gyro.rotationRate : Vector3.zero;
            Vector3 deviceAcceleration = gyroSupported ? Input.gyro.userAcceleration : Vector3.zero;
            
            // Process touch input
            bool isTouching = Input.touchCount > 0;
            Vector2 touchPosition = isTouching ? Input.GetTouch(0).position : Vector2.zero;
            Vector2 touchDelta = isTouching ? Input.GetTouch(0).deltaPosition : Vector2.zero;
            bool touchStarted = isTouching && Input.GetTouch(0).phase == TouchPhase.Began;
            bool touchEnded = Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled;
            
            Entities
                .WithAll<PlayerInputComponent>()
                .ForEach((ref PlayerInputComponent input, ref VehicleComponent vehicle) =>
                {
                    // Update touch input data
                    input.TouchPosition = touchPosition;
                    input.TouchDelta = touchDelta;
                    input.IsTouching = isTouching;
                    input.TouchStarted = touchStarted;
                    input.TouchEnded = touchEnded;
                    
                    // Update gyroscope data
                    input.GyroRotationRate = gyroRotationRate;
                    input.DeviceAcceleration = deviceAcceleration;
                    input.GyroEnabled = gyroSupported;
                    
                    // Calculate steering input
                    float steerInput = 0f;
                    
                    if (input.UseTiltSteering && gyroSupported)
                    {
                        // Use gyroscope for steering
                        steerInput = -input.GyroRotationRate.y * input.GyroSensitivity;
                        steerInput = math.clamp(steerInput, -1f, 1f);
                    }
                    else if (isTouching)
                    {
                        // Use touch for steering
                        float touchX = (touchPosition.x - currentScreenCenter.x) / currentScreenCenter.x;
                        steerInput = math.clamp(touchX * input.SteeringSensitivity, -1f, 1f);
                    }
                    
                    // Apply dead zone
                    if (math.abs(steerInput) < input.DeadZone)
                        steerInput = 0f;
                    
                    // Apply invert steering
                    if (input.InvertSteering)
                        steerInput = -steerInput;
                    
                    input.SteerInput = steerInput;
                    input.TouchSteering = steerInput;
                    
                    // Handle acceleration input
                    if (input.UseButtonAcceleration)
                    {
                        input.AccelInput = input.AccelButtonPressed ? 1f : 0f;
                    }
                    else
                    {
                        // Auto-acceleration for mobile
                        input.AccelInput = 1f;
                    }
                    
                    // Handle brake input
                    input.BrakeInput = input.BrakeButtonPressed ? 1f : 0f;
                    
                    // Update vehicle with input
                    vehicle.SteerInput = input.SteerInput;
                    vehicle.AccelerationInput = input.AccelInput;
                    vehicle.BrakeInput = input.BrakeInput;
                    vehicle.UseGyroSteering = input.UseTiltSteering;
                    vehicle.GyroSensitivity = input.GyroSensitivity;
                    
                }).Run();
        }
        
        protected override void OnDestroy()
        {
            // Disable gyroscope when system is destroyed
            if (gyroEnabled)
            {
                Input.gyro.enabled = false;
            }
        }
    }
} 