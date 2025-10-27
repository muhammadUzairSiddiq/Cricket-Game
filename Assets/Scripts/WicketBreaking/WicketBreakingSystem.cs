using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Wicket Breaking System for Cricket Game
    /// Handles realistic wicket breaking when ball hits the wicket
    /// </summary>
    public class WicketBreakingSystem : MonoBehaviour
    {
        [Header("Wicket Components")]
        [SerializeField] private Transform[] wicketStumps; // Array of stump GameObjects
        [SerializeField] private Transform[] wicketBails; // Array of bail GameObjects
        
        [ContextMenu("Test Break Wicket")]
        public void TestBreakWicket()
        {
            Debug.Log("🎳 TEST: Manually breaking wicket...");
            BreakWicket(Vector3.forward * 10f, transform.position);
        }
        
        [Header("Breaking Settings")]
        [SerializeField] private float breakForce = 10f; // Force applied to break wicket
        [SerializeField] private float breakTorque = 5f; // Torque for rotation effect
        [SerializeField] private float breakDelay = 0.1f; // Delay before breaking
        [SerializeField] private float speedForAllStumpsBreak = 14f; // Speed threshold: if ball speed > this, break all stumps
        [SerializeField] private float severeBreakForce = 20f; // Higher force for severe breaks
        
        [Header("Physics Settings")]
        [SerializeField] private float bailLifetime = 3f; // How long bails stay in scene
        [SerializeField] private float stumpLifetime = 5f; // How long stumps stay in scene
        [SerializeField] private bool enableGravity = true; // Enable gravity on broken pieces
        
        [Header("Effects")]
        [SerializeField] private GameObject breakEffectPrefab; // Particle effect for breaking
        [SerializeField] private AudioClip breakSound; // Sound effect for breaking
        
        private bool isBroken = false;
        private AudioSource audioSource;
        
        // Store original positions and rotations for reset
        private Vector3[] originalStumpPositions;
        private Quaternion[] originalStumpRotations;
        private Vector3[] originalBailPositions;
        private Quaternion[] originalBailRotations;
        private bool positionsSaved = false;
        
        void Start()
        {
            SetupWicketComponents();
            SetupAudioSource();
            SaveOriginalPositions();
        }
        
        /// <summary>
        /// Save original positions and rotations of wicket components
        /// </summary>
        void SaveOriginalPositions()
        {
            if (positionsSaved) return;
            
            // Save stump positions
            if (wicketStumps != null && wicketStumps.Length > 0)
            {
                originalStumpPositions = new Vector3[wicketStumps.Length];
                originalStumpRotations = new Quaternion[wicketStumps.Length];
                
                for (int i = 0; i < wicketStumps.Length; i++)
                {
                    if (wicketStumps[i] != null)
                    {
                        originalStumpPositions[i] = wicketStumps[i].position;
                        originalStumpRotations[i] = wicketStumps[i].rotation;
                    }
                }
            }
            
            // Save bail positions
            if (wicketBails != null && wicketBails.Length > 0)
            {
                originalBailPositions = new Vector3[wicketBails.Length];
                originalBailRotations = new Quaternion[wicketBails.Length];
                
                for (int i = 0; i < wicketBails.Length; i++)
                {
                    if (wicketBails[i] != null)
                    {
                        originalBailPositions[i] = wicketBails[i].position;
                        originalBailRotations[i] = wicketBails[i].rotation;
                    }
                }
            }
            
            positionsSaved = true;
        }
        
        /// <summary>
        /// Setup wicket components automatically if not assigned
        /// </summary>
        void SetupWicketComponents()
        {
            if (wicketStumps == null || wicketStumps.Length == 0)
            {
                // Find all child objects that might be stumps
                Transform[] children = GetComponentsInChildren<Transform>();
                System.Collections.Generic.List<Transform> stumps = new System.Collections.Generic.List<Transform>();
                
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("stump") || 
                        child.name.ToLower().Contains("cylinder") ||
                        child.name.ToLower().Contains("wicket"))
                    {
                        stumps.Add(child);
                    }
                }
                wicketStumps = stumps.ToArray();
            }
            
            if (wicketBails == null || wicketBails.Length == 0)
            {
                // Find all child objects that might be bails
                Transform[] children = GetComponentsInChildren<Transform>();
                System.Collections.Generic.List<Transform> bails = new System.Collections.Generic.List<Transform>();
                
                foreach (Transform child in children)
                {
                    if (child.name.ToLower().Contains("bail") || 
                        child.name.ToLower().Contains("top"))
                    {
                        bails.Add(child);
                    }
                }
                wicketBails = bails.ToArray();
            }
        }
        
        /// <summary>
        /// Setup audio source for break sound
        /// </summary>
        void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }
        
        /// <summary>
        /// Break the wicket when ball hits it
        /// </summary>
        public void BreakWicket(Vector3 ballVelocity, Vector3 hitPoint)
        {
            // Only break if not already broken (prevents multiple breaks on same wicket state)
            if (isBroken)
            {
                Debug.Log("🎳 Wicket already broken, waiting for reset...");
                return;
            }
            
            Debug.Log($"🎳 BREAKING WICKET! Ball speed: {ballVelocity.magnitude}, Hit point: {hitPoint}");
            isBroken = true;
            
            // Play break sound
            if (breakSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(breakSound);
            }
            
            // Spawn break effect
            if (breakEffectPrefab != null)
            {
                GameObject effect = Instantiate(breakEffectPrefab, hitPoint, Quaternion.identity);
                Destroy(effect, 2f); // Destroy effect after 2 seconds
            }
            
            // Get ball speed
            float ballSpeed = ballVelocity.magnitude;
            bool breakAllStumps = ballSpeed > speedForAllStumpsBreak;
            
            Debug.Log($"🎳 Ball speed: {ballSpeed:F2}, Threshold: {speedForAllStumpsBreak}, Breaking all: {breakAllStumps}");
            
            // ALWAYS break bails first (they should fly off)
            BreakBails(ballVelocity, hitPoint);
            
            // SIMPLE AND RELIABLE SYSTEM:
            // - If speed > threshold: Break ALL stumps (hard hit = total destruction)
            // - If speed <= threshold: Break ONLY the hit stump (precise hit)
            // This works for ANY length (yorker, full, etc.) because it's speed-based, not length-based
            if (breakAllStumps)
            {
                Debug.Log($"🎳 BREAKING ALL STUMPS (Speed: {ballSpeed:F2} > {speedForAllStumpsBreak})");
                BreakAllStumpsWithForce(ballVelocity, hitPoint, true);
            }
            else
            {
                Debug.Log($"🎳 BREAKING HIT STUMP ONLY (Speed: {ballSpeed:F2} <= {speedForAllStumpsBreak})");
                BreakOnlyHitStump(ballVelocity, hitPoint);
            }
        }
        
        /// <summary>
        /// Break the bails (top pieces) - they always fly off
        /// </summary>
        void BreakBails(Vector3 ballVelocity, Vector3 hitPoint)
        {
            foreach (Transform bail in wicketBails)
            {
                if (bail == null) continue;
                
                Debug.Log($"🎳 Breaking bail: {bail.name}");
                
            // Add Rigidbody if not present
            Rigidbody bailRb = bail.GetComponent<Rigidbody>();
            if (bailRb == null)
            {
                bailRb = bail.gameObject.AddComponent<Rigidbody>();
            }
            
            // CRITICAL: Disable kinematic to allow physics when breaking
            bailRb.isKinematic = false;
                
                // Configure rigidbody
                bailRb.useGravity = enableGravity;
                bailRb.mass = 0.1f; // Light mass for bails
                
                // Calculate break direction and force
                Vector3 breakDirection = (bail.position - hitPoint).normalized;
                if (breakDirection == Vector3.zero)
                {
                    breakDirection = Vector3.up + Random.insideUnitSphere * 0.5f;
                }
                
            // Apply force to break the bail
            bailRb.AddForce(breakDirection * breakForce, ForceMode.Impulse);
            bailRb.AddTorque(Random.insideUnitSphere * breakTorque, ForceMode.Impulse);
            
            Debug.Log($"🎳 Applied force to bail: {breakDirection * breakForce}");
            
            // Reset bail position after lifetime instead of destroying (use stump lifetime for both to synchronize)
            StartCoroutine(ResetBailAfterDelay(bail, stumpLifetime));
            }
        }
        
        /// <summary>
        /// Break ONLY the stump that was hit (speed <= 14)
        /// </summary>
        void BreakOnlyHitStump(Vector3 ballVelocity, Vector3 hitPoint)
        {
            // Find which stump was actually hit by checking distances
            Transform hitStump = null;
            float minDistance = float.MaxValue;
            int hitIndex = -1;
            int count = 0;
            
            Debug.Log($"🎳 Finding hit stump among {wicketStumps.Length} stumps...");
            Debug.Log($"🎳 Hit point: {hitPoint}");
            
            // Loop through all stumps to find the closest (actually hit)
            foreach (Transform stump in wicketStumps)
            {
                if (stump == null)
                {
                    Debug.LogWarning($"🎳 Stump {count} is NULL!");
                    count++;
                    continue;
                }
                
                float distance = Vector3.Distance(hitPoint, stump.position);
                Debug.Log($"🎳 Stump {count}: {stump.name} at {stump.position}, Distance: {distance:F2}");
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    hitStump = stump;
                    hitIndex = count;
                }
                count++;
            }
            
            // Break ONLY the stump that was hit with normal force
            if (hitStump != null)
            {
                Debug.Log($"🎳 ✅ HIT STUMP FOUND: {hitStump.name} (Index: {hitIndex}), Distance: {minDistance:F2}");
                BreakSingleStumpWithForce(hitStump, ballVelocity, hitPoint, false);
            }
            else
            {
                Debug.LogError("🎳 ❌ No stump found to break!");
            }
        }
        
        /// <summary>
        /// Break all stumps when speed is very high - SIMPLE AND RELIABLE
        /// </summary>
        void BreakAllStumpsWithForce(Vector3 ballVelocity, Vector3 hitPoint, bool severe)
        {
            Debug.Log($"🎳 Breaking ALL 3 stumps with {(severe ? "SEVERE" : "NORMAL")} force!");
            
            // Break all stumps with higher force for severe breaks
            foreach (Transform stump in wicketStumps)
            {
                if (stump != null)
                {
                    BreakSingleStumpWithForce(stump, ballVelocity, hitPoint, severe);
                }
            }
        }
        
        /// <summary>
        /// Break a single stump with force option
        /// </summary>
        void BreakSingleStumpWithForce(Transform stump, Vector3 ballVelocity, Vector3 hitPoint, bool severe)
        {
            if (stump == null)
            {
                Debug.LogError($"🎳 Cannot break - stump is null!");
                return;
            }
            
            Debug.Log($"🎳 🎳 BREAKING STUMP: {stump.name} 🎳 🎳");
            Debug.Log($"🎳 Stump position: {stump.position}");
            Debug.Log($"🎳 Hit point: {hitPoint}");
            
            // Add Rigidbody if not present
            Rigidbody stumpRb = stump.GetComponent<Rigidbody>();
            if (stumpRb == null)
            {
                stumpRb = stump.gameObject.AddComponent<Rigidbody>();
                Debug.Log($"🎳 Added Rigidbody to {stump.name}");
            }
            
            // CRITICAL: Disable kinematic to allow physics when breaking
            stumpRb.isKinematic = false;
            Debug.Log($"🎳 Disabled kinematic for {stump.name}");
            
            // Configure rigidbody
            stumpRb.useGravity = enableGravity;
            stumpRb.mass = 1f; // Heavier mass for stumps
            
            // Calculate break direction (mostly upward and slightly random)
            Vector3 breakDirection = Vector3.up + Random.insideUnitSphere * 0.3f;
            breakDirection.y = Mathf.Abs(breakDirection.y); // Ensure upward force
            
            // Use severe force if breaking all stumps (high speed)
            float force = severe ? severeBreakForce : breakForce;
            float torque = severe ? breakTorque * 1.5f : breakTorque;
            
            // Apply force to break the stump
            Vector3 appliedForce = breakDirection * force * 0.7f;
            stumpRb.AddForce(appliedForce, ForceMode.Impulse);
            stumpRb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
            
            Debug.Log($"🎳 Applied {(severe ? "SEVERE" : "NORMAL")} force to {stump.name}: {appliedForce}");
            Debug.Log($"🎳 Stump {stump.name} should now break and fall!");
            
            // Reset stump position after lifetime instead of destroying
            StartCoroutine(ResetStumpAfterDelay(stump, stumpLifetime));
        }
        
        /// <summary>
        /// Reset bail to original position
        /// </summary>
        System.Collections.IEnumerator ResetBailAfterDelay(Transform bail, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (bail != null)
            {
                // Find original position
                int index = System.Array.IndexOf(wicketBails, bail);
                if (index >= 0 && originalBailPositions != null && index < originalBailPositions.Length)
                {
                    // Simply reset position - colliders handle ground collision
                    bail.position = originalBailPositions[index];
                    bail.rotation = originalBailRotations[index];
                    
                    // Remove Rigidbody to return to original state
                    Rigidbody bailRb = bail.GetComponent<Rigidbody>();
                    if (bailRb != null)
                    {
                        Destroy(bailRb);
                        Debug.Log($"🎳 Removed Rigidbody from bail {index}");
                    }
                    
                    Debug.Log($"🎳 Reset bail {index} to position: {originalBailPositions[index]}");
                }
            }
        }
        
        /// <summary>
        /// Reset stump to original position
        /// </summary>
        System.Collections.IEnumerator ResetStumpAfterDelay(Transform stump, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (stump != null)
            {
                // Find original position
                int index = System.Array.IndexOf(wicketStumps, stump);
                if (index >= 0 && originalStumpPositions != null && index < originalStumpPositions.Length)
                {
                    // Simply reset position - colliders handle ground collision
                    stump.position = originalStumpPositions[index];
                    stump.rotation = originalStumpRotations[index];
                    
                    // Remove Rigidbody to return to original state
                    Rigidbody stumpRb = stump.GetComponent<Rigidbody>();
                    if (stumpRb != null)
                    {
                        Destroy(stumpRb);
                        Debug.Log($"🎳 Removed Rigidbody from stump {index}");
                    }
                    
                    Debug.Log($"🎳 Reset stump {index} to position: {originalStumpPositions[index]}");
                    
                    // Reset wicket state to allow breaking again
                    isBroken = false;
                }
            }
        }
        
        /// <summary>
        /// Reset wicket to original state and allow breaking again
        /// </summary>
        public void ResetWicket()
        {
            isBroken = false;
            SaveOriginalPositions(); // Re-save positions in case they changed
            
            // Re-enable all wicket components
            if (wicketStumps != null)
            {
                foreach (Transform stump in wicketStumps)
                {
                    if (stump != null)
                    {
                        Rigidbody stumpRb = stump.GetComponent<Rigidbody>();
                        if (stumpRb != null)
                        {
                            stumpRb.isKinematic = false; // Re-enable physics
                        }
                    }
                }
            }
            
            if (wicketBails != null)
            {
                foreach (Transform bail in wicketBails)
                {
                    if (bail != null)
                    {
                        Rigidbody bailRb = bail.GetComponent<Rigidbody>();
                        if (bailRb != null)
                        {
                            bailRb.isKinematic = false; // Re-enable physics
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if wicket is broken
        /// </summary>
        public bool IsBroken()
        {
            return isBroken;
        }
        
        /// <summary>
        /// Draw debug gizmos to show wicket components
        /// </summary>
        void OnDrawGizmos()
        {
            if (wicketStumps != null)
            {
                Gizmos.color = new Color(0.6f, 0.4f, 0.2f); // Brown color
                foreach (Transform stump in wicketStumps)
                {
                    if (stump != null)
                    {
                        Gizmos.DrawWireCube(stump.position, Vector3.one * 0.2f);
                    }
                }
            }
            
            if (wicketBails != null)
            {
                Gizmos.color = Color.white;
                foreach (Transform bail in wicketBails)
                {
                    if (bail != null)
                    {
                        Gizmos.DrawWireCube(bail.position, Vector3.one * 0.1f);
                    }
                }
            }
        }
    }
}
