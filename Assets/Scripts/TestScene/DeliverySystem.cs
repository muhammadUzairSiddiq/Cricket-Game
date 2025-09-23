using UnityEngine;

namespace CricketGame
{
    public enum DeliveryType
    {
        Flat,
        SeamIn,
        SeamOut
    }
    
    public class DeliverySystem : MonoBehaviour
    {
        [Header("Delivery Settings")]
        [SerializeField] private DeliveryType currentDeliveryType = DeliveryType.Flat;
        
        [Header("Delivery Components")]
        [SerializeField] private FlatDelivery flatDelivery;
        [UnityEngine.Serialization.FormerlySerializedAs("inSwingDelivery")]
        [SerializeField] private SeamInDelivery seamInDelivery;
        [UnityEngine.Serialization.FormerlySerializedAs("outSwingDelivery")]
        [SerializeField] private SeamOutDelivery seamOutDelivery;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        /// <summary>
        /// Get the current delivery type
        /// </summary>
        public DeliveryType GetCurrentDeliveryType()
        {
            return currentDeliveryType;
        }
        
        /// <summary>
        /// Set the delivery type
        /// </summary>
        public void SetDeliveryType(DeliveryType deliveryType)
        {
            currentDeliveryType = deliveryType;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 DeliverySystem: Switched to {deliveryType} delivery");
            }
        }
        
        /// <summary>
        /// Calculate trajectory for the current delivery type
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            switch (currentDeliveryType)
            {
                case DeliveryType.Flat:
                    return flatDelivery?.CalculateTrajectory(startPos, targetPos, ballSpeed) ?? targetPos;
                    
                case DeliveryType.SeamIn:
                    return seamInDelivery?.CalculateTrajectory(startPos, targetPos, ballSpeed) ?? targetPos;
                    
                case DeliveryType.SeamOut:
                    return seamOutDelivery?.CalculateTrajectory(startPos, targetPos, ballSpeed) ?? targetPos;
                    
                default:
                    return targetPos;
            }
        }
        
        /// <summary>
        /// Get delivery direction for trajectory calculation
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            switch (currentDeliveryType)
            {
                case DeliveryType.Flat:
                    return flatDelivery?.GetDeliveryDirection(startPos, targetPos, ballSpeed) ?? (targetPos - startPos).normalized;
                    
                case DeliveryType.SeamIn:
                    return seamInDelivery?.GetDeliveryDirection(startPos, targetPos, ballSpeed) ?? (targetPos - startPos).normalized;
                    
                case DeliveryType.SeamOut:
                    return seamOutDelivery?.GetDeliveryDirection(startPos, targetPos, ballSpeed) ?? (targetPos - startPos).normalized;
                    
                default:
                    return (targetPos - startPos).normalized;
            }
        }
        
        /// <summary>
        /// Reset delivery state for new ball
        /// </summary>
        public void ResetDelivery()
        {
            flatDelivery?.ResetDelivery();
            seamInDelivery?.ResetDelivery();
            seamOutDelivery?.ResetDelivery();
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 DeliverySystem: Reset all delivery types");
            }
        }
        
        /// <summary>
        /// Get delivery info for UI display
        /// </summary>
        public string GetDeliveryInfo()
        {
            return $"Current Delivery: {currentDeliveryType}";
        }
    }
}
