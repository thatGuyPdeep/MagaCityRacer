using UnityEngine;

namespace MagaCityRacer.Bootstrap
{
    /// <summary>
    /// UNITY SCENE SETUP - First scene configuration
    /// Sets up camera, lighting, and basic scene elements for free
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Camera Settings")]
        public Vector3 CameraPosition = new Vector3(0, 20, -30);
        public Vector3 CameraRotation = new Vector3(20, 0, 0);
        
        [Header("Lighting Settings")]
        public Color SunlightColor = new Color(1f, 0.9f, 0.8f, 1f);
        public float SunlightIntensity = 1.2f;
        public Vector3 SunDirection = new Vector3(30, 45, 0);
        
        [Header("Neo-Tokyo Atmosphere")]
        public Color FogColor = new Color(0.1f, 0.05f, 0.2f, 1f);
        public float FogDensity = 0.01f;
        public bool EnableFog = true;

        void Awake()
        {
            SetupCamera();
            SetupLighting();
            SetupAtmosphere();
            
            Debug.Log("🎬 Scene setup complete - Ready for Neo-Tokyo racing!");
        }

        private void SetupCamera()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Create main camera if it doesn't exist
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            
            // Position camera for racing view
            mainCamera.transform.position = CameraPosition;
            mainCamera.transform.rotation = Quaternion.Euler(CameraRotation);
            
            // Camera settings for mobile performance
            mainCamera.fieldOfView = 75f; // Wider FOV for racing
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f; // Limit draw distance for performance
            
            Debug.Log($"📷 Camera positioned at {CameraPosition}");
        }

        private void SetupLighting()
        {
            // Create directional light (sun)
            var sunLight = new GameObject("Directional Light");
            var light = sunLight.AddComponent<Light>();
            
            light.type = LightType.Directional;
            light.color = SunlightColor;
            light.intensity = SunlightIntensity;
            sunLight.transform.rotation = Quaternion.Euler(SunDirection);
            
            // Mobile optimization - single light source
            light.shadows = LightShadows.Soft; // Soft shadows for better visuals
            light.shadowStrength = 0.8f;
            
            Debug.Log("☀️ Directional lighting configured");
        }

        private void SetupAtmosphere()
        {
            if (EnableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = FogColor;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = FogDensity;
                
                Debug.Log("🌫️ Neo-Tokyo atmospheric fog enabled");
            }
            
            // Set ambient lighting for Neo-Tokyo mood
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.2f, 0.1f, 0.3f, 1f); // Purple sky
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Dark horizon
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.1f, 1f); // Very dark ground
            
            Debug.Log("🌃 Neo-Tokyo ambient lighting configured");
        }
    }
} 