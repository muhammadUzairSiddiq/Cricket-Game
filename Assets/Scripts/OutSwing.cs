using UnityEngine;
using System.Collections;

namespace CricketGame
{
    public class OutSwing : MonoBehaviour
    {
        [Header("Out Swing Settings")]
        [Tooltip("Enable/disable out swing effect")]
        [SerializeField] private bool enableOutSwing = true;
        
        [Tooltip("Base swing force multiplier")]
        [SerializeField] private float baseSwingForce = 1.0f;
        
        [Tooltip("Minimum swing at speed 9 (low speed = less swing)")]
        [SerializeField] private float minSwingAtSpeed9 = 0.2f;
        
        [Tooltip("Maximum swing at speed 16 (high speed = extreme swing)")]
        [SerializeField] private float maxSwingAtSpeed16 = 2.5f;
        
        [Header("Swing Direction")]
        [Tooltip("Direction of out swing (positive X = right, negative X = left)")]
        [SerializeField] private Vector3 swingDirection = new Vector3(1f, 0f, 0f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        private Rigidbody ballRigidbody;
        private bool swingApplied = false;
        
        void Start()
        {
            ballRigidbody = GetComponent<Rigidbody>();
            if (ballRigidbody == null)
            {
                Debug.LogError("🎯 OutSwing: No Rigidbody found on ball!");
                enabled = false;
                return;
            }
            
            if (showDebugLogs)
            {
                Debug.Log("🎯 OutSwing: Ready to apply out swing effect");
            }
        }
        
        /// <summary>
        /// Calculate swing trajectory parameters for bowling system
        /// </summary>
        public Vector3 CalculateSwingTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableOutSwing)
                return Vector3.zero;
                
            // Calculate swing force based on speed (9 = less swing, 16 = extreme swing)
            float swingForce = CalculateSwingForce(ballSpeed);
            
            // Calculate swing offset (how much to curve right)
            float swingOffset = swingForce * 2f; // Maximum 5 units right at max swing
            
            // Create curved target position
            Vector3 swingTarget = targetPos + new Vector3(swingOffset, 0, 0);
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 OutSwing: Calculated swing trajectory - Force: {swingForce:F2}, Offset: {swingOffset:F2}");
            }
            
            return swingTarget;
        }
        
        /// <summary>
        /// Get swing direction for trajectory calculation
        /// </summary>
        public Vector3 GetSwingDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableOutSwing)
                return (targetPos - startPos).normalized;
                
            float swingForce = CalculateSwingForce(ballSpeed);
            Vector3 baseDirection = (targetPos - startPos).normalized;
            
            // Add rightward curve to the direction
            Vector3 swingDirection = baseDirection + new Vector3(swingForce * 0.3f, 0, 0);
            
            return swingDirection.normalized;
        }
        
        /// <summary>
        /// Calculate swing force based on ball speed
        /// </summary>
        private float CalculateSwingForce(float speed)
        {
            // Linear interpolation between min swing (speed 9) and max swing (speed 16)
            float normalizedSpeed = Mathf.InverseLerp(9f, 16f, speed);
            float swingForce = Mathf.Lerp(minSwingAtSpeed9, maxSwingAtSpeed16, normalizedSpeed);
            
            return swingForce;
        }
        
        /// <summary>
        /// Reset swing for new ball
        /// </summary>
        public void ResetSwing()
        {
            swingApplied = false;
            if (showDebugLogs)
            {
                Debug.Log("🎯 OutSwing: Reset for new ball");
            }
        }
        
        /// <summary>
        /// Get current swing settings for UI display
        /// </summary>
        public (float minSwing, float maxSwing) GetSwingRange()
        {
            return (minSwingAtSpeed9, maxSwingAtSpeed16);
        }
        
        /// <summary>
        /// Update swing settings from UI
        /// </summary>
        public void UpdateSwingSettings(float minSwing, float maxSwing, float baseForce)
        {
            minSwingAtSpeed9 = minSwing;
            maxSwingAtSpeed16 = maxSwing;
            baseSwingForce = baseForce;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 OutSwing: Updated settings - Min: {minSwing}, Max: {maxSwing}, Base: {baseForce}");
            }
        }
    }
}
