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
        
        // Check BowlingController
        if (bowlingController != null)
        {
            
            // Check manual spawn point
            Transform manualSpawn = bowlingController.GetType().GetField("spawnPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bowlingController) as Transform;
            
            if (manualSpawn != null)
            {
            }
            else
            {
            }
            
            // Check current bowler spawn position
            Transform currentSpawn = bowlingController.GetCurrentBowlerSpawnPosition();
            if (currentSpawn != null)
            {
            }
            else
            {
            }
        }
        else
        {
        }
        
        // Check PlayerAnimationController
        if (playerAnimationController != null)
        {
            
            // Check animation spawn point
            Transform animSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (animSpawn != null)
            {
            }
            else
            {
            }
            
            // Check current animated position
            Vector3 animatedPos = playerAnimationController.GetCurrentAnimatedSpawnPosition();
        }
        else
        {
        }
        
        // Check target
        if (target != null)
        {
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Test ball spawning to see which spawn point is actually used
    /// </summary>
    [ContextMenu("Test Ball Spawning")]
    public void TestBallSpawning()
    {
        
        if (bowlingController != null)
        {
            // Check if manual input is enabled
            bool manualInput = bowlingController.GetType().GetField("enableManualKeyInput", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bowlingController) as bool? ?? false;
            
            if (manualInput)
            {
            }
            else
            {
                
                if (playerAnimationController != null)
                {
                    Transform animSpawn = playerAnimationController.GetAnimationSpawnPoint();
                    if (animSpawn != null)
                    {
                    }
                }
            }
            
            // Try to spawn a ball
            bowlingController.InstantiateNewBall();
        }
    }
    
    /// <summary>
    /// Test animation event manually to see if it triggers the correct spawn behavior
    /// </summary>
    [ContextMenu("Test Animation Event")]
    public void TestAnimationEvent()
    {
        
        if (playerAnimationController != null)
        {
            playerAnimationController.OnBallReleased();
        }
        else
        {
        }
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
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Comprehensive test of spawn point behavior
    /// </summary>
    [ContextMenu("Comprehensive Spawn Point Test")]
    public void ComprehensiveSpawnPointTest()
    {
        
        if (playerAnimationController != null)
        {
            Transform beforeSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (beforeSpawn != null)
            {

            }
            playerAnimationController.ForceRefreshSpawnPointReference();
            Transform afterSpawn = playerAnimationController.GetAnimationSpawnPoint();
            if (afterSpawn != null)
            {

            }
            if (bowlingController != null)
            {
                bowlingController.InstantiateNewBall();
            }
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Clean up all bowlers and test fresh instantiation
    /// </summary>
    [ContextMenu("Clean Up and Test Fresh")]
    public void CleanUpAndTestFresh()
    {
        
        if (bowlingController != null)
        {
            bowlingController.CleanUpAllBowlers();
            StartCoroutine(TestAfterCleanup());
        }
        else
        {
        }
    }
    
    private System.Collections.IEnumerator TestAfterCleanup()
    {
        yield return null; // Wait one frame
        if (bowlingController != null)
        {
            bowlingController.InstantiateSelectedBowler();
            yield return null; // Wait one more frame
            bowlingController.InstantiateNewBall();
        }
    }
}
