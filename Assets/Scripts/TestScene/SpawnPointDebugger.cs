using UnityEngine;
using CricketGame;

/// <summary>
/// Debug script to test and verify ball spawn point behavior
/// Helps identify issues with animation-driven vs static spawn points
/// </summary>
public class SpawnPointDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BowlingController bowlingController;
    [SerializeField] private PlayerAnimationController playerAnimationController;
    [SerializeField] private Transform target;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color spawnPointColor = Color.green;
    [SerializeField] private Color animatedSpawnColor = Color.red;
    [SerializeField] private Color targetColor = Color.blue;
    
    private void Start()
    {
        // Auto-find references if not assigned
        if (bowlingController == null)
            bowlingController = FindObjectOfType<BowlingController>();
        
        if (playerAnimationController == null)
            playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        
        if (target == null)
        {
            GameObject targetObj = GameObject.FindWithTag("Target");
            if (targetObj != null)
                target = targetObj.transform;
        }
        
        Debug.Log("🎯 SpawnPointDebugger initialized");
    }
    
    private void Update()
    {
        // Show real-time spawn point positions
        if (enableDebugLogs && Input.GetKeyDown(KeyCode.D))
        {
            DebugSpawnPoints();
        }
        
        // Test ball spawning
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestBallSpawning();
        }
        
        // Test animation event manually
        if (Input.GetKeyDown(KeyCode.Y))
        {
            TestAnimationEvent();
        }
    }
    
    /// <summary>
    /// Debug all spawn point positions and references
    /// </summary>
    [ContextMenu("Debug Spawn Points")]
    public void DebugSpawnPoints()
    {
        Debug.Log("🎯 ========== SPAWN POINT DEBUG ==========");
        
        // Check BowlingController
        if (bowlingController != null)
        {
            Debug.Log($"🎯 BowlingController: {bowlingController.name}");
            
            // Check manual spawn point
            Transform manualSpawn = bowlingController.GetType().GetField("spawnPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bowlingController) as Transform;
            
            if (manualSpawn != null)
            {
                Debug.Log($"🎯 Manual Spawn Point: {manualSpawn.name} at {manualSpawn.position}");
            }
            else
            {
                Debug.LogError("🎯 Manual Spawn Point: NULL");
            }
            
            // Check current bowler spawn position
            Transform currentSpawn = bowlingController.GetCurrentBowlerSpawnPosition();
            if (currentSpawn != null)
            {
                Debug.Log($"🎯 Current Bowler Spawn: {currentSpawn.name} at {currentSpawn.position}");
            }
            else
            {
                Debug.LogError("🎯 Current Bowler Spawn: NULL");
            }
        }
        else
        {
            Debug.LogError("🎯 BowlingController: NULL");
        }
        
        // Check PlayerAnimationController
        if (playerAnimationController != null)
        {
            Debug.Log($"🎯 PlayerAnimationController: {playerAnimationController.name}");
            
            // Check animation spawn point
            Transform animSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (animSpawn != null)
            {
                Debug.Log($"🎯 Animation Spawn Point: {animSpawn.name} at {animSpawn.position}");
            }
            else
            {
                Debug.LogError("🎯 Animation Spawn Point: NULL");
            }
            
            // Check current animated position
            Vector3 animatedPos = playerAnimationController.GetCurrentAnimatedSpawnPosition();
            Debug.Log($"🎯 Current Animated Position: {animatedPos}");
        }
        else
        {
            Debug.LogError("🎯 PlayerAnimationController: NULL");
        }
        
        // Check target
        if (target != null)
        {
            Debug.Log($"🎯 Target: {target.name} at {target.position}");
        }
        else
        {
            Debug.LogError("🎯 Target: NULL");
        }
        
        Debug.Log("🎯 ======================================");
    }
    
    /// <summary>
    /// Test ball spawning to see which spawn point is actually used
    /// </summary>
    [ContextMenu("Test Ball Spawning")]
    public void TestBallSpawning()
    {
        Debug.Log("🎯 ========== TESTING BALL SPAWNING ==========");
        
        if (bowlingController != null)
        {
            // Check if manual input is enabled
            bool manualInput = bowlingController.GetType().GetField("enableManualKeyInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bowlingController) as bool? ?? false;
            
            Debug.Log($"🎯 Manual Input Enabled: {manualInput}");
            
            if (manualInput)
            {
                Debug.Log("🎯 Using MANUAL spawn point (BowlingController.spawnPoint)");
            }
            else
            {
                Debug.Log("🎯 Using ANIMATION spawn point (PlayerAnimationController.animationSpawnPoint)");
                
                if (playerAnimationController != null)
                {
                    Transform animSpawn = playerAnimationController.GetAnimationSpawnPoint();
                    if (animSpawn != null)
                    {
                        Debug.Log($"🎯 Animation spawn point: {animSpawn.name} at {animSpawn.position}");
                    }
                }
            }
            
            // Try to spawn a ball
            bowlingController.InstantiateNewBall();
        }
        
        Debug.Log("🎯 ==========================================");
    }
    
    /// <summary>
    /// Test animation event manually to see if it triggers the correct spawn behavior
    /// </summary>
    [ContextMenu("Test Animation Event")]
    public void TestAnimationEvent()
    {
        Debug.Log("🎯 ========== TESTING ANIMATION EVENT ==========");
        
        if (playerAnimationController != null)
        {
            Debug.Log("🎯 Manually triggering OnBallReleased() animation event...");
            playerAnimationController.OnBallReleased();
        }
        else
        {
            Debug.LogError("🎯 PlayerAnimationController not found!");
        }
        
        Debug.Log("🎯 ============================================");
    }
    
    /// <summary>
    /// Visualize spawn points in Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw manual spawn point
        if (bowlingController != null)
        {
            Transform manualSpawn = bowlingController.GetType().GetField("spawnPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bowlingController) as Transform;
            
            if (manualSpawn != null)
            {
                Gizmos.color = spawnPointColor;
                Gizmos.DrawWireSphere(manualSpawn.position, 0.2f);
                Gizmos.DrawWireCube(manualSpawn.position, Vector3.one * 0.1f);
            }
        }
        
        // Draw animated spawn point
        if (playerAnimationController != null)
        {
            Transform animSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (animSpawn != null)
            {
                Gizmos.color = animatedSpawnColor;
                Gizmos.DrawWireSphere(animSpawn.position, 0.15f);
                Gizmos.DrawWireCube(animSpawn.position, Vector3.one * 0.08f);
            }
        }
        
        // Draw target
        if (target != null)
        {
            Gizmos.color = targetColor;
            Gizmos.DrawWireSphere(target.position, 0.3f);
            Gizmos.DrawWireCube(target.position, Vector3.one * 0.2f);
        }
    }
    
    /// <summary>
    /// Force refresh animation spawn point reference
    /// </summary>
    [ContextMenu("Force Refresh Animation Spawn Point")]
    public void ForceRefreshAnimationSpawnPoint()
    {
        if (playerAnimationController != null)
        {
            playerAnimationController.ForceRefreshSpawnPointReference();
            Debug.Log("🎯 Forced refresh of animation spawn point reference");
        }
        else
        {
            Debug.LogError("🎯 PlayerAnimationController not found!");
        }
    }
    
    /// <summary>
    /// Comprehensive test of spawn point behavior
    /// </summary>
    [ContextMenu("Comprehensive Spawn Point Test")]
    public void ComprehensiveSpawnPointTest()
    {
        Debug.Log("🎯 ========== COMPREHENSIVE SPAWN POINT TEST ==========");
        
        if (playerAnimationController != null)
        {
            Debug.Log("🎯 1. BEFORE REFRESH:");
            Transform beforeSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (beforeSpawn != null)
            {
                Debug.Log($"🎯   Spawn point: {beforeSpawn.name}");
                Debug.Log($"🎯   Position: {beforeSpawn.position}");
                Debug.Log($"🎯   Scene: {beforeSpawn.gameObject.scene.name}");
                Debug.Log($"🎯   Parent: {(beforeSpawn.parent != null ? beforeSpawn.parent.name : "NULL")}");
            }
            
            Debug.Log("🎯 2. FORCING REFRESH:");
            playerAnimationController.ForceRefreshSpawnPointReference();
            
            Debug.Log("🎯 3. AFTER REFRESH:");
            Transform afterSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (afterSpawn != null)
            {
                Debug.Log($"🎯   Spawn point: {afterSpawn.name}");
                Debug.Log($"🎯   Position: {afterSpawn.position}");
                Debug.Log($"🎯   Scene: {afterSpawn.gameObject.scene.name}");
                Debug.Log($"🎯   Parent: {(afterSpawn.parent != null ? afterSpawn.parent.name : "NULL")}");
            }
            
            Debug.Log("🎯 4. TESTING BALL SPAWNING:");
            if (bowlingController != null)
            {
                bowlingController.InstantiateNewBall();
            }
        }
        else
        {
            Debug.LogError("🎯 PlayerAnimationController not found!");
        }
        
        Debug.Log("🎯 ===================================================");
    }
    
    /// <summary>
    /// Clean up all bowlers and test fresh instantiation
    /// </summary>
    [ContextMenu("Clean Up and Test Fresh")]
    public void CleanUpAndTestFresh()
    {
        Debug.Log("🎯 ========== CLEAN UP AND TEST FRESH ==========");
        
        if (bowlingController != null)
        {
            Debug.Log("🎯 1. CLEANING UP ALL EXISTING BOWLERS:");
            bowlingController.CleanUpAllBowlers();
            
            Debug.Log("🎯 2. WAITING ONE FRAME...");
            StartCoroutine(TestAfterCleanup());
        }
        else
        {
            Debug.LogError("🎯 BowlingController not found!");
        }
        
        Debug.Log("🎯 ==============================================");
    }
    
    private System.Collections.IEnumerator TestAfterCleanup()
    {
        yield return null; // Wait one frame
        
        Debug.Log("🎯 3. INSTANTIATING FRESH BOWLER:");
        if (bowlingController != null)
        {
            bowlingController.InstantiateSelectedBowler();
            
            Debug.Log("🎯 4. TESTING BALL SPAWNING:");
            yield return null; // Wait one more frame
            bowlingController.InstantiateNewBall();
        }
    }
}
