using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// ScriptableObject for ball physics and delivery settings
    /// Clean, organized, and easy to manage
    /// </summary>
    [CreateAssetMenu(fileName = "BallSettings", menuName = "Cricket Game/Ball Settings")]
    public class BallSettingsSO : ScriptableObject
    {
        [Header("Global Ball Physics")]
        [SerializeField] private float globalBallSpeed = 12f;
        [SerializeField] private float arcHeight = 1f;
        [SerializeField] private float gravity = 10.5f;
        [SerializeField] private float mass = 0.16f;
        [SerializeField] private float drag = 0.1f;
        [SerializeField] private float angularDrag = 0.1f;
        [SerializeField] private float ballRadius = 0.036f;
        [SerializeField] private bool useRealisticPhysics = true;
        [SerializeField] private float destroyDelay = 5f;

        [Header("Bounce Settings")]
        [SerializeField] private float bounceForce = 0.8f;
        [SerializeField] private float bounceFriction = 0.8f;
        [SerializeField] private int maxBounces = 3;

        [Header("Bowling Length Settings")]
        [SerializeField] private BowlingLengthSettings yorker = new BowlingLengthSettings();
        [SerializeField] private BowlingLengthSettings fullLength = new BowlingLengthSettings();
        [SerializeField] private BowlingLengthSettings goodLength = new BowlingLengthSettings();
        [SerializeField] private BowlingLengthSettings shortLength = new BowlingLengthSettings();
        [SerializeField] private BowlingLengthSettings bouncer = new BowlingLengthSettings();

        [Header("Current Rotation Display (Read-Only)")]
        [SerializeField, ReadOnly] private float currentYorkerRotationX = 0f;
        [SerializeField, ReadOnly] private float currentFullLengthRotationX = 0f;
        [SerializeField, ReadOnly] private float currentGoodLengthRotationX = 0f;
        [SerializeField, ReadOnly] private float currentShortLengthRotationX = 0f;
        [SerializeField, ReadOnly] private float currentBouncerRotationX = 0f;

        // Public properties for global settings
        public float GlobalBallSpeed => Mathf.Round(globalBallSpeed);
        public float ArcHeight => arcHeight;
        public float Gravity => gravity;
        public float Mass => mass;
        public float Drag => drag;
        public float AngularDrag => angularDrag;
        public float BallRadius => ballRadius;
        public bool UseRealisticPhysics => useRealisticPhysics;
        public float DestroyDelay => destroyDelay;
        public float BounceForce => bounceForce;
        public float BounceFriction => bounceFriction;
        public int MaxBounces => maxBounces;

        // Bowling length settings
        public BowlingLengthSettings Yorker => yorker;
        public BowlingLengthSettings FullLength => fullLength;
        public BowlingLengthSettings GoodLength => goodLength;
        public BowlingLengthSettings ShortLength => shortLength;
        public BowlingLengthSettings Bouncer => bouncer;

        // Current rotation display
        public float CurrentYorkerRotationX => currentYorkerRotationX;
        public float CurrentFullLengthRotationX => currentFullLengthRotationX;
        public float CurrentGoodLengthRotationX => currentGoodLengthRotationX;
        public float CurrentShortLengthRotationX => currentShortLengthRotationX;
        public float CurrentBouncerRotationX => currentBouncerRotationX;

        /// <summary>
        /// Get dynamic X rotation based on ball speed for a specific length
        /// </summary>
        public float GetDynamicRotationX(BowlingLength length, float ballSpeed)
        {
            BowlingLengthSettings settings = GetLengthSettings(length);
            float dynamicRotation = CalculateRotationX(settings, ballSpeed);
            UpdateCurrentRotationDisplay(length, dynamicRotation);
            return dynamicRotation;
        }

        /// <summary>
        /// Set global ball speed (rounded to integer)
        /// </summary>
        public void SetGlobalBallSpeed(float newSpeed)
        {
            float roundedSpeed = Mathf.Round(newSpeed);
            globalBallSpeed = Mathf.Clamp(roundedSpeed, 12f, 16f);
            UpdateAllCurrentRotations();
            
            // Mark ScriptableObject as dirty to update Inspector
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        /// <summary>
        /// Set ball speed (alias for SetGlobalBallSpeed)
        /// </summary>
        public void SetBallSpeed(float newSpeed)
        {
            SetGlobalBallSpeed(newSpeed);
        }
        
        /// <summary>
        /// Force update all rotation displays (for testing)
        /// </summary>
        [ContextMenu("Force Update Rotation Display")]
        public void ForceUpdateRotationDisplay()
        {
            Debug.Log($"🎯 FORCE UPDATE: globalBallSpeed={globalBallSpeed:F1}");
            UpdateAllCurrentRotations();
        }

        /// <summary>
        /// Get settings for a specific bowling length
        /// </summary>
        public BowlingLengthSettings GetLengthSettings(BowlingLength length)
        {
            switch (length)
            {
                case BowlingLength.Yorker: return yorker;
                case BowlingLength.FullLength: return fullLength;
                case BowlingLength.GoodLength: return goodLength;
                case BowlingLength.ShortLength: return shortLength;
                case BowlingLength.Bouncer: return bouncer;
                default: return yorker;
            }
        }

        /// <summary>
        /// Calculate X rotation based on speed for given settings
        /// </summary>
        private float CalculateRotationX(BowlingLengthSettings settings, float ballSpeed)
        {
            float roundedSpeed = Mathf.Round(ballSpeed);
            float speedFactor = Mathf.InverseLerp(12f, 16f, roundedSpeed);
            float dynamicRotation = Mathf.Lerp(settings.rotationXMin, settings.rotationXMax, speedFactor);
            float result = Mathf.Clamp(dynamicRotation, Mathf.Min(settings.rotationXMin, settings.rotationXMax), Mathf.Max(settings.rotationXMin, settings.rotationXMax));
            
            Debug.Log($"🎯 CalculateRotationX: Speed={ballSpeed:F1}→{roundedSpeed:F0}, Min={settings.rotationXMin:F1}, Max={settings.rotationXMax:F1}, Factor={speedFactor:F2}, Result={result:F1}");
            return result;
        }

        /// <summary>
        /// Update current rotation display for a specific length
        /// </summary>
        private void UpdateCurrentRotationDisplay(BowlingLength length, float rotation)
        {
            switch (length)
            {
                case BowlingLength.Yorker: currentYorkerRotationX = rotation; break;
                case BowlingLength.FullLength: currentFullLengthRotationX = rotation; break;
                case BowlingLength.GoodLength: currentGoodLengthRotationX = rotation; break;
                case BowlingLength.ShortLength: currentShortLengthRotationX = rotation; break;
                case BowlingLength.Bouncer: currentBouncerRotationX = rotation; break;
            }
        }

        /// <summary>
        /// Update all current rotation displays
        /// </summary>
        private void UpdateAllCurrentRotations()
        {
            Debug.Log($"🎯 UpdateAllCurrentRotations: globalBallSpeed={globalBallSpeed:F1}");
            currentYorkerRotationX = CalculateRotationX(yorker, globalBallSpeed);
            currentFullLengthRotationX = CalculateRotationX(fullLength, globalBallSpeed);
            currentGoodLengthRotationX = CalculateRotationX(goodLength, globalBallSpeed);
            currentShortLengthRotationX = CalculateRotationX(shortLength, globalBallSpeed);
            currentBouncerRotationX = CalculateRotationX(bouncer, globalBallSpeed);
            
            Debug.Log($"🎯 Updated Display Values: Yorker={currentYorkerRotationX:F1}, Full={currentFullLengthRotationX:F1}, Good={currentGoodLengthRotationX:F1}, Short={currentShortLengthRotationX:F1}, Bouncer={currentBouncerRotationX:F1}");
            
            // Mark ScriptableObject as dirty to update Inspector
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        /// <summary>
        /// Initialize default values
        /// </summary>
        private void OnEnable()
        {
            if (yorker.rotationXMin == 0 && yorker.rotationXMax == 0)
            {
                InitializeDefaultValues();
            }
        }

        /// <summary>
        /// Set up default values for all bowling lengths
        /// </summary>
        private void InitializeDefaultValues()
        {
            // Yorker settings
            yorker.arcHeight = 0.5f;
            yorker.bounceForce = 0.7f;
            yorker.bounceFriction = 0.9f;
            yorker.gravity = 10.0f;
            yorker.rotationXMin = 40f;
            yorker.rotationXMax = 80f;

            // Full length settings
            fullLength.arcHeight = 0.8f;
            fullLength.bounceForce = 0.8f;
            fullLength.bounceFriction = 0.8f;
            fullLength.gravity = 9.5f;
            fullLength.rotationXMin = 35f;
            fullLength.rotationXMax = 75f;

            // Good length settings
            goodLength.arcHeight = 1.0f;
            goodLength.bounceForce = 0.9f;
            goodLength.bounceFriction = 0.7f;
            goodLength.gravity = 9.0f;
            goodLength.rotationXMin = 30f;
            goodLength.rotationXMax = 70f;

            // Short length settings
            shortLength.arcHeight = 1.2f;
            shortLength.bounceForce = 1.0f;
            shortLength.bounceFriction = 0.6f;
            shortLength.gravity = 9.5f;
            shortLength.rotationXMin = 25f;
            shortLength.rotationXMax = 65f;

            // Bouncer settings
            bouncer.arcHeight = 1.5f;
            bouncer.bounceForce = 1.2f;
            bouncer.bounceFriction = 0.5f;
            bouncer.gravity = 10.0f;
            bouncer.rotationXMin = 20f;
            bouncer.rotationXMax = 60f;
        }
    }

    /// <summary>
    /// Settings for a specific bowling length
    /// </summary>
    [System.Serializable]
    public class BowlingLengthSettings
    {
        [Header("Arc & Bounce")]
        public float arcHeight = 1f;
        public float bounceForce = 0.8f;
        public float bounceFriction = 0.8f;

        [Header("Physics")]
        public float gravity = 10.5f;

        [Header("X Rotation Range")]
        public float rotationXMin = 30f;
        public float rotationXMax = 70f;
    }

    /// <summary>
    /// ReadOnly attribute for Inspector display
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}
