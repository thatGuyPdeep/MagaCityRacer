using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MagaCityRacer.Components;

namespace MagaCityRacer.Systems
{
    /// <summary>
    /// System responsible for vehicle movement and physics in the racing game
    /// Optimized for mobile performance using DOTS ECS
    /// </summary>
    [BurstCompile]
    public partial struct VehicleMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VehicleComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Process vehicle movement for all vehicles
            var vehicleMovementJob = new VehicleMovementJob
            {
                DeltaTime = deltaTime
            };
            
            vehicleMovementJob.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct VehicleMovementJob : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
        public void Execute(ref VehicleComponent vehicle, ref LocalTransform transform)
        {
            // Calculate forward and right directions
            float3 forward = math.mul(transform.Rotation, math.forward());
            float3 right = math.mul(transform.Rotation, math.right());
            
            // Apply steering
            float steerAngle = vehicle.SteerInput * vehicle.TurnSpeed * DeltaTime;
            if (math.abs(vehicle.CurrentSpeed) > 0.1f)
            {
                quaternion steerRotation = quaternion.AxisAngle(math.up(), math.radians(steerAngle));
                transform.Rotation = math.mul(transform.Rotation, steerRotation);
            }
            
            // Calculate acceleration
            float motorForce = vehicle.AccelerationInput * vehicle.Acceleration;
            float brakeForce = vehicle.BrakeInput * vehicle.Deceleration;
            
            // Apply forces
            float targetSpeed = motorForce - brakeForce;
            
            // Speed calculation with drift
            if (vehicle.AccelerationInput > 0)
            {
                vehicle.CurrentSpeed = math.lerp(vehicle.CurrentSpeed, 
                    math.min(targetSpeed, vehicle.MaxSpeed), 
                    DeltaTime * 3f);
            }
            else if (vehicle.BrakeInput > 0)
            {
                vehicle.CurrentSpeed = math.lerp(vehicle.CurrentSpeed, 
                    math.max(targetSpeed, 0f), 
                    DeltaTime * 5f);
            }
            else
            {
                // Natural deceleration
                vehicle.CurrentSpeed = math.lerp(vehicle.CurrentSpeed, 0f, DeltaTime * 1f);
            }
            
            // Update velocity
            vehicle.Velocity = forward * vehicle.CurrentSpeed;
            
            // Apply drift factor for realistic mobile racing feel
            vehicle.Velocity = math.lerp(vehicle.Velocity, forward * vehicle.CurrentSpeed, vehicle.DriftFactor);
            
            // Update position
            transform.Position += vehicle.Velocity * DeltaTime;
            vehicle.Position = transform.Position;
            vehicle.Rotation = transform.Rotation;
            
            // Update lap time
            vehicle.LapTime += DeltaTime;
        }
    }
} 