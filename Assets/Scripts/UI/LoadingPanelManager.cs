using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

namespace CricketGame.UI
{
    /// <summary>
    /// Professional, reusable loading panel manager with smooth radial fill pulse animation.
    /// One cycle: light → dark → light, then stops automatically.
    /// </summary>
    public class LoadingPanelManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static LoadingPanelManager _instance;
        
        /// <summary>
        /// Static instance for global access. Creates one if it doesn't exist.
        /// </summary>
        public static LoadingPanelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance in scene
                    _instance = FindObjectOfType<LoadingPanelManager>();
                    
                    if (_instance == null)
                    {
                        // Create new instance if none exists
                        GameObject go = new GameObject("LoadingPanelManager");
                        _instance = go.AddComponent<LoadingPanelManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Loading Panel References")]
        [Tooltip("The UI Image component with Radial 90 fill that will cover the screen")]
        [SerializeField] private Image loadingPanelImage;
        
        [Header("Pulse Animation Settings")]
        [Tooltip("Duration for one complete cycle (light → dark → light) in seconds")]
        [SerializeField] private float pulseDuration = 2f;
        
        [Tooltip("Minimum opacity (0 = transparent)")]
        [Range(0f, 1f)]
        [SerializeField] private float minOpacity = 0f;
        
        [Tooltip("Maximum opacity (1 = fully opaque black)")]
        [Range(0f, 1f)]
        [SerializeField] private float maxOpacity = 1f;
        
        [Tooltip("Animation curve for the pulse (ease in/out by default)")]
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Simple Animation Settings")]
        [SerializeField] private float simpleFadeDuration = 0.25f;
        [SerializeField] private float simpleHoldDuration = 0.2f;
        [SerializeField] private AnimationCurve simpleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public enum LoadingAnimationMode
        {
            Pulse,
            Simple
        }

        [Header("Animation Selection")]
        [SerializeField] private LoadingAnimationMode defaultAnimationMode = LoadingAnimationMode.Pulse;
        
        [Header("Panel Setup")]
        [Tooltip("Automatically find Loading Panel in scene if not assigned")]
        [SerializeField] private bool autoFindLoadingPanel = true;
        
        [Tooltip("Ensure panel covers entire screen on Start")]
        [SerializeField] private bool ensureFullScreenCoverage = true;
        
        
        #endregion
        
        #region Private Fields
        
        private Coroutine activeAnimationCoroutine;
        private bool isAnimationRunning = false;
        private LoadingAnimationMode currentAnimationMode;
        private bool cachedFillClockwise;
        private int cachedFillOrigin;
        private Color baseColor; // Store the original color you set in Inspector
        
        #endregion
        
        #region Unity Lifecycle
        
		private void Awake()
		{
			// Ensure singleton pattern
			if (_instance == null)
			{
				_instance = this;

				Transform rootTransform = transform.root;
				if (rootTransform != null)
				{
					DontDestroyOnLoad(rootTransform.gameObject);
				}
				else
				{
					DontDestroyOnLoad(gameObject);
				}
			}
			else if (_instance != this)
			{
				Destroy(gameObject);
				return;
			}
		}
        
        private void Start()
        {
            InitializeLoadingPanel();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize and setup the loading panel
        /// </summary>
        private void InitializeLoadingPanel()
        {
            // Auto-find loading panel if enabled and not assigned
			if (loadingPanelImage == null && autoFindLoadingPanel)
			{
				GameObject loadingPanelGO = GameObject.Find("Loading Panel");
				if (loadingPanelGO != null)
				{
					loadingPanelImage = loadingPanelGO.GetComponent<Image>();
				}
			}
            
            // Validate panel setup
            if (loadingPanelImage == null)
            {
                return;
            }
            
            // Ensure panel is configured correctly
            ValidatePanelSetup();
            
            // Store the base color you set in Inspector (so we can preserve it during animation)
            baseColor = loadingPanelImage.color;

            cachedFillClockwise = loadingPanelImage.fillClockwise;
            cachedFillOrigin = loadingPanelImage.fillOrigin;

            currentAnimationMode = defaultAnimationMode;
            ApplyVisualSetup(defaultAnimationMode, false);

            // Ensure full screen coverage
            if (ensureFullScreenCoverage)
            {
                EnsureFullScreenCoverage();
            }
            
            // Initialize panel state (start at light/min opacity)
            SetOpacity(minOpacity);
            loadingPanelImage.gameObject.SetActive(true);
            
        }
        
        /// <summary>
        /// Validate that the loading panel is configured with Radial 90 fill
        /// </summary>
        private void ValidatePanelSetup()
        {
            if (loadingPanelImage == null) return;
            
            // Check image type
            if (loadingPanelImage.type != Image.Type.Filled)
            {
                loadingPanelImage.type = Image.Type.Filled;
            }
            
            // Check fill method (should be Radial 90)
            if (loadingPanelImage.fillMethod != Image.FillMethod.Radial90)
            {
                loadingPanelImage.fillMethod = Image.FillMethod.Radial90;
            }
            
            // Don't override color - let user set it in Inspector
        }
        
        /// <summary>
        /// Ensure the panel covers the entire screen
        /// </summary>
        private void EnsureFullScreenCoverage()
        {
            if (loadingPanelImage == null) return;
            
            RectTransform rectTransform = loadingPanelImage.GetComponent<RectTransform>();
            if (rectTransform == null) return;
            
            // Set to full screen stretch
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Ensure it's on top (highest sorting order)
            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // Move to top of hierarchy to ensure it renders on top
                rectTransform.SetAsLastSibling();
            }
        }
        
        #endregion
        
        #region Public API - Static Methods
        
        /// <summary>
        /// Start one pulse cycle (light → dark → light) and stop automatically.
        /// Can be called again to restart from beginning.
        /// Call this from anywhere: LoadingPanelManager.StartPulse();
        /// </summary>
        public static void StartPulse()
        {
            if (Instance == null)
            {
                return;
            }
            
            Instance.PlayAnimationInternal(LoadingAnimationMode.Pulse);
        }

        /// <summary>
        /// Start simple fade animation.
        /// </summary>
        public static void StartSimple()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.PlayAnimationInternal(LoadingAnimationMode.Simple);
        }

