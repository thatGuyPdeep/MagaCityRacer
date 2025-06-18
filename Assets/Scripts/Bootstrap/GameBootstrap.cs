using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MagaCityRacer.Components;
using MagaCityRacer.Systems;

namespace MagaCityRacer.Bootstrap
{
    /// <summary>
    /// Main bootstrap class for initializing the racing game
    /// Sets up ECS world, systems, and creates initial entities
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Game Configuration")]
        public GameObject playerVehiclePrefab;
        public Transform[] spawnPoints;
        public int numberOfAICars = 7;
        
        [Header("Track Configuration")]
        public Transform[] waypoints;
        public float trackWidth = 20f;
        public int totalLaps = 3;
        
        [Header("Mobile Settings")]
        public bool enableGyroscope = true;
        public bool useButtonAcceleration = false;
        public float defaultSteeringSensitivity = 1f;
        public float defaultGyroSensitivity = 1f;
        
        private EntityManager entityManager;
        private World defaultWorld;
        private Entity playerEntity;
        private Entity cameraEntity;
        private Entity trackEntity;
        
        void Start()
        {
            InitializeECSWorld();
            CreateTrackEntities();
            CreatePlayerVehicle();
            CreateAIVehicles();
            CreateCameraEntity();
            SetupMobileSettings();
            
            Debug.Log("MagaCityRacer: Game Bootstrap Complete");
        }
        
        private void InitializeECSWorld()
        {
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            entityManager = defaultWorld.EntityManager;
            
            // Ensure required systems are created
            var systemGroup = defaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();
            
            // Add our custom systems if they don't exist
            if (defaultWorld.GetExistingSystemManaged<VehicleMovementSystem>() == null)
            {
                var vehicleSystem = defaultWorld.CreateSystemManaged<VehicleMovementSystem>();
                systemGroup.AddSystemToUpdateList(vehicleSystem);
            }
            
            if (defaultWorld.GetExistingSystemManaged<PlayerInputSystem>() == null)
            {
                var inputSystem = defaultWorld.CreateSystemManaged<PlayerInputSystem>();
                var initGroup = defaultWorld.GetOrCreateSystemManaged<InitializationSystemGroup>();
                initGroup.AddSystemToUpdateList(inputSystem);
            }
            
            if (defaultWorld.GetExistingSystemManaged<CameraFollowSystem>() == null)
            {
                var cameraSystem = defaultWorld.CreateSystemManaged<CameraFollowSystem>();
                var lateGroup = defaultWorld.GetOrCreateSystemManaged<LateSimulationSystemGroup>();
                lateGroup.AddSystemToUpdateList(cameraSystem);
            }
        }
        
