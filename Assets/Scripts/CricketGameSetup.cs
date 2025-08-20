using UnityEngine;
using System.Collections.Generic;

namespace CricketGame
{
    /// <summary>
    /// Cricket Game Setup Script
    /// Automatically configures the entire cricket bowling system
    /// </summary>
    public class CricketGameSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool createMissingObjects = true;
        [SerializeField] private bool setupPhysics = true;
        [SerializeField] private bool setupMaterials = true;
        [SerializeField] private bool setupAudio = true;
        
        [Header("Object References")]
        [SerializeField] private GameObject bowlingMachine;
        [SerializeField] private GameObject cricketBall;
        [SerializeField] private GameObject wicket;
        [SerializeField] private GameObject pitch;
        [SerializeField] private GameObject ground;
        
        [Header("Generated Materials")]
        [SerializeField] private Material ballMaterial;
        [SerializeField] private Material wicketMaterial;
        [SerializeField] private Material pitchMaterial;
        [SerializeField] private Material groundMaterial;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip bowlSound;
        [SerializeField] private AudioClip bounceSound;
        [SerializeField] private AudioClip wicketHitSound;
        
        // Private variables
        private CricketBowlingSystem bowlingSystem;
        private BowlingMachineController machineController;
        private CricketBall ballScript;
        private bool isSetup = false;
        
        void Start()
        {
            if (setupOnStart)
            {
                SetupCricketGame();
            }
        }
        
        /// <summary>
        /// Main setup function - call this to setup everything
        /// </summary>
        [ContextMenu("Setup Cricket Game")]
        public void SetupCricketGame()
        {
            if (isSetup)
            {
                Debug.Log("Cricket game is already setup!");
                return;
            }
            
            Debug.Log("Starting Cricket Game Setup...");
            
            // Find or create objects
            FindOrCreateObjects();
            
            // Setup physics
            if (setupPhysics)
            {
                SetupPhysics();
            }
            
            // Setup materials
            if (setupMaterials)
            {
                SetupMaterials();
            }
            
            // Setup audio
            if (setupAudio)
            {
                SetupAudio();
            }
            
            // Setup bowling system
            SetupBowlingSystem();
            
            // Setup bowling machine
            SetupBowlingMachine();
            
            // Setup ball
            SetupBall();
            
            // Setup wicket
            SetupWicket();
            
            // Setup pitch and ground
            SetupPitchAndGround();
            
            // Final configuration
            FinalizeSetup();
            
            isSetup = true;
            Debug.Log("Cricket Game Setup Complete! Press SPACE to bowl.");
        }
        
        /// <summary>
        /// Find or create required objects
        /// </summary>
        void FindOrCreateObjects()
        {
            // Find bowling machine
            if (bowlingMachine == null)
            {
                bowlingMachine = GameObject.Find("BowlingMachine");
                if (bowlingMachine == null && createMissingObjects)
                {
                    bowlingMachine = CreateBowlingMachine();
                }
            }
            
            // Find cricket ball
            if (cricketBall == null)
            {
                cricketBall = GameObject.Find("CricketBall");
                if (cricketBall == null && createMissingObjects)
                {
                    cricketBall = CreateCricketBall();
                }
            }
            
            // Find wicket
            if (wicket == null)
            {
                wicket = GameObject.Find("Wicket");
                if (wicket == null && createMissingObjects)
                {
                    wicket = CreateWicket();
                }
            }
            
            // Find pitch
            if (pitch == null)
            {
                pitch = GameObject.Find("Pitch");
                if (pitch == null && createMissingObjects)
                {
                    pitch = CreatePitch();
                }
            }
            
            // Find ground
            if (ground == null)
            {
                ground = GameObject.Find("Ground");
                if (ground == null && createMissingObjects)
                {
                    ground = CreateGround();
                }
            }
        }
        
        /// <summary>
        /// Create bowling machine if missing
        /// </summary>
        GameObject CreateBowlingMachine()
        {
            GameObject machine = new GameObject("BowlingMachine");
            machine.transform.position = new Vector3(0, 1.5f, -10f);
            
            // Add visual representation
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(machine.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1f, 2f, 1f);
            body.name = "MachineBody";
            
            // Add release point
            GameObject releasePoint = new GameObject("ReleasePoint");
            releasePoint.transform.SetParent(machine.transform);
            releasePoint.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
            
            // Add ball holder
            GameObject ballHolder = new GameObject("BallHolder");
            ballHolder.transform.SetParent(machine.transform);
            ballHolder.transform.localPosition = new Vector3(0, 0.5f, 0.3f);
            
            // Add light
            Light light = machine.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3f;
            light.intensity = 2f;
            light.color = Color.green;
            
            Debug.Log("Created Bowling Machine");
            return machine;
        }
        
