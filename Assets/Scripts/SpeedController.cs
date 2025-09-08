using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CricketGame
{
    /// <summary>
    /// Controls ball speed via UI slider and updates ball prefab settings
    /// </summary>
    public class SpeedController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider speedSlider;
        [SerializeField] private TextMeshProUGUI speedText;
        
        [Header("Ball Settings")]
        [SerializeField] private BallSettings ballSettingsPrefab; // Reference to ball prefab's BallSettings
        [SerializeField] private ContinuousBowlingTest_WithBounce bowlingController; // Reference to bowling controller
        
        [Header("Speed Settings")]
        [SerializeField] private float minSpeed = 9f;
        [SerializeField] private float maxSpeed = 16f;
        [SerializeField] private float currentSpeed = 12f;
        
        [Header("Display Settings")]
        [SerializeField] private bool showSpeedInKmh = true; // Show as 90km/h, 100km/h etc.
        [SerializeField] private float speedMultiplier = 10f; // 9 m/s = 90 km/h
        
        void Start()
        {
            // Initialize slider
            if (speedSlider != null)
            {
                speedSlider.minValue = minSpeed;
                speedSlider.maxValue = maxSpeed;
                speedSlider.value = currentSpeed;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            }
            
            // Update initial display
            UpdateSpeedDisplay();
            UpdateBallSpeed();
        }
        
        /// <summary>
        /// Called when slider value changes
        /// </summary>
        public void OnSpeedChanged(float newSpeed)
        {
            currentSpeed = newSpeed;
            UpdateSpeedDisplay();
            UpdateBallSpeed();
        }
        
        /// <summary>
        /// Update the speed text display
        /// </summary>
        private void UpdateSpeedDisplay()
        {
            if (speedText != null)
            {
                if (showSpeedInKmh)
                {
                    float displaySpeed = currentSpeed * speedMultiplier;
                    speedText.text = $"{displaySpeed:F0} km/h";
                }
                else
                {
                    speedText.text = $"{currentSpeed:F1} m/s";
                }
            }
        }
        
        /// <summary>
        /// Update the ball speed in the prefab and bowling controller
        /// </summary>
        private void UpdateBallSpeed()
        {
            // Update ball settings prefab
            if (ballSettingsPrefab != null)
            {
                ballSettingsPrefab.SetGlobalBallSpeed(currentSpeed);
                Debug.Log($"🎯 SPEED CONTROLLER: Updated ball prefab speed to {currentSpeed} m/s");
            }
            
            // Update bowling controller if it has a ball settings reference
            if (bowlingController != null)
            {
                // Get the ball settings from bowling controller and update it
                var ballSettings = bowlingController.GetComponent<BallSettings>();
                if (ballSettings != null)
                {
                    ballSettings.SetGlobalBallSpeed(currentSpeed);
                    Debug.Log($"🎯 SPEED CONTROLLER: Updated bowling controller speed to {currentSpeed} m/s");
                }
            }
        }
        
        /// <summary>
        /// Get current speed (for other scripts to access)
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }
        
        /// <summary>
        /// Set speed programmatically
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
            currentSpeed = newSpeed;
            
            if (speedSlider != null)
            {
                speedSlider.value = newSpeed;
            }
            
            UpdateSpeedDisplay();
            UpdateBallSpeed();
        }
        
        /// <summary>
        /// Reset to default speed
        /// </summary>
        public void ResetToDefault()
        {
            SetSpeed(12f);
        }
        
        void OnValidate()
        {
            // Update in editor when values change
            if (Application.isPlaying)
            {
                UpdateSpeedDisplay();
                UpdateBallSpeed();
            }
        }
    }
}
