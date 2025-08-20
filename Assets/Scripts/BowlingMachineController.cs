using System.Collections;
using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Bowling Machine Controller
    /// Handles bowling machine behavior and ball release mechanism
    /// </summary>
    public class BowlingMachineController : MonoBehaviour
    {
        [Header("Machine Setup")]
        [SerializeField] private Transform ballHolder;
        [SerializeField] private Transform releasePoint;
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform targetPoint;
        
        [Header("Bowling Mechanism")]
        [SerializeField] private float bowlForce = 25f;
        [SerializeField] private float bowlAngle = 15f; // Degrees from horizontal
        [SerializeField] private float bowlHeight = 1.5f; // Height above ground
        [SerializeField] private float releaseDelay = 0.1f;
        
        [Header("Machine Behavior")]
        [SerializeField] private bool autoBowl = false;
        [SerializeField] private float autoBowlInterval = 3f;
        [SerializeField] private bool randomizeDirection = true;
        [SerializeField] private float directionVariation = 10f; // Degrees
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem releaseEffect;
        [SerializeField] private AudioSource bowlSound;
        [SerializeField] private Light machineLight;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material idleMaterial;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool usePhysicsPrediction = true;
        [SerializeField] private int predictionSteps = 50;
        [SerializeField] private float predictionTimeStep = 0.1f;
        
        // Private variables
        private bool isReady = true;
        private bool isBowling = false;
        private GameObject currentBall;
        private Rigidbody currentBallRigidbody;
        private Vector3 originalBallPosition;
        private float lastBowlTime = 0f;
        private LineRenderer trajectoryLine;
        
        // Events
        public System.Action<GameObject> OnBallReleased;
        public System.Action<GameObject> OnBallHitTarget;
        public System.Action<GameObject> OnBallMissed;
        
        void Start()
        {
            InitializeMachine();
        }
        
        void Update()
        {
            HandleInput();
            UpdateMachineState();
            
            if (autoBowl && Time.time - lastBowlTime > autoBowlInterval)
            {
                BowlBall();
            }
        }
        
        /// <summary>
        /// Initialize the bowling machine
        /// </summary>
        void InitializeMachine()
        {
            // Create trajectory line
            CreateTrajectoryLine();
            
            // Setup materials
            SetupMachineMaterials();
            
            // Setup effects
            SetupEffects();
            
            // Position ball holder
            if (ballHolder != null)
            {
                originalBallPosition = ballHolder.position;
            }
            
            Debug.Log("Bowling Machine initialized successfully!");
        }
        
        /// <summary>
        /// Create trajectory prediction line
        /// </summary>
        void CreateTrajectoryLine()
        {
            if (usePhysicsPrediction)
            {
                trajectoryLine = gameObject.AddComponent<LineRenderer>();
                trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
                trajectoryLine.startColor = Color.yellow;
                trajectoryLine.endColor = Color.red;
                trajectoryLine.startWidth = 0.05f;
                trajectoryLine.endWidth = 0.02f;
                trajectoryLine.positionCount = predictionSteps;
                trajectoryLine.enabled = false;
            }
        }
        
        /// <summary>
        /// Setup machine materials
        /// </summary>
        void SetupMachineMaterials()
        {
            if (activeMaterial != null && idleMaterial != null)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.material = idleMaterial;
                }
            }
        }
        
        /// <summary>
        /// Setup visual and audio effects
        /// </summary>
        void SetupEffects()
        {
            if (releaseEffect == null)
            {
                releaseEffect = GetComponentInChildren<ParticleSystem>();
            }
            
            if (bowlSound == null)
            {
                bowlSound = GetComponent<AudioSource>();
            }
            
            if (machineLight == null)
            {
                machineLight = GetComponentInChildren<Light>();
            }
        }
        
        /// <summary>
        /// Handle user input
        /// </summary>
        void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) && isReady && !isBowling)
            {
                BowlBall();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetMachine();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleTrajectory();
            }
        }
        
        /// <summary>
        /// Update machine state and visual feedback
        /// </summary>
        void UpdateMachineState()
        {
            // Update machine light
            if (machineLight != null)
            {
                machineLight.color = isReady ? Color.green : Color.red;
                machineLight.intensity = isReady ? 2f : 0.5f;
            }
            
            // Update materials
            UpdateMachineMaterials();
            
            // Update trajectory prediction
            if (usePhysicsPrediction && trajectoryLine != null)
            {
                UpdateTrajectoryPrediction();
            }
        }
        
        /// <summary>
        /// Update machine materials based on state
        /// </summary>
        void UpdateMachineMaterials()
        {
            if (activeMaterial != null && idleMaterial != null)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                Material targetMaterial = isReady ? idleMaterial : activeMaterial;
                
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.material != targetMaterial)
                    {
                        renderer.material = targetMaterial;
                    }
                }
            }
        }
        
        /// <summary>
        /// Main bowling function
        /// </summary>
        public void BowlBall()
        {
            if (!isReady || isBowling) return;
            
            StartCoroutine(BowlingSequence());
        }
        
        /// <summary>
        /// Complete bowling sequence
        /// </summary>
        IEnumerator BowlingSequence()
        {
            isBowling = true;
            isReady = false;
            
            // Prepare ball
            PrepareBall();
            
            // Calculate bowling parameters
            Vector3 direction = CalculateBowlingDirection();
            float force = CalculateBowlingForce();
            
            // Release ball
            yield return new WaitForSeconds(releaseDelay);
            ReleaseBall(direction, force);
            
            // Wait for ball to complete trajectory
            yield return new WaitForSeconds(5f);
            
            // Reset machine
            ResetMachine();
            
            isBowling = false;
            isReady = true;
            lastBowlTime = Time.time;
        }
        
        /// <summary>
        /// Prepare ball for bowling
        /// </summary>
        void PrepareBall()
        {
            if (ballPrefab != null && ballHolder != null)
            {
                // Create new ball
                currentBall = Instantiate(ballPrefab, ballHolder.position, Quaternion.identity);
                currentBallRigidbody = currentBall.GetComponent<Rigidbody>();
                
                if (currentBallRigidbody != null)
                {
                    currentBallRigidbody.isKinematic = true;
                }
                
                // Position ball at holder
                currentBall.transform.position = ballHolder.position;
            }
        }
        
        /// <summary>
        /// Calculate bowling direction
        /// </summary>
        Vector3 CalculateBowlingDirection()
        {
            Vector3 baseDirection = Vector3.forward; // Default forward direction
            
            if (targetPoint != null)
            {
                baseDirection = (targetPoint.position - releasePoint.position).normalized;
            }
            
            // Apply angle variation
            Vector3 direction = Quaternion.Euler(-bowlAngle, 0, 0) * baseDirection;
            
            // Apply random direction variation
            if (randomizeDirection)
            {
                float randomAngle = Random.Range(-directionVariation, directionVariation);
                direction = Quaternion.Euler(0, randomAngle, 0) * direction;
            }
            
            return direction;
        }
        
        /// <summary>
        /// Calculate bowling force
        /// </summary>
        float CalculateBowlingForce()
        {
            float baseForce = bowlForce;
            
            // Add some variation to make it more realistic
            float variation = Random.Range(0.9f, 1.1f);
            
            return baseForce * variation;
        }
        
        /// <summary>
        /// Release the ball with calculated parameters
        /// </summary>
        void ReleaseBall(Vector3 direction, float force)
        {
            if (currentBall == null || currentBallRigidbody == null) return;
            
            // Position ball at release point
            currentBall.transform.position = releasePoint.position;
            
            // Enable physics
            currentBallRigidbody.isKinematic = false;
            
            // Apply force
            currentBallRigidbody.AddForce(direction * force, ForceMode.Impulse);
            
            // Trigger effects
            if (releaseEffect != null)
            {
                releaseEffect.Play();
            }
            
            if (bowlSound != null)
            {
                bowlSound.Play();
            }
            
            // Trigger event
            OnBallReleased?.Invoke(currentBall);
            
            // Start trajectory tracking
            StartCoroutine(TrackBallTrajectory());
        }
        
        /// <summary>
        /// Track ball trajectory and detect hits/misses
        /// </summary>
        IEnumerator TrackBallTrajectory()
        {
            float elapsed = 0f;
            float maxTime = 10f; // Maximum tracking time
            
            while (elapsed < maxTime && currentBall != null)
            {
                elapsed += Time.deltaTime;
                
                // Check if ball hit target
                if (targetPoint != null)
                {
                    float distance = Vector3.Distance(currentBall.transform.position, targetPoint.position);
                    if (distance < 0.5f) // Hit threshold
                    {
                        OnBallHitTarget?.Invoke(currentBall);
                        break;
                    }
                }
                
                // Check if ball is out of bounds or stopped
                if (currentBallRigidbody != null)
                {
                    if (currentBallRigidbody.linearVelocity.magnitude < 0.1f)
                    {
                        OnBallMissed?.Invoke(currentBall);
                        break;
                    }
                }
                
                yield return null;
            }
            
            // Clean up ball
            if (currentBall != null)
            {
                Destroy(currentBall, 2f);
            }
        }
        
        /// <summary>
        /// Update trajectory prediction
        /// </summary>
        void UpdateTrajectoryPrediction()
        {
            if (trajectoryLine == null || !trajectoryLine.enabled) return;
            
            Vector3 direction = CalculateBowlingDirection();
            float force = CalculateBowlingForce();
            
            Vector3 pos = releasePoint.position;
            Vector3 vel = direction * force;
            float timeStep = predictionTimeStep;
            
            for (int i = 0; i < predictionSteps; i++)
            {
                trajectoryLine.SetPosition(i, pos);
                
                // Apply physics
                vel.y -= 9.81f * timeStep; // Gravity
                pos += vel * timeStep;
                
                if (pos.y < 0) break; // Stop at ground
            }
        }
        
        /// <summary>
        /// Reset the bowling machine
        /// </summary>
        public void ResetMachine()
        {
            isReady = true;
            isBowling = false;
            
            // Clean up current ball
            if (currentBall != null)
            {
                Destroy(currentBall);
                currentBall = null;
                currentBallRigidbody = null;
            }
            
            // Reset ball holder position
            if (ballHolder != null)
            {
                ballHolder.position = originalBallPosition;
            }
        }
        
        /// <summary>
        /// Toggle trajectory prediction visibility
        /// </summary>
        void ToggleTrajectory()
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = !trajectoryLine.enabled;
            }
        }
        
        /// <summary>
        /// Set target point for bowling
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            targetPoint = newTarget;
        }
        
        /// <summary>
        /// Set bowling force
        /// </summary>
        public void SetBowlingForce(float force)
        {
            bowlForce = Mathf.Clamp(force, 10f, 50f);
        }
        
        /// <summary>
        /// Set bowling angle
        /// </summary>
        public void SetBowlingAngle(float angle)
        {
            bowlAngle = Mathf.Clamp(angle, 0f, 45f);
        }
        
        /// <summary>
        /// Toggle auto bowling
        /// </summary>
        public void ToggleAutoBowling()
        {
            autoBowl = !autoBowl;
        }
        
        /// <summary>
        /// Get machine status
        /// </summary>
        public string GetMachineStatus()
        {
            return $"Status: {(isReady ? "Ready" : "Bowling")}\n" +
                   $"Force: {bowlForce:F1} N\n" +
                   $"Angle: {bowlAngle:F1}°\n" +
                   $"Auto Bowl: {(autoBowl ? "On" : "Off")}\n" +
                   $"Last Bowl: {Time.time - lastBowlTime:F1}s ago";
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
        /// Context menu function to reset machine
        /// </summary>
        [ContextMenu("Reset Machine")]
        void ResetMachineContext()
        {
            ResetMachine();
        }
        
        /// <summary>
        /// Context menu function to toggle trajectory
        /// </summary>
        [ContextMenu("Toggle Trajectory")]
        void ToggleTrajectoryContext()
        {
            ToggleTrajectory();
        }
        
        /// <summary>
        /// Context menu function to toggle auto bowling
        /// </summary>
        [ContextMenu("Toggle Auto Bowling")]
        void ToggleAutoBowlingContext()
        {
            ToggleAutoBowling();
        }
    }
}