        /// <summary>
        /// Create cricket ball if missing
        /// </summary>
        GameObject CreateCricketBall()
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "CricketBall";
            ball.transform.position = new Vector3(0, 1.5f, -9.5f);
            ball.transform.localScale = Vector3.one * 0.072f; // Standard cricket ball size
            
            // Add cricket ball script
            ball.AddComponent<CricketBall>();
            
            Debug.Log("Created Cricket Ball");
            return ball;
        }
        
        /// <summary>
        /// Create wicket if missing
        /// </summary>
        GameObject CreateWicket()
        {
            GameObject wicketObj = new GameObject("Wicket");
            wicketObj.transform.position = new Vector3(0, 0, 10f);
            
            // Create stumps
            for (int i = 0; i < 3; i++)
            {
                GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stump.transform.SetParent(wicketObj.transform);
                stump.transform.localPosition = new Vector3((i - 1) * 0.1f, 0.7f, 0);
                stump.transform.localScale = new Vector3(0.02f, 0.7f, 0.02f);
                stump.name = $"Stump_{i + 1}";
            }
            
            // Add bails
            GameObject bail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bail.transform.SetParent(wicketObj.transform);
            bail.transform.localPosition = new Vector3(0, 1.4f, 0);
            bail.transform.localScale = new Vector3(0.3f, 0.02f, 0.02f);
            bail.name = "Bail";
            
            // Add tag for collision detection
            wicketObj.tag = "Wicket";
            
            Debug.Log("Created Wicket");
            return wicketObj;
        }
        
        /// <summary>
        /// Create pitch if missing
        /// </summary>
        GameObject CreatePitch()
        {
            GameObject pitchObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitchObj.name = "Pitch";
            pitchObj.transform.position = new Vector3(0, 0.01f, 0);
            pitchObj.transform.localScale = new Vector3(3f, 0.02f, 22f);
            
            // Add tag
            pitchObj.tag = "Pitch";
            
            Debug.Log("Created Pitch");
            return pitchObj;
        }
        
        /// <summary>
        /// Create ground if missing
        /// </summary>
        GameObject CreateGround()
        {
            GameObject groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundObj.name = "Ground";
            groundObj.transform.position = new Vector3(0, 0, 0);
            groundObj.transform.localScale = new Vector3(10f, 1f, 10f);
            
            // Add tag
            groundObj.tag = "Ground";
            
            Debug.Log("Created Ground");
            return groundObj;
        }
        
        /// <summary>
        /// Setup physics for all objects
        /// </summary>
        void SetupPhysics()
        {
            // Setup ground physics
            if (ground != null)
            {
                Rigidbody groundRb = ground.GetComponent<Rigidbody>();
                if (groundRb == null)
                {
                    groundRb = ground.AddComponent<Rigidbody>();
                }
                groundRb.isKinematic = true;
                groundRb.useGravity = false;
                
                // Add ground collider if missing
                if (ground.GetComponent<Collider>() == null)
                {
                    ground.AddComponent<BoxCollider>();
                }
            }
            
            // Setup pitch physics
            if (pitch != null)
            {
                Rigidbody pitchRb = pitch.GetComponent<Rigidbody>();
                if (pitchRb == null)
                {
                    pitchRb = pitch.AddComponent<Rigidbody>();
                }
                pitchRb.isKinematic = true;
                pitchRb.useGravity = false;
                
                // Add pitch collider if missing
                if (pitch.GetComponent<Collider>() == null)
                {
                    pitch.AddComponent<BoxCollider>();
                }
            }
            
            // Setup wicket physics
            if (wicket != null)
            {
                // Add trigger collider for wicket detection
                BoxCollider wicketCollider = wicket.GetComponent<BoxCollider>();
                if (wicketCollider == null)
                {
                    wicketCollider = wicket.AddComponent<BoxCollider>();
                }
                wicketCollider.isTrigger = true;
                wicketCollider.size = new Vector3(0.5f, 1.5f, 0.5f);
                wicketCollider.center = new Vector3(0, 0.75f, 0);
            }
            
            Debug.Log("Physics setup complete");
        }
        
        /// <summary>
        /// Setup materials for all objects
        /// </summary>
        void SetupMaterials()
        {
            // Create ball material
            if (ballMaterial == null)
            {
                ballMaterial = new Material(Shader.Find("Standard"));
                ballMaterial.color = Color.red;
                ballMaterial.SetFloat("_Metallic", 0.1f);
                ballMaterial.SetFloat("_Smoothness", 0.3f);
            }
            
            // Create wicket material
            if (wicketMaterial == null)
            {
                wicketMaterial = new Material(Shader.Find("Standard"));
                wicketMaterial.color = Color.white;
                wicketMaterial.SetFloat("_Metallic", 0.0f);
                wicketMaterial.SetFloat("_Smoothness", 0.1f);
            }
            
            // Create pitch material
            if (pitchMaterial == null)
            {
                pitchMaterial = new Material(Shader.Find("Standard"));
                pitchMaterial.color = new Color(0.4f, 0.3f, 0.2f); // Brown
                pitchMaterial.SetFloat("_Metallic", 0.0f);
                pitchMaterial.SetFloat("_Smoothness", 0.0f);
            }
            
            // Create ground material
            if (groundMaterial == null)
            {
                groundMaterial = new Material(Shader.Find("Standard"));
                groundMaterial.color = new Color(0.2f, 0.5f, 0.2f); // Green
                groundMaterial.SetFloat("_Metallic", 0.0f);
                groundMaterial.SetFloat("_Smoothness", 0.1f);
            }
            
            // Apply materials
            if (cricketBall != null)
            {
                Renderer ballRenderer = cricketBall.GetComponent<Renderer>();
                if (ballRenderer != null)
                {
                    ballRenderer.material = ballMaterial;
                }
            }
            
            if (wicket != null)
            {
                Renderer[] wicketRenderers = wicket.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in wicketRenderers)
                {
                    renderer.material = wicketMaterial;
                }
            }
            
            if (pitch != null)
            {
                Renderer pitchRenderer = pitch.GetComponent<Renderer>();
                if (pitchRenderer != null)
                {
                    pitchRenderer.material = pitchMaterial;
                }
            }
            
            if (ground != null)
            {
                Renderer groundRenderer = ground.GetComponent<Renderer>();
                if (groundRenderer != null)
                {
                    groundRenderer.material = groundMaterial;
                }
            }
            
            Debug.Log("Materials setup complete");
        }
        
        /// <summary>
        /// Setup audio for the game
        /// </summary>
        void SetupAudio()
        {
            // Create audio source on bowling machine
            if (bowlingMachine != null)
            {
                AudioSource audioSource = bowlingMachine.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = bowlingMachine.AddComponent<AudioSource>();
                }
                
                audioSource.playOnAwake = false;
                audioSource.volume = 0.7f;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.maxDistance = 20f;
            }
            
            Debug.Log("Audio setup complete");
        }
        
        /// <summary>
        /// Setup the main bowling system
        /// </summary>
        void SetupBowlingSystem()
        {
            if (bowlingMachine == null) return;
            
            // Add bowling system script
            bowlingSystem = bowlingMachine.GetComponent<CricketBowlingSystem>();
            if (bowlingSystem == null)
            {
                bowlingSystem = bowlingMachine.AddComponent<CricketBowlingSystem>();
            }
            
            // Configure bowling system
            var serializedObject = new UnityEditor.SerializedObject(bowlingSystem);
            var ballSpawnPoint = serializedObject.FindProperty("ballSpawnPoint");
            var cricketBallProp = serializedObject.FindProperty("cricketBall");
            var wicketTarget = serializedObject.FindProperty("wicketTarget");
            
            if (ballSpawnPoint != null && bowlingMachine.transform.Find("ReleasePoint") != null)
            {
                ballSpawnPoint.objectReferenceValue = bowlingMachine.transform.Find("ReleasePoint");
            }
            
            if (cricketBallProp != null)
            {
                cricketBallProp.objectReferenceValue = cricketBall;
            }
            
            if (wicketTarget != null)
            {
                wicketTarget.objectReferenceValue = wicket;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log("Bowling system setup complete");
        }
        
        /// <summary>
        /// Setup the bowling machine controller
        /// </summary>
        void SetupBowlingMachine()
        {
            if (bowlingMachine == null) return;
            
            // Add machine controller script
            machineController = bowlingMachine.GetComponent<BowlingMachineController>();
            if (machineController == null)
            {
                machineController = bowlingMachine.AddComponent<BowlingMachineController>();
            }
            
            // Configure machine controller
            var serializedObject = new UnityEditor.SerializedObject(machineController);
            var ballHolder = serializedObject.FindProperty("ballHolder");
            var releasePoint = serializedObject.FindProperty("releasePoint");
            var ballPrefab = serializedObject.FindProperty("ballPrefab");
            var targetPoint = serializedObject.FindProperty("targetPoint");
            
            if (ballHolder != null && bowlingMachine.transform.Find("BallHolder") != null)
            {
                ballHolder.objectReferenceValue = bowlingMachine.transform.Find("BallHolder");
            }
            
            if (releasePoint != null && bowlingMachine.transform.Find("ReleasePoint") != null)
            {
                releasePoint.objectReferenceValue = bowlingMachine.transform.Find("ReleasePoint");
            }
            
            if (ballPrefab != null)
            {
                ballPrefab.objectReferenceValue = cricketBall;
            }
            
            if (targetPoint != null)
            {
                targetPoint.objectReferenceValue = wicket;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log("Bowling machine controller setup complete");
        }
        
        /// <summary>
        /// Setup the cricket ball
        /// </summary>
        void SetupBall()
        {
            if (cricketBall == null) return;
            
            // Get or add cricket ball script
            ballScript = cricketBall.GetComponent<CricketBall>();
            if (ballScript == null)
            {
                ballScript = cricketBall.AddComponent<CricketBall>();
            }
            
            // Add rigidbody if missing
            Rigidbody ballRb = cricketBall.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                ballRb = cricketBall.AddComponent<Rigidbody>();
            }
            
            // Configure rigidbody
            ballRb.mass = 0.16f; // Standard cricket ball mass
            ballRb.linearDamping = 0.02f;
            ballRb.angularDamping = 0.05f;
            ballRb.useGravity = true;
            ballRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Add collider if missing
            if (cricketBall.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = cricketBall.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }
            
            Debug.Log("Cricket ball setup complete");
        }
        
        /// <summary>
        /// Setup the wicket
        /// </summary>
        void SetupWicket()
        {
            if (wicket == null) return;
            
            // Add tag if missing
            if (wicket.tag != "Wicket")
            {
                wicket.tag = "Wicket";
            }
            
            Debug.Log("Wicket setup complete");
        }
        
        /// <summary>
        /// Setup pitch and ground
        /// </summary>
        void SetupPitchAndGround()
        {
            // Setup pitch
            if (pitch != null)
            {
                if (pitch.tag != "Pitch")
                {
                    pitch.tag = "Pitch";
                }
            }
            
            // Setup ground
            if (ground != null)
            {
                if (ground.tag != "Ground")
                {
                    ground.tag = "Ground";
                }
            }
            
            Debug.Log("Pitch and ground setup complete");
        }
        
        /// <summary>
        /// Finalize the setup
        /// </summary>
        void FinalizeSetup()
        {
            // Position everything correctly
            if (bowlingMachine != null)
            {
                bowlingMachine.transform.position = new Vector3(0, 1.5f, -10f);
            }
            
            if (cricketBall != null)
            {
                cricketBall.transform.position = new Vector3(0, 1.5f, -9.5f);
            }
            
            if (wicket != null)
            {
                wicket.transform.position = new Vector3(0, 0, 10f);
            }
            
            if (pitch != null)
            {
                pitch.transform.position = new Vector3(0, 0.01f, 0);
            }
            
            if (ground != null)
            {
                ground.transform.position = new Vector3(0, 0, 0);
            }
            
            Debug.Log("Setup finalization complete");
        }
        
        /// <summary>
        /// Reset the entire setup
        /// </summary>
        [ContextMenu("Reset Setup")]
        public void ResetSetup()
        {
            isSetup = false;
            
            // Remove components
            if (bowlingMachine != null)
            {
                DestroyImmediate(bowlingMachine.GetComponent<CricketBowlingSystem>());
                DestroyImmediate(bowlingMachine.GetComponent<BowlingMachineController>());
            }
            
            if (cricketBall != null)
            {
                DestroyImmediate(cricketBall.GetComponent<CricketBall>());
            }
            
            Debug.Log("Setup reset complete. Call SetupCricketGame() to setup again.");
        }
        
        /// <summary>
        /// Get setup status
        /// </summary>
        public string GetSetupStatus()
        {
            return $"Setup Status: {(isSetup ? "Complete" : "Incomplete")}\n" +
                   $"Bowling Machine: {(bowlingMachine != null ? "Found" : "Missing")}\n" +
                   $"Cricket Ball: {(cricketBall != null ? "Found" : "Missing")}\n" +
                   $"Wicket: {(wicket != null ? "Found" : "Missing")}\n" +
                   $"Pitch: {(pitch != null ? "Found" : "Missing")}\n" +
                   $"Ground: {(ground != null ? "Found" : "Missing")}";
        }
        
        /// <summary>
        /// Test the bowling system
        /// </summary>
        [ContextMenu("Test Bowling")]
        public void TestBowling()
        {
            if (!isSetup)
            {
                Debug.LogWarning("Setup not complete! Please setup the cricket game first.");
                return;
            }
            
            if (bowlingSystem != null)
            {
                bowlingSystem.BowlBall();
                Debug.Log("Test bowling initiated!");
            }
            else
            {
                Debug.LogError("Bowling system not found!");
            }
        }
    }
}