        /// <summary>
        /// Play loading animation using configured default mode.
        /// </summary>
        public static void PlayAnimation()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.PlayAnimationInternal(Instance.defaultAnimationMode);
        }

        /// <summary>
        /// Play loading animation using override mode.
        /// </summary>
        public static void PlayAnimation(LoadingAnimationMode mode)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.PlayAnimationInternal(mode);
        }

        /// <summary>
        /// Stop any running animation immediately and reset the panel.
        /// </summary>
        public static void StopAnimation()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.StopAnimationInternal();
        }

        /// <summary>
        /// Change the default animation mode at runtime.
        /// </summary>
        public static void SetDefaultAnimationMode(LoadingAnimationMode mode)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.defaultAnimationMode = mode;
        }
 
        /// <summary>
        /// Check if pulse animation is currently running
        /// </summary>
        public static bool IsAnimationRunning()
        {
            return Instance != null && Instance.isAnimationRunning;
        }
        
        #endregion
        
        #region Internal Methods
        
        private void PlayAnimationInternal(LoadingAnimationMode mode)
        {
            StopAnimationInternal();

            switch (mode)
            {
                case LoadingAnimationMode.Simple:
                    StartSimpleAnimation();
                    break;
                case LoadingAnimationMode.Pulse:
                default:
                    StartPulseAnimation();
                    break;
            }
        }

        private void StopAnimationInternal()
        {
            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
                activeAnimationCoroutine = null;
            }

            isAnimationRunning = false;
            SetOpacity(minOpacity);
            ApplyVisualSetup(defaultAnimationMode, false);
        }

        /// <summary>
        /// Start one pulse cycle (light → dark → light) - one cycle then stops
        /// </summary>
        private void StartPulseAnimation()
        {
            if (loadingPanelImage == null)
            {
                return;
            }
            
            currentAnimationMode = LoadingAnimationMode.Pulse;
            ApplyVisualSetup(currentAnimationMode, true);

            // Start new pulse (one cycle only)
            isAnimationRunning = true;
            activeAnimationCoroutine = StartCoroutine(PulseAnimationCycle());
            
        }

        /// <summary>
        /// Start simple fade in/out animation.
        /// </summary>
        private void StartSimpleAnimation()
        {
            if (loadingPanelImage == null)
            {
                return;
            }

            currentAnimationMode = LoadingAnimationMode.Simple;
            ApplyVisualSetup(currentAnimationMode, true);

            isAnimationRunning = true;
            activeAnimationCoroutine = StartCoroutine(SimpleAnimationCycle());

        }
        
        /// <summary>
        /// One complete pulse cycle (light → dark → light) then stops
        /// </summary>
        private IEnumerator PulseAnimationCycle()
        {
            if (loadingPanelImage == null) yield break;
            
            // Phase 1: Light to Dark (minOpacity → maxOpacity)
            yield return StartCoroutine(AnimateOpacity(minOpacity, maxOpacity, pulseDuration / 2f, pulseCurve));
            
            // Phase 2: Dark to Light (maxOpacity → minOpacity)
            yield return StartCoroutine(AnimateOpacity(maxOpacity, minOpacity, pulseDuration / 2f, pulseCurve));
            
            // Animation complete - stop automatically
            isAnimationRunning = false;
            activeAnimationCoroutine = null;
            
        }

        private IEnumerator SimpleAnimationCycle()
        {
            if (loadingPanelImage == null)
            {
                yield break;
            }

            float fadeDuration = Mathf.Max(0.0001f, simpleFadeDuration);

            yield return StartCoroutine(AnimateOpacity(minOpacity, maxOpacity, fadeDuration, simpleCurve));

            if (simpleHoldDuration > 0f)
            {
                yield return new WaitForSeconds(simpleHoldDuration);
            }

            yield return StartCoroutine(AnimateOpacity(maxOpacity, minOpacity, fadeDuration, simpleCurve));

            isAnimationRunning = false;
            activeAnimationCoroutine = null;

        }
 
        /// <summary>
        /// Animate opacity from start to end using pulse curve
        /// </summary>
        private IEnumerator AnimateOpacity(float startOpacity, float endOpacity, float duration, AnimationCurve curve)
        {
            if (loadingPanelImage == null) yield break;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                
                // Evaluate curve for smooth animation
                float curveValue = curve != null ? curve.Evaluate(normalizedTime) : normalizedTime;
                
                // Interpolate opacity
                float opacity = Mathf.Lerp(startOpacity, endOpacity, curveValue);
                SetOpacity(opacity);
                
                yield return null;
            }
            
            // Ensure final value
            SetOpacity(endOpacity);
        }
        
        /// <summary>
        /// Set opacity using fillAmount for Radial 90 fill effect
        /// Preserves the color and alpha you set in Inspector
        /// </summary>
        private void SetOpacity(float opacity)
        {
            if (loadingPanelImage == null) return;
            
            opacity = Mathf.Clamp01(opacity);
            
            if (currentAnimationMode == LoadingAnimationMode.Pulse)
            {
                // For Radial 90 fill, control fillAmount (0 = no fill/transparent, 1 = full fill)
                loadingPanelImage.fillAmount = opacity;
            }
            else
            {
                loadingPanelImage.fillAmount = 1f;
            }

            // Use the stored base color and multiply its alpha by opacity for smooth fade
            // This preserves whatever color and base opacity you set in Inspector
            Color currentColor = baseColor;
            currentColor.a = baseColor.a * opacity;
            loadingPanelImage.color = currentColor;
        }

        private void ApplyVisualSetup(LoadingAnimationMode mode, bool duringAnimation)
        {
            if (loadingPanelImage == null)
            {
                return;
            }

            switch (mode)
            {
                case LoadingAnimationMode.Simple:
                    loadingPanelImage.type = Image.Type.Simple;
                    loadingPanelImage.fillAmount = 1f;
                    break;
                case LoadingAnimationMode.Pulse:
                default:
                    loadingPanelImage.type = Image.Type.Filled;
                    loadingPanelImage.fillMethod = Image.FillMethod.Radial90;
                    loadingPanelImage.fillClockwise = cachedFillClockwise;
                    loadingPanelImage.fillOrigin = cachedFillOrigin;
                    break;
            }

            if (!duringAnimation)
            {
                currentAnimationMode = mode;
            }
        }
        
        #endregion
        
        #region Editor Utilities
        
        /// <summary>
        /// Test pulse animation (Editor context menu)
        /// </summary>
        [ContextMenu("Test Pulse")]
        private void TestPulse()
        {
            StartPulseAnimation();
        }
        
        #endregion
    }
}

