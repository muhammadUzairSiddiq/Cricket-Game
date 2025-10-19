using UnityEngine;
using CricketGame;

namespace CricketGame
{
    /// <summary>
    /// Handles user input during gameplay
    /// Manages Space key input to trigger bowler's bowling animation
    /// </summary>
    public class GameplayInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [Tooltip("Enable/disable input handling")]
        public bool enableInput = true;
        
        [Tooltip("Key to trigger bowling animation")]
        public KeyCode bowlingKey = KeyCode.Space;
        
        [Header("Animation Settings")]
        [Tooltip("Animation trigger name in the Animator")]
        public string bowlingAnimationTrigger = "StartBow";
        
        [Header("Debug Settings")]
        [Tooltip("Show debug logs for input events")]
        public bool enableDebugLogs = true;
        
        // Component references
        private BowlingController bowlingController;
        private PlayerAnimationController playerAnimationController;
        private Animator bowlerAnimator;
        
        // Animation state tracking
        private bool isBowlingAnimationPlaying = false;
        private float lastBowlingTime = 0f;
        private float bowlingCooldown = 2f; // Minimum time between bowling attempts
        
        void Awake()
        {
            // Auto-find components
            InitializeReferences();
        }
        
        void Start()
        {
            if (enableDebugLogs)
                Debug.Log("🎮 GameplayInputHandler: Input system initialized");
            
            // Wait a frame for bowler instantiation, then refresh references
            StartCoroutine(RefreshReferencesAfterInstantiation());
        }
        
