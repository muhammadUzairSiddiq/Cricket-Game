using System.Collections;
using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Continuous Bowling Test System - FIXED VERSION
    /// Automatically bowls ball to target with realistic cricket physics, waits 3 seconds, returns to original position, and repeats
    /// </summary>
    public class ContinuousBowlingTest_Fixed : MonoBehaviour
    {
        [Header("Test Setup")]
        [SerializeField] private GameObject ball;
        [SerializeField] private Transform target;
        [SerializeField] private Transform ballSpawnPoint;
        
        [Header("Bowling Settings")]
        [SerializeField] private float ballSpeed = 30f;
        [SerializeField] private float arcHeight = 1.5f; // 🎯 REDUCED: Much lower arc for realistic cricket
        [SerializeField] private float returnSpeed = 15f;
        [SerializeField] private float waitTimeAfterLanding = 3f;
        
        [Header("Physics")]
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private bool useRealisticPhysics = true;
        
        [Header("Controls")]
        [SerializeField] private KeyCode startKey = KeyCode.Space;
        [SerializeField] private KeyCode stopKey = KeyCode.Escape;
        
        // Private variables
        private Vector3 originalBallPosition;
        private Rigidbody ballRigidbody;
        private bool isRunning = false;
        private bool isReturning = false;
        private bool hasLanded = false;
        private Coroutine bowlingCoroutine;
        
        void Start()
        {
            SetupTest();
        }
        
        void Update()
        {
            HandleInput();
        }
        
        /// <summary>
        /// Setup the test system
        /// </summary>
        void SetupTest()
        {
            if (ball == null)
            {
                Debug.LogError("Ball not assigned! Please assign a ball GameObject.");
                return;
            }
            
            if (target == null)
            {
                Debug.LogError("Target not assigned! Please assign a target Transform.");
                return;
            }
            
            if (ballSpawnPoint == null)
            {
                Debug.LogError("Ball spawn point not assigned! Please assign a spawn point Transform.");
                return;
            }
            
            // Store original position
            originalBallPosition = ball.transform.position;
            
            // Setup ball physics
            SetupBallPhysics();
            
            Debug.Log("Continuous Bowling Test System initialized!");
            Debug.Log($"Ball: {ball.name}");
            Debug.Log($"Target: {target.name}");
            Debug.Log($"Original Position: {originalBallPosition}");
        }
        
        /// <summary>
        /// Setup ball physics components
        /// </summary>
        void SetupBallPhysics()
        {
            ballRigidbody = ball.GetComponent<Rigidbody>();
            if (ballRigidbody == null)
            {
                ballRigidbody = ball.AddComponent<Rigidbody>();
            }
            
            // 🎯 IMPROVED: Configure rigidbody for realistic cricket ball physics
            ballRigidbody.mass = 0.16f; // Standard cricket ball weight
            ballRigidbody.linearDamping = 0.02f; // Very low drag for realistic air resistance
            ballRigidbody.angularDamping = 0.02f; // Low angular drag
            ballRigidbody.useGravity = useRealisticPhysics;
            ballRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add collider if missing
            if (ball.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = ball.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }
            
            // Add trail renderer for visual effect
            TrailRenderer trail = ball.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = ball.AddComponent<TrailRenderer>();
                trail.time = 0.8f;
                trail.startWidth = 0.05f;
                trail.endWidth = 0.01f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 1f, 1f, 0.4f);
                trail.endColor = new Color(1f, 1f, 1f, 0f);
            }
        }
        
        /// <summary>
        /// Handle input controls
        /// </summary>
        void HandleInput()
        {
            if (Input.GetKeyDown(startKey) && !isRunning)
            {
                StartContinuousBowling();
            }
            
            if (Input.GetKeyDown(stopKey) && isRunning)
            {
                StopContinuousBowling();
            }
        }
        
        /// <summary>
        /// Start the continuous bowling loop
        /// </summary>
        public void StartContinuousBowling()
        {
            if (isRunning) return;
            
            isRunning = true;
            Debug.Log("🏏 Starting continuous bowling test...");
            
            // Start the bowling sequence
            bowlingCoroutine = StartCoroutine(ContinuousBowlingLoop());
        }
        
        /// <summary>
        /// Stop the continuous bowling loop
        /// </summary>
        public void StopContinuousBowling()
        {
            if (!isRunning) return;
            
            isRunning = false;
            
            if (bowlingCoroutine != null)
            {
                StopCoroutine(bowlingCoroutine);
                bowlingCoroutine = null;
            }
            
            Debug.Log("🏏 Continuous bowling test stopped.");
        }
        
        /// <summary>
        /// Main continuous bowling loop
        /// </summary>
        IEnumerator ContinuousBowlingLoop()
        {
            while (isRunning)
            {
                // Bowl the ball to target
                yield return StartCoroutine(BowlToTarget());
                
                // Wait for ball to land
                yield return StartCoroutine(WaitForLanding());
                
                // Wait additional time after landing
                Debug.Log($"⏰ Waiting {waitTimeAfterLanding} seconds after landing...");
                yield return new WaitForSeconds(waitTimeAfterLanding);
                
                // Return ball to original position
                yield return StartCoroutine(ReturnBallToOriginal());
                
                // Small delay before next bowl
                yield return new WaitForSeconds(0.5f);
                
                Debug.Log("🔄 Ready for next bowl!");
            }
        }
        
        /// <summary>
        /// Bowl the ball to the target with REALISTIC cricket physics
        /// </summary>
        IEnumerator BowlToTarget()
        {
            Debug.Log("🏏 Bowling ball to target with realistic cricket physics...");
            
            // Reset landing flag
            hasLanded = false;
            
            // Calculate trajectory to target
            Vector3 targetPosition = target.position;
            Vector3 startPosition = ball.transform.position;
            
            // Calculate horizontal distance
            Vector3 horizontalStart = new Vector3(startPosition.x, 0, startPosition.z);
            Vector3 horizontalTarget = new Vector3(targetPosition.x, 0, targetPosition.z);
            float horizontalDistance = Vector3.Distance(horizontalStart, horizontalTarget);
            
            // Calculate time to reach target
            float timeToReach = horizontalDistance / ballSpeed;
            
            // 🎯 FIXED: Calculate realistic cricket bowling arc (much lower than mortar shell)
            // Cricket balls follow a gentle, low arc - not a high parabola
            float heightDifference = targetPosition.y - startPosition.y;
            
            // 🎯 KEY FIX: Use much smaller arc height for realistic cricket bowling
            float realisticArcHeight = arcHeight * 0.2f; // Only 20% of the original arc height
            
            // Calculate required Y velocity for realistic arc
            float requiredYVelocity = (heightDifference + realisticArcHeight + 0.5f * gravity * timeToReach * timeToReach) / timeToReach;
            
            // 🎯 ENSURE: Y velocity is not too high (cricket balls don't go very high)
            float maxYVelocity = ballSpeed * 0.25f; // Max Y velocity should be only 25% of horizontal speed
            if (requiredYVelocity > maxYVelocity)
            {
                requiredYVelocity = maxYVelocity;
                Debug.Log($"🏏 Y velocity capped to {maxYVelocity:F1} m/s for realistic cricket arc");
            }
            
            // Calculate horizontal velocity
            Vector3 horizontalDirection = (horizontalTarget - horizontalStart).normalized;
            Vector3 horizontalVelocity = horizontalDirection * ballSpeed;
            
            // Combine velocities
            Vector3 initialVelocity = horizontalVelocity;
            initialVelocity.y = requiredYVelocity;
            
            // Apply velocity to ball
            if (useRealisticPhysics)
            {
                ballRigidbody.linearVelocity = initialVelocity;
            }
            else
            {
                // Use kinematic movement for precise control
                StartCoroutine(MoveBallKinematic(startPosition, targetPosition, timeToReach));
            }
            
            Debug.Log($"🏏 Ball launched with velocity: {initialVelocity.magnitude:F1} m/s");
            Debug.Log($"🏏 Horizontal velocity: {horizontalVelocity.magnitude:F1} m/s");
            Debug.Log($"🏏 Y velocity: {requiredYVelocity:F1} m/s");
            Debug.Log($"🏏 Expected time to target: {timeToReach:F2} seconds");
            Debug.Log($"🏏 Arc height: {realisticArcHeight:F1}m (realistic cricket bowling)");
            
            // Wait for ball to reach target area
            yield return new WaitForSeconds(timeToReach);
            
            // Mark as landed
            hasLanded = true;
            Debug.Log("🎯 Ball has landed on target!");
        }
        
        /// <summary>
        /// Move ball using kinematic movement with realistic cricket arc
        /// </summary>
        IEnumerator MoveBallKinematic(Vector3 startPos, Vector3 endPos, float duration)
        {
            float elapsed = 0f;
            
            // 🎯 FIXED: Use realistic cricket bowling arc (much lower)
            float realisticArcHeight = arcHeight * 0.2f; // Same reduction as physics version
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 🎯 CREATE REALISTIC CRICKET BOWLING ARC: Much lower and more natural
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                
                // Use a more realistic arc curve for cricket bowling
                // Cricket balls follow a gentle, low arc, not a high parabola
                float arcCurve = Mathf.Sin(t * Mathf.PI) * realisticArcHeight;
                currentPos.y += arcCurve;
                
                ball.transform.position = currentPos;
                
                yield return null;
            }
            
            // Ensure ball is exactly at target
            ball.transform.position = endPos;
        }
        
        /// <summary>
        /// Wait for ball to land on target
        /// </summary>
        IEnumerator WaitForLanding()
        {
            // Wait a bit more to ensure ball has settled
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("🎯 Ball has settled on target");
        }
        
        /// <summary>
        /// Return ball to original position
        /// </summary>
        IEnumerator ReturnBallToOriginal()
        {
            Debug.Log("🔄 Returning ball to original position...");
            
            isReturning = true;
            
            Vector3 currentPos = ball.transform.position;
            Vector3 targetPos = originalBallPosition;
            float distance = Vector3.Distance(currentPos, targetPos);
            float timeToReturn = distance / returnSpeed;
            
            // Disable physics during return
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = true;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
            }
            
            // Smooth return movement
            float elapsed = 0f;
            while (elapsed < timeToReturn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / timeToReturn;
                
                // Smooth interpolation
                t = Mathf.SmoothStep(0f, 1f, t);
                
                ball.transform.position = Vector3.Lerp(currentPos, targetPos, t);
                
                yield return null;
            }
            
            // Ensure exact positioning
            ball.transform.position = targetPos;
            
            // Re-enable physics
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = false;
            }
            
            isReturning = false;
            Debug.Log("🔄 Ball returned to original position");
        }
        
        /// <summary>
        /// Reset ball to original position immediately
        /// </summary>
        public void ResetBall()
        {
            if (ball != null)
            {
                ball.transform.position = originalBallPosition;
                
                if (ballRigidbody != null)
                {
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                }
                
                Debug.Log("🔄 Ball reset to original position");
            }
        }
        
        /// <summary>
        /// Get current test status
        /// </summary>
        public string GetTestStatus()
        {
            return $"Test Running: {isRunning}\n" +
                   $"Ball Returning: {isReturning}\n" +
                   $"Has Landed: {hasLanded}\n" +
                   $"Ball Position: {ball?.transform.position}\n" +
                   $"Target Position: {target?.position}";
        }
        
        /// <summary>
        /// Context menu functions for testing
        /// </summary>
        [ContextMenu("Start Test")]
        void StartTestContext()
        {
            StartContinuousBowling();
        }
        
        [ContextMenu("Stop Test")]
        void StopTestContext()
        {
            StopContinuousBowling();
        }
        
        [ContextMenu("Reset Ball")]
        void ResetBallContext()
        {
            ResetBall();
        }
        
        [ContextMenu("Show Status")]
        void ShowStatusContext()
        {
            Debug.Log(GetTestStatus());
        }
        
        /// <summary>
        /// Draw gizmos for visualization
        /// </summary>
        void OnDrawGizmos()
        {
            if (ball != null && target != null)
            {
                // Draw ball trajectory
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ball.transform.position, target.position);
                
                // Draw target
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(target.position, 0.2f);
                
                // Draw original position
                if (Application.isPlaying)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(originalBallPosition, 0.15f);
                }
            }
        }
    }
}
