using UnityEngine;

namespace CricketGame
{
    public enum DeliveryType
    {
        Flat,       // Straight delivery
        SeamIn,     // Seam in delivery (curves in)
        SeamOut,    // Seam out delivery (curves out)
        Inswing,    // In swing delivery (curves in towards batsman)
        Outswing,   // Out swing delivery (curves away from batsman)
        LegSpin     // Leg spin delivery (pre-target tail in, post-target turn)
    }

    /// <summary>
    /// Delivery System - Manages different bowling delivery types and their trajectories
    /// </summary>
    public class DeliverySystem : MonoBehaviour
    {
        [Header("Delivery Settings")]
        [SerializeField] private DeliveryType currentDeliveryType = DeliveryType.Flat;
        [SerializeField] private bool enableDeliverySystem = true;
        
        [Header("Delivery Components")]
        [SerializeField] private FlatDelivery flatDelivery;
        [SerializeField] private SeamInDelivery seamInDelivery;
        [SerializeField] private SeamOutDelivery seamOutDelivery;
        [SerializeField] private InswingDelivery inswingDelivery;
        [SerializeField] private OutswingDelivery outswingDelivery;
        [SerializeField] private LegSpinDelivery legSpinDelivery;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            // Auto-find delivery components if not assigned
            if (flatDelivery == null)
                flatDelivery = GetComponent<FlatDelivery>();
            if (seamInDelivery == null)
                seamInDelivery = GetComponent<SeamInDelivery>();
            if (seamOutDelivery == null)
                seamOutDelivery = GetComponent<SeamOutDelivery>();
            if (inswingDelivery == null)
                inswingDelivery = GetComponent<InswingDelivery>();
            if (outswingDelivery == null)
                outswingDelivery = GetComponent<OutswingDelivery>();
            if (legSpinDelivery == null)
                legSpinDelivery = GetComponent<LegSpinDelivery>();
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 DELIVERY SYSTEM: Initialized with {currentDeliveryType} delivery");
            }
        }
        
        /// <summary>
        /// Calculate trajectory based on current delivery type
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableDeliverySystem)
                return targetPos;
            
            switch (currentDeliveryType)
            {
                case DeliveryType.Flat:
                    if (flatDelivery != null)
                        return flatDelivery.CalculateTrajectory(startPos, targetPos, ballSpeed);
                    break;
                    
                case DeliveryType.SeamIn:
                    if (seamInDelivery != null)
                        return seamInDelivery.CalculateTrajectory(startPos, targetPos, ballSpeed);
                    break;
                    
                case DeliveryType.SeamOut:
                    if (seamOutDelivery != null)
                        return seamOutDelivery.CalculateTrajectory(startPos, targetPos, ballSpeed);
                    break;
                    
                case DeliveryType.Inswing:
                    if (inswingDelivery != null)
                        return inswingDelivery.CalculateTrajectory(startPos, targetPos, ballSpeed);
                    break;
                case DeliveryType.Outswing:
                    if (outswingDelivery != null)
                        return outswingDelivery.CalculateTrajectory(startPos, targetPos, ballSpeed);
                    break;
                case DeliveryType.LegSpin:
                    if (legSpinDelivery != null)
                        return legSpinDelivery.CalculateTrajectory(startPos, targetPos, ballSpeed);
                    break;
            }
            
            // Fallback to straight trajectory
            return targetPos;
        }
        
        /// <summary>
        /// Get current delivery type
        /// </summary>
        public DeliveryType GetCurrentDeliveryType()
        {
            return currentDeliveryType;
        }
        
        /// <summary>
        /// Set delivery type
        /// </summary>
        public void SetDeliveryType(DeliveryType newType)
        {
            currentDeliveryType = newType;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 DELIVERY SYSTEM: Switched to {currentDeliveryType} delivery");
            }
        }
        
        /// <summary>
        /// Reset delivery system for new ball
        /// </summary>
        public void ResetDelivery()
        {
            // Reset all delivery components
            if (flatDelivery != null)
                flatDelivery.ResetDelivery();
            if (seamInDelivery != null)
                seamInDelivery.ResetDelivery();
            if (seamOutDelivery != null)
                seamOutDelivery.ResetDelivery();
            if (inswingDelivery != null)
                inswingDelivery.ResetDelivery();
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 DELIVERY SYSTEM: Reset for new ball ({currentDeliveryType})");
            }
        }
        
        /// <summary>
        /// Get delivery direction for current type
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableDeliverySystem)
                return (targetPos - startPos).normalized;
            
            switch (currentDeliveryType)
            {
                case DeliveryType.Flat:
                    if (flatDelivery != null)
                        return flatDelivery.GetDeliveryDirection(startPos, targetPos, ballSpeed);
                    break;
                    
                case DeliveryType.SeamIn:
                    if (seamInDelivery != null)
                        return seamInDelivery.GetDeliveryDirection(startPos, targetPos, ballSpeed);
                    break;
                    
                case DeliveryType.SeamOut:
                    if (seamOutDelivery != null)
                        return seamOutDelivery.GetDeliveryDirection(startPos, targetPos, ballSpeed);
                    break;
                    
                case DeliveryType.Inswing:
                    if (inswingDelivery != null)
                        return inswingDelivery.GetDeliveryDirection(startPos, targetPos, ballSpeed);
                    break;
                case DeliveryType.Outswing:
                    if (outswingDelivery != null)
                        return outswingDelivery.GetDeliveryDirection(startPos, targetPos, ballSpeed);
                    break;
                case DeliveryType.LegSpin:
                    if (legSpinDelivery != null)
                        return legSpinDelivery.GetDeliveryDirection(startPos, targetPos, ballSpeed);
                    break;
            }
            
            // Fallback to straight direction
            return (targetPos - startPos).normalized;
        }
        
        /// <summary>
        /// Get delivery info string
        /// </summary>
        public string GetDeliveryInfo()
        {
            switch (currentDeliveryType)
            {
                case DeliveryType.Flat:
                    return flatDelivery != null ? flatDelivery.GetDeliveryInfo() : "Flat Delivery - Straight trajectory";
                case DeliveryType.SeamIn:
                    return seamInDelivery != null ? seamInDelivery.GetDeliveryInfo() : "Seam In Delivery - Curves in towards batsman";
                case DeliveryType.SeamOut:
                    return seamOutDelivery != null ? seamOutDelivery.GetDeliveryInfo() : "Seam Out Delivery - Curves away from batsman";
                case DeliveryType.Inswing:
                    return inswingDelivery != null ? inswingDelivery.GetDeliveryInfo() : "In Swing Delivery - Curves in towards batsman";
                case DeliveryType.Outswing:
                    return outswingDelivery != null ? outswingDelivery.GetDeliveryInfo() : "Out Swing Delivery - Curves away from batsman";
                case DeliveryType.LegSpin:
                    return legSpinDelivery != null ? legSpinDelivery.GetDeliveryInfo() : "Leg Spin Delivery - Curves in, then turns after pitching";
                default:
                    return "Unknown Delivery Type";
            }
        }
        
        /// <summary>
        /// Switch to flat delivery
        /// </summary>
        public void SwitchToFlatDelivery()
        {
            SetDeliveryType(DeliveryType.Flat);
        }
        
        /// <summary>
        /// Switch to seam in delivery
        /// </summary>
        public void SwitchToSeamInDelivery()
        {
            SetDeliveryType(DeliveryType.SeamIn);
        }
        
        /// <summary>
        /// Switch to seam out delivery
        /// </summary>
        public void SwitchToSeamOutDelivery()
        {
            SetDeliveryType(DeliveryType.SeamOut);
        }
        
        /// <summary>
        /// Switch to inswing delivery
        /// </summary>
        public void SwitchToInswingDelivery()
        {
            SetDeliveryType(DeliveryType.Inswing);
        }
        
        /// <summary>
        /// Switch to outswing delivery
        /// </summary>
        public void SwitchToOutswingDelivery()
        {
            SetDeliveryType(DeliveryType.Outswing);
        }
        
        /// <summary>
        /// Switch to leg spin delivery
        /// </summary>
        public void SwitchToLegSpinDelivery()
        {
            SetDeliveryType(DeliveryType.LegSpin);
        }
        
        /// <summary>
        /// Enable/disable delivery system
        /// </summary>
        public void SetDeliverySystemEnabled(bool enabled)
        {
            enableDeliverySystem = enabled;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 DELIVERY SYSTEM: {(enabled ? "Enabled" : "Disabled")}");
            }
        }
        
        /// <summary>
        /// Check if delivery system is enabled
        /// </summary>
        public bool IsDeliverySystemEnabled()
        {
            return enableDeliverySystem;
        }
        
        // Context menu for testing
        [ContextMenu("Test Current Delivery")]
        void TestCurrentDelivery()
        {
            Debug.Log($"🎯 DELIVERY TEST: {GetDeliveryInfo()}");
            Debug.Log($"🎯 DELIVERY TEST: Type = {currentDeliveryType}");
            Debug.Log($"🎯 DELIVERY TEST: Enabled = {enableDeliverySystem}");
        }
        
        [ContextMenu("Switch to Flat")]
        void SwitchToFlatContext()
        {
            SwitchToFlatDelivery();
        }
        
        [ContextMenu("Switch to Seam In")]
        void SwitchToSeamInContext()
        {
            SwitchToSeamInDelivery();
        }
        
        [ContextMenu("Switch to Seam Out")]
        void SwitchToSeamOutContext()
        {
            SwitchToSeamOutDelivery();
        }
        
        [ContextMenu("Switch to Inswing")]
        void SwitchToInswingContext()
        {
            SwitchToInswingDelivery();
        }
        
        [ContextMenu("Switch to Outswing")]
        void SwitchToOutswingContext()
        {
            SwitchToOutswingDelivery();
        }
    }
}