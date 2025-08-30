using System.Collections;
using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Continuous Bowling Test System - WITH BOUNCE PHYSICS
    /// Automatically bowls ball to target with realistic cricket physics and bouncing, waits 3 seconds, returns to original position, and repeats
    /// </summary>
    public class ContinuousBowlingTest_WithBounce : MonoBehaviour
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
        [SerializeField] private float bounceForce = 0.8f; // 🎯 FIXED: Increased bounce force for more visible bouncing (0.8 = 80% of impact velocity)
        [SerializeField] private float bounceFriction = 0.85f; // 🎯 FIXED: Increased friction to maintain bounce energy longer (0.85 = 85% of velocity preserved)
        [SerializeField] private float maxBounces = 3; // 🎯 FIXED: Increased max bounces for more visible bouncing
        
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
        private int currentBounces = 0; // 🎯 NEW: Track number of bounces
        private bool isBouncing = false; // 🎯 NEW: Track if ball is currently bouncing
        private Vector3 lastBouncePosition; // 🎯 NEW: Store position of last bounce
        private Transform spawnPoint; // 🎯 NEW: Internal spawn point reference
        
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
            
            // Store spawn point reference
            spawnPoint = ballSpawnPoint;
            
            // Store original position
            originalBallPosition = spawnPoint.position;
            
            // Setup ball physics
            SetupBallPhysics();
            
            Debug.Log("Continuous Bowling Test System with Bounce Physics initialized!");
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
            
            // 🎯 NEW: Add bounce physics component for realistic cricket ball bouncing
            CricketBallBounce bounceComponent = ball.GetComponent<CricketBallBounce>();
            if (bounceComponent == null)
            {
                bounceComponent = ball.AddComponent<CricketBallBounce>();
                bounceComponent.Initialize(this);
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
        /// 🎯 NEW: Setup physics for instantiated ball instances
        /// </summary>
        void SetupBallPhysicsForInstance(GameObject ballInstance)
        {
            Rigidbody instanceRigidbody = ballInstance.GetComponent<Rigidbody>();
            if (instanceRigidbody == null)
            {
                instanceRigidbody = ballInstance.AddComponent<Rigidbody>();
            }
            
            // 🎯 CRITICAL: Configure rigidbody for realistic cricket ball physics
            instanceRigidbody.mass = 0.16f; // Standard cricket ball weight
            instanceRigidbody.linearDamping = 0.02f; // Very low drag for realistic air resistance
            instanceRigidbody.angularDamping = 0.02f; // Low angular drag
            instanceRigidbody.useGravity = useRealisticPhysics;
            instanceRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add collider if missing
            if (ballInstance.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = ballInstance.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }
            
            // 🎯 CRITICAL: Add bounce physics component for realistic cricket ball bouncing
            CricketBallBounce bounceComponent = ballInstance.GetComponent<CricketBallBounce>();
            if (bounceComponent == null)
            {
                bounceComponent = ballInstance.AddComponent<CricketBallBounce>();
                bounceComponent.Initialize(this);
                Debug.Log("🏏 Bounce component added to ball instance");
            }
            else
            {
                bounceComponent.Initialize(this);
                Debug.Log("🏏 Bounce component initialized for ball instance");
            }
            
            // Add trail renderer for visual effect
            TrailRenderer trail = ballInstance.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = ballInstance.AddComponent<TrailRenderer>();
                trail.time = 0.8f;
                trail.startWidth = 0.05f;
                trail.endWidth = 0.01f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = new Color(1f, 1f, 0.4f);
                trail.endColor = new Color(1f, 1f, 0f);
            }
            
            Debug.Log("🏏 Ball instance physics setup complete!");
        }
        
        /// <summary>
        /// Handle input controls
        /// </summary>
        void HandleInput()
        {
            // 🎯 NEW: Instantiate new ball with S key
            if (Input.GetKeyDown(KeyCode.S))
            {
                InstantiateNewBall();
            }
            
            // 🎯 NEW: Bowl current ball with SPACE key
            if (Input.GetKeyDown(KeyCode.Space))
            {
                BowlCurrentBall();
            }
            
            // Manual ball reset with R key (kept for compatibility)
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetBall();
            }
        }
        
        /// <summary>
        /// 🎯 NEW APPROACH: Simple ball management system
        /// </summary>
        
        // 🎯 NEW: Ball lifecycle management
        private GameObject currentBallInstance;
        private bool ballIsBowled = false;
        
        /// <summary>
        /// 🎯 NEW: Instantiate new ball at spawn point
        /// </summary>
        public void InstantiateNewBall()
        {
            // Destroy existing ball instance if any (NOT the prefab reference!)
            if (currentBallInstance != null)
            {
                DestroyImmediate(currentBallInstance);
                currentBallInstance = null;
                Debug.Log("🏏 Destroyed previous ball instance");
            }
            
            // Instantiate new ball at spawn point
            if (ball != null && spawnPoint != null)
            {
                currentBallInstance = Instantiate(ball, spawnPoint.position, spawnPoint.rotation);
                Debug.Log($"🏏 New ball instantiated at: {spawnPoint.position}");
                
                // Reset state
                ballIsBowled = false;
                hasLanded = false;
                ResetBounceState();
                
                // 🎯 FIXED: Keep original ball prefab reference, only update instance references
                ballRigidbody = currentBallInstance.GetComponent<Rigidbody>();
                
                // 🎯 CRITICAL: Setup physics for the new ball instance
                SetupBallPhysicsForInstance(currentBallInstance);
                
                Debug.Log("🏏 Ball ready for bowling! Press SPACE to bowl");
            }
            else
            {
                Debug.LogError("🏏 Cannot instantiate ball - missing ball prefab or spawn point!");
            }
        }
        
        /// <summary>
        /// 🎯 NEW: Bowl the current ball
        /// </summary>
        public void BowlCurrentBall()
        {
            if (currentBallInstance == null)
            {
                Debug.LogWarning("🏏 No ball to bowl! Press S to create a new ball first.");
                return;
            }
            
            if (ballIsBowled)
            {
                Debug.LogWarning("🏏 Ball already bowled! Wait for it to be destroyed or press S for new ball.");
                return;
            }
            
            Debug.Log("🏏 Bowling current ball...");
            ballIsBowled = true;
            
            // Start bowling process
            StartCoroutine(BowlAndDestroy());
        }
        
        /// <summary>
        /// 🎯 NEW: Bowl ball - ball will destroy itself after 5 seconds
        /// </summary>
        IEnumerator BowlAndDestroy()
        {
            // Bowl the ball
            yield return StartCoroutine(BowlToTarget());
            
            // Wait for ball to land and bounce
            yield return StartCoroutine(WaitForLanding());
            
            // Ball will destroy itself after 5 seconds via BallAutoDestroy script
            Debug.Log("🎯 Ball has landed and bounced - it will destroy itself in 5 seconds");
            Debug.Log("🏏 Press S to create a new ball when ready!");
        }
        
        /// <summary>
        /// 🎯 NEW: Start continuous bowling test (kept for compatibility)
        /// </summary>
        public void StartContinuousBowling()
        {
            Debug.Log("🏏 Continuous bowling disabled - use S for new ball, SPACE to bowl!");
        }
        
        /// <summary>
        /// 🎯 NEW: Stop continuous bowling test (kept for compatibility)
        /// </summary>
        public void StopContinuousBowling()
        {
            Debug.Log("🏏 Continuous bowling disabled - use S for new ball, SPACE to bowl!");
        }
        
        /// <summary>
        /// 🎯 WORKING: Bowl the ball to target with working return system
        /// </summary>
        IEnumerator BowlToTarget()
        {
            Debug.Log("🏏 Bowling ball to target...");
            
            // 🎯 FIXED: Use current ball instance instead of prefab reference
            GameObject ballToBowl = currentBallInstance != null ? currentBallInstance : ball;
            Rigidbody ballRigidbodyToUse = ballToBowl.GetComponent<Rigidbody>();
            
            if (ballToBowl == null)
            {
                Debug.LogError("🏏 No ball to bowl!");
                yield break;
            }
            
            // 🎯 SAFETY: Ensure ball is at correct starting position
            if (Vector3.Distance(ballToBowl.transform.position, originalBallPosition) > 0.1f)
            {
                Debug.LogWarning("🏏 Ball not at original position! Forcing reset...");
                ballToBowl.transform.position = originalBallPosition;
                if (ballRigidbodyToUse != null)
                {
                    ballRigidbodyToUse.linearVelocity = Vector3.zero;
                    ballRigidbodyToUse.angularVelocity = Vector3.zero;
                }
            }
            
            // Reset landing flag and bounce state
            hasLanded = false;
            ResetBounceState();
            
            // Calculate trajectory to target
            Vector3 targetPosition = target.position;
            Vector3 startPosition = ballToBowl.transform.position;
            
            // Calculate horizontal distance
            Vector3 horizontalStart = new Vector3(startPosition.x, 0, startPosition.z);
            Vector3 horizontalTarget = new Vector3(targetPosition.x, 0, targetPosition.z);
            float horizontalDistance = Vector3.Distance(horizontalStart, horizontalTarget);
            
            // Calculate time to reach target
            float timeToReach = horizontalDistance / ballSpeed;
            
            // Calculate realistic cricket bowling arc
            float heightDifference = targetPosition.y - startPosition.y;
            float realisticArcHeight = arcHeight * 0.2f; // Use your working arc height
            
            // Calculate required Y velocity for realistic arc
            float requiredYVelocity = (heightDifference + realisticArcHeight + 0.5f * gravity * timeToReach * timeToReach) / timeToReach;
            
            // Cap Y velocity for realistic cricket arc
            float maxYVelocity = ballSpeed * 0.25f;
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
                ballRigidbodyToUse.linearVelocity = initialVelocity;
            }
            else
            {
                // Use kinematic movement for precise control
                StartCoroutine(MoveBallKinematic(startPosition, targetPosition, timeToReach));
            }
            
            Debug.Log($"🏏 Ball launched with velocity: {initialVelocity.magnitude:F1} m/s");
            Debug.Log($"🏏 Expected time to target: {timeToReach:F2} seconds");
            
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
        /// 🎯 WORKING: Wait for ball to land on target and finish bouncing
        /// </summary>
        IEnumerator WaitForLanding()
        {
            Debug.Log("🎯 Waiting for ball to finish bouncing and settle...");
            
            // Wait for initial landing
            yield return new WaitForSeconds(0.3f);
            
            // Wait for bounces to complete
            while (isBouncing && currentBounces < maxBounces)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // Additional wait to ensure ball has settled
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"🎯 Ball has settled after {currentBounces} bounces");
            
            // 🎯 FORCE: Stop bouncing if still bouncing
            if (isBouncing)
            {
                isBouncing = false;
                Debug.Log("🎯 Force stopped bouncing - ball ready to return");
            }
            
            // 🎯 CRITICAL: Ensure ball is ready for return
            if (ballRigidbody != null)
            {
                // Force stop any remaining movement
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
                Debug.Log("🎯 Ball movement stopped - ready for return");
            }
        }
        
        /// <summary>
        /// Handle ball bounce event from CricketBallBounce component
        /// </summary>
        public void OnBallBounce(Vector3 bouncePosition, Vector3 bounceVelocity)
        {
            currentBounces++;
            isBouncing = true;
            lastBouncePosition = bouncePosition;
            
            Debug.Log($"🏏 Ball bounced! Bounce #{currentBounces} at {bouncePosition}");
            Debug.Log($"🏏 Bounce velocity: {bounceVelocity.magnitude:F1} m/s");
            
            // 🎯 FIXED: Apply enhanced bounce physics for more visible bouncing
            GameObject ballToBounce = currentBallInstance != null ? currentBallInstance : ball;
            Rigidbody ballRigidbodyToBounce = ballToBounce.GetComponent<Rigidbody>();
            
            if (ballRigidbodyToBounce != null && currentBounces <= maxBounces)
            {
                // Calculate bounce velocity with friction
                Vector3 newVelocity = bounceVelocity * bounceFriction;
                
                // 🎯 ENHANCED: Apply stronger bounce force for more visible bouncing
                float enhancedBounceForce = bounceForce;
                if (currentBounces == 1)
                {
                    enhancedBounceForce = bounceForce * 1.2f; // First bounce is stronger
                }
                else if (currentBounces == 2)
                {
                    enhancedBounceForce = bounceForce * 0.9f; // Second bounce slightly weaker
                }
                else
                {
                    enhancedBounceForce = bounceForce * 0.7f; // Third bounce weaker
                }
                
                // Apply enhanced bounce force to Y velocity
                newVelocity.y = Mathf.Abs(bounceVelocity.y) * enhancedBounceForce;
                
                // 🎯 ADDITIONAL: Preserve some horizontal momentum for realistic cricket bounce
                newVelocity.x *= 0.9f; // Keep 90% of horizontal velocity
                newVelocity.z *= 0.9f; // Keep 90% of horizontal velocity
                
                // Apply the enhanced velocity
                ballRigidbodyToBounce.linearVelocity = newVelocity;
                
                Debug.Log($"🏏 Enhanced bounce physics applied! Bounce #{currentBounces}");
                Debug.Log($"🏏 Enhanced bounce force: {enhancedBounceForce:F2}");
                Debug.Log($"🏏 New velocity: {newVelocity.magnitude:F1} m/s");
            }
            
            // Stop bouncing if max bounces reached
            if (currentBounces >= maxBounces)
            {
                isBouncing = false;
                Debug.Log("🏏 Max bounces reached - ball settling");
            }
        }
        
        /// <summary>
        /// Reset bounce state for new bowl
        /// </summary>
        void ResetBounceState()
        {
            currentBounces = 0;
            isBouncing = false;
            lastBouncePosition = Vector3.zero;
        }
        
        /// <summary>
        /// Get the target transform (for bounce component)
        /// </summary>
        public Transform GetTarget()
        {
            return target;
        }
        
        /// <summary>
        /// 🎯 AGGRESSIVE FIX: Return ball to original position with forced positioning
        /// </summary>
        IEnumerator ReturnBallToOriginal()
        {
            Debug.Log("🔄 AGGRESSIVE: Returning ball to original position...");
            
            isReturning = true;
            
            Vector3 currentPos = ball.transform.position;
            Vector3 targetPos = originalBallPosition;
            float distance = Vector3.Distance(currentPos, targetPos);
            float timeToReturn = distance / returnSpeed;
            
            Debug.Log($"🔄 Distance to return: {distance:F2}m, Time: {timeToReturn:F2}s");
            Debug.Log($"🔄 From: {currentPos} To: {targetPos}");
            
            // 🎯 AGGRESSIVE STOP: Completely disable physics
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = true;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
                ballRigidbody.useGravity = false;
                ballRigidbody.linearDamping = 0f;
                ballRigidbody.angularDamping = 0f;
                ballRigidbody.mass = 0.1f; // Make it very light during return
            }
            
            // 🎯 IMMEDIATE: Force ball to stop moving
            ball.transform.position = currentPos;
            
            // 🎯 AGGRESSIVE: Fast return movement with forced positioning
            float elapsed = 0f;
            while (elapsed < timeToReturn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / timeToReturn;
                
                // Fast interpolation
                t = Mathf.SmoothStep(0f, 1f, t);
                
                Vector3 newPosition = Vector3.Lerp(currentPos, targetPos, t);
                
                // 🎯 FORCE: Set position every frame
                ball.transform.position = newPosition;
                
                // Debug position every few frames
                if (Mathf.FloorToInt(elapsed * 10) % 5 == 0)
                {
                    Debug.Log($"🔄 Return progress: {t:P0} - Position: {newPosition}");
                }
                
                yield return null;
            }
            
            // 🎯 FORCE FINAL: Ensure exact positioning
            ball.transform.position = targetPos;
            Debug.Log($"🔄 Final position: {ball.transform.position}");
            
            // 🎯 RESET: Re-enable physics with clean state
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = false;
                ballRigidbody.useGravity = useRealisticPhysics;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
                ballRigidbody.linearDamping = 0.02f;
                ballRigidbody.angularDamping = 0.02f;
                ballRigidbody.mass = 1f; // Restore normal mass
            }
            
            // 🎯 VERIFY: Double-check position
            if (Vector3.Distance(ball.transform.position, targetPos) > 0.01f)
            {
                Debug.LogWarning("🔄 Position mismatch! Forcing final position...");
                ball.transform.position = targetPos;
            }
            
            isReturning = false;
            Debug.Log("🔄 Ball successfully returned to original position!");
        }
        
        /// <summary>
        /// 🎯 ENHANCED: Reset ball to original position immediately with force
        /// </summary>
        public void ResetBall()
        {
            if (ball != null)
            {
                Debug.Log("🔄 FORCE RESET: Ball to original position...");
                
                // 🎯 FORCE: Stop all physics immediately
                if (ballRigidbody != null)
                {
                    ballRigidbody.isKinematic = true;
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                    ballRigidbody.useGravity = false;
                }
                
                // 🎯 FORCE: Set position
                ball.transform.position = originalBallPosition;
                
                // 🎯 RESET: Physics state
                if (ballRigidbody != null)
                {
                    ballRigidbody.isKinematic = false;
                    ballRigidbody.useGravity = useRealisticPhysics;
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                }
                
                ResetBounceState();
                Debug.Log("🔄 Ball FORCE RESET to original position!");
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
                   $"Current Bounces: {currentBounces}\n" +
                   $"Is Bouncing: {isBouncing}\n" +
                   $"Ball Position: {ball?.transform.position}\n" +
                   $"Target Position: {target?.position}";
        }
        
        /// <summary>
        /// 🎯 NEW: Context menu functions for simple ball management
        /// </summary>
        [ContextMenu("Create New Ball")]
        void CreateNewBallContext()
        {
            InstantiateNewBall();
        }
        
        [ContextMenu("Bowl Current Ball")]
        void BowlCurrentBallContext()
        {
            BowlCurrentBall();
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
        /// 🎯 ENHANCED: Draw gizmos for precise visualization and debugging
        /// </summary>
        void OnDrawGizmos()
        {
            if (ball != null && target != null)
            {
                // 🎯 PRECISE: Draw exact ball trajectory
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ball.transform.position, target.position);
                
                // 🎯 PRECISE: Draw target with larger indicator
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(target.position, 0.3f);
                
                // 🎯 PRECISE: Draw landing zone (where ball should hit)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, 0.1f);
                
                // 🎯 PRECISE: Draw original position
                if (Application.isPlaying)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(originalBallPosition, 0.15f);
                }
                
                // 🎯 PRECISE: Draw bounce positions
                if (Application.isPlaying && lastBouncePosition != Vector3.zero)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(lastBouncePosition, 0.1f);
                }
                
                // 🎯 PRECISE: Draw predicted landing point
                if (Application.isPlaying && ballRigidbody != null)
                {
                    Vector3 velocity = ballRigidbody.linearVelocity;
                    if (velocity.magnitude > 0.1f)
                    {
                        // Calculate where ball will land based on current velocity
                        float timeToLand = Mathf.Abs(ball.transform.position.y - target.position.y) / Mathf.Abs(velocity.y);
                        Vector3 predictedLanding = ball.transform.position + velocity * timeToLand;
                        
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(predictedLanding, 0.2f);
                        Gizmos.DrawLine(ball.transform.position, predictedLanding);
                    }
                }
            }
        }
    }
}
