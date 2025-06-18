using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace MagaCityRacer.Components
{
    /// <summary>
    /// Component for track waypoints and racing track data
    /// </summary>
    public struct WaypointComponent : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float Width;
        public int WaypointIndex;
        public int NextWaypointIndex;
        public bool IsStartFinish;
        public bool IsCheckpoint;
    }
    
    /// <summary>
    /// Component for track segments and boundaries
    /// </summary>
    public struct TrackSegmentComponent : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public float3 LeftBoundary;
        public float3 RightBoundary;
        public float SegmentLength;
        public int SegmentIndex;
        public float SpeedLimit;
        public bool IsStraight;
        public float TurnRadius;
    }
    
    /// <summary>
    /// Component for track metadata and race management
    /// </summary>
    public struct TrackDataComponent : IComponentData
    {
        public int TotalWaypoints;
        public int TotalLaps;
        public float TrackLength;
        public float TrackWidth;
        public float3 StartPosition;
        public quaternion StartRotation;
        
        // Track Settings
        public bool ReverseDirection;
        public float DifficultyLevel;
        public int MaxVehicles;
        
        // Track Type (adapted from Megacity)
        public TrackType Type;
        public CityTheme Theme;
        
        public static TrackDataComponent CreateDefault()
        {
            return new TrackDataComponent
            {
                TotalWaypoints = 10,
                TotalLaps = 3,
                TrackLength = 1000f,
                TrackWidth = 20f,
                StartPosition = float3.zero,
                StartRotation = quaternion.identity,
                ReverseDirection = false,
                DifficultyLevel = 1f,
                MaxVehicles = 8,
                Type = TrackType.City,
                Theme = CityTheme.Modern
            };
        }
    }
    
    public enum TrackType
    {
        City,
        Highway,
        Circuit,
        Mountain,
        Desert
    }
    
    public enum CityTheme
    {
        Modern,
        Cyberpunk,
        Industrial,
        Residential,
        Downtown
    }
} 