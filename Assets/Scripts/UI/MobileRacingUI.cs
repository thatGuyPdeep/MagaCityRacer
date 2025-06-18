using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using TMPro;
using MagaCityRacer.Components;

namespace MagaCityRacer.UI
{
    /// <summary>
    /// Mobile-optimized racing UI with touch controls and HUD
    /// </summary>
    public class MobileRacingUI : MonoBehaviour
    {
        [Header("Control Buttons")]
        public Button accelerateButton;
        public Button brakeButton;
        public Button boostButton;
        public Button handbrakeButton;
        public Toggle gyroToggle;
        
        [Header("HUD Elements")]
        public TextMeshProUGUI speedText;
        public TextMeshProUGUI lapTimeText;
        public TextMeshProUGUI bestLapText;
        public TextMeshProUGUI lapCountText;
        public Slider speedometer;
        public Image minimap;
        
        [Header("Settings Panel")]
        public GameObject settingsPanel;
        public Slider steeringSensitivity;
        public Slider gyroSensitivity;
        public Toggle useButtonAcceleration;
        public Toggle invertSteering;
        
        [Header("Mobile Specific")]
        public RectTransform steeringArea;
        public Image steeringVisual;
        public float steeringRange = 100f;
        
        private EntityManager entityManager;
        private Entity playerEntity;
        private bool settingsVisible = false;
        
        void Start()
        {
            SetupUI();
            SetupEntityReferences();
            ConfigureMobileLayout();
        }
        
        void Update()
        {
            UpdateHUD();
            HandleTouchSteering();
            UpdatePlayerInput();
        }
        
        private void SetupUI()
        {
            // Setup button events
            if (accelerateButton != null)
            {
                accelerateButton.onClick.AddListener(() => SetAcceleration(true));
            }
            
            if (brakeButton != null)
            {
                brakeButton.onClick.AddListener(() => SetBrake(true));
            }
            
            if (boostButton != null)
            {
                boostButton.onClick.AddListener(() => SetBoost(true));
            }
            
            if (handbrakeButton != null)
            {
                handbrakeButton.onClick.AddListener(() => SetHandbrake(true));
            }
            
            if (gyroToggle != null)
            {
                gyroToggle.onValueChanged.AddListener(ToggleGyroSteering);
            }
            
            // Setup settings sliders
            if (steeringSensitivity != null)
            {
                steeringSensitivity.onValueChanged.AddListener(SetSteeringSensitivity);
                steeringSensitivity.value = 1f;
            }
            
            if (gyroSensitivity != null)
            {
                gyroSensitivity.onValueChanged.AddListener(SetGyroSensitivity);
                gyroSensitivity.value = 1f;
            }
        }
        
        private void SetupEntityReferences()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Find player entity (you would set this up based on your entity creation)
            var query = entityManager.CreateEntityQuery(typeof(VehicleComponent), typeof(PlayerInputComponent));
            if (query.CalculateEntityCount() > 0)
            {
                playerEntity = query.GetSingletonEntity();
            }
        }
        
        private void ConfigureMobileLayout()
        {
            // Adapt UI for different screen sizes and orientations
            float screenAspect = (float)Screen.width / Screen.height;
            
            if (screenAspect > 1.5f) // Landscape
            {
                // Position controls for landscape mode
                ConfigureLandscapeLayout();
            }
            else // Portrait or square
            {
                // Position controls for portrait mode
                ConfigurePortraitLayout();
            }
        }
        
        private void ConfigureLandscapeLayout()
        {
            // Move steering area to left side
            if (steeringArea != null)
            {
                steeringArea.anchorMin = new Vector2(0f, 0f);
                steeringArea.anchorMax = new Vector2(0.4f, 0.6f);
            }
            
            // Position buttons on right side
            PositionButtonsForLandscape();
        }
        
        private void ConfigurePortraitLayout()
        {
            // Center steering area at bottom
            if (steeringArea != null)
            {
                steeringArea.anchorMin = new Vector2(0.1f, 0f);
                steeringArea.anchorMax = new Vector2(0.9f, 0.3f);
            }
            
            // Position buttons vertically
            PositionButtonsForPortrait();
        }
        
        private void PositionButtonsForLandscape()
        {
            // Position accelerate and brake buttons on the right side
            if (accelerateButton != null)
            {
                var rectTransform = accelerateButton.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.8f, 0.1f);
                rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
            }
            
