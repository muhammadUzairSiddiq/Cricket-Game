using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Ball Speed Boost System - Adds extra speed after ball hits target
    /// Speed boost varies by initial speed but doesn't affect bounce physics
    /// </summary>
    public class BallSpeedBoost : MonoBehaviour
    {
        [Header("Speed Boost Settings")]
        [SerializeField] private bool enableSpeedBoost = true;
        [SerializeField] private float boostMultiplier = 1.0f; // Global boost multiplier
        
        [Header("Speed Boost Values")]
        [Tooltip("Speed boost for different initial speeds (m/s)")]
        [SerializeField] private float speed9Boost = 0f;    // No boost
        [SerializeField] private float speed10Boost = 0.5f; // Very slight boost
        [SerializeField] private float speed11Boost = 1f;  // Slight more boost
        [SerializeField] private float speed12Boost = 1.5f; // Very slight boost
        [SerializeField] private float speed13Boost = 2f;  // No boost
        [SerializeField] private float speed14Boost = 2.5f; // Very slight boost
        [SerializeField] private float speed15Boost = 3f;  // No boost
        [SerializeField] private float speed16Boost = 3.5f; // Very slight boost
        
        [Header("Boost Application")]
        [SerializeField] private float boostDelay = 0.1f; // Delay before applying boost (seconds)
        [SerializeField] private bool applyBoostOnTargetHit = true; // Apply boost when hitting target
        [SerializeField] private bool applyBoostOnBounce = false; // Apply boost on each bounce
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        // Private variables
        private Rigidbody ballRigidbody;
        private float initialSpeed;
        private bool boostApplied = false;
        private bool hasHitTarget = false;
        
        void Start()
        {
            ballRigidbody = GetComponent<Rigidbody>();
			if (ballRigidbody == null)
			{
				enabled = false;
				return;
			}
            
            // Don't store initial speed here - it will be set when ball is launched
            initialSpeed = 0f;
            
        }
        
        void Update()
        {
            // Check if ball has hit target and apply boost
            if (enableSpeedBoost && !boostApplied && hasHitTarget)
            {
                ApplySpeedBoost();
            }
        }
        
        /// <summary>
        /// Called when ball hits target
        /// </summary>
        public void OnTargetHit()
        {
            if (applyBoostOnTargetHit && !boostApplied)
            {
                hasHitTarget = true;
                // Apply boost after delay
                Invoke(nameof(ApplySpeedBoost), boostDelay);
            }
        }
        
        /// <summary>
        /// Set the initial speed for boost calculation
        /// </summary>
        public void SetInitialSpeed(float speed)
        {
            initialSpeed = speed;
        }
        
        /// <summary>
        /// Check if speed boost is properly configured
        /// </summary>
        public void CheckConfiguration()
        {
        }
        
        /// <summary>
        /// Called when ball bounces
        /// </summary>
        public void OnBallBounce()
        {
            if (applyBoostOnBounce && !boostApplied)
            {
                ApplySpeedBoost();
            }
        }
        
        /// <summary>
        /// Apply speed boost based on initial speed
        /// </summary>
        private void ApplySpeedBoost()
        {
            if (boostApplied || ballRigidbody == null) return;
            
            // Get boost amount based on initial speed
            float boostAmount = GetBoostForSpeed(initialSpeed);
            
            if (boostAmount > 0f)
            {
                // Apply boost to current velocity direction
                Vector3 currentVelocity = ballRigidbody.linearVelocity;
                Vector3 velocityDirection = currentVelocity.normalized;
                
                // Calculate new speed (current speed + boost)
                float currentSpeed = currentVelocity.magnitude;
                float newSpeed = currentSpeed + (boostAmount * boostMultiplier);
                
                // Apply new velocity
                Vector3 boostedVelocity = velocityDirection * newSpeed;
                ballRigidbody.linearVelocity = boostedVelocity;
                
                boostApplied = true;
            }
        }
        
        /// <summary>
        /// Get boost amount for given speed
        /// </summary>
        private float GetBoostForSpeed(float speed)
        {
            // Linear interpolation between speed points
            if (speed <= 9f) return speed9Boost;
            if (speed <= 10f) return Mathf.Lerp(speed9Boost, speed10Boost, (speed - 9f) / 1f);
            if (speed <= 11f) return Mathf.Lerp(speed10Boost, speed11Boost, (speed - 10f) / 1f);
            if (speed <= 12f) return Mathf.Lerp(speed11Boost, speed12Boost, (speed - 11f) / 1f);
            if (speed <= 13f) return Mathf.Lerp(speed12Boost, speed13Boost, (speed - 12f) / 1f);
            if (speed <= 14f) return Mathf.Lerp(speed13Boost, speed14Boost, (speed - 13f) / 1f);
            if (speed <= 15f) return Mathf.Lerp(speed14Boost, speed15Boost, (speed - 14f) / 1f);
            if (speed <= 16f) return Mathf.Lerp(speed15Boost, speed16Boost, (speed - 15f) / 1f);
            return speed16Boost;
        }
        
        /// <summary>
        /// Reset boost system (useful for ball reuse)
        /// </summary>
        public void ResetBoost()
        {
            boostApplied = false;
            hasHitTarget = false;
            initialSpeed = ballRigidbody != null ? ballRigidbody.linearVelocity.magnitude : 0f;
            
        }
        
        /// <summary>
        /// Set boost multiplier (for dynamic adjustment)
        /// </summary>
        public void SetBoostMultiplier(float multiplier)
        {
            boostMultiplier = Mathf.Clamp(multiplier, 0f, 5f);
        }
        
        /// <summary>
        /// Enable/disable speed boost
        /// </summary>
        public void SetBoostEnabled(bool enabled)
        {
            enableSpeedBoost = enabled;
        }
        
        /// <summary>
        /// Get current boost amount for display
        /// </summary>
        public float GetCurrentBoostAmount()
        {
            return GetBoostForSpeed(initialSpeed);
        }
        
        void OnValidate()
        {
            // Clamp boost values to reasonable ranges
            speed9Boost = Mathf.Clamp(speed9Boost, 0f, 10f);
            speed10Boost = Mathf.Clamp(speed10Boost, 0f, 10f);
            speed11Boost = Mathf.Clamp(speed11Boost, 0f, 10f);
            speed12Boost = Mathf.Clamp(speed12Boost, 0f, 10f);
            speed13Boost = Mathf.Clamp(speed13Boost, 0f, 10f);
            speed14Boost = Mathf.Clamp(speed14Boost, 0f, 10f);
            speed15Boost = Mathf.Clamp(speed15Boost, 0f, 10f);
            speed16Boost = Mathf.Clamp(speed16Boost, 0f, 10f);
        }
    }
}
