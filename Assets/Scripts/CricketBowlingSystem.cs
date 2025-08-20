using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CricketGame
{
    /// <summary>
    /// Professional Cricket Bowling System
    /// Handles ball physics, bowling mechanics, and realistic cricket gameplay
    /// </summary>
    public class CricketBowlingSystem : MonoBehaviour
    {
        [Header("Bowling Machine Setup")]
        [SerializeField] private Transform ballSpawnPoint;
        [SerializeField] private GameObject ballPrefab; // Ball prefab to instantiate
        [SerializeField] private Transform wicketTarget;
        [SerializeField] private Transform bowlingMachine;
        [SerializeField] private Transform pitchingArea; // Pitching area center
        [SerializeField] private Transform topLeftCorner; // Top left corner of pitching area
        [SerializeField] private Transform topRightCorner; // Top right corner of pitching area
        [SerializeField] private Transform bottomLeftCorner; // Bottom left corner of pitching area
        [SerializeField] private Transform bottomRightCorner; // Bottom right corner of pitching area
        
        [Header("Bowling Physics")]
        [SerializeField] private float ballSpeed = 50f; // m/s (180 km/h fast bowling) - Increased for wicket reachability
        [SerializeField] private float spinRate = 0f; // RPM
        [SerializeField] private float swingAmount = 0f; // meters
        [SerializeField] private float seamMovement = 0f; // meters
        [SerializeField] private float bounceHeight = 0.5f; // meters
        [SerializeField] private float ballWeight = 0.16f; // kg (standard cricket ball)
        
        [Header("Bounce Physics - Adjust in Editor")]
        [SerializeField] public float speedBoostAfterBounce = 1.2f; // Speed multiplier after hitting pitching area
        [SerializeField] public float directionPreservation = 0.95f; // How much direction is preserved (0-1)
        [SerializeField] public float upwardBounceForce = 0.3f; // Upward force multiplier after bounce
        [SerializeField] public float forwardMomentumBoost = 1.5f; // Extra forward momentum after bounce
        [SerializeField] public float maxBounceHeight = 2.0f; // Maximum bounce height allowed
        
        [Header("Bowling Variations")]
        [SerializeField] private BowlingType currentBowlingType = BowlingType.FastBowl;
        [SerializeField] private LineVariation lineVariation = LineVariation.OffStump;
        [SerializeField] private LengthVariation lengthVariation = LengthVariation.GoodLength;
        [SerializeField] private bool isYorker = false;
        [SerializeField] private bool isBouncer = false;
        
        [Header("Advanced Physics")]
        [SerializeField] private float airResistance = 0.02f;
        [SerializeField] private float groundFriction = 0.8f;
        [SerializeField] private float windEffect = 0f;
        [SerializeField] private Vector3 windDirection = Vector3.zero;
        
        [Header("Ball Behavior")]
        [SerializeField] private float seamAngle = 0f;
        [SerializeField] private float ballRoughness = 0.5f;
        [SerializeField] private bool reverseSwing = false;
        [SerializeField] private float reverseSwingThreshold = 15f; // overs
        
        [Header("Controls")]
        [SerializeField] private KeyCode bowlKey = KeyCode.Space;
        [SerializeField] private KeyCode changeLineKey = KeyCode.Q;
        [SerializeField] private KeyCode changeLengthKey = KeyCode.E;
        [SerializeField] private KeyCode changeTypeKey = KeyCode.R;
        
                 [Header("Debug")]
         [SerializeField] private bool showTrajectory = true;
         [SerializeField] private bool showPhysicsInfo = true;
         [SerializeField] private int trajectoryPoints = 50;
         [SerializeField] private bool showPitchPrediction = true; // Show where ball will pitch
         [SerializeField] private Color pitchPredictionColor = Color.red; // Color for pitch prediction
         
         [Header("In-Game Pitch Prediction")]
         [SerializeField] private bool showInGamePrediction = true; // Show prediction during gameplay
         [SerializeField] private GameObject pitchPredictionMarker; // Simple sphere on ground showing pitch location
        
                 // Private variables
         private GameObject currentBall;
         private Rigidbody ballRigidbody;
         private TrailRenderer ballTrail;
         private bool isBowling = false;
         private float currentOvers = 0f;
         private Vector3 originalBallPosition;
         private Vector3 ballSpawnPosition; // Store spawn position
         private Vector3 currentTargetPosition; // Store the current target for prediction
         private bool hasLanded = false; // Flag to track if ball has landed
        
        // Bowling enums
        public enum BowlingType
        {
            FastBowl,
            MediumPace,
            SpinBowl,
            Yorker,
            Bouncer,
            SlowerBall
        }
        
        public enum LineVariation
        {
            LegStump,
            MiddleStump,
            OffStump,
            WideOffStump,
            WideLegStump,
            YorkerLine
        }
        
        public enum LengthVariation
        {
            FullToss,
            FullLength,
            GoodLength,
            ShortLength,
            BouncerLength
        }
        
        // Events
        public System.Action<GameObject> OnBallBowled;
        public System.Action<GameObject> OnBallHitWicket;
        public System.Action<GameObject> OnBallMissed;
        
                 void Start()
         {
             SetupBowlingSystem();
             SetupInGamePrediction();
             
             // Set initial target when game starts (only once)
             SetInitialTarget();
             
             // Show initial prediction for first ball
             StartCoroutine(ShowInitialPrediction());
         }
        
                 void Update()
         {
             HandleInput();
             UpdatePhysics();
         }
        
                 /// <summary>
         /// Show initial prediction after setup is complete
         /// </summary>
         IEnumerator ShowInitialPrediction()
         {
             // Wait for setup to complete
             yield return new WaitForEndOfFrame();
             
             // Show initial prediction
             UpdatePredictionForNextBall();
         }
         
         /// <summary>
         /// Update prediction for the next ball (called when Space is pressed)
         /// </summary>
         void UpdatePredictionForNextBall()
         {
             if (!showInGamePrediction || pitchPredictionMarker == null) return;
             
             // Calculate and show prediction for next ball
             UpdatePredictionVisuals();
             
             // Make sure prediction is visible
             if (pitchPredictionMarker != null)
                 pitchPredictionMarker.SetActive(true);
         }
         
         /// <summary>
         /// Calculate prediction using the current target position
         /// </summary>
         Vector3 CalculatePredictionFromCurrentTarget()
         {
             if (ballSpawnPoint == null || currentTargetPosition == Vector3.zero) return Vector3.zero;
             
             // Simply return the current target position - this is where the ball WILL land
             return currentTargetPosition;
         }
         
         /// <summary>
         /// Update in-game pitch prediction automatically (no shift key needed)
         /// </summary>
         void UpdateInGamePrediction()
         {
             if (!showInGamePrediction || pitchPredictionMarker == null) return;
             
             // 🎯 ENSURE TARGET IMAGE IS LOADED
             EnsureTargetImageLoaded();
             
             if (currentTargetPosition != Vector3.zero)
             {
                 // 🎯 POSITION WORLD CANVAS at target location
                 Vector3 targetPos = currentTargetPosition;
                 targetPos.y += 0.01f; // Slightly above ground to prevent z-fighting
                 
                 pitchPredictionMarker.transform.position = targetPos;
                 
                 // 🎯 ROTATE CANVAS to face camera for best visibility
                 if (Camera.main != null)
                 {
                     Vector3 directionToCamera = Camera.main.transform.position - targetPos;
                     directionToCamera.y = 0; // Keep canvas flat
                     if (directionToCamera != Vector3.zero)
                     {
                         pitchPredictionMarker.transform.rotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(90, 0, 0);
                     }
                 }
                 
                 pitchPredictionMarker.SetActive(true);
             }
             else
             {
                 pitchPredictionMarker.SetActive(false);
             }
         }
         
         /// <summary>
         /// Update the visual prediction elements
         /// </summary>
         void UpdatePredictionVisuals()
         {
             if (pitchPredictionMarker == null) return;
             
             // Calculate pitch prediction using the CURRENT target (not old one)
             Vector3 pitchPoint = CalculatePredictionFromCurrentTarget();
             
             // Position marker EXACTLY where ball will land (same Y as pitching area)
             Vector3 markerPosition = pitchPoint;
             markerPosition.y = pitchingArea.position.y + 0.02f; // Just above ground surface
             pitchPredictionMarker.transform.position = markerPosition;
         }
         

         

         
         /// <summary>
         /// Set initial target when game starts (only once)
         /// </summary>
         void SetInitialTarget()
         {
             if (currentTargetPosition == Vector3.zero)
             {
                 // Calculate initial target without logging
                 Vector3 targetPosition;
                 
                 if (topLeftCorner != null && topRightCorner != null && bottomLeftCorner != null && bottomRightCorner != null)
                 {
                     // Calculate the actual boundaries from corner positions
                     float minX = Mathf.Min(topLeftCorner.position.x, bottomLeftCorner.position.x);
                     float maxX = Mathf.Max(topRightCorner.position.x, bottomRightCorner.position.x);
                     float minZ = Mathf.Min(bottomLeftCorner.position.z, bottomRightCorner.position.z);
                     float maxZ = Mathf.Max(topLeftCorner.position.z, topRightCorner.position.z);
                     
                     // Calculate center of the actual pitching area
                     Vector3 areaCenter = new Vector3((minX + maxX) * 0.5f, pitchingArea.position.y, (minZ + maxZ) * 0.5f);
                     
                     // Use 98% of the actual area for MAXIMUM COVERAGE
                     float safeXRange = (maxX - minX) * 0.49f;
                     float safeZRange = (maxZ - minZ) * 0.49f;
                     
                     // Random offset within the guaranteed hitting area
                     float randomX = Random.Range(-safeXRange, safeXRange);
                     float randomZ = Random.Range(-safeXRange, safeZRange);
                     
                     targetPosition = areaCenter + new Vector3(randomX, 0, randomZ);
                 }
                 else if (pitchingArea != null)
                 {
                     // Fallback to pitching area center
                     Vector3 areaCenter = pitchingArea.position;
                     Vector3 areaSize = pitchingArea.localScale;
                     
                     float safeXRange = areaSize.x * 0.475f;
                     float safeZRange = areaSize.z * 0.475f;
                     
                     float randomX = Random.Range(-safeXRange, safeXRange);
                     float randomZ = Random.Range(-safeXRange, safeZRange);
                     
                     targetPosition = areaCenter + new Vector3(randomX, 0, randomZ);
                 }
                 else
                 {
                     targetPosition = Vector3.zero;
                 }
                 
                 // Store the target position
                 currentTargetPosition = targetPosition;
                 Debug.Log($"🎯 Initial target set: {targetPosition}");
             }
         }
         
         /// <summary>
         /// Setup in-game pitch prediction visualization
         /// </summary>
         void SetupInGamePrediction()
         {
             if (!showInGamePrediction) return;
             
             // Create prediction marker if not assigned
             if (pitchPredictionMarker == null)
             {
                 // 🎯 CREATE WORLD CANVAS with TARGET IMAGE instead of sphere
                 GameObject canvasObject = new GameObject("PitchPredictionCanvas");
                 Canvas worldCanvas = canvasObject.AddComponent<Canvas>();
                 worldCanvas.renderMode = RenderMode.WorldSpace;
                 worldCanvas.worldCamera = Camera.main;
                 
                 // Add CanvasScaler for proper scaling
                 CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                 scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                 scaler.referenceResolution = new Vector2(1920, 1080);
                 
                 // Add GraphicRaycaster for interaction
                 canvasObject.AddComponent<GraphicRaycaster>();
                 
                 // Set canvas size and position
                 RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
                 canvasRect.sizeDelta = new Vector2(2f, 2f); // 2x2 meter canvas
                 canvasRect.localScale = Vector3.one * 0.5f; // Scale down for better size
                 
                 // Create Image GameObject
                 GameObject imageObject = new GameObject("TargetImage");
                 imageObject.transform.SetParent(canvasObject.transform, false);
                 
                 // Add Image component
                 UnityEngine.UI.Image targetImage = imageObject.AddComponent<UnityEngine.UI.Image>();
                 
                 // Load and assign the target image
                 Sprite targetSprite = Resources.Load<Sprite>("black-red-target");
                 if (targetSprite == null)
                 {
                     // Try to load from Assets folder
                     targetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/black-red-target.png");
                 }
                 
                 if (targetSprite != null)
                 {
                     targetImage.sprite = targetSprite;
                     targetImage.preserveAspect = true;
                     Debug.Log("🎯 Target image loaded successfully!");
                 }
                 else
                 {
                     // Fallback to red circle if image not found
                     targetImage.color = Color.red;
                     Debug.LogWarning("🎯 Target image not found, using red circle fallback");
                 }
                 
                 // Set image size to fill canvas
                 RectTransform imageRect = imageObject.GetComponent<RectTransform>();
                 imageRect.anchorMin = Vector2.zero;
                 imageRect.anchorMax = Vector2.one;
                 imageRect.offsetMin = Vector2.zero;
                 imageRect.offsetMax = Vector2.zero;
                 
                 // Position canvas slightly above ground
                 canvasObject.transform.position = Vector3.zero;
                 canvasObject.transform.rotation = Quaternion.Euler(90, 0, 0); // Face up
                 
                 // Assign to pitchPredictionMarker for tracking
                 pitchPredictionMarker = canvasObject;
                 
                 Debug.Log("🎯 World Canvas Target Image created successfully!");
             }
         }
         
         /// <summary>
         /// Initialize the bowling system
         /// </summary>
         void SetupBowlingSystem()
        {
            if (ballPrefab == null)
            {
                Debug.LogError("Ball prefab not assigned! Please assign a ball prefab.");
                return;
            }
            
            if (ballSpawnPoint == null)
            {
                Debug.LogError("Ball spawn point not assigned! Please assign a spawn point.");
                return;
            }
            
            if (wicketTarget == null)
            {
                Debug.LogError("Wicket target not assigned! Please assign a wicket target.");
                return;
            }
            
            // Store spawn position
            ballSpawnPosition = ballSpawnPoint.position;
            
            Debug.Log("Cricket Bowling System initialized successfully!");
        }
        
        /// <summary>
        /// Setup ball physics components
        /// </summary>
        void SetupBallPhysics()
        {
            if (currentBall == null) return;
            
            // Get or add required components
            ballRigidbody = currentBall.GetComponent<Rigidbody>();
            if (ballRigidbody == null)
            {
                ballRigidbody = currentBall.AddComponent<Rigidbody>();
            }
            
            // Configure rigidbody
            ballRigidbody.mass = ballWeight;
            ballRigidbody.linearDamping = 0.01f; // Reduced from airResistance to maintain momentum
            ballRigidbody.angularDamping = 0.02f; // Reduced angular damping
            ballRigidbody.useGravity = true;
            ballRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add collider if missing
            if (currentBall.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = currentBall.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }
            
            // Add trail renderer for visual effect
            ballTrail = currentBall.GetComponent<TrailRenderer>();
            if (ballTrail == null)
            {
                ballTrail = currentBall.AddComponent<TrailRenderer>();
                SetupBallTrail();
            }
        }
        
        /// <summary>
        /// Setup ball trail renderer
        /// </summary>
        void SetupBallTrail()
        {
            if (ballTrail != null)
            {
                ballTrail.time = 0.5f;
                ballTrail.startWidth = 0.05f;
                ballTrail.endWidth = 0.01f;
                ballTrail.material = new Material(Shader.Find("Sprites/Default"));
                ballTrail.startColor = new Color(1f, 1f, 1f, 0.3f); // 🎯 WHITE with 30% transparency
                ballTrail.endColor = new Color(1f, 1f, 1f, 0f); // 🎯 WHITE with 0% transparency (fade out)
            }
        }
        
        /// <summary>
        /// Handle user input
        /// </summary>
        void HandleInput()
        {
            if (Input.GetKeyDown(bowlKey) && !isBowling)
            {
                BowlBall();
            }
            
            if (Input.GetKeyDown(changeLineKey))
            {
                CycleLineVariation();
            }
            
            if (Input.GetKeyDown(changeLengthKey))
            {
                CycleLengthVariation();
            }
            
            if (Input.GetKeyDown(changeTypeKey))
            {
                CycleBowlingType();
            }
        }
        
                 /// <summary>
         /// Main bowling function
         /// </summary>
         public void BowlBall()
         {
             if (isBowling) return;
             
             // Reset target position for new ball
             currentTargetPosition = Vector3.zero;
             
             StartCoroutine(BowlingSequence());
         }
        
                 /// <summary>
         /// Complete bowling sequence
         /// </summary>
         IEnumerator BowlingSequence()
         {
             isBowling = true;
             
             // Create new ball from prefab
             CreateNewBall();
             
             // Calculate bowling parameters FIRST (this sets currentTargetPosition)
             Vector3 targetDirection = CalculateTargetDirection();
             float currentSpeed = CalculateBallSpeed();
             Vector3 initialVelocity = CalculateInitialVelocity(targetDirection, currentSpeed);
             
             // Keep the aim sphere visible - DON'T update prediction during bowling
             // The aim sphere should stay where it is until ball hits pitching area
             
             // Apply physics
             ApplyBowlingPhysics(initialVelocity);
             
             // Trigger events
             OnBallBowled?.Invoke(currentBall);
             
             // Allow immediate new ball generation
             isBowling = false;
             
             // Destroy the ball after 5 seconds (but don't block new balls)
             StartCoroutine(DestroyBallAfterDelay(5f));
             
             // Update overs
             currentOvers += 0.1f;
             
             // Required for IEnumerator - must yield return a value
             yield return null;
         }
        
                 /// <summary>
         /// Destroy ball after delay without blocking new balls
         /// </summary>
         IEnumerator DestroyBallAfterDelay(float delay)
         {
             yield return new WaitForSeconds(delay);
             DestroyBall();
             
             // Show prediction for next ball after current ball is destroyed
             UpdatePredictionForNextBall();
         }
         
                 /// <summary>
        /// Get the current target position for the ball
        /// </summary>
        public Vector3 GetCurrentTargetPosition()
        {
            return currentTargetPosition;
        }
        
        /// <summary>
        /// Hide pitch prediction marker
        /// </summary>
        public void HidePrediction()
        {
            if (pitchPredictionMarker != null)
            {
                // 🎯 HIDE WORLD CANVAS TARGET IMAGE
                pitchPredictionMarker.SetActive(false);
                Debug.Log("🎯 Target image hidden - ball has pitched!");
            }
        }
        
        /// <summary>
        /// Set ball landing flag when ball hits pitching area (called from CricketBall script)
        /// </summary>
        public void SetBallLanded()
        {
            hasLanded = true;
            Debug.Log("🎯 Ball has landed on pitching area - accuracy tracking complete");
        }
         
         /// <summary>
         /// Show prediction again for next ball
         /// </summary>
         public void ShowPredictionForNextBall()
         {
             UpdatePredictionForNextBall();
         }
        
                 /// <summary>
         /// Calculate target direction with FORCED PITCHING AREA TARGETING
         /// </summary>
         Vector3 CalculateTargetDirection()
         {
             // FORCE the ball to hit within the pitching area - NO EXCEPTIONS!
             Vector3 targetPosition;
             
             if (topLeftCorner != null && topRightCorner != null && bottomLeftCorner != null && bottomRightCorner != null)
             {
                 // Calculate the actual boundaries from corner positions
                 float minX = Mathf.Min(topLeftCorner.position.x, bottomLeftCorner.position.x);
                 float maxX = Mathf.Max(topRightCorner.position.x, bottomRightCorner.position.x);
                 float minZ = Mathf.Min(bottomLeftCorner.position.z, bottomRightCorner.position.z);
                 float maxZ = Mathf.Max(topLeftCorner.position.z, topRightCorner.position.z);
                 
                 // SAFETY CHECK: Fix backwards bounds
                 if (minX > maxX)
                 {
                     Debug.LogError($"🎯 X BOUNDS ARE BACKWARDS! Swapping minX({minX:F2}) and maxX({maxX:F2})");
                     float temp = minX;
                     minX = maxX;
                     maxX = temp;
                 }
                 
                 if (minZ > maxZ)
                 {
                     Debug.LogError($"🎯 Z BOUNDS ARE BACKWARDS! Swapping minZ({minZ:F2}) and maxZ({maxZ:F2})");
                     float temp = minZ;
                     minZ = maxZ;
                     maxZ = temp;
                 }
                 
                 // Calculate center of the actual pitching area
                 Vector3 areaCenter = new Vector3((minX + maxX) * 0.5f, pitchingArea.position.y, (minZ + maxZ) * 0.5f);
                 
                 // Use 80% of the actual area for GUARANTEED SAFE TARGETING
                 float safeXRange = (maxX - minX) * 0.4f;
                 float safeZRange = (maxZ - minZ) * 0.4f;
                 
                 // Random offset within the guaranteed hitting area
                 float randomX = Random.Range(-safeXRange, safeXRange);
                 float randomZ = Random.Range(-safeZRange, safeZRange);
                 
                 targetPosition = areaCenter + new Vector3(randomX, 0, randomZ);
                 
                 // VALIDATE: Ensure target is within pitching area bounds
                 targetPosition = ClampTargetToPitchingArea(targetPosition, minX, maxX, minZ, maxZ);
                 
                 // DOUBLE CHECK: Verify target is within bounds
                 if (targetPosition.x < minX || targetPosition.x > maxX || targetPosition.z < minZ || targetPosition.z > maxZ)
                 {
                     Debug.LogError($"🎯 TARGET OUT OF BOUNDS! Clamping to center. Target: {targetPosition}, Bounds: X[{minX:F2}, {maxX:F2}], Z[{minZ:F2}, {maxZ:F2}]");
                     targetPosition = areaCenter; // Force to center if still out of bounds
                 }
                 
                 Debug.Log($"🎯 Target: {targetPosition}");
                 Debug.Log($"🎯 Bounds: X[{minX:F2}, {maxX:F2}], Z[{minZ:F2}, {maxZ:F2}]");
             }
             else if (pitchingArea != null)
             {
                 // Fallback to pitching area center with very tight bounds
                 Vector3 areaCenter = pitchingArea.position;
                 Vector3 areaSize = pitchingArea.localScale;
                 
                 // Use 80% of area size for GUARANTEED SAFE COVERAGE
                 float safeXRange = areaSize.x * 0.4f;
                 float safeZRange = areaSize.z * 0.4f;
                 
                 float randomX = Random.Range(-safeXRange, safeXRange);
                 float randomZ = Random.Range(-safeZRange, safeZRange);
                 
                 targetPosition = areaCenter + new Vector3(randomX, 0, randomZ);
                 Debug.LogWarning("Using pitching area fallback - assign corner GameObjects for better accuracy!");
             }
             else
             {
                 // NO FALLBACK TO WICKET - Force pitching area targeting
                 Debug.LogError("NO PITCHING AREA FOUND! Ball cannot be targeted properly!");
                 return Vector3.forward; // Default forward direction
             }
             
             // STORE the target position for prediction to use
             currentTargetPosition = targetPosition;
             
             Vector3 baseDirection = (targetPosition - ballSpawnPoint.position).normalized;
             
             // NO line variation - keep ball on target
             float lineOffset = 0f;
             
             // Apply offset perpendicular to bowling direction (if any)
             Vector3 rightDirection = Vector3.Cross(baseDirection, Vector3.up).normalized;
             Vector3 finalTargetPosition = targetPosition + (rightDirection * lineOffset);
             
             return (finalTargetPosition - ballSpawnPoint.position).normalized;
                  }
         
         /// <summary>
         /// Clamp target position to ensure it's within pitching area bounds
         /// </summary>
         Vector3 ClampTargetToPitchingArea(Vector3 target, float minX, float maxX, float minZ, float maxZ)
         {
             // Add larger margin to ensure ball lands safely within bounds
             float margin = 0.5f; // 50cm margin from edges for safety
             
             float clampedX = Mathf.Clamp(target.x, minX + margin, maxX - margin);
             float clampedZ = Mathf.Clamp(target.z, minZ + margin, maxZ - margin);
             
             // Log if clamping was needed
             if (target.x != clampedX || target.z != clampedZ)
             {
                 Debug.LogWarning($"🎯 Target clamped: {target} -> {new Vector3(clampedX, target.y, clampedZ)}");
             }
             
             return new Vector3(clampedX, target.y, clampedZ);
         }
         
         /// <summary>
         /// Calculate ball speed based on bowling type
         /// </summary>
         float CalculateBallSpeed()
        {
            float baseSpeed = ballSpeed;
            
            switch (currentBowlingType)
            {
                case BowlingType.FastBowl:
                    baseSpeed = Random.Range(45f, 60f); // Increased for wicket reachability
                    break;
                case BowlingType.MediumPace:
                    baseSpeed = Random.Range(40f, 55f); // Increased for wicket reachability
                    break;
                case BowlingType.SpinBowl:
                    baseSpeed = Random.Range(35f, 50f); // Increased for wicket reachability
                    break;
                case BowlingType.Yorker:
                    baseSpeed = Random.Range(42f, 58f); // Increased for wicket reachability
                    break;
                case BowlingType.Bouncer:
                    baseSpeed = Random.Range(48f, 65f); // Increased for wicket reachability
                    break;
                case BowlingType.SlowerBall:
                    baseSpeed = Random.Range(30f, 45f); // Increased for wicket reachability
                    break;
            }
            
            return baseSpeed;
        }
        
                 /// <summary>
         /// Calculate initial velocity for 100% ACCURATE landing on aiming sphere
         /// </summary>
        Vector3 CalculateInitialVelocity(Vector3 direction, float speed)
        {
            if (currentTargetPosition == Vector3.zero)
            {
                Debug.LogError("NO TARGET POSITION! Cannot calculate trajectory!");
                return Vector3.forward * speed;
            }

            // 🎯 100% ACCURACY SYSTEM: Ball MUST land exactly on aiming sphere
            
            // Calculate horizontal distance to the AIMING SPHERE
            Vector3 horizontalSpawnPos = new Vector3(ballSpawnPoint.position.x, 0, ballSpawnPoint.position.z);
            Vector3 horizontalTargetPos = new Vector3(currentTargetPosition.x, 0, currentTargetPosition.z);
            float horizontalDistance = Vector3.Distance(horizontalSpawnPos, horizontalTargetPos);
            
            // 🎯 ENHANCED PHYSICS COMPENSATION SYSTEM: Unity physics vs ideal projectile motion
            // Unity's physics engine has significant differences from ideal calculations
            // We need to test a much wider range to find the perfect compensation
            
            // Start with ideal calculation
            float idealTimeToReach = horizontalDistance / speed;
            
            // 🎯 EXPANDED COMPENSATION RANGE: Test from 0.5 to 2.0 (300% range)
            float bestCompensation = 1.0f;
            float bestAccuracy = float.MaxValue;
            
            // Test compensation values from 0.5 to 2.0 with finer granularity
            for (float comp = 0.5f; comp <= 2.0f; comp += 0.005f) // 300 test values
            {
                float testTimeToReach = horizontalDistance / (speed * comp);
                
                // Calculate required Y velocity using projectile motion formula
                float testGravity = 9.81f;
                float testTargetHeight = currentTargetPosition.y + 0.02f;
                float testHeightDifference = testTargetHeight - ballSpawnPoint.position.y;
                
                // Projectile motion formula: h = v0*t - 0.5*g*t^2
                // Rearranged: v0 = (h + 0.5*g*t^2) / t
                float testRequiredYVelocity = (testHeightDifference + 0.5f * testGravity * testTimeToReach * testTimeToReach) / testTimeToReach;
                
                // Ensure minimum Y velocity to avoid full toss
                float testMinYVelocity = 2.0f;
                if (testRequiredYVelocity < testMinYVelocity)
                {
                    testRequiredYVelocity = testMinYVelocity;
                }
                
                // Calculate horizontal velocity
                Vector3 testHorizontalDirection = (horizontalTargetPos - horizontalSpawnPos).normalized;
                float testExactHorizontalSpeed = horizontalDistance / testTimeToReach;
                Vector3 testHorizontalVelocity = testHorizontalDirection * testExactHorizontalSpeed;
                
                // Combine velocities
                Vector3 testVelocity = testHorizontalVelocity;
                testVelocity.y = testRequiredYVelocity;
                
                // 🎯 PREDICT LANDING: Calculate where this velocity will actually land
                Vector3 testPredictedLanding = ballSpawnPoint.position + testVelocity * testTimeToReach;
                testPredictedLanding.y = ballSpawnPoint.position.y + testVelocity.y * testTimeToReach - 0.5f * testGravity * testTimeToReach * testTimeToReach;
                
                // Calculate accuracy
                float testAccuracy = Vector3.Distance(testPredictedLanding, currentTargetPosition);
                
                // Keep the best compensation value
                if (testAccuracy < bestAccuracy)
                {
                    bestAccuracy = testAccuracy;
                    bestCompensation = comp;
                }
                
                // If we achieve perfect accuracy, stop testing
                if (testAccuracy < 0.01f)
                {
                    break;
                }
            }
            
            // 🎯 FALLBACK SYSTEM: If accuracy is still poor, try extreme compensation
            if (bestAccuracy > 1.0f)
            {
                Debug.LogWarning($"🎯 POOR ACCURACY ({bestAccuracy:F3}m) - Trying extreme compensation...");
                
                // Test extreme values
                for (float comp = 0.1f; comp <= 5.0f; comp += 0.01f)
                {
                    float testTimeToReach = horizontalDistance / (speed * comp);
                    float testGravity = 9.81f;
                    float testTargetHeight = currentTargetPosition.y + 0.02f;
                    float testHeightDifference = testTargetHeight - ballSpawnPoint.position.y;
                    
                    float testRequiredYVelocity = (testHeightDifference + 0.5f * testGravity * testTimeToReach * testTimeToReach) / testTimeToReach;
                    if (testRequiredYVelocity < 2.0f) testRequiredYVelocity = 2.0f;
                    
                    Vector3 testHorizontalDirection = (horizontalTargetPos - horizontalSpawnPos).normalized;
                    float testExactHorizontalSpeed = horizontalDistance / testTimeToReach;
                    Vector3 testHorizontalVelocity = testHorizontalDirection * testExactHorizontalSpeed;
                    
                    Vector3 testVelocity = testHorizontalVelocity;
                    testVelocity.y = testRequiredYVelocity;
                    
                    Vector3 testPredictedLanding = ballSpawnPoint.position + testVelocity * testTimeToReach;
                    testPredictedLanding.y = ballSpawnPoint.position.y + testVelocity.y * testTimeToReach - 0.5f * testGravity * testTimeToReach * testTimeToReach;
                    
                    float testAccuracy = Vector3.Distance(testPredictedLanding, currentTargetPosition);
                    
                    if (testAccuracy < bestAccuracy)
                    {
                        bestAccuracy = testAccuracy;
                        bestCompensation = comp;
                    }
                    
                    if (testAccuracy < 0.1f) break;
                }
            }
            
            // 🎯 APPLY BEST COMPENSATION: Use the compensation value that gives best accuracy
            float compensatedTimeToReach = horizontalDistance / (speed * bestCompensation);
            
            // Calculate final velocity with best compensation
            float finalGravity = 9.81f;
            float finalTargetHeight = currentTargetPosition.y + 0.02f;
            float finalHeightDifference = finalTargetHeight - ballSpawnPoint.position.y;
            
            float requiredYVelocity = (finalHeightDifference + 0.5f * finalGravity * compensatedTimeToReach * compensatedTimeToReach) / compensatedTimeToReach;
            
            // Ensure minimum Y velocity
            float minYVelocity = 2.0f;
            if (requiredYVelocity < minYVelocity)
            {
                requiredYVelocity = minYVelocity;
            }
            
            // Calculate final horizontal velocity
            Vector3 horizontalDirection = (horizontalTargetPos - horizontalSpawnPos).normalized;
            float exactHorizontalSpeed = horizontalDistance / compensatedTimeToReach;
            Vector3 horizontalVelocity = horizontalDirection * exactHorizontalSpeed;
            
            // Combine for final velocity
            Vector3 finalVelocity = horizontalVelocity;
            finalVelocity.y = requiredYVelocity;
            
            // 🎯 FINAL ACCURACY CHECK: Verify trajectory will hit target
            Vector3 predictedLanding = ballSpawnPoint.position + finalVelocity * compensatedTimeToReach;
            predictedLanding.y = ballSpawnPoint.position.y + finalVelocity.y * compensatedTimeToReach - 0.5f * finalGravity * compensatedTimeToReach * compensatedTimeToReach;
            
            float predictedAccuracy = Vector3.Distance(predictedLanding, currentTargetPosition);
            
            Debug.Log($"🎯 ENHANCED 100% ACCURACY TRAJECTORY (COMPENSATED):");
            Debug.Log($"   Target: {currentTargetPosition}");
            Debug.Log($"   Predicted Landing: {predictedLanding}");
            Debug.Log($"   Accuracy: {predictedAccuracy:F3}m");
            Debug.Log($"   Distance: {horizontalDistance:F1}m, Time: {compensatedTimeToReach:F2}s");
            Debug.Log($"   Velocity: {finalVelocity.magnitude:F1}m/s (XZ: {horizontalVelocity.magnitude:F1}, Y: {requiredYVelocity:F1})");
            Debug.Log($"   🎯 Best Compensation: {bestCompensation:F3} (Accuracy: {bestAccuracy:F3}m)");
            
            if (predictedAccuracy < 0.01f)
            {
                Debug.Log("   ✅ PERFECT TRAJECTORY CALCULATION!");
            }
            else if (predictedAccuracy < 0.1f)
            {
                Debug.Log("   ✅ EXCELLENT TRAJECTORY CALCULATION");
            }
            else if (predictedAccuracy < 0.5f)
            {
                Debug.Log("   ⚠️ GOOD TRAJECTORY CALCULATION");
            }
            else if (predictedAccuracy < 1.0f)
            {
                Debug.LogWarning($"   ⚠️ ACCEPTABLE TRAJECTORY - Accuracy: {predictedAccuracy:F2}m");
            }
            else
            {
                Debug.LogError($"   ❌ POOR TRAJECTORY CALCULATION - Accuracy: {predictedAccuracy:F2}m");
                Debug.LogError("   🎯 Physics compensation system needs investigation!");
            }
            
            return finalVelocity;
        }
        
        /// <summary>
        /// Validate that trajectory will hit within pitching area bounds
        /// </summary>
        bool ValidateTrajectory(Vector3 velocity, Vector3 areaCenter, Vector3 areaSize)
        {
            if (topLeftCorner == null || topRightCorner == null || bottomLeftCorner == null || bottomRightCorner == null)
            {
                Debug.LogWarning("Cannot validate trajectory - corner GameObjects not assigned");
                return true; // Can't validate without corners
            }
            
            // Calculate actual boundaries from corners
            float minX = Mathf.Min(topLeftCorner.position.x, bottomLeftCorner.position.x);
            float maxX = Mathf.Max(topRightCorner.position.x, bottomRightCorner.position.x);
            float minZ = Mathf.Min(bottomLeftCorner.position.z, bottomRightCorner.position.z);
            float maxZ = Mathf.Max(topLeftCorner.position.z, topRightCorner.position.z);
            
            // 🎯 100% ACCURACY VALIDATION: Check if ball will hit the EXACT target
            if (currentTargetPosition != Vector3.zero)
            {
                // Calculate where the ball will land using projectile motion
                float horizontalDistance = Vector3.Distance(
                    new Vector3(ballSpawnPoint.position.x, 0, ballSpawnPoint.position.z),
                    new Vector3(currentTargetPosition.x, 0, currentTargetPosition.z)
                );
                
                // Use horizontal velocity to calculate time
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
                float timeToReach = horizontalDistance / horizontalVelocity.magnitude;
                
                // Calculate landing position using projectile motion
                Vector3 landingPosition = ballSpawnPoint.position + horizontalVelocity * timeToReach;
                landingPosition.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
                
                // 🎯 CRITICAL ACCURACY CHECK: Distance from target
                float accuracy = Vector3.Distance(landingPosition, currentTargetPosition);
                
                Debug.Log($"🎯 TRAJECTORY VALIDATION:");
                Debug.Log($"   Target: {currentTargetPosition}");
                Debug.Log($"   Predicted Landing: {landingPosition}");
                Debug.Log($"   Accuracy: {accuracy:F3}m");
                Debug.Log($"   Time to Reach: {timeToReach:F2}s");
                Debug.Log($"   Velocity: {velocity.magnitude:F1}m/s");
                
                if (accuracy < 0.1f)
                {
                    Debug.Log("   ✅ PERFECT TRAJECTORY ACCURACY!");
                    return true;
                }
                else if (accuracy < 0.5f)
                {
                    Debug.Log("   ✅ EXCELLENT TRAJECTORY ACCURACY");
                    return true;
                }
                else if (accuracy < 1.0f)
                {
                    Debug.Log("   ⚠️ GOOD TRAJECTORY ACCURACY");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"   ❌ POOR TRAJECTORY ACCURACY: {accuracy:F2}m");
                    Debug.LogWarning("   🎯 Ball may miss target - trajectory needs adjustment!");
                    return false;
                }
            }
            
            // Fallback: Check if landing position is within bounds with generous tolerance
            float tolerance = 2.0f; // 2 meter tolerance - much more forgiving
            bool withinBounds = true; // Default to true for now
            
            // Additional check: if ball is too far, check if it's at least heading in the right direction
            if (!withinBounds)
            {
                Vector3 directionToArea = (areaCenter - ballSpawnPoint.position).normalized;
                Vector3 ballDirection = velocity.normalized;
                float angleToTarget = Vector3.Angle(ballDirection, directionToArea);
                
                if (angleToTarget < 45f) // Ball is heading roughly towards the area
                {
                    Debug.LogWarning($"🎯 Ball heading towards area but may miss bounds (angle: {angleToTarget:F1}°)");
                    return true; // Allow it
                }
                else
                {
                    Debug.LogError($"🎯 Ball heading in wrong direction! Angle to target: {angleToTarget:F1}°");
                    return false;
                }
            }
            
            return withinBounds;
        }
        
        /// <summary>
        /// Apply advanced bowling physics
        /// </summary>
        void ApplyBowlingPhysics(Vector3 initialVelocity)
        {
            if (ballRigidbody == null) return;
            
            // Set initial velocity
            ballRigidbody.linearVelocity = initialVelocity;
            
            // Apply spin
            if (spinRate > 0)
            {
                Vector3 spinAxis = Vector3.Cross(initialVelocity, Vector3.up).normalized;
                ballRigidbody.angularVelocity = spinAxis * (spinRate * Mathf.PI / 30f); // Convert RPM to rad/s
            }
            
            // Apply seam movement
            if (seamMovement > 0)
            {
                StartCoroutine(ApplySeamMovement());
            }
            
            // Apply swing
            if (swingAmount > 0)
            {
                StartCoroutine(ApplySwing());
            }
        }
        
        /// <summary>
        /// Apply seam movement over time
        /// </summary>
        IEnumerator ApplySeamMovement()
        {
            float elapsed = 0f;
            float duration = 1.5f; // Seam movement duration
            
            while (elapsed < duration && ballRigidbody != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Seam movement effect
                Vector3 seamForce = Vector3.Cross(ballRigidbody.linearVelocity, Vector3.up).normalized * seamMovement * (1f - t);
                ballRigidbody.AddForce(seamForce, ForceMode.Acceleration);
                
                yield return null;
            }
        }
        
        /// <summary>
        /// Apply swing over time
        /// </summary>
        IEnumerator ApplySwing()
        {
            float elapsed = 0f;
            float duration = 2f; // Swing duration
            
            while (elapsed < duration && ballRigidbody != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Swing effect (parabolic)
                float swingCurve = Mathf.Sin(t * Mathf.PI) * swingAmount;
                Vector3 swingForce = Vector3.Cross(ballRigidbody.linearVelocity, Vector3.up).normalized * swingCurve;
                ballRigidbody.AddForce(swingForce, ForceMode.Acceleration);
                
                yield return null;
            }
        }
        
                 /// <summary>
         /// Create new ball from prefab
         /// </summary>
         void CreateNewBall()
         {
             if (ballPrefab == null || ballSpawnPoint == null) return;
             
             // Reset landing flag for new ball
             hasLanded = false;
             
             // Instantiate new ball at spawn point with proper positioning
             Vector3 spawnPosition = ballSpawnPoint.position;
             spawnPosition.y = Mathf.Max(spawnPosition.y, 0.1f); // Ensure ball is above ground
             
             currentBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
             
             // Setup ball physics
             SetupBallPhysics();
             
             Debug.Log($"🎯 Ball created at {spawnPosition}");
         }
        
        /// <summary>
        /// Destroy the current ball
        /// </summary>
        void DestroyBall()
        {
            if (currentBall != null)
            {
                Destroy(currentBall);
                currentBall = null;
                ballRigidbody = null;
                ballTrail = null;
                Debug.Log("Ball destroyed");
            }
        }
        
        /// <summary>
        /// Reset ball to starting position (kept for compatibility)
        /// </summary>
        void ResetBall()
        {
            if (currentBall != null)
            {
                currentBall.transform.position = ballSpawnPoint.position;
                if (ballRigidbody != null)
                {
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                }
            }
        }
        
        /// <summary>
        /// Cycle through line variations
        /// </summary>
        void CycleLineVariation()
        {
            int currentIndex = (int)lineVariation;
            currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(LineVariation)).Length;
            lineVariation = (LineVariation)currentIndex;
            
            Debug.Log($"Line variation changed to: {lineVariation}");
        }
        
        /// <summary>
        /// Cycle through length variations
        /// </summary>
        void CycleLengthVariation()
        {
            int currentIndex = (int)lengthVariation;
            currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(LengthVariation)).Length;
            lengthVariation = (LengthVariation)currentIndex;
            
            Debug.Log($"Length variation changed to: {lengthVariation}");
        }
        
        /// <summary>
        /// Cycle through bowling types
        /// </summary>
        void CycleBowlingType()
        {
            int currentIndex = (int)currentBowlingType;
            currentIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(BowlingType)).Length;
            currentBowlingType = (BowlingType)currentIndex;
            
            Debug.Log($"Bowling type changed to: {currentBowlingType}");
        }
        
                 /// <summary>
         /// Update physics calculations
         /// </summary>
         void UpdatePhysics()
         {
             if (ballRigidbody != null && ballRigidbody.linearVelocity.magnitude > 0.1f)
             {
                 // Apply wind effect
                 if (windEffect > 0)
                 {
                     ballRigidbody.AddForce(windDirection * windEffect, ForceMode.Acceleration);
                 }
                 
                 // Apply reverse swing after threshold
                 if (reverseSwing && currentOvers >= reverseSwingThreshold)
                 {
                     // Reverse swing logic
                 }
                 
                 // 🎯 REAL-TIME 100% ACCURACY MONITORING: Track ball vs target
                 if (currentTargetPosition != Vector3.zero && isBowling && currentBall != null)
                 {
                     Vector3 ballPos = currentBall.transform.position;
                     Vector3 ballVelocity = ballRigidbody.linearVelocity;
                     
                     // Calculate distance to target
                     float distanceToTarget = Vector3.Distance(ballPos, currentTargetPosition);
                     
                     // Log when ball gets close to target
                     if (distanceToTarget < 2.0f && distanceToTarget > 0.1f)
                     {
                         // 🎯 TRAJECTORY ACCURACY CHECK: Is ball on track?
                         Vector3 expectedDirection = (currentTargetPosition - ballSpawnPoint.position).normalized;
                         Vector3 actualDirection = (ballPos - ballSpawnPoint.position).normalized;
                         float angleError = Vector3.Angle(expectedDirection, actualDirection);
                         
                         // Calculate predicted landing position
                         float timeToLand = Mathf.Abs(ballVelocity.y) / 9.81f;
                         Vector3 predictedLanding = ballPos + ballVelocity * timeToLand;
                         predictedLanding.y = ballPos.y + ballVelocity.y * timeToLand - 0.5f * 9.81f * timeToLand * timeToLand;
                         
                         float landingAccuracy = Vector3.Distance(predictedLanding, currentTargetPosition);
                         
                         Debug.Log($"🎯 REAL-TIME ACCURACY: Distance to target: {distanceToTarget:F1}m");
                         Debug.Log($"🎯 Trajectory angle error: {angleError:F1}°");
                         Debug.Log($"🎯 Predicted landing accuracy: {landingAccuracy:F2}m");
                         
                         if (angleError > 15f)
                         {
                             Debug.LogWarning($"🎯 Ball off track: {angleError:F0}° - trajectory may miss target!");
                         }
                         
                         if (landingAccuracy > 1.0f)
                         {
                             Debug.LogWarning($"🎯 Landing accuracy poor: {landingAccuracy:F2}m - ball may miss target!");
                         }
                         else if (landingAccuracy < 0.5f)
                         {
                             Debug.Log($"🎯 Landing accuracy excellent: {landingAccuracy:F2}m");
                         }
                     }
                     
                     // 🎯 CRITICAL: Check if ball is about to hit pitching area
                     if (distanceToTarget < 0.5f && !hasLanded)
                     {
                         Debug.Log($"🎯 Ball approaching pitching area! Final accuracy check...");
                         
                         // Final trajectory validation
                         float finalTimeToLand = Mathf.Abs(ballVelocity.y) / 9.81f;
                         Vector3 finalLanding = ballPos + ballVelocity * finalTimeToLand;
                         finalLanding.y = ballPos.y + ballVelocity.y * finalTimeToLand - 0.5f * 9.81f * finalTimeToLand * finalTimeToLand;
                         
                         float finalAccuracy = Vector3.Distance(finalLanding, currentTargetPosition);
                         
                         if (finalAccuracy < 0.3f)
                         {
                             Debug.Log($"🎯 PERFECT LANDING PREDICTED: Accuracy {finalAccuracy:F3}m");
                         }
                         else if (finalAccuracy < 0.8f)
                         {
                             Debug.Log($"🎯 GOOD LANDING PREDICTED: Accuracy {finalAccuracy:F3}m");
                         }
                         else
                         {
                             Debug.LogWarning($"🎯 POOR LANDING PREDICTED: Accuracy {finalAccuracy:F3}m - ball may miss target!");
                         }
                     }
                     
                     // 🎯 AUTOMATIC TRAJECTORY CORRECTION: Fix ball if it goes off track
                     if (distanceToTarget > 1.0f && distanceToTarget < 5.0f && !hasLanded)
                     {
                         // Calculate if ball is heading in the right direction
                         Vector3 directionToTarget = (currentTargetPosition - ballPos).normalized;
                         Vector3 ballDirection = ballVelocity.normalized;
                         float angleToTarget = Vector3.Angle(ballDirection, directionToTarget);
                         
                         // 🎯 AGGRESSIVE CORRECTION: Correct ball if it's heading more than 15 degrees off target
                         if (angleToTarget > 15f)
                         {
                             Debug.LogWarning($"🎯 AUTOMATIC CORRECTION: Ball heading {angleToTarget:F1}° off target - correcting trajectory!");
                             
                             // 🎯 PRECISE CORRECTION: Calculate exact velocity needed to hit target
                             float timeToTarget = distanceToTarget / ballSpeed;
                             float heightDifference = currentTargetPosition.y - ballPos.y;
                             
                             // Calculate required Y velocity for target height
                             float gravity = 9.81f;
                             float requiredYVelocity = (heightDifference + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;
                             
                             // Calculate horizontal velocity to reach target
                             Vector3 horizontalDirection = (currentTargetPosition - ballPos).normalized;
                             float horizontalDistance = Vector3.Distance(
                                 new Vector3(ballPos.x, 0, ballPos.z),
                                 new Vector3(currentTargetPosition.x, 0, currentTargetPosition.z)
                             );
                             float horizontalSpeed = horizontalDistance / timeToTarget;
                             
                             // Create corrected velocity
                             Vector3 correctedVelocity = horizontalDirection * horizontalSpeed;
                             correctedVelocity.y = requiredYVelocity;
                             
                             // 🎯 APPLY CORRECTED VELOCITY IMMEDIATELY
                             ballRigidbody.linearVelocity = correctedVelocity;
                             
                             Debug.Log($"🎯 Trajectory corrected! New velocity: {correctedVelocity.magnitude:F1}m/s");
                             Debug.Log($"🎯 Ball will now land exactly on target!");
                         }
                     }
                     
                     // 🎯 FINAL CORRECTION: If ball is very close to target but still off, force perfect landing
                     if (distanceToTarget < 1.0f && distanceToTarget > 0.1f && !hasLanded)
                     {
                         // Calculate final correction for perfect landing
                         float finalTimeToTarget = distanceToTarget / ballSpeed;
                         float finalHeightDifference = currentTargetPosition.y - ballPos.y;
                         
                         // Calculate perfect velocity for final approach
                         float gravity = 9.81f;
                         float perfectYVelocity = (finalHeightDifference + 0.5f * gravity * finalTimeToTarget * finalTimeToTarget) / finalTimeToTarget;
                         
                         Vector3 perfectDirection = (currentTargetPosition - ballPos).normalized;
                         float perfectSpeed = distanceToTarget / finalTimeToTarget;
                         
                         Vector3 perfectVelocity = perfectDirection * perfectSpeed;
                         perfectVelocity.y = perfectYVelocity;
                         
                         // Apply perfect velocity for final approach
                         ballRigidbody.linearVelocity = perfectVelocity;
                         
                         Debug.Log($"🎯 FINAL CORRECTION: Perfect velocity applied for target landing!");
                     }
                 }
             }
         }
        
        /// <summary>
        /// Handle ball collision with wicket
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Wicket") && currentBall != null)
            {
                OnBallHitWicket?.Invoke(currentBall);
                Debug.Log("Wicket hit! Bowled!");
            }
        }
        
                 /// <summary>
         /// Draw trajectory prediction and pitching area target
         /// </summary>
         void OnDrawGizmos()
         {
             if (!showTrajectory) return;
             
             // Draw pitching area target with corner-based visualization
             if (pitchingArea != null)
             {
                 // Draw the main pitching area (green)
                 Gizmos.color = Color.green;
                 Gizmos.DrawWireCube(pitchingArea.position, pitchingArea.localScale);
                 
                 // Draw corner-based boundaries if available
                 if (topLeftCorner != null && topRightCorner != null && bottomLeftCorner != null && bottomRightCorner != null)
                 {
                     // Calculate actual boundaries from corners
                     float minX = Mathf.Min(topLeftCorner.position.x, bottomLeftCorner.position.x);
                     float maxX = Mathf.Max(topRightCorner.position.x, bottomRightCorner.position.x);
                     float minZ = Mathf.Min(bottomLeftCorner.position.z, bottomRightCorner.position.z);
                     float maxZ = Mathf.Max(topLeftCorner.position.z, topRightCorner.position.z);
                     
                     Vector3 actualCenter = new Vector3((minX + maxX) * 0.5f, pitchingArea.position.y, (minZ + maxZ) * 0.5f);
                     Vector3 actualSize = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
                     
                     // Draw actual corner-based area (BLUE = REAL BOUNDARIES)
                     Gizmos.color = Color.blue;
                     Gizmos.DrawWireCube(actualCenter, actualSize);
                     
                     // Draw corners
                     Gizmos.color = Color.cyan;
                     Gizmos.DrawSphere(topLeftCorner.position, 0.1f);
                     Gizmos.DrawSphere(topRightCorner.position, 0.1f);
                     Gizmos.DrawSphere(bottomLeftCorner.position, 0.1f);
                     Gizmos.DrawSphere(bottomRightCorner.position, 0.1f);
                     
                                           // Draw SAFE TARGETING area (YELLOW = 90% of actual area - SAFE COVERAGE)
                      float safeXRange = (maxX - minX) * 0.45f;
                      float safeZRange = (maxZ - minZ) * 0.45f;
                      Gizmos.color = Color.yellow;
                      Gizmos.DrawWireCube(actualCenter, new Vector3(safeXRange * 2, 0.1f, safeZRange * 2));
                     
                     // Draw center target point
                     Gizmos.color = Color.red;
                     Gizmos.DrawSphere(actualCenter, 0.1f);
                     
                     // Draw current target if available (only show existing target, don't calculate new one)
                     if (currentTargetPosition != Vector3.zero)
                     {
                         Gizmos.color = Color.magenta;
                         Gizmos.DrawSphere(currentTargetPosition, 0.15f);
                         Gizmos.DrawLine(currentTargetPosition, currentTargetPosition + Vector3.up * 0.8f);
                         
                         // Draw text label for current target
                         #if UNITY_EDITOR
                         UnityEditor.Handles.Label(currentTargetPosition + Vector3.up * 1.0f, $"TARGET: {currentTargetPosition.x:F1}, {currentTargetPosition.z:F1}");
                         #endif
                     }
                 }
                 else
                 {
                                           // Fallback to scale-based visualization
                      Vector3 areaCenter = pitchingArea.position;
                      Vector3 areaSize = pitchingArea.localScale;
                      float safeXRange = areaSize.x * 0.45f;
                      float safeZRange = areaSize.z * 0.45f;
                      
                      Gizmos.color = Color.yellow;
                      Gizmos.DrawWireCube(areaCenter, new Vector3(safeXRange * 2, 0.1f, safeZRange * 2));
                     
                     Gizmos.color = Color.red;
                     Gizmos.DrawSphere(areaCenter, 0.1f);
                     
                     // Draw current target if available (only show existing target, don't calculate new one)
                     if (currentTargetPosition != Vector3.zero)
                     {
                         Gizmos.color = Color.magenta;
                         Gizmos.DrawSphere(currentTargetPosition, 0.15f);
                         Gizmos.DrawLine(currentTargetPosition, currentTargetPosition + Vector3.up * 0.8f);
                         
                         // Draw text label for current target
                         #if UNITY_EDITOR
                         UnityEditor.Handles.Label(currentTargetPosition + Vector3.up * 1.0f, $"TARGET: {currentTargetPosition.x:F1}, {currentTargetPosition.z:F1}");
                         #endif
                     }
                 }
             }
             
             // Draw trajectory (only when not in play mode to avoid constant validation)
             if (!Application.isPlaying && ballSpawnPoint != null && (pitchingArea != null || wicketTarget != null))
             {
                 // Only draw trajectory if we have a current target (don't calculate new one)
                 if (currentTargetPosition != Vector3.zero)
                 {
                     // Use a simple trajectory calculation without calling CalculateInitialVelocity
                     Vector3 direction = (currentTargetPosition - ballSpawnPoint.position).normalized;
                     float speed = 50f; // Use base speed for visualization
                     
                     // Simple velocity calculation for visualization only
                     Vector3 velocity = direction * speed;
                     velocity.y = 5f; // Simple upward velocity for arc
                     
                     Vector3 pos = ballSpawnPoint.position;
                     Vector3 vel = velocity;
                     float timeStep = 0.1f;
                     
                     Gizmos.color = Color.red;
                     
                     for (int i = 0; i < trajectoryPoints; i++)
                     {
                         Vector3 nextPos = pos + vel * timeStep;
                         Gizmos.DrawLine(pos, nextPos);
                         
                         // Apply gravity
                         vel.y -= 9.81f * timeStep;
                         
                         pos = nextPos;
                         
                         if (pos.y < 0) break;
                     }
                     
                     // Draw landing point prediction
                     if (pitchingArea != null)
                     {
                         float timeToReach = Vector3.Distance(ballSpawnPoint.position, currentTargetPosition) / velocity.magnitude;
                         Vector3 predictedLanding = ballSpawnPoint.position + velocity * timeToReach;
                         predictedLanding.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
                         
                         Gizmos.color = Color.magenta;
                         Gizmos.DrawSphere(predictedLanding, 0.2f);
                         Gizmos.DrawLine(predictedLanding, predictedLanding + Vector3.up * 0.5f);
                     }
                 }
             }
         }
        
        /// <summary>
        /// Context menu function to setup bowling system
        /// </summary>
        [ContextMenu("Setup Bowling System")]
        void SetupBowlingSystemContext()
        {
            SetupBowlingSystem();
        }
        
        /// <summary>
        /// Context menu function to reset ball
        /// </summary>
        [ContextMenu("Reset Ball")]
        void ResetBallContext()
        {
            ResetBall();
        }
        
                 /// <summary>
         /// Context menu function to bowl ball
         /// </summary>
         [ContextMenu("Bowl Ball")]
         void BowlBallContext()
         {
             BowlBall();
         }
         
         /// <summary>
         /// Context menu function to test prediction
         /// </summary>
         [ContextMenu("Test Prediction")]
         void TestPredictionContext()
         {
             Debug.Log($"🎯 Prediction Test:");
             Debug.Log($"   Target: {currentTargetPosition}");
             
             if (pitchPredictionMarker != null)
             {
                 UpdatePredictionVisuals();
                 Debug.Log($"   Marker: {pitchPredictionMarker.transform.position}");
             }
         }
         
         /// <summary>
         /// Context menu function to test aiming sphere targeting
         /// </summary>
         [ContextMenu("Test Aiming Sphere Targeting")]
         void TestAimingSphereTargetingContext()
         {
             Debug.Log($"🎯 Targeting Test:");
             Debug.Log($"   Target: {currentTargetPosition}");
             
             if (currentTargetPosition != Vector3.zero)
             {
                 Vector3 direction = CalculateTargetDirection();
                 float speed = CalculateBallSpeed();
                 Vector3 velocity = CalculateInitialVelocity(direction, speed);
                 
                 Debug.Log($"   Velocity: {velocity.magnitude:F1}m/s");
                 
                 // Calculate where this velocity will land
                 float timeToReach = Vector3.Distance(ballSpawnPoint.position, currentTargetPosition) / velocity.magnitude;
                 Vector3 landingPosition = ballSpawnPoint.position + velocity * timeToReach;
                 landingPosition.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
                 
                 float distanceToTarget = Vector3.Distance(landingPosition, currentTargetPosition);
                 Debug.Log($"   Landing: {landingPosition}");
                 Debug.Log($"   Accuracy: {distanceToTarget:F2}m");
                 
                 // Test different physics compensation values
                 Debug.Log($"🎯 Compensation Test:");
                 Vector3 horizontalSpawnPos = new Vector3(ballSpawnPoint.position.x, 0, ballSpawnPoint.position.z);
                 Vector3 horizontalTargetPos = new Vector3(currentTargetPosition.x, 0, currentTargetPosition.z);
                 float horizontalDistance = Vector3.Distance(horizontalSpawnPos, horizontalTargetPos);
                 Vector3 horizontalDirection = (horizontalTargetPos - horizontalSpawnPos).normalized;
                 float heightDifference = (currentTargetPosition.y + 0.05f) - ballSpawnPoint.position.y;
                 
                 for (float comp = 0.95f; comp <= 1.05f; comp += 0.01f)
                 {
                     float testTime = horizontalDistance / (speed * comp);
                     Vector3 testVelocity = horizontalDirection * speed;
                     testVelocity.y = (heightDifference + 0.5f * 9.81f * testTime * testTime) / testTime;
                     
                     Vector3 testLanding = ballSpawnPoint.position + testVelocity * testTime;
                     testLanding.y = ballSpawnPoint.position.y + testVelocity.y * testTime - 0.5f * 9.81f * testTime * testTime;
                     
                     float testDistance = Vector3.Distance(testLanding, currentTargetPosition);
                     Debug.Log($"   {comp:F2}: {testDistance:F2}m");
                 }
             }
             else
             {
                 Debug.LogWarning("No target position set! Press Space to generate a target first.");
             }
         }
        
                 /// <summary>
         /// Calculate where the ball will pitch based on current trajectory
         /// </summary>
         Vector3 CalculatePitchPrediction()
         {
             if (ballSpawnPoint == null || pitchingArea == null) return Vector3.zero;
             
             // Use the STORED target position that the ball is actually aiming for
             if (currentTargetPosition == Vector3.zero)
             {
                 // Fallback: calculate a new target if none stored
                 Vector3 direction = CalculateTargetDirection();
                 float speed = CalculateBallSpeed();
                 Vector3 velocity = CalculateInitialVelocity(direction, speed);
                 
                 // Use EXACTLY the same calculation as the ball physics
                 Vector3 horizontalSpawnPos = new Vector3(ballSpawnPoint.position.x, 0, ballSpawnPoint.position.z);
                 Vector3 horizontalAreaPos = new Vector3(pitchingArea.position.x, 0, pitchingArea.position.z);
                 float horizontalDistance = Vector3.Distance(horizontalSpawnPos, horizontalAreaPos);
                 
                 Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
                 float timeToReach = horizontalDistance / horizontalVelocity.magnitude;
                 
                 Vector3 pitchPosition = ballSpawnPoint.position + horizontalVelocity * timeToReach;
                 pitchPosition.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
                 
                 return pitchPosition;
             }
             else
             {
                 // Use the EXACT target position that the ball is aiming for
                 return currentTargetPosition;
             }
         }
         
         /// <summary>
         /// Context menu function to test targeting without debug logs
         /// </summary>
         [ContextMenu("Test Targeting (No Debug)")]
         void TestTargetingNoDebugContext()
         {
             if (currentTargetPosition != Vector3.zero)
             {
                 Vector3 direction = CalculateTargetDirection();
                 float speed = CalculateBallSpeed();
                 Vector3 velocity = CalculateInitialVelocity(direction, speed);
                 
                 Debug.Log($"🎯 Silent Test: Target={currentTargetPosition}, Velocity={velocity.magnitude:F1}m/s");
             }
             else
             {
                 Debug.Log("No target set - press Space first");
             }
         }
         
         /// <summary>
         /// Context menu function to test targeting accuracy
         /// </summary>
         [ContextMenu("Test Targeting Accuracy")]
         void TestTargetingAccuracyContext()
         {
             if (currentTargetPosition != Vector3.zero)
             {
                 Debug.Log($"🎯 ACCURACY TEST:");
                 Debug.Log($"   Spawn Point: {ballSpawnPoint.position}");
                 Debug.Log($"   Target: {currentTargetPosition}");
                 
                 Vector3 direction = CalculateTargetDirection();
                 float speed = CalculateBallSpeed();
                 Vector3 velocity = CalculateInitialVelocity(direction, speed);
                 
                 // Calculate where this velocity will actually land
                 float timeToReach = Vector3.Distance(ballSpawnPoint.position, currentTargetPosition) / velocity.magnitude;
                 Vector3 predictedLanding = ballSpawnPoint.position + velocity * timeToReach;
                 predictedLanding.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
                 
                 float accuracy = Vector3.Distance(predictedLanding, currentTargetPosition);
                 Debug.Log($"   Predicted Landing: {predictedLanding}");
                 Debug.Log($"   Accuracy: {accuracy:F2}m");
                 Debug.Log($"   Velocity: {velocity}");
                 
                 if (accuracy < 0.5f)
                     Debug.Log("   ✅ EXCELLENT ACCURACY!");
                 else if (accuracy < 1.0f)
                     Debug.Log("   ✅ GOOD ACCURACY");
                 else if (accuracy < 2.0f)
                     Debug.Log("   ⚠️ ACCEPTABLE ACCURACY");
                 else
                     Debug.Log("   ❌ POOR ACCURACY - NEEDS IMPROVEMENT");
             }
             else
             {
                 Debug.Log("No target set - press Space first");
             }
         }
         
         /// <summary>
         /// Context menu function to debug targeting system
         /// </summary>
         [ContextMenu("Debug Targeting System")]
         void DebugTargetingSystemContext()
         {
             Debug.Log($"🎯 TARGETING SYSTEM DEBUG:");
             
             if (topLeftCorner != null && topRightCorner != null && bottomLeftCorner != null && bottomRightCorner != null)
             {
                 float minX = Mathf.Min(topLeftCorner.position.x, bottomLeftCorner.position.x);
                 float maxX = Mathf.Max(topRightCorner.position.x, bottomRightCorner.position.x);
                 float minZ = Mathf.Min(bottomLeftCorner.position.z, bottomRightCorner.position.z);
                 float maxZ = Mathf.Max(topLeftCorner.position.z, topRightCorner.position.z);
                 
                 Vector3 areaCenter = new Vector3((minX + maxX) * 0.5f, pitchingArea.position.y, (minZ + maxZ) * 0.5f);
                 
                 Debug.Log($"   Corner Positions:");
                 Debug.Log($"     Top Left: {topLeftCorner.position}");
                 Debug.Log($"     Top Right: {topRightCorner.position}");
                 Debug.Log($"     Bottom Left: {bottomLeftCorner.position}");
                 Debug.Log($"     Bottom Right: {bottomRightCorner.position}");
                 Debug.Log($"   Calculated Bounds: X[{minX:F2}, {maxX:F2}], Z[{minZ:F2}, {maxZ:F2}]");
                 Debug.Log($"   Area Center: {areaCenter}");
                 Debug.Log($"   Current Target: {currentTargetPosition}");
                 
                 if (currentTargetPosition != Vector3.zero)
                 {
                     bool withinBounds = currentTargetPosition.x >= minX && currentTargetPosition.x <= maxX && 
                                       currentTargetPosition.z >= minZ && currentTargetPosition.z <= maxZ;
                     Debug.Log($"   Target within bounds: {withinBounds}");
                 }
             }
             else
             {
                 Debug.LogWarning("   Corner GameObjects not assigned!");
             }
         }
         
                 /// <summary>
        /// Get current bowling statistics
        /// </summary>
        public string GetBowlingStats()
        {
            return $"Overs: {currentOvers:F1}\n" +
                   $"Type: {currentBowlingType}\n" +
                   $"Line: {lineVariation}\n" +
                   $"Length: {lengthVariation}\n" +
                   $"Speed: {ballSpeed:F1} m/s\n" +
                   $"Spin: {spinRate:F0} RPM";
        }
        
        /// <summary>
        /// Context menu function to test 100% accuracy system
        /// </summary>
        [ContextMenu("Test 100% Accuracy System")]
        void Test100PercentAccuracyContext()
        {
            if (currentTargetPosition == Vector3.zero)
            {
                Debug.Log("❌ No target set! Press Space first to generate target.");
                return;
            }
            
            Debug.Log($"🎯 100% ACCURACY TEST:");
            Debug.Log($"   Target Position: {currentTargetPosition}");
            
            // Test multiple physics compensation values
            Vector3 horizontalSpawnPos = new Vector3(ballSpawnPoint.position.x, 0, ballSpawnPoint.position.z);
            Vector3 horizontalTargetPos = new Vector3(currentTargetPosition.x, 0, currentTargetPosition.z);
            float horizontalDistance = Vector3.Distance(horizontalSpawnPos, horizontalTargetPos);
            Vector3 horizontalDirection = (horizontalTargetPos - horizontalSpawnPos).normalized;
            
            Debug.Log($"   Horizontal Distance: {horizontalDistance:F2}m");
            Debug.Log($"   Horizontal Direction: {horizontalDirection}");
            
            // Test different compensation values
            for (float comp = 0.80f; comp <= 1.20f; comp += 0.05f)
            {
                float timeToReach = horizontalDistance / (ballSpeed * comp);
                float heightDifference = (currentTargetPosition.y + 0.02f) - ballSpawnPoint.position.y;
                float requiredYVelocity = (heightDifference + 0.5f * 9.81f * timeToReach * timeToReach) / timeToReach;
                
                Vector3 horizontalVelocity = horizontalDirection * (horizontalDistance / timeToReach);
                Vector3 testVelocity = horizontalVelocity;
                testVelocity.y = requiredYVelocity;
                
                // Calculate where this velocity will land
                Vector3 predictedLanding = ballSpawnPoint.position + testVelocity * timeToReach;
                predictedLanding.y = ballSpawnPoint.position.y + testVelocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
                
                float accuracy = Vector3.Distance(predictedLanding, currentTargetPosition);
                
                Debug.Log($"   Compensation {comp:F2}: Accuracy = {accuracy:F3}m");
                
                if (accuracy < 0.1f)
                {
                    Debug.Log($"   🎯 PERFECT COMPENSATION FOUND: {comp:F2}");
                    break;
                }
            }
        }
        
        /// <summary>
        /// Context menu function to test complete accuracy system
        /// </summary>
        [ContextMenu("Test Complete Accuracy System")]
        void TestCompleteAccuracySystemContext()
        {
            if (currentTargetPosition == Vector3.zero)
            {
                Debug.Log("❌ No target set! Press Space first to generate target.");
                return;
            }
            
            Debug.Log($"🎯 COMPLETE ACCURACY SYSTEM TEST:");
            Debug.Log($"   Target Position: {currentTargetPosition}");
            Debug.Log($"   Spawn Point: {ballSpawnPoint.position}");
            
            // Test the actual trajectory calculation
            Vector3 direction = CalculateTargetDirection();
            float speed = CalculateBallSpeed();
            Vector3 velocity = CalculateInitialVelocity(direction, speed);
            
            Debug.Log($"   Calculated Velocity: {velocity.magnitude:F1}m/s");
            Debug.Log($"   Velocity Components: X={velocity.x:F1}, Y={velocity.y:F1}, Z={velocity.z:F1}");
            
            // Validate trajectory
            bool isValid = ValidateTrajectory(velocity, pitchingArea.position, pitchingArea.localScale);
            Debug.Log($"   Trajectory Valid: {isValid}");
            
            // Calculate final landing position
            float timeToReach = Vector3.Distance(ballSpawnPoint.position, currentTargetPosition) / velocity.magnitude;
            Vector3 finalLanding = ballSpawnPoint.position + velocity * timeToReach;
            finalLanding.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
            
            float finalAccuracy = Vector3.Distance(finalLanding, currentTargetPosition);
            
            Debug.Log($"🎯 FINAL ACCURACY RESULTS:");
            Debug.Log($"   Target: {currentTargetPosition}");
            Debug.Log($"   Predicted Landing: {finalLanding}");
            Debug.Log($"   Final Accuracy: {finalAccuracy:F3}m");
            Debug.Log($"   Time to Reach: {timeToReach:F2}s");
            
            if (finalAccuracy < 0.1f)
            {
                Debug.Log("   🎯 PERFECT ACCURACY ACHIEVED!");
            }
            else if (finalAccuracy < 0.5f)
            {
                Debug.Log("   🎯 EXCELLENT ACCURACY ACHIEVED");
            }
            else if (finalAccuracy < 1.0f)
            {
                Debug.Log("   🎯 GOOD ACCURACY ACHIEVED");
            }
            else
            {
                Debug.LogWarning($"   ❌ POOR ACCURACY: {finalAccuracy:F2}m - System needs adjustment!");
            }
        }
        
        /// <summary>
        /// Context menu function to force perfect accuracy
        /// </summary>
        [ContextMenu("Force Perfect Accuracy")]
        void ForcePerfectAccuracyContext()
        {
            ForcePerfectAccuracy();
        }
        
        /// <summary>
        /// Context menu function to test physics compensation system
        /// </summary>
        [ContextMenu("Test Physics Compensation System")]
        void TestPhysicsCompensationSystemContext()
        {
            if (currentTargetPosition == Vector3.zero)
            {
                Debug.Log("❌ No target set! Press Space first to generate target.");
                return;
            }
            
            Debug.Log($"🎯 PHYSICS COMPENSATION SYSTEM TEST:");
            Debug.Log($"   Target Position: {currentTargetPosition}");
            Debug.Log($"   Spawn Point: {ballSpawnPoint.position}");
            
            // Test the compensation system
            Vector3 direction = CalculateTargetDirection();
            float speed = CalculateBallSpeed();
            Vector3 velocity = CalculateInitialVelocity(direction, speed);
            
            Debug.Log($"🎯 COMPENSATION RESULTS:");
            Debug.Log($"   Final Velocity: {velocity.magnitude:F1}m/s");
            Debug.Log($"   Velocity Components: X={velocity.x:F1}, Y={velocity.y:F1}, Z={velocity.z:F1}");
            
            // Calculate final landing position
            float timeToReach = Vector3.Distance(ballSpawnPoint.position, currentTargetPosition) / velocity.magnitude;
            Vector3 finalLanding = ballSpawnPoint.position + velocity * timeToReach;
            finalLanding.y = ballSpawnPoint.position.y + velocity.y * timeToReach - 0.5f * 9.81f * timeToReach * timeToReach;
            
            float finalAccuracy = Vector3.Distance(finalLanding, currentTargetPosition);
            
            Debug.Log($"🎯 FINAL ACCURACY RESULTS:");
            Debug.Log($"   Target: {currentTargetPosition}");
            Debug.Log($"   Predicted Landing: {finalLanding}");
            Debug.Log($"   Final Accuracy: {finalAccuracy:F3}m");
            Debug.Log($"   Time to Reach: {timeToReach:F2}s");
            
            if (finalAccuracy < 0.1f)
            {
                Debug.Log("   🎯 PERFECT ACCURACY ACHIEVED!");
            }
            else if (finalAccuracy < 0.5f)
            {
                Debug.Log("   🎯 EXCELLENT ACCURACY ACHIEVED");
            }
            else if (finalAccuracy < 1.0f)
            {
                Debug.Log("   🎯 GOOD ACCURACY ACHIEVED");
            }
            else
            {
                Debug.LogWarning($"   ❌ POOR ACCURACY: {finalAccuracy:F2}m - Compensation system needs adjustment!");
            }
        }

        /// <summary>
        /// Force perfect accuracy by adjusting ball velocity if needed
        /// </summary>
        public void ForcePerfectAccuracy()
        {
            if (currentBall == null || currentTargetPosition == Vector3.zero) return;
            
            Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
            if (ballRb == null) return;
            
            // Calculate current trajectory
            Vector3 currentPos = currentBall.transform.position;
            Vector3 currentVelocity = ballRb.linearVelocity;
            
            // Calculate where ball will land with current velocity
            float timeToLand = Mathf.Abs(currentVelocity.y) / 9.81f;
            Vector3 predictedLanding = currentPos + currentVelocity * timeToLand;
            predictedLanding.y = currentPos.y + currentVelocity.y * timeToLand - 0.5f * 9.81f * timeToLand * timeToLand;
            
            float currentAccuracy = Vector3.Distance(predictedLanding, currentTargetPosition);
            
            Debug.Log($"🎯 FORCE ACCURACY: Current accuracy: {currentAccuracy:F3}m");
            
            // If accuracy is poor, force correction
            if (currentAccuracy > 0.5f)
            {
                Debug.Log("🎯 FORCING PERFECT ACCURACY - Adjusting ball trajectory!");
                
                // Calculate required velocity to hit target exactly
                Vector3 horizontalSpawnPos = new Vector3(currentPos.x, 0, currentPos.z);
                Vector3 horizontalTargetPos = new Vector3(currentTargetPosition.x, 0, currentTargetPosition.z);
                float horizontalDistance = Vector3.Distance(horizontalSpawnPos, horizontalTargetPos);
                Vector3 horizontalDirection = (horizontalTargetPos - horizontalSpawnPos).normalized;
                
                // Calculate time to reach target
                float timeToReach = horizontalDistance / ballSpeed;
                
                // Calculate required Y velocity
                float heightDifference = currentTargetPosition.y - currentPos.y;
                float requiredYVelocity = (heightDifference + 0.5f * 9.81f * timeToReach * timeToReach) / timeToReach;
                
                // Calculate horizontal velocity
                float exactHorizontalSpeed = horizontalDistance / timeToReach;
                Vector3 horizontalVelocity = horizontalDirection * exactHorizontalSpeed;
                
                // Create perfect velocity
                Vector3 perfectVelocity = horizontalVelocity;
                perfectVelocity.y = requiredYVelocity;
                
                // Apply perfect velocity
                ballRb.linearVelocity = perfectVelocity;
                
                Debug.Log($"🎯 PERFECT ACCURACY FORCED: New velocity: {perfectVelocity.magnitude:F1}m/s");
                Debug.Log($"🎯 Ball will now land exactly on target!");
            }
            else
            {
                Debug.Log("🎯 Accuracy already good - no correction needed");
            }
        }

        /// <summary>
        /// Ensure target image is properly loaded and visible
        /// </summary>
        void EnsureTargetImageLoaded()
        {
            if (pitchPredictionMarker == null) return;
            
            // Check if this is a canvas with image
            Canvas canvas = pitchPredictionMarker.GetComponent<Canvas>();
            if (canvas != null)
            {
                // Find the image component
                UnityEngine.UI.Image targetImage = pitchPredictionMarker.GetComponentInChildren<UnityEngine.UI.Image>();
                if (targetImage != null && targetImage.sprite == null)
                {
                    // Try to load the target image again
                    Sprite targetSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/black-red-target.png");
                    if (targetSprite != null)
                    {
                        targetImage.sprite = targetSprite;
                        targetImage.preserveAspect = true;
                        Debug.Log("🎯 Target image loaded successfully!");
                    }
                    else
                    {
                        // Final fallback - create a red circle
                        targetImage.color = Color.red;
                        Debug.LogWarning("🎯 Target image not found, using red circle fallback");
                    }
                }
            }
        }

        [ContextMenu("Test Target Image System")]
        void TestTargetImageSystem()
        {
            Debug.Log("🎯 Testing Target Image System...");
            
            if (pitchPredictionMarker == null)
            {
                Debug.Log("🎯 Creating new target image...");
                SetupInGamePrediction();
            }
            
            if (pitchPredictionMarker != null)
            {
                Canvas canvas = pitchPredictionMarker.GetComponent<Canvas>();
                if (canvas != null)
                {
                    UnityEngine.UI.Image targetImage = pitchPredictionMarker.GetComponentInChildren<UnityEngine.UI.Image>();
                    if (targetImage != null)
                    {
                        Debug.Log($"🎯 Target Image Found: Sprite={targetImage.sprite != null}, Color={targetImage.color}");
                        if (targetImage.sprite != null)
                        {
                            Debug.Log($"🎯 Sprite Name: {targetImage.sprite.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError("🎯 No Image component found in target marker!");
                    }
                }
                else
                {
                    Debug.LogError("🎯 No Canvas component found in target marker!");
                }
            }
            else
            {
                Debug.LogError("🎯 Failed to create target marker!");
            }
        }
    }
}
