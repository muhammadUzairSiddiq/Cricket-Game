using UnityEngine;

/// <summary>
/// Locks GameObject position and rotation to prevent drift from root motion or other systems.
/// Attach this to the bowler to prevent position/rotation changes.
/// </summary>
public class PositionLock : MonoBehaviour
{
    [Header("Lock Settings")]
    [SerializeField] private bool lockPosition = true;
    [SerializeField] private bool lockRotation = true;
    [SerializeField] private bool lockX = true;
    [SerializeField] private bool lockY = true;
    [SerializeField] private bool lockZ = true;
    
    [Header("Target Position")]
    [SerializeField] private Vector3 lockedPosition;
    [SerializeField] private Quaternion lockedRotation;
    [SerializeField] private bool isLocked = false;
    
    private Transform cachedTransform;
    
    void Awake()
    {
        cachedTransform = transform;
    }
    
    void LateUpdate()
    {
        // Only lock if explicitly enabled AND not during active gameplay (bowling)
        // Check if Animator is playing an animation that requires movement
        Animator animator = GetComponent<Animator>();
        bool isAnimating = animator != null && animator.enabled && animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0 && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1;
        
        // Only lock when NOT animating (idle state)
        if (isLocked && !isAnimating && cachedTransform != null)
        {
            // Lock position
            if (lockPosition)
            {
                Vector3 currentPos = cachedTransform.position;
                Vector3 targetPos = lockedPosition;
                
                if (!lockX) targetPos.x = currentPos.x;
                if (!lockY) targetPos.y = currentPos.y;
                if (!lockZ) targetPos.z = currentPos.z;
                
                if (Vector3.Distance(currentPos, targetPos) > 0.001f)
                {
                    cachedTransform.position = targetPos;
                }
            }
            
            // Lock rotation
            if (lockRotation)
            {
                if (Quaternion.Angle(cachedTransform.rotation, lockedRotation) > 0.1f)
                {
                    cachedTransform.rotation = lockedRotation;
                }
            }
        }
    }
    
    /// <summary>
    /// Lock the current position and rotation
    /// </summary>
    public void LockCurrentPosition()
    {
        if (cachedTransform == null) cachedTransform = transform;
        lockedPosition = cachedTransform.position;
        lockedRotation = cachedTransform.rotation;
        isLocked = true;
    }
    
    /// <summary>
    /// Lock to a specific position and rotation
    /// </summary>
    public void LockToPosition(Vector3 position, Quaternion rotation)
    {
        lockedPosition = position;
        lockedRotation = rotation;
        isLocked = true;
        
        // Force immediate set
        if (cachedTransform == null) cachedTransform = transform;
        cachedTransform.SetPositionAndRotation(position, rotation);
    }
    
    /// <summary>
    /// Unlock position (allow movement)
    /// </summary>
    public void Unlock()
    {
        isLocked = false;
    }
    
    /// <summary>
    /// Check if position is locked
    /// </summary>
    public bool IsLocked => isLocked;
}

