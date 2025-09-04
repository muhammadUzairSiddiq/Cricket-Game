using UnityEngine;
using System.Collections;

/// <summary>
/// 🎯 Simple script to automatically destroy ball after 5 seconds
/// Attach this to the ball prefab - it will work regardless of physics state
/// </summary>
public class BallAutoDestroy : MonoBehaviour
{
    [Header("Auto Destroy Settings")]
    [SerializeField] private float destroyDelay = 5f; // Time to wait before destroying
    [SerializeField] private bool startTimerOnStart = true; // Start timer when ball is created
    
    private bool timerStarted = false;
    
    void Start()
    {
        if (startTimerOnStart)
        {
            StartDestroyTimer();
        }
    }
    
    /// <summary>
    /// Start the destroy timer
    /// </summary>
    public void StartDestroyTimer()
    {
        if (!timerStarted)
        {
            timerStarted = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }
    
    /// <summary>
    /// Destroy the ball after specified delay
    /// </summary>
    IEnumerator DestroyAfterDelay()
    {
        // Ball will be destroyed after delay
        
        // Wait for the specified delay
        yield return new WaitForSeconds(destroyDelay);
        
        // Force stop all physics before destroying
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            // Ball physics stopped before destruction
        }
        
        // Destroy the ball
        // Destroying ball after delay
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Manually destroy the ball immediately
    /// </summary>
    public void DestroyImmediately()
    {
        // Manually destroying ball
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Reset the destroy timer
    /// </summary>
    public void ResetTimer()
    {
        timerStarted = false;
        StopAllCoroutines();
        // Destroy timer reset
    }
    
    /// <summary>
    /// Change the destroy delay
    /// </summary>
    public void SetDestroyDelay(float newDelay)
    {
        destroyDelay = newDelay;
        if (timerStarted)
        {
            ResetTimer();
            StartDestroyTimer();
        }
    }
}
