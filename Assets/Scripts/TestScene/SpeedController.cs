using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

namespace CricketGame
{
    /// <summary>
    /// Controls ball speed via UI slider and updates ball prefab settings
    /// </summary>
    public class SpeedController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Slider speedSlider;
        [SerializeField] private TextMeshProUGUI speedText;
        
        [Header("Ball Settings")]
        [SerializeField] private BallSettingsSO ballSettingsSO; // Reference to ball settings ScriptableObject
        [SerializeField] private BowlingController bowlingController; // Reference to bowling controller
        
        [Header("Speed Settings")]
        [SerializeField] private int minSpeed = 12;
        [SerializeField] private int maxSpeed = 17;
        [SerializeField] private int currentSpeed = 12;
        
        [Header("Display Settings")]
        [SerializeField] private bool showSpeedInKmh = true; // Show as 90km/h, 100km/h etc.
        [SerializeField] private float speedMultiplier = 10f; // 9 m/s = 90 km/h
        
        [Header("Auto Movement Settings")]
        [SerializeField] private bool enableAutoMovementDefault = true; // Default state (can be overridden)
        [SerializeField] private float autoMovementSpeed = 2f; // How fast the slider moves (cycles per second)
        private float autoTime = 0f; // internal time for ping-pong
        [SerializeField] private bool stopOnAnyTap = true; // Stop auto on any screen tap/click
        
        // Runtime auto movement state (can be disabled by user interaction)
        private bool enableAutoMovement = true;
        
        private bool isUserInteracting = false;
        private bool isMovingUp = true;
        private Coroutine resumeCoroutine;
        private bool isProgrammaticallyChangingValue = false; // Track when we're changing value via script
		private RectTransform speedSliderRect;
		private Canvas speedSliderCanvas;
		private Camera speedSliderCamera;
		private RectTransform speedPanelRect;
		private Canvas speedPanelCanvas;
		private Camera speedPanelCamera;
		private static Camera cachedMainCamera;
        
        void Start()
        {
            // Initialize auto movement from default
            enableAutoMovement = enableAutoMovementDefault;
            
            // Initialize slider with integer steps
            if (speedSlider != null)
            {
                speedSlider.minValue = minSpeed;
                speedSlider.maxValue = maxSpeed;
                speedSlider.wholeNumbers = true; // Only allow integer values
                speedSlider.value = currentSpeed;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
                
                // Initialize movement direction based on starting position
                float startNormalized = (currentSpeed - minSpeed) / (maxSpeed - minSpeed);
                isMovingUp = startNormalized < 0.5f; // If less than middle, move up, else move down
                
                // Add event triggers for detecting user interaction
                SetupSliderEvents();
            }
            
            // Update initial display
            UpdateSpeedDisplay();
            UpdateBallSpeed();
			CacheUIReferences();
        }
        
