using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MagaCityRacer.Components;

namespace MagaCityRacer.Systems
{
    /// <summary>
    /// System for managing racing camera behavior including follow, look-ahead, and dynamic FOV
    /// Optimized for mobile racing games with smooth camera movement
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class CameraFollowSystem : SystemBase
    {
        private Camera mainCamera;
        private Transform cameraTransform;
        
        protected override void OnCreate()
        {
            RequireForUpdate<RacingCameraComponent>();
            RequireForUpdate<CameraTargetComponent>();
        }
        
        protected override void OnStartRunning()
        {
            // Get the main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindObjectOfType<Camera>();
            }
            
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        protected override void OnUpdate()
        {
            if (mainCamera == null || cameraTransform == null)
                return;
                
            float deltaTime = Time.DeltaTime;
            
            Entities
                .WithAll<RacingCameraComponent, CameraTargetComponent>()
                .WithoutBurst()
                .ForEach((ref RacingCameraComponent racingCamera, in CameraTargetComponent cameraTarget) =>
                {
                    if (!cameraTarget.IsActive || cameraTarget.TargetEntity == Entity.Null)
                        return;
                    
                    // Get target vehicle data
                    if (!SystemAPI.Exists(cameraTarget.TargetEntity))
                        return;
                        
                    var targetTransform = SystemAPI.GetComponent<LocalTransform>(cameraTarget.TargetEntity);
                    var targetVehicle = SystemAPI.GetComponent<VehicleComponent>(cameraTarget.TargetEntity);
                    
                    UpdateCameraPosition(ref racingCamera, in targetTransform, in targetVehicle, deltaTime);
                    UpdateCameraRotation(ref racingCamera, in targetTransform, in targetVehicle, deltaTime);
                    UpdateCameraEffects(ref racingCamera, in targetVehicle, deltaTime);
                    
                }).Run();
        }
        
        private void UpdateCameraPosition(ref RacingCameraComponent racingCamera, in LocalTransform targetTransform, in VehicleComponent vehicle, float deltaTime)
        {
            float3 targetPosition = targetTransform.Position;
            float3 targetForward = math.mul(targetTransform.Rotation, math.forward());
            float3 targetRight = math.mul(targetTransform.Rotation, math.right());
            
            // Calculate desired camera position based on mode
            float3 desiredPosition = CalculateDesiredPosition(racingCamera, targetPosition, targetForward, targetRight, vehicle);
            
            // Apply speed-based offset adjustments
            float speedFactor = math.saturate(vehicle.CurrentSpeed / vehicle.MaxSpeed);
            float3 speedOffset = -targetForward * (speedFactor * racingCamera.SpeedInfluence);
            desiredPosition += speedOffset;
            
            // Apply turn-based offset
            float turnOffset = vehicle.SteerInput * racingCamera.TurnInfluence;
            desiredPosition += targetRight * turnOffset;
            
            // Smooth camera movement
            float3 currentPosition = cameraTransform.position;
            float3 newPosition = math.lerp(currentPosition, desiredPosition, deltaTime * racingCamera.FollowDamping);
            
            // Apply camera shake if active
            if (racingCamera.ShakeTimer > 0)
            {
                float shakeAmount = racingCamera.ShakeIntensity * (racingCamera.ShakeTimer / racingCamera.ShakeDuration);
                float3 shake = new float3(
                    UnityEngine.Random.Range(-shakeAmount, shakeAmount),
                    UnityEngine.Random.Range(-shakeAmount, shakeAmount),
                    UnityEngine.Random.Range(-shakeAmount, shakeAmount)
                );
                newPosition += shake;
                racingCamera.ShakeTimer -= Time.DeltaTime;
            }
            
            cameraTransform.position = newPosition;
        }
        
        private void UpdateCameraRotation(ref RacingCameraComponent racingCamera, in LocalTransform targetTransform, in VehicleComponent vehicle, float deltaTime)
        {
            float3 targetPosition = targetTransform.Position;
            float3 targetForward = math.mul(targetTransform.Rotation, math.forward());
            
            // Calculate look-at position with look-ahead
            float3 lookAheadPosition = targetPosition + targetForward * racingCamera.LookAheadDistance;
            lookAheadPosition += racingCamera.LookOffset;
            
            // Smooth look rotation
            Vector3 lookDirection = (lookAheadPosition - cameraTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, deltaTime * racingCamera.LookDamping);
        }
        
        private void UpdateCameraEffects(ref RacingCameraComponent racingCamera, in VehicleComponent vehicle, float deltaTime)
        {
            // Dynamic FOV based on speed
            float speedFactor = math.saturate(vehicle.CurrentSpeed / vehicle.MaxSpeed);
            float targetFOV = racingCamera.FOVBase + (speedFactor * racingCamera.FOVSpeedMultiplier);
            targetFOV = math.min(targetFOV, racingCamera.MaxFOV);
            
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, deltaTime * 2f);
        }
        
        private float3 CalculateDesiredPosition(RacingCameraComponent racingCamera, float3 targetPosition, float3 targetForward, float3 targetRight, VehicleComponent vehicle)
        {
            switch (racingCamera.CurrentMode)
            {
                case CameraMode.Follow:
                    return targetPosition + racingCamera.FollowOffset.x * targetRight + 
                           racingCamera.FollowOffset.y * math.up() + 
                           racingCamera.FollowOffset.z * targetForward;
                
                case CameraMode.Cockpit:
                    return targetPosition + new float3(0, 1.2f, 0.5f);
                
                case CameraMode.Hood:
                    return targetPosition + new float3(0, 0.8f, 1.5f);
                
                case CameraMode.Cinematic:
                    // Dynamic cinematic positioning
                    float angle = Time.time * 0.5f;
                    float radius = racingCamera.FollowDistance;
                    return targetPosition + new float3(
                        math.cos(angle) * radius,
                        racingCamera.FollowHeight,
                        math.sin(angle) * radius
                    );
                
                default:
                    return targetPosition + racingCamera.FollowOffset;
            }
        }
    }
} 