            if (brakeButton != null)
            {
                var rectTransform = brakeButton.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.6f, 0.1f);
                rectTransform.anchorMax = new Vector2(0.75f, 0.25f);
            }
        }
        
        private void PositionButtonsForPortrait()
        {
            // Position buttons horizontally at bottom
            if (accelerateButton != null)
            {
                var rectTransform = accelerateButton.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.6f, 0.05f);
                rectTransform.anchorMax = new Vector2(0.85f, 0.2f);
            }
            
            if (brakeButton != null)
            {
                var rectTransform = brakeButton.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.15f, 0.05f);
                rectTransform.anchorMax = new Vector2(0.4f, 0.2f);
            }
        }
        
        private void UpdateHUD()
        {
            if (playerEntity == Entity.Null || !entityManager.Exists(playerEntity))
                return;
            
            var vehicle = entityManager.GetComponentData<VehicleComponent>(playerEntity);
            
            // Update speed display
            if (speedText != null)
            {
                speedText.text = $"{vehicle.CurrentSpeed:F0} km/h";
            }
            
            if (speedometer != null)
            {
                speedometer.value = vehicle.CurrentSpeed / vehicle.MaxSpeed;
            }
            
            // Update lap information
            if (lapTimeText != null)
            {
                lapTimeText.text = FormatTime(vehicle.LapTime);
            }
            
            if (bestLapText != null && vehicle.BestLapTime < float.MaxValue)
            {
                bestLapText.text = $"Best: {FormatTime(vehicle.BestLapTime)}";
            }
            
            if (lapCountText != null)
            {
                lapCountText.text = $"Lap {vehicle.CurrentLap + 1}";
            }
        }
        
        private void HandleTouchSteering()
        {
            if (steeringArea == null || playerEntity == Entity.Null)
                return;
            
            // Handle touch input within steering area
            foreach (Touch touch in Input.touches)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    steeringArea, touch.position, null, out localPoint))
                {
                    // Calculate steering input based on touch position
                    float steerInput = Mathf.Clamp(localPoint.x / steeringRange, -1f, 1f);
                    
                    // Update visual feedback
                    if (steeringVisual != null)
                    {
                        steeringVisual.rectTransform.anchoredPosition = new Vector2(localPoint.x, 0);
                    }
                    
                    // Apply to player input component
                    UpdateSteeringInput(steerInput);
                }
            }
        }
        
        private void UpdatePlayerInput()
        {
            if (playerEntity == Entity.Null || !entityManager.Exists(playerEntity))
                return;
            
            var input = entityManager.GetComponentData<PlayerInputComponent>(playerEntity);
            
            // Update button states
            input.AccelButtonPressed = IsButtonPressed(accelerateButton);
            input.BrakeButtonPressed = IsButtonPressed(brakeButton);
            input.BoostButtonPressed = IsButtonPressed(boostButton);
            input.HandbrakePressed = IsButtonPressed(handbrakeButton);
            
            entityManager.SetComponentData(playerEntity, input);
        }
        
        private bool IsButtonPressed(Button button)
        {
            // This is a simplified check - in a real implementation,
            // you would track button press/release events properly
            return button != null && button.gameObject.activeInHierarchy;
        }
        
        private void UpdateSteeringInput(float steerInput)
        {
            if (playerEntity == Entity.Null || !entityManager.Exists(playerEntity))
                return;
            
            var input = entityManager.GetComponentData<PlayerInputComponent>(playerEntity);
            input.TouchSteering = steerInput;
            entityManager.SetComponentData(playerEntity, input);
        }
        
        private string FormatTime(float timeSeconds)
        {
            int minutes = Mathf.FloorToInt(timeSeconds / 60f);
            float seconds = timeSeconds % 60f;
            return $"{minutes:0}:{seconds:00.000}";
        }
        
        // UI Event Handlers
        public void SetAcceleration(bool pressed) { /* Handle acceleration button */ }
        public void SetBrake(bool pressed) { /* Handle brake button */ }
        public void SetBoost(bool pressed) { /* Handle boost button */ }
        public void SetHandbrake(bool pressed) { /* Handle handbrake button */ }
        
        public void ToggleGyroSteering(bool enabled)
        {
            if (playerEntity == Entity.Null) return;
            
            var input = entityManager.GetComponentData<PlayerInputComponent>(playerEntity);
            input.UseTiltSteering = enabled;
            entityManager.SetComponentData(playerEntity, input);
        }
        
        public void SetSteeringSensitivity(float sensitivity)
        {
            if (playerEntity == Entity.Null) return;
            
            var input = entityManager.GetComponentData<PlayerInputComponent>(playerEntity);
            input.SteeringSensitivity = sensitivity;
            entityManager.SetComponentData(playerEntity, input);
        }
        
        public void SetGyroSensitivity(float sensitivity)
        {
            if (playerEntity == Entity.Null) return;
            
            var input = entityManager.GetComponentData<PlayerInputComponent>(playerEntity);
            input.GyroSensitivity = sensitivity;
            entityManager.SetComponentData(playerEntity, input);
        }
        
        public void ToggleSettings()
        {
            settingsVisible = !settingsVisible;
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(settingsVisible);
            }
        }
    }
} 