        void Update()
        {
            // Check for user interaction first (must check every frame)
            CheckUserInteraction();
            
            // Auto-move slider when user is not interacting
            if (enableAutoMovement && !isUserInteracting && speedSlider != null)
            {
                // Only move if slider is not being interacted with
                if (speedSlider.interactable)
                {
                    // Linear ping-pong 0..1..0 using time
                    autoTime += Time.deltaTime * autoMovementSpeed;
                    float t = Mathf.PingPong(autoTime, 1f); // 0->1->0
                    
                    // Map to min/max and snap to integer (since slider uses whole numbers)
                    float mapped = Mathf.Lerp(minSpeed, maxSpeed, t);
                    int target = Mathf.RoundToInt(mapped);
                    
                    if (speedSlider.value != target)
                    {
                        isProgrammaticallyChangingValue = true;
                        speedSlider.value = target; // triggers OnSpeedChanged -> updates display and SO
                        isProgrammaticallyChangingValue = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if user is interacting with slider (primary detection method)
        /// </summary>
        private void CheckUserInteraction()
        {
			CacheUIReferences();

            if (speedSlider == null) return;
            
            bool inputDetected = false;

            // Global tap/click anywhere to stop auto movement immediately
            if (stopOnAnyTap)
            {
                // Mouse click anywhere - check if it's on the slider
                if (Input.GetMouseButtonDown(0))
                {
                    // Check if click is on slider or speed UI
                    if (IsPointOverSlider(Input.mousePosition) || IsPointOverSpeedUI(Input.mousePosition))
                    {
                        isUserInteracting = true;
                        speedConfirmed = true; // User has selected a speed
                        enableAutoMovement = false; // Stop auto movement permanently
                        inputDetected = true;
                        return; // Stop here; resume will be handled on release
                    }
                }
                // Touch anywhere - check if it's on the slider
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch t = Input.GetTouch(i);
                        if (t.phase == TouchPhase.Began)
                        {
                            // Check if touch is on slider or speed UI
                            if (IsPointOverSlider(t.position) || IsPointOverSpeedUI(t.position))
                            {
                                isUserInteracting = true;
                                speedConfirmed = true; // User has selected a speed
                                enableAutoMovement = false; // Stop auto movement permanently
                                inputDetected = true;
                                return; // Stop here; resume will be handled on touch end
                            }
                        }
                    }
                }
            }
            
            // Check mouse input
            if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            {
                if (IsPointOverSlider(Input.mousePosition))
                {
                    isUserInteracting = true;
                    inputDetected = true;
                }
            }
            
            // Check if mouse was just released
            if (Input.GetMouseButtonUp(0) && isUserInteracting)
            {
                if (resumeCoroutine != null)
                {
                    StopCoroutine(resumeCoroutine);
                }
                resumeCoroutine = StartCoroutine(ResumeAutoMovement());
                return;
            }
            
            // Check touch input for mobile
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (isUserInteracting)
                        {
                            if (resumeCoroutine != null)
                            {
                                StopCoroutine(resumeCoroutine);
                            }
                            resumeCoroutine = StartCoroutine(ResumeAutoMovement());
                        }
                        continue;
                    }
                    
                    // Check if touch is on slider
                    if (IsPointOverSlider(touch.position))
                    {
                        isUserInteracting = true;
                        inputDetected = true;
                        break;
                    }
                }
            }
            