        /// <summary>
        /// Wait for bowler instantiation then refresh references
        /// </summary>
        private System.Collections.IEnumerator RefreshReferencesAfterInstantiation()
        {
            // Wait a few frames for BowlingController to instantiate the bowler
            yield return new WaitForSeconds(0.5f);
            
            if (enableDebugLogs)
                Debug.Log("🎮 Refreshing references after bowler instantiation...");
            
            InitializeReferences();
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎮 Final reference check:");
                Debug.Log($"🎮   - PlayerAnimationController: {(playerAnimationController != null ? playerAnimationController.name : "NULL")}");
                Debug.Log($"🎮   - Animator: {(bowlerAnimator != null ? bowlerAnimator.name : "NULL")}");
            }
        }
        
        void Update()
        {
            if (!enableInput) return;
            
            HandleBowlingInput();
        }
        
        /// <summary>
        /// Initialize component references
        /// </summary>
        private void InitializeReferences()
        {
            // Find BowlingController
            if (bowlingController == null)
            {
                bowlingController = FindObjectOfType<BowlingController>();
                if (bowlingController != null && enableDebugLogs)
                {
                    Debug.Log($"🎮 Found BowlingController: {bowlingController.name}");
                }
            }
            
            // Get PlayerAnimationController from BowlingController
            if (bowlingController != null)
            {
                playerAnimationController = bowlingController.GetPlayerAnimationController();
                if (playerAnimationController != null && enableDebugLogs)
                {
                    Debug.Log($"🎮 Found PlayerAnimationController: {playerAnimationController.name}");
                    
                    // Get the Animator from the PlayerAnimationController's GameObject
                    bowlerAnimator = playerAnimationController.GetComponent<Animator>();
                    if (bowlerAnimator != null && enableDebugLogs)
                    {
                        Debug.Log($"🎮 Found Animator: {bowlerAnimator.name}");
                    }
                    else
                    {
                        Debug.LogWarning("🎮 No Animator found on PlayerAnimationController GameObject");
                    }
                }
                else
                {
                    Debug.LogWarning("🎮 No PlayerAnimationController found - make sure bowler is instantiated");
                }
            }
            else
            {
                Debug.LogWarning("🎮 No BowlingController found in scene");
            }
        }
        
        /// <summary>
        /// Handle bowling input (Space key)
        /// </summary>
        private void HandleBowlingInput()
        {
            // Check if Space key is pressed
            if (Input.GetKeyDown(bowlingKey))
            {
                // Check cooldown to prevent spam
                if (Time.time - lastBowlingTime < bowlingCooldown)
                {
                    if (enableDebugLogs)
                        Debug.Log($"🎮 Bowling on cooldown - wait {(bowlingCooldown - (Time.time - lastBowlingTime)):F1} seconds");
                    return;
                }
                
                // Check if bowling animation is already playing
                if (isBowlingAnimationPlaying)
                {
                    if (enableDebugLogs)
                        Debug.Log("🎮 Bowling animation already playing - ignoring input");
                    return;
                }
                
                // Trigger bowling animation
                TriggerBowlingAnimation();
            }
        }
        
        /// <summary>
        /// Trigger the bowler's bowling animation
        /// </summary>
        public void TriggerBowlingAnimation()
        {
            if (enableDebugLogs)
                Debug.Log("🎮 === TRIGGERING BOWLING ANIMATION ===");
            
            // Validate components
            if (bowlerAnimator == null)
            {
                Debug.LogError("🎮 ❌ Cannot trigger bowling - no Animator found");
                return;
            }
            
            if (playerAnimationController == null)
            {
                Debug.LogError("🎮 ❌ Cannot trigger bowling - no PlayerAnimationController found");
                return;
            }
            
            // Check if the bowling animation trigger exists
            if (!HasAnimationTrigger(bowlingAnimationTrigger))
            {
                Debug.LogWarning($"🎮 ⚠️ Animation trigger '{bowlingAnimationTrigger}' not found in Animator Controller");
                Debug.LogWarning("🎮 Available triggers:");
                LogAvailableTriggers();
                return;
            }
            
            // Set cooldown
            lastBowlingTime = Time.time;
            isBowlingAnimationPlaying = true;
            
            // Trigger the animation
            bowlerAnimator.SetTrigger(bowlingAnimationTrigger);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎮 ✅ Triggered bowling animation: {bowlingAnimationTrigger}");
                Debug.Log($"🎮 Bowler: {playerAnimationController.gameObject.name}");
                Debug.Log($"🎮 Animation State: Playing");
            }
            
            // Start monitoring animation state
            StartCoroutine(MonitorBowlingAnimation());
        }
        
        /// <summary>
        /// Check if the specified animation trigger exists
        /// </summary>
        private bool HasAnimationTrigger(string triggerName)
        {
            if (bowlerAnimator == null) return false;
            
            // Check if the trigger parameter exists
            foreach (AnimatorControllerParameter param in bowlerAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger && param.name == triggerName)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Log all available animation triggers for debugging
        /// </summary>
        private void LogAvailableTriggers()
        {
            if (bowlerAnimator == null) return;
            
            foreach (AnimatorControllerParameter param in bowlerAnimator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    Debug.Log($"🎮   - {param.name}");
                }
            }
        }
        
        /// <summary>
        /// Monitor bowling animation state
        /// </summary>
        private System.Collections.IEnumerator MonitorBowlingAnimation()
        {
            if (enableDebugLogs)
                Debug.Log("🎮 Monitoring bowling animation state...");
            
            // Wait for animation to start
            yield return new WaitForEndOfFrame();
            
            // Monitor until animation is no longer playing
            while (isBowlingAnimationPlaying)
            {
                // Check if the bowling animation is still playing
                AnimatorStateInfo stateInfo = bowlerAnimator.GetCurrentAnimatorStateInfo(0);
                
                // You can check for specific animation state names here if needed
                // For now, we'll use a simple timeout
                if (Time.time - lastBowlingTime > 10f) // 10 second timeout
                {
                    if (enableDebugLogs)
                        Debug.Log("🎮 Bowling animation timeout - resetting state");
                    break;
                }
                
                yield return new WaitForSeconds(0.1f); // Check every 100ms
            }
            
            // Reset bowling state
            isBowlingAnimationPlaying = false;
            if (enableDebugLogs)
                Debug.Log("🎮 Bowling animation completed - ready for next input");
        }
        
        /// <summary>
        /// Force reset bowling animation state (for debugging)
        /// </summary>
        [ContextMenu("Force Reset Bowling State")]
        public void ForceResetBowlingState()
        {
            isBowlingAnimationPlaying = false;
            lastBowlingTime = 0f;
            if (enableDebugLogs)
                Debug.Log("🎮 ✅ Bowling state force reset");
        }
        
        /// <summary>
        /// Check input system status
        /// </summary>
        [ContextMenu("Check Input System Status")]
        public void CheckInputSystemStatus()
        {
            Debug.Log("🎮 === INPUT SYSTEM STATUS ===");
            Debug.Log($"🎮 Input Enabled: {enableInput}");
            Debug.Log($"🎮 Bowling Key: {bowlingKey}");
            Debug.Log($"🎮 Animation Trigger: {bowlingAnimationTrigger}");
            Debug.Log($"🎮 Bowling Controller: {(bowlingController != null ? bowlingController.name : "NULL")}");
            Debug.Log($"🎮 Player Animation Controller: {(playerAnimationController != null ? playerAnimationController.name : "NULL")}");
            Debug.Log($"🎮 Animator: {(bowlerAnimator != null ? bowlerAnimator.name : "NULL")}");
            Debug.Log($"🎮 Is Bowling Animation Playing: {isBowlingAnimationPlaying}");
            Debug.Log($"🎮 Last Bowling Time: {lastBowlingTime}");
            Debug.Log($"🎮 Cooldown Remaining: {(Time.time - lastBowlingTime < bowlingCooldown ? (bowlingCooldown - (Time.time - lastBowlingTime)).ToString("F1") + "s" : "Ready")}");
            Debug.Log("🎮 =========================");
        }
        
        /// <summary>
        /// Manually trigger bowling (for testing)
        /// </summary>
        [ContextMenu("Manual Trigger Bowling")]
        public void ManualTriggerBowling()
        {
            if (enableDebugLogs)
                Debug.Log("🎮 Manual bowling trigger activated");
            TriggerBowlingAnimation();
        }
        
        /// <summary>
        /// Refresh component references (for when bowler is instantiated)
        /// </summary>
        [ContextMenu("Refresh Component References")]
        public void RefreshComponentReferences()
        {
            if (enableDebugLogs)
                Debug.Log("🎮 Refreshing component references...");
            
            InitializeReferences();
            
            if (enableDebugLogs)
                Debug.Log("🎮 ✅ Component references refreshed");
        }
    }
}
