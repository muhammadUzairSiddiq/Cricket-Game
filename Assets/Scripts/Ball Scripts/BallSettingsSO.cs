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




        /// <summary>
        /// Set global ball speed (rounded to integer)
        /// </summary>
        public void SetGlobalBallSpeed(float newSpeed)
        {
            float roundedSpeed = Mathf.Round(newSpeed);
            globalBallSpeed = Mathf.Clamp(roundedSpeed, 12f, 16f);
            
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
        



    }


}