            // If we detected input, user is interacting
            if (inputDetected)
            {
                isUserInteracting = true;
            }
        }
        
        /// <summary>
        /// Check if a screen point is over the slider
        /// </summary>
        private bool IsPointOverSlider(Vector2 screenPoint)
        {
			CacheUIReferences();

			if (speedSliderRect == null)
			{
				return false;
			}

			return RectTransformUtility.RectangleContainsScreenPoint(speedSliderRect, screenPoint, speedSliderCamera);
        }
        
        /// <summary>
        /// Check if a screen point is over the speed UI (slider or panel)
        /// </summary>
        private bool IsPointOverSpeedUI(Vector2 screenPoint)
        {
			CacheUIReferences();

			if (speedPanelRect != null)
            {
				if (RectTransformUtility.RectangleContainsScreenPoint(speedPanelRect, screenPoint, speedPanelCamera))
				{
					return true;
				}
            }
            
            // Fallback: check slider
            return IsPointOverSlider(screenPoint);
        }
        
        /// <summary>
        /// Setup event triggers to detect user interaction with slider
        /// </summary>
        private void SetupSliderEvents()
        {
            if (speedSlider == null) return;
            
            // Add EventTrigger to slider GameObject
            EventTrigger trigger = speedSlider.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = speedSlider.gameObject.AddComponent<EventTrigger>();
            }
            
            // Clear existing triggers to avoid duplicates
            trigger.triggers.Clear();
            
            // Pointer down event
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { OnPointerDown((PointerEventData)data); });
            trigger.triggers.Add(pointerDown);
            
            // Pointer up event
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { OnPointerUp((PointerEventData)data); });
            trigger.triggers.Add(pointerUp);
            
            // Drag event
            EventTrigger.Entry drag = new EventTrigger.Entry();
            drag.eventID = EventTriggerType.Drag;
            drag.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
            trigger.triggers.Add(drag);
            
            // Also add to handle if it exists (slider handle is where users typically click)
            Transform handle = speedSlider.transform.Find("Handle Slide Area/Handle") ?? speedSlider.transform.Find("Handle");
            if (handle != null)
            {
                EventTrigger handleTrigger = handle.gameObject.GetComponent<EventTrigger>();
                if (handleTrigger == null)
                {
                    handleTrigger = handle.gameObject.AddComponent<EventTrigger>();
                }
                handleTrigger.triggers.Clear();
                handleTrigger.triggers.Add(pointerDown);
                handleTrigger.triggers.Add(pointerUp);
                handleTrigger.triggers.Add(drag);
            }
        }

		private void CacheUIReferences()
		{
			if (speedSlider != null)
			{
				if (speedSliderRect == null)
				{
					speedSliderRect = speedSlider.GetComponent<RectTransform>();
				}
				if (speedSliderCanvas == null)
				{
					speedSliderCanvas = speedSlider.GetComponentInParent<Canvas>();
				}
				speedSliderCamera = ResolveCanvasCamera(speedSliderCanvas);
			}

			if (speedPanelRoot != null)
			{
				if (speedPanelRect == null)
				{
					speedPanelRect = speedPanelRoot.GetComponent<RectTransform>();
				}
				if (speedPanelCanvas == null)
				{
					speedPanelCanvas = speedPanelRoot.GetComponentInParent<Canvas>();
				}
				speedPanelCamera = ResolveCanvasCamera(speedPanelCanvas);
			}
		}

		private Camera ResolveCanvasCamera(Canvas canvas)
		{
			if (canvas == null)
			{
				return GetMainCamera();
			}

			switch (canvas.renderMode)
			{
				case RenderMode.ScreenSpaceOverlay:
					return null;
				case RenderMode.ScreenSpaceCamera:
				case RenderMode.WorldSpace:
					return canvas.worldCamera != null ? canvas.worldCamera : GetMainCamera();
				default:
					return GetMainCamera();
			}
		}

		private static Camera GetMainCamera()
		{
			if (cachedMainCamera == null)
			{
				cachedMainCamera = Camera.main;
			}

			return cachedMainCamera;
		}
        
        /// <summary>
        /// Called when user starts interacting with slider
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            isUserInteracting = true;
            speedConfirmed = true; // User has selected a speed
            enableAutoMovement = false; // Stop auto movement permanently
            // Cancel any pending resume
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
        }
        
        /// <summary>
        /// Called when user stops interacting with slider
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            // Small delay to allow slider to settle, then resume auto-movement
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
            }
            resumeCoroutine = StartCoroutine(ResumeAutoMovement());
        }
        
        /// <summary>
        /// Resume auto movement after user releases
        /// </summary>
        private IEnumerator ResumeAutoMovement()
        {
            yield return new WaitForSeconds(0.1f);
            
            // Reset autoTime to current position for smooth continuation
            if (speedSlider != null)
            {
                float currentNormalized = (speedSlider.value - minSpeed) / (maxSpeed - minSpeed);
                autoTime = currentNormalized; // Continue from current position
            }
            
            isUserInteracting = false;
            resumeCoroutine = null;
        }
        
        /// <summary>
        /// Called when user is dragging the slider
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            isUserInteracting = true;
            speedConfirmed = true; // User has selected a speed
            enableAutoMovement = false; // Stop auto movement permanently
        }
        
        /// <summary>
        /// Called when slider value changes
        /// </summary>
        public void OnSpeedChanged(float newSpeed)
        {
            currentSpeed = Mathf.RoundToInt(newSpeed); // Convert to integer
            
            // If value changed and we're not setting it programmatically, user is interacting
            if (!isProgrammaticallyChangingValue)
            {
                isUserInteracting = true;
                speedConfirmed = true; // User has selected a speed
                enableAutoMovement = false; // Stop auto movement permanently
                // Cancel any pending resume
                if (resumeCoroutine != null)
                {
                    StopCoroutine(resumeCoroutine);
                    resumeCoroutine = null;
                }
            }
            
            UpdateSpeedDisplay();
            UpdateBallSpeed();
        }
        
        /// <summary>
        /// Update the speed text display
        /// </summary>
        private void UpdateSpeedDisplay()
        {
            if (speedText != null)
            {
                if (showSpeedInKmh)
                {
                    float displaySpeed = currentSpeed * speedMultiplier;
                    speedText.text = $"{displaySpeed:F0} km/h";
                }
                else
                {
                    speedText.text = $"{currentSpeed:F1} m/s";
                }
            }
        }
        
        /// <summary>
        /// Update the ball speed in the prefab and bowling controller
        /// </summary>
        private void UpdateBallSpeed()
        {
            // Update ball settings ScriptableObject
			if (ballSettingsSO != null)
			{
				ballSettingsSO.SetGlobalBallSpeed(currentSpeed);
			}
        }
        
        /// <summary>
        /// Get current speed (for other scripts to access)
        /// </summary>
        public int GetCurrentSpeed()
        {
            return currentSpeed;
        }
        
        /// <summary>
        /// Set speed programmatically
        /// </summary>
        public void SetSpeed(int newSpeed)
        {
            newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
            currentSpeed = newSpeed;
            
            if (speedSlider != null)
            {
                isProgrammaticallyChangingValue = true;
                speedSlider.value = newSpeed;
                isProgrammaticallyChangingValue = false;
            }
            
            UpdateSpeedDisplay();
            UpdateBallSpeed();
        }
        
        /// <summary>
        /// Reset to default speed
        /// </summary>
        public void ResetToDefault()
        {
            SetSpeed(12);
        }
        
        void OnValidate()
        {
            // Update in editor when values change
            if (Application.isPlaying)
            {
                UpdateSpeedDisplay();
                UpdateBallSpeed();
            }
        }

        [Header("UI Activation")]
        [SerializeField] private GameObject speedPanelRoot; // Root GameObject containing speed UI

        private bool speedConfirmed = false; // Track if user has confirmed/selected a speed

        /// <summary>
        /// Activate or deactivate the speed UI
        /// </summary>
        public void ActivateUI(bool activate)
        {
            if (speedPanelRoot != null)
            {
                speedPanelRoot.SetActive(activate);
            }
            else
            {
                // Fallback: try to find parent panel
                Transform parent = transform.parent;
                if (parent != null)
                {
                    parent.gameObject.SetActive(activate);
                }
                else
                {
                    gameObject.SetActive(activate);
                }
            }
            
            // Reset speed confirmation when UI is activated
            if (activate)
            {
                speedConfirmed = false;
            }

			CacheUIReferences();
        }

        /// <summary>
        /// Check if speed has been selected by user
        /// </summary>
        public bool IsSpeedSelected
        {
            get
            {
                // Speed is considered selected if user has interacted and confirmed it
                return speedConfirmed;
            }
        }
        
        /// <summary>
        /// Reset speed selection state (called when entering PitchCam state)
        /// </summary>
        public void ResetSpeedSelection()
        {
            // CRITICAL: Reset all flags FIRST to prevent immediate state transition
            speedConfirmed = false;
            isUserInteracting = false;
            enableAutoMovement = enableAutoMovementDefault; // Re-enable auto movement from default setting
            autoTime = 0f; // Reset auto time to start fresh
            
            // Cancel any pending resume coroutine
            if (resumeCoroutine != null)
            {
                StopCoroutine(resumeCoroutine);
                resumeCoroutine = null;
            }
            
            // Reset slider interaction state
            if (speedSlider != null)
            {
                // Recalculate starting direction based on current position
                float startNormalized = (currentSpeed - minSpeed) / (maxSpeed - minSpeed);
                isMovingUp = startNormalized < 0.5f;
            }
            
			// CRITICAL: Force a frame delay to ensure state machine processes the reset
			// This prevents the state from immediately transitioning if speedConfirmed was true
			if (isActiveAndEnabled && gameObject.activeInHierarchy)
			{
				StartCoroutine(DelayedResetConfirmation());
			}
        }
        
        /// <summary>
        /// Delayed reset to ensure state machine has time to process the reset
        /// </summary>
        private System.Collections.IEnumerator DelayedResetConfirmation()
        {
            yield return null; // Wait one frame
            // Double-check that speedConfirmed is still false after frame delay
			if (speedConfirmed)
			{
				speedConfirmed = false;
			}
        }
    }
}
