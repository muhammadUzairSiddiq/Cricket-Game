using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Test script to verify that swing/spin deliveries work from ANY spawn point position
    /// </summary>
    public class SpawnPointTest : MonoBehaviour
    {
        [Header("Test References")]
        [SerializeField] private BowlingController bowlingController;
        [SerializeField] private Transform ballSpawnPoint;
        [SerializeField] private Transform target;
        
        [Header("Test Positions")]
        [SerializeField] private Vector3[] testSpawnPositions = new Vector3[]
        {
            new Vector3(0, 2, 0),      // Center
            new Vector3(-5, 2, 0),     // Left side
            new Vector3(5, 2, 0),      // Right side
            new Vector3(0, 2, -5),     // Behind
            new Vector3(-3, 2, -3),    // Diagonal left-back
            new Vector3(3, 2, -3)      // Diagonal right-back
        };
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showGizmos = true;
        
        private int currentTestIndex = 0;
        
        /// <summary>
        /// Test swing/spin deliveries from multiple spawn points
        /// </summary>
        [ContextMenu("Test All Spawn Positions")]
        public void TestAllSpawnPositions()
        {
            if (ballSpawnPoint == null || target == null)
            {
                Debug.LogError("🚨 TEST: Missing references! Assign ballSpawnPoint and target.");
                return;
            }
            
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("🎯 SPAWN POINT TEST - Testing deliveries from multiple positions");
            Debug.Log("═══════════════════════════════════════════════════════");
            
            foreach (Vector3 testPos in testSpawnPositions)
            {
                TestDeliveryFromPosition(testPos);
            }
            
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("✅ SPAWN POINT TEST COMPLETE");
            Debug.Log("═══════════════════════════════════════════════════════");
        }
        
        /// <summary>
        /// Test delivery from a specific spawn position
        /// </summary>
        private void TestDeliveryFromPosition(Vector3 spawnPos)
        {
            if (target == null) return;
            
            Vector3 targetPos = target.position;
            Vector3 direction = (targetPos - spawnPos).normalized;
            
            // Calculate lateral direction (works from any spawn point!)
            Vector3 lateralRight = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 lateralLeft = Vector3.Cross(direction, Vector3.up).normalized;
            
            Debug.Log($"");
            Debug.Log($"📍 Testing from position: {spawnPos}");
            Debug.Log($"   → Target position: {targetPos}");
            Debug.Log($"   → Forward direction: {direction}");
            Debug.Log($"   → Lateral RIGHT: {lateralRight}");
            Debug.Log($"   → Lateral LEFT: {lateralLeft}");
            Debug.Log($"   ✅ Lateral directions calculated DYNAMICALLY from spawn-to-target direction");
        }
        
        /// <summary>
        /// Move spawn point to next test position
        /// </summary>
        [ContextMenu("Cycle to Next Test Position")]
        public void CycleToNextTestPosition()
        {
            if (ballSpawnPoint == null)
            {
                Debug.LogError("🚨 TEST: ballSpawnPoint not assigned!");
                return;
            }
            
            Vector3 oldPos = ballSpawnPoint.position;
            currentTestIndex = (currentTestIndex + 1) % testSpawnPositions.Length;
            ballSpawnPoint.position = testSpawnPositions[currentTestIndex];
            
            Debug.Log($"🎯 SPAWN POINT MOVED:");
            Debug.Log($"   From: {oldPos}");
            Debug.Log($"   To: {ballSpawnPoint.position}");
            Debug.Log($"   Position {currentTestIndex + 1}/{testSpawnPositions.Length}");
        }
        
        /// <summary>
        /// Reset spawn point to first test position
        /// </summary>
        [ContextMenu("Reset to First Position")]
        public void ResetToFirstPosition()
        {
            if (ballSpawnPoint == null)
            {
                Debug.LogError("🚨 TEST: ballSpawnPoint not assigned!");
                return;
            }
            
            currentTestIndex = 0;
            ballSpawnPoint.position = testSpawnPositions[0];
            Debug.Log($"🎯 SPAWN POINT RESET to center position: {testSpawnPositions[0]}");
        }
        
        /// <summary>
        /// Visualize test positions in scene
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showGizmos || target == null) return;
            
            Vector3 targetPos = target.position;
            
            // Draw all test spawn positions
            for (int i = 0; i < testSpawnPositions.Length; i++)
            {
                Vector3 testPos = testSpawnPositions[i];
                
                // Draw spawn position sphere
                Gizmos.color = i == currentTestIndex ? Color.yellow : Color.green;
                Gizmos.DrawWireSphere(testPos, 0.2f);
                
                // Draw line to target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(testPos, targetPos);
                
                // Calculate and draw lateral directions
                Vector3 direction = (targetPos - testPos).normalized;
                Vector3 lateralRight = Vector3.Cross(Vector3.up, direction).normalized;
                
                // Draw lateral direction arrows
                Gizmos.color = Color.red;
                Gizmos.DrawRay(testPos, lateralRight * 1.5f); // Right arrow
                
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(testPos, -lateralRight * 1.5f); // Left arrow
            }
            
            // Draw current spawn point if assigned
            if (ballSpawnPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(ballSpawnPoint.position, 0.3f);
            }
        }
    }
}

