using UnityEngine;
using UnityEngine.UI;

namespace CricketGame
{
    public class DeliverySystemSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BowlingController bowlingController;
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
			if (bowlingController == null || deliverySystem == null)
			{
				return;
			}
		}
        
        /// <summary>
        /// Setup UI buttons to switch delivery types
        /// </summary>
        private void SetupUIButtons()
        {
			if (bowlingController == null)
			{
				return;
			}
            
            // Setup Flat Delivery Button
			if (flatDeliveryButton != null)
			{
				flatDeliveryButton.onClick.AddListener(() => {
					bowlingController.SwitchToFlatDelivery();
				});
			}
            
            // Setup Seam In Button
			if (seamInButton != null)
			{
				seamInButton.onClick.AddListener(() => {
					bowlingController.SwitchToInSwingDelivery();
				});
			}

			if (seamOutButton != null)
			{
				seamOutButton.onClick.AddListener(() => {
					bowlingController.SwitchToOutSwingDelivery();
				});
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
			if (deliverySystem == null)
			{
				return;
			}
        }
    }
}
