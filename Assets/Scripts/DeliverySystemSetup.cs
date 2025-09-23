using UnityEngine;
using UnityEngine.UI;

namespace CricketGame
{
    public class DeliverySystemSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ContinuousBowlingTest_WithBounce bowlingController;
        [SerializeField] private DeliverySystem deliverySystem;
        
        [Header("UI Buttons")]
        [SerializeField] private Button flatDeliveryButton;
        [UnityEngine.Serialization.FormerlySerializedAs("inSwingButton")]
        [SerializeField] private Button seamInButton;
        [UnityEngine.Serialization.FormerlySerializedAs("outSwingButton")]
        [SerializeField] private Button seamOutButton;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            SetupDeliverySystem();
            SetupUIButtons();
        }
        
        /// <summary>
        /// Connect delivery system to bowling controller
        /// </summary>
        private void SetupDeliverySystem()
        {
            if (bowlingController != null && deliverySystem != null)
            {
                // This will be set via inspector, but we can verify the connection
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 SETUP: DeliverySystem connected to BowlingController");
                    Debug.Log($"🎯 SETUP: Current delivery type: {deliverySystem.GetCurrentDeliveryType()}");
                }
            }
            else
            {
                Debug.LogError("🎯 SETUP: Missing references! Please assign BowlingController and DeliverySystem in inspector.");
            }
        }
        
        /// <summary>
        /// Setup UI buttons to switch delivery types
        /// </summary>
        private void SetupUIButtons()
        {
            if (bowlingController == null)
            {
                Debug.LogError("🎯 SETUP: BowlingController not assigned!");
                return;
            }
            
            // Setup Flat Delivery Button
            if (flatDeliveryButton != null)
            {
                flatDeliveryButton.onClick.AddListener(() => {
                    bowlingController.SwitchToFlatDelivery();
                    if (showDebugLogs) Debug.Log("🎯 SETUP: Flat delivery button clicked");
                });
            }
            
            // Setup Seam In Button
            if (seamInButton != null)
            {
                seamInButton.onClick.AddListener(() => {
                    bowlingController.SwitchToInSwingDelivery();
                    if (showDebugLogs) Debug.Log("🎯 SETUP: Seam in button clicked");
                });
            }
            
            // Setup Seam Out Button
            if (seamOutButton != null)
            {
                seamOutButton.onClick.AddListener(() => {
                    bowlingController.SwitchToOutSwingDelivery();
                    if (showDebugLogs) Debug.Log("🎯 SETUP: Seam out button clicked");
                });
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 SETUP: UI buttons configured - Flat: {flatDeliveryButton != null}, Seam In: {seamInButton != null}, Seam Out: {seamOutButton != null}");
            }
        }
        
        /// <summary>
        /// Manual setup method for testing
        /// </summary>
        [ContextMenu("Setup Delivery System")]
        public void ManualSetup()
        {
            SetupDeliverySystem();
            SetupUIButtons();
        }
        
        /// <summary>
        /// Test speed boost with current delivery type
        /// </summary>
        [ContextMenu("Test Speed Boost")]
        public void TestSpeedBoost()
        {
            if (deliverySystem != null)
            {
                Debug.Log($"🎯 TEST: Current delivery type: {deliverySystem.GetCurrentDeliveryType()}");
                Debug.Log($"🎯 TEST: Speed boost should work with this delivery type");
            }
            else
            {
                Debug.LogError("🎯 TEST: DeliverySystem not assigned!");
            }
        }
    }
}
