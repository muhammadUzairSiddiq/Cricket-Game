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
        [SerializeField] private BallSettingsSO ballSettingsSO; // Reference to ball settings ScriptableObject
        [SerializeField] private BowlingController bowlingController; // Reference to bowling controller
        
        [Header("Speed Settings")]
        [SerializeField] private int minSpeed = 12;
        [SerializeField] private int maxSpeed = 17;
        [SerializeField] private int currentSpeed = 12;
        
        [Header("Display Settings")]
        [SerializeField] private bool showSpeedInKmh = true; // Show as 90km/h, 100km/h etc.
        [SerializeField] private float speedMultiplier = 10f; // 9 m/s = 90 km/h
        
        void Start()
        {
            // Initialize slider with integer steps
            if (speedSlider != null)
            {
                speedSlider.minValue = minSpeed;
                speedSlider.maxValue = maxSpeed;
                speedSlider.wholeNumbers = true; // Only allow integer values
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
            currentSpeed = Mathf.RoundToInt(newSpeed); // Convert to integer
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
            // Update ball settings ScriptableObject
            if (ballSettingsSO != null)
            {
                Debug.Log($"🎯 SPEED CONTROLLER: Calling SetGlobalBallSpeed({currentSpeed}) on ballSettingsSO");
                ballSettingsSO.SetGlobalBallSpeed(currentSpeed);
                Debug.Log($"🎯 SPEED CONTROLLER: Updated ball settings speed to {currentSpeed} m/s");
            }
            else
            {
                Debug.LogError("🚨 SPEED CONTROLLER: ballSettingsSO is null! Please assign it in the Inspector.");
            }
        }
        
        /// <summary>
        /// Get current speed (for other scripts to access)
        /// </summary>
        public int GetCurrentSpeed()
        {
            return currentSpeed;
        }
        
        /// <summary>
        /// Set speed programmatically
        /// </summary>
        public void SetSpeed(int newSpeed)
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
            SetSpeed(12);
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
