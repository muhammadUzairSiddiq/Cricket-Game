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
            
            InitializeReferences();
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
			if (bowlingController == null)
			{
				bowlingController = FindObjectOfType<BowlingController>();
			}

			if (bowlingController != null)
			{
				playerAnimationController = bowlingController.GetPlayerAnimationController();
				if (playerAnimationController != null)
				{
					bowlerAnimator = playerAnimationController.GetComponent<Animator>();
				}
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
                    return;
                }
                
                // Check if bowling animation is already playing
                if (isBowlingAnimationPlaying)
                {
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
			if (bowlerAnimator == null)
			{
				return;
			}

			if (playerAnimationController == null)
			{
				return;
			}

			if (!HasAnimationTrigger(bowlingAnimationTrigger))
			{
				LogAvailableTriggers();
				return;
			}

			lastBowlingTime = Time.time;
			isBowlingAnimationPlaying = true;
			bowlerAnimator.SetTrigger(bowlingAnimationTrigger);

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
		}
        
        /// <summary>
        /// Monitor bowling animation state
        /// </summary>
        private System.Collections.IEnumerator MonitorBowlingAnimation()
        {
            
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
                    break;
                }
                
                yield return new WaitForSeconds(0.1f); // Check every 100ms
            }
            
            // Reset bowling state
            isBowlingAnimationPlaying = false;
        }
        
        /// <summary>
        /// Force reset bowling animation state (for debugging)
        /// </summary>
        [ContextMenu("Force Reset Bowling State")]
        public void ForceResetBowlingState()
        {
            isBowlingAnimationPlaying = false;
            lastBowlingTime = 0f;
        }
        
        /// <summary>
        /// Check input system status
        /// </summary>
        [ContextMenu("Check Input System Status")]
        public void CheckInputSystemStatus()
        {

        }
        
        /// <summary>
        /// Manually trigger bowling (for testing)
        /// </summary>
        [ContextMenu("Manual Trigger Bowling")]
        public void ManualTriggerBowling()
        {
            TriggerBowlingAnimation();
        }
        
        /// <summary>
        /// Refresh component references (for when bowler is instantiated)
        /// </summary>
        [ContextMenu("Refresh Component References")]
        public void RefreshComponentReferences()
        {
            
            InitializeReferences();
        }
    }
}
