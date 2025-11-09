using UnityEngine;
using CricketGame.UI;

namespace CricketGame.UI
{
    /// <summary>
    /// Simple test script to test Loading Panel functionality
    /// Add this to any GameObject (like a button or empty GameObject) to test
    /// </summary>
    public class LoadingPanelTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private float testDuration = 0.5f;
        [SerializeField] private bool autoTestOnStart = false;
        [SerializeField] private float autoTestDelay = 2f;
        
        void Start()
        {
            if (autoTestOnStart)
            {
                Invoke(nameof(RunTestSequence), autoTestDelay);
            }
        }
        
        /// <summary>
        /// Test the loading panel pulse animation
        /// </summary>
        [ContextMenu("Test Loading Panel Pulse")]
        public void RunTestSequence()
        {
            if (LoadingPanelManager.Instance == null)
            {
                return;
            }
            LoadingPanelManager.StartPulse();
        }
        
        /// <summary>
        /// Test via keyboard - Press L to start pulse
        /// </summary>
        void Update()
        {
            // Press 'L' to start one pulse cycle (completes and stops automatically)
            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadingPanelManager.StartPulse();
            }
        }
    }
}

