using UnityEngine;

namespace MagaCityRacer.Systems
{
    /// <summary>
    /// BASIC VEHICLE CONTROLLER - Immediate Racing Gameplay
    /// Simple touch/keyboard controls for testing vehicle movement
    /// Will be replaced with full DOTS system later
    /// </summary>
    public class BasicVehicleController : MonoBehaviour
    {
        [Header("Vehicle Settings")]
        public float MaxSpeed = 50f;
        public float Acceleration = 10f;
        public float Steering = 80f;
        public float Braking = 50f;
        
        [Header("Mobile Controls")]
        public bool UseTouchControls = true;
        public bool UseGyroscope = false;
        
        private Rigidbody vehicleRigidbody;
        private float motorInput;
        private float steerInput;
        
        void Start()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = gameObject.AddComponent<Rigidbody>();
                vehicleRigidbody.mass = 1200f;
                vehicleRigidbody.drag = 0.3f;
                vehicleRigidbody.angularDrag = 3f;
            }
            
            // Enable gyroscope if available and requested
            if (UseGyroscope && SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                Debug.Log("🎮 Gyroscope controls enabled");
            }
            
            Debug.Log($"🚗 Vehicle controller initialized - Max Speed: {MaxSpeed} km/h");
        }
        
        void Update()
        {
            GetInput();
            ApplyVehiclePhysics();
        }
        
        private void GetInput()
        {
            if (UseTouchControls && Application.isMobilePlatform)
            {
                GetMobileInput();
            }
            else
            {
                GetKeyboardInput();
            }
        }
        
        private void GetMobileInput()
        {
            // Simple touch controls for testing
            motorInput = 0f;
            steerInput = 0f;
            
            // Accelerate when touching screen
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                // Left side = steer left, Right side = steer right
                // Top half = accelerate, Bottom half = brake
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;
                
                // Steering based on horizontal touch position
                if (touch.position.x < screenWidth * 0.3f)
                {
                    steerInput = -1f; // Left
                }
                else if (touch.position.x > screenWidth * 0.7f)
                {
                    steerInput = 1f; // Right
                }
                
                // Motor based on vertical touch position
                if (touch.position.y > screenHeight * 0.5f)
                {
                    motorInput = 1f; // Accelerate
                }
                else
                {
                    motorInput = -0.5f; // Brake
                }
            }
            
            // Gyroscope steering (if enabled)
            if (UseGyroscope && Input.gyro.enabled)
            {
                steerInput = -Input.gyro.rotationRate.z * 2f;
                steerInput = Mathf.Clamp(steerInput, -1f, 1f);
            }
        }
        
        private void GetKeyboardInput()
        {
            // Keyboard controls for testing in editor
            motorInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
        }
        
        private void ApplyVehiclePhysics()
        {
            // Apply motor force
            Vector3 motorForce = transform.forward * motorInput * Acceleration * 100f;
            vehicleRigidbody.AddForce(motorForce);
            
            // Apply steering
            if (vehicleRigidbody.velocity.magnitude > 0.5f)
            {
                float steerAngle = steerInput * Steering * Time.deltaTime;
                transform.Rotate(0, steerAngle, 0);
            }
            
            // Speed limiting
            if (vehicleRigidbody.velocity.magnitude > MaxSpeed)
            {
                vehicleRigidbody.velocity = vehicleRigidbody.velocity.normalized * MaxSpeed;
            }
            
            // Simple downforce for stability
            vehicleRigidbody.AddForce(-transform.up * vehicleRigidbody.velocity.magnitude * 2f);
        }
        
        void OnGUI()
        {
            // Debug info for testing
            if (Application.isEditor)
            {
                GUILayout.BeginArea(new Rect(10, 10, 200, 100));
                GUILayout.Label($"Speed: {vehicleRigidbody.velocity.magnitude:F1} m/s");
                GUILayout.Label($"Motor: {motorInput:F2}");
                GUILayout.Label($"Steer: {steerInput:F2}");
                GUILayout.EndArea();
            }
        }
    }
}

/*
BASIC VEHICLE CONTROLLER FEATURES:
✅ Touch controls for mobile testing
✅ Gyroscope steering support
✅ Keyboard controls for editor testing
✅ Simple physics-based movement
✅ Speed limiting and stability
✅ Debug information display

USAGE:
1. Add to player vehicle GameObject
2. Configure MaxSpeed, Acceleration, Steering
3. Enable UseTouchControls for mobile
4. Test in editor with WASD keys
5. Build to mobile for touch testing

NEXT STEPS:
- Replace with full DOTS ECS system
- Add wheel colliders for realistic physics
- Implement advanced mobile UI
- Add particle effects for exhaust/sparks
*/ 