        private void CreateTrackEntities()
        {
            // Create main track entity
            trackEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(trackEntity, TrackDataComponent.CreateDefault());
            
            var trackData = entityManager.GetComponentData<TrackDataComponent>(trackEntity);
            trackData.TotalWaypoints = waypoints?.Length ?? 10;
            trackData.TotalLaps = totalLaps;
            trackData.TrackWidth = trackWidth;
            trackData.Type = TrackType.City;
            trackData.Theme = CityTheme.Modern;
            entityManager.SetComponentData(trackEntity, trackData);
            
            // Create waypoint entities
            if (waypoints != null && waypoints.Length > 0)
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    var waypointEntity = entityManager.CreateEntity();
                    var waypoint = new WaypointComponent
                    {
                        Position = waypoints[i].position,
                        Rotation = waypoints[i].rotation,
                        Width = trackWidth,
                        WaypointIndex = i,
                        NextWaypointIndex = (i + 1) % waypoints.Length,
                        IsStartFinish = i == 0,
                        IsCheckpoint = i % 3 == 0
                    };
                    
                    entityManager.AddComponentData(waypointEntity, waypoint);
                    entityManager.AddComponentData(waypointEntity, LocalTransform.FromPositionRotation(
                        waypoints[i].position, waypoints[i].rotation));
                }
            }
        }
        
        private void CreatePlayerVehicle()
        {
            // Create player entity
            playerEntity = entityManager.CreateEntity();
            
            // Add vehicle component
            var vehicleComponent = VehicleComponent.CreateDefault();
            vehicleComponent.MaxSpeed = 60f; // Mobile-optimized speed
            vehicleComponent.Acceleration = 25f;
            vehicleComponent.TurnSpeed = 150f;
            entityManager.AddComponentData(playerEntity, vehicleComponent);
            
            // Add player input component
            var inputComponent = PlayerInputComponent.CreateDefault();
            inputComponent.UseTiltSteering = enableGyroscope;
            inputComponent.UseButtonAcceleration = useButtonAcceleration;
            inputComponent.SteeringSensitivity = defaultSteeringSensitivity;
            inputComponent.GyroSensitivity = defaultGyroSensitivity;
            entityManager.AddComponentData(playerEntity, inputComponent);
            
            // Set spawn position
            float3 spawnPosition = spawnPoints != null && spawnPoints.Length > 0 ? 
                spawnPoints[0].position : float3.zero;
            quaternion spawnRotation = spawnPoints != null && spawnPoints.Length > 0 ? 
                spawnPoints[0].rotation : quaternion.identity;
                
            entityManager.AddComponentData(playerEntity, LocalTransform.FromPositionRotation(
                spawnPosition, spawnRotation));
            
            vehicleComponent.Position = spawnPosition;
            vehicleComponent.Rotation = spawnRotation;
            entityManager.SetComponentData(playerEntity, vehicleComponent);
            
            Debug.Log($"Player vehicle created at {spawnPosition}");
        }
        
        private void CreateAIVehicles()
        {
            for (int i = 0; i < numberOfAICars && i + 1 < (spawnPoints?.Length ?? 0); i++)
            {
                var aiEntity = entityManager.CreateEntity();
                
                // Add vehicle component
                var vehicleComponent = VehicleComponent.CreateDefault();
                vehicleComponent.MaxSpeed = UnityEngine.Random.Range(45f, 55f); // Varied AI speeds
                vehicleComponent.Acceleration = UnityEngine.Random.Range(20f, 25f);
                vehicleComponent.TurnSpeed = UnityEngine.Random.Range(120f, 160f);
                entityManager.AddComponentData(aiEntity, vehicleComponent);
                
                // Set spawn position (offset from player)
                float3 aiSpawnPosition = spawnPoints[i + 1].position;
                quaternion aiSpawnRotation = spawnPoints[i + 1].rotation;
                
                entityManager.AddComponentData(aiEntity, LocalTransform.FromPositionRotation(
                    aiSpawnPosition, aiSpawnRotation));
                
                vehicleComponent.Position = aiSpawnPosition;
                vehicleComponent.Rotation = aiSpawnRotation;
                entityManager.SetComponentData(aiEntity, vehicleComponent);
            }
            
            Debug.Log($"Created {numberOfAICars} AI vehicles");
        }
        
        private void CreateCameraEntity()
        {
            cameraEntity = entityManager.CreateEntity();
            
            // Add camera components
            var cameraTarget = new CameraTargetComponent
            {
                TargetEntity = playerEntity,
                Offset = new float3(0, 2, -6),
                LookAtOffset = new float3(0, 1, 0),
                FollowSpeed = 5f,
                LookSpeed = 3f,
                IsActive = true
            };
            entityManager.AddComponentData(cameraEntity, cameraTarget);
            
            var racingCamera = RacingCameraComponent.CreateDefault();
            racingCamera.CurrentMode = CameraMode.Follow;
            racingCamera.AutoSwitchModes = true;
            racingCamera.TouchSensitivity = 1f;
            entityManager.AddComponentData(cameraEntity, racingCamera);
            
            Debug.Log("Racing camera entity created");
        }
        
        private void SetupMobileSettings()
        {
            // Configure mobile-specific settings
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            // Set quality settings for mobile
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2; // 2x MSAA for mobile balance
            
            // Configure physics for mobile performance
            Physics.defaultSolverIterations = 6;
            Physics.defaultSolverVelocityIterations = 1;
            
            Debug.Log("Mobile settings configured");
        }
        
        public Entity GetPlayerEntity()
        {
            return playerEntity;
        }
        
        public Entity GetCameraEntity()
        {
            return cameraEntity;
        }
        
        public Entity GetTrackEntity()
        {
            return trackEntity;
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            // Handle mobile app lifecycle
            if (pauseStatus)
            {
                // Game paused - reduce performance
                Application.targetFrameRate = 30;
            }
            else
            {
                // Game resumed - restore performance
                Application.targetFrameRate = 60;
            }
        }
    }
} 