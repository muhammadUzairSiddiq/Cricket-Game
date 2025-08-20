using UnityEngine;
using System.Collections.Generic; // Added for List

namespace CricketGame
{
    /// <summary>
    /// Simple Cricket Setup - Works with existing scene objects
    /// </summary>
    public class SimpleCricketSetup : MonoBehaviour
    {
        [Header("Existing Objects")]
        [SerializeField] private GameObject bowlingMachine; // Your existing "Machine" object
        [SerializeField] private GameObject ballPrefab; // Ball prefab to instantiate
        [SerializeField] private GameObject wicket; // Your existing "Wicket" object
        [SerializeField] private GameObject pitch; // Your existing "Pitch" object
        [SerializeField] private GameObject ground; // Your existing "Ground" object
        [SerializeField] private GameObject pitchingArea; // Pitching area center
        [SerializeField] private GameObject topLeftCorner; // Top left corner of pitching area
        [SerializeField] private GameObject topRightCorner; // Top right corner of pitching area
        [SerializeField] private GameObject bottomLeftCorner; // Bottom left corner of pitching area
        [SerializeField] private GameObject bottomRightCorner; // Bottom right corner of pitching area
        
        [Header("Setup Options")]
        [SerializeField] private bool setupOnStart = true;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupCricketGame();
            }
        }
        
        /// <summary>
        /// Simple setup that works with existing objects
        /// </summary>
        [ContextMenu("Setup Cricket Game")]
        public void SetupCricketGame()
        {
            Debug.Log("Starting Simple Cricket Setup...");
            
            // Find existing objects by name
            FindExistingObjects();
            
            // Setup the bowling system on your existing machine
            SetupBowlingMachine();
            
            // Setup the ball physics
            SetupBall();
            
            // Setup wicket collision
            SetupWicket();
            
            // Setup pitching area as the real pitch
            SetupPitchingArea();
            
            Debug.Log("Simple Cricket Setup Complete! Press SPACE to bowl.");
        }
        
        /// <summary>
        /// Find existing objects in the scene
        /// </summary>
        void FindExistingObjects()
        {
            // Find your existing objects by name
            if (bowlingMachine == null)
                bowlingMachine = GameObject.Find("Machine");
            
            // Note: ballPrefab should be assigned in inspector
            
            if (wicket == null)
                wicket = GameObject.Find("Wicket");
            
            if (pitch == null)
                pitch = GameObject.Find("Pitch");
            
            if (ground == null)
                ground = GameObject.Find("Ground");
            
            // Find pitching area as child of Pitch
            if (pitchingArea == null && pitch != null)
            {
                pitchingArea = pitch.transform.Find("Pitching Area")?.gameObject;
                if (pitchingArea == null)
                {
                    // Try alternative names
                    pitchingArea = pitch.transform.Find("PitchingArea")?.gameObject;
                }
            }
            
            // Find corner GameObjects automatically
            if (topLeftCorner == null)
                topLeftCorner = GameObject.Find("Top Left corner");
            if (topRightCorner == null)
                topRightCorner = GameObject.Find("Top Right corner");
            if (bottomLeftCorner == null)
                bottomLeftCorner = GameObject.Find("bottom left corner (1)");
            if (bottomRightCorner == null)
                bottomRightCorner = GameObject.Find("bottom right corner");
            
            // Log what we found
            Debug.Log($"Found objects: Machine={bowlingMachine != null}, Ball Prefab={ballPrefab != null}, Wicket={wicket != null}, Pitching Area={pitchingArea != null}");
            Debug.Log($"Found corners: TL={topLeftCorner != null}, TR={topRightCorner != null}, BL={bottomLeftCorner != null}, BR={bottomRightCorner != null}");
        }
        
        /// <summary>
        /// Setup bowling machine with proper spawn point
        /// </summary>
        void SetupBowlingMachine()
        {
            if (bowlingMachine == null)
            {
                Debug.LogError("Bowling machine not found! Please assign it in the inspector.");
                return;
            }

            // Use existing BallSpawnPoint - don't create new one
            Transform spawnPoint = bowlingMachine.transform.Find("BallSpawnPoint");
            if (spawnPoint == null)
            {
                Debug.LogError("BallSpawnPoint not found as child of Machine! Please check your hierarchy.");
                return;
            }

            Debug.Log($"Using existing BallSpawnPoint at position: {spawnPoint.position}");

            // Add cricket bowling system if missing
            CricketBowlingSystem bowlingSystem = bowlingMachine.GetComponent<CricketBowlingSystem>();
            if (bowlingSystem == null)
            {
                bowlingSystem = bowlingMachine.AddComponent<CricketBowlingSystem>();
            }

            // Configure the bowling system references
            var serializedObject = new UnityEditor.SerializedObject(bowlingSystem);
            
            // Set ball spawn point (your existing one)
            var spawnPointProp = serializedObject.FindProperty("ballSpawnPoint");
            if (spawnPointProp != null)
            {
                spawnPointProp.objectReferenceValue = spawnPoint;
                Debug.Log("BallSpawnPoint assigned to bowling system");
            }

            // Set ball prefab (use only ONE ball prefab)
            var ballPrefabProp = serializedObject.FindProperty("ballPrefab");
            if (ballPrefabProp != null)
            {
                ballPrefabProp.objectReferenceValue = ballPrefab;
                Debug.Log("Ball prefab assigned to bowling system");
            }

            // Set wicket target
            var wicketProp = serializedObject.FindProperty("wicketTarget");
            if (wicketProp != null && wicket != null)
            {
                wicketProp.objectReferenceValue = wicket.transform;
                Debug.Log("Wicket target assigned to bowling system");
            }

            // Set bowling machine
            var machineProp = serializedObject.FindProperty("bowlingMachine");
            if (machineProp != null)
            {
                machineProp.objectReferenceValue = bowlingMachine.transform;
                Debug.Log("Bowling machine assigned to bowling system");
            }

            // Set pitching area
            var pitchingAreaProp = serializedObject.FindProperty("pitchingArea");
            if (pitchingAreaProp != null && pitchingArea != null)
            {
                pitchingAreaProp.objectReferenceValue = pitchingArea.transform;
                Debug.Log("Pitching area assigned to bowling system");
            }
            else if (pitchingArea == null)
            {
                Debug.LogWarning("Pitching area not found! Ball will target wicket directly.");
            }
            
            // Set corner references for precise boundary detection
            var topLeftProp = serializedObject.FindProperty("topLeftCorner");
            var topRightProp = serializedObject.FindProperty("topRightCorner");
            var bottomLeftProp = serializedObject.FindProperty("bottomLeftCorner");
            var bottomRightProp = serializedObject.FindProperty("bottomRightCorner");
            
            if (topLeftProp != null && topLeftCorner != null)
            {
                topLeftProp.objectReferenceValue = topLeftCorner.transform;
                Debug.Log("Top Left corner assigned to bowling system");
            }
            if (topRightProp != null && topRightCorner != null)
            {
                topRightProp.objectReferenceValue = topRightCorner.transform;
                Debug.Log("Top Right corner assigned to bowling system");
            }
            if (bottomLeftProp != null && bottomLeftCorner != null)
            {
                bottomLeftProp.objectReferenceValue = bottomLeftCorner.transform;
                Debug.Log("Bottom Left corner assigned to bowling system");
            }
            if (bottomRightProp != null && bottomRightCorner != null)
            {
                bottomRightProp.objectReferenceValue = bottomRightCorner.transform;
                Debug.Log("Bottom Right corner assigned to bowling system");
            }

            serializedObject.ApplyModifiedProperties();

            Debug.Log("Bowling machine setup complete using your existing objects!");
        }
        
        /// <summary>
        /// Clean up duplicate balls and fix positioning
        /// </summary>
        void CleanupDuplicateBalls()
        {
            // Find all ball objects in the scene
            GameObject[] allBalls = GameObject.FindGameObjectsWithTag("Ball");
            List<GameObject> ballsToKeep = new List<GameObject>();
            
            // Keep only the original ball prefab, destroy clones
            foreach (GameObject ball in allBalls)
            {
                if (ball.name.Contains("(Clone)"))
                {
                    Debug.Log($"Destroying duplicate ball: {ball.name}");
                    DestroyImmediate(ball);
                }
                else if (ball.name == "BALL" || ball.name == "CricketBall")
                {
                    // This is the original ball, keep it
                    ballsToKeep.Add(ball);
                }
            }
            
            // If we have multiple original balls, keep only one
            if (ballsToKeep.Count > 1)
            {
                Debug.Log("Multiple original balls found, keeping only the first one");
                for (int i = 1; i < ballsToKeep.Count; i++)
                {
                    DestroyImmediate(ballsToKeep[i]);
                }
            }
            
            Debug.Log("Duplicate balls cleaned up!");
        }

        /// <summary>
        /// Setup ball physics
        /// </summary>
        void SetupBall()
        {
            // Note: ballPrefab should be assigned in inspector
            if (ballPrefab == null)
            {
                Debug.LogError("Ball prefab not assigned! Please assign it in the inspector.");
                return;
            }

            // Clean up any duplicate balls first
            CleanupDuplicateBalls();

            // Add cricket ball script to prefab if missing
            CricketBall ballScript = ballPrefab.GetComponent<CricketBall>();
            if (ballScript == null)
            {
                ballScript = ballPrefab.AddComponent<CricketBall>();
            }

            // Add rigidbody to prefab if missing
            Rigidbody ballRb = ballPrefab.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                ballRb = ballPrefab.AddComponent<Rigidbody>();
            }

            // Configure rigidbody
            ballRb.mass = 0.16f;
            ballRb.linearDamping = 0.01f; // Very low damping to maintain momentum
            ballRb.angularDamping = 0.01f; // Very low angular damping
            ballRb.useGravity = true;
            ballRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            ballRb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement

            // Add collider if missing
            if (ballPrefab.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = ballPrefab.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }

            // Add ball tag if missing
            if (ballPrefab.tag != "Ball")
            {
                ballPrefab.tag = "Ball";
            }

            Debug.Log("Ball prefab setup complete!");
        }
        
        /// <summary>
        /// Setup wicket collision
        /// </summary>
        void SetupWicket()
        {
            if (wicket == null)
            {
                Debug.LogError("Wicket not found! Please assign it in the inspector.");
                return;
            }
            
            // Add tag if missing
            if (wicket.tag != "Wicket")
            {
                wicket.tag = "Wicket";
            }
            
            // Add trigger collider if missing
            BoxCollider wicketCollider = wicket.GetComponent<BoxCollider>();
            if (wicketCollider == null)
            {
                wicketCollider = wicket.AddComponent<BoxCollider>();
            }
            
            // Configure collider
            wicketCollider.isTrigger = true;
            wicketCollider.size = new Vector3(0.5f, 1.5f, 0.5f);
            wicketCollider.center = new Vector3(0, 0.75f, 0);
            
            Debug.Log("Wicket setup complete");
        }
        
        /// <summary>
        /// Setup pitching area as the real pitch for ball bouncing
        /// </summary>
        void SetupPitchingArea()
        {
            if (pitchingArea == null)
            {
                Debug.LogError("Pitching area not found! Please assign it in the inspector.");
                return;
            }

            // FIX NEGATIVE SCALE ISSUE - This is causing the collision problem!
            Vector3 currentScale = pitchingArea.transform.localScale;
            if (currentScale.x < 0 || currentScale.y < 0 || currentScale.z < 0)
            {
                Debug.LogWarning($"Fixing negative scale on Pitching Area: {currentScale}");
                // Force positive scale values
                Vector3 fixedScale = new Vector3(
                    Mathf.Abs(currentScale.x),
                    Mathf.Abs(currentScale.y),
                    Mathf.Abs(currentScale.z)
                );
                pitchingArea.transform.localScale = fixedScale;
                Debug.Log($"Fixed scale from {currentScale} to {fixedScale}");
            }

            // Add collider if missing
            Collider areaCollider = pitchingArea.GetComponent<Collider>();
            if (areaCollider == null)
            {
                BoxCollider boxCollider = pitchingArea.AddComponent<BoxCollider>();
                // Use the FIXED size of the pitching area
                boxCollider.size = pitchingArea.transform.localScale;
                boxCollider.center = Vector3.zero;
                
                // Store reference for later use
                areaCollider = boxCollider;
            }
            else
            {
                // Update existing collider size to match fixed scale
                if (areaCollider is BoxCollider boxColl)
                {
                    boxColl.size = pitchingArea.transform.localScale;
                    Debug.Log($"Updated existing BoxCollider size to: {boxColl.size}");
                }
            }

            // Ensure the collider is NOT a trigger (we want physical collision)
            if (areaCollider is BoxCollider boxColl2)
            {
                boxColl2.isTrigger = false;
            }

            // Remove any existing physics material - we're using custom bounce physics
            areaCollider.material = null;

            // Add tag for collision detection
            if (pitchingArea.tag != "PitchingArea")
            {
                pitchingArea.tag = "PitchingArea";
            }

            Debug.Log($"Pitching area setup complete! Fixed Size: {pitchingArea.transform.localScale}, Tag: {pitchingArea.tag}, Collider: {areaCollider.GetType().Name}");
        }
        
        /// <summary>
        /// Test the bowling system
        /// </summary>
        [ContextMenu("Test Bowling")]
        public void TestBowling()
        {
            if (bowlingMachine == null)
            {
                Debug.LogError("Bowling machine not found!");
                return;
            }
            
            CricketBowlingSystem bowlingSystem = bowlingMachine.GetComponent<CricketBowlingSystem>();
            if (bowlingSystem != null)
            {
                bowlingSystem.BowlBall();
                Debug.Log("Test bowling initiated!");
            }
            else
            {
                Debug.LogError("Bowling system not found on machine!");
            }
        }
        
        /// <summary>
        /// Get setup status
        /// </summary>
        public string GetSetupStatus()
        {
            return $"Setup Status:\n" +
                   $"Machine: {(bowlingMachine != null ? "Found" : "Missing")}\n" +
                   $"Ball Prefab: {(ballPrefab != null ? "Found" : "Missing")}\n" +
                   $"Wicket: {(wicket != null ? "Found" : "Missing")}\n" +
                   $"Pitch: {(pitch != null ? "Found" : "Missing")}\n" +
                   $"Ground: {(ground != null ? "Found" : "Missing")}";
        }
        
        /// <summary>
        /// Fix negative scale on pitching area (context menu)
        /// </summary>
        [ContextMenu("Fix Pitching Area Scale")]
        public void FixPitchingAreaScale()
        {
            if (pitchingArea == null)
            {
                Debug.LogError("Pitching area not assigned!");
                return;
            }
            
            Vector3 currentScale = pitchingArea.transform.localScale;
            if (currentScale.x < 0 || currentScale.y < 0 || currentScale.z < 0)
            {
                Vector3 fixedScale = new Vector3(
                    Mathf.Abs(currentScale.x),
                    Mathf.Abs(currentScale.y),
                    Mathf.Abs(currentScale.z)
                );
                pitchingArea.transform.localScale = fixedScale;
                Debug.Log($"Fixed negative scale: {currentScale} → {fixedScale}");
                
                // Also update the collider if it exists
                Collider areaCollider = pitchingArea.GetComponent<Collider>();
                if (areaCollider is BoxCollider boxColl)
                {
                    boxColl.size = fixedScale;
                    Debug.Log($"Updated BoxCollider size to: {boxColl.size}");
                }
            }
            else
            {
                Debug.Log("Pitching area scale is already positive!");
            }
        }
    }
}
