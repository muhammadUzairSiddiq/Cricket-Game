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
        [SerializeField] private bool enableAutoMovement = true;
        [SerializeField] private float autoMovementSpeed = 2f; // How fast the slider moves (cycles per second)
        private float autoTime = 0f; // internal time for ping-pong
        [SerializeField] private bool stopOnAnyTap = true; // Stop auto on any screen tap/click
        
        private bool isUserInteracting = false;
        private bool isMovingUp = true;
        private Coroutine resumeCoroutine;
        private bool isProgrammaticallyChangingValue = false; // Track when we're changing value via script
        
        void Start()
        {
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
            if (speedSlider == null) return;
            
            bool inputDetected = false;

            // Global tap/click anywhere to stop auto movement immediately
            if (stopOnAnyTap)
            {
                // Mouse click anywhere
                if (Input.GetMouseButtonDown(0))
                {
                    isUserInteracting = true;
                    inputDetected = true;
                    return; // Stop here; resume will be handled on release
                }
                // Touch anywhere
                if (Input.touchCount > 0)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        Touch t = Input.GetTouch(i);
                        if (t.phase == TouchPhase.Began)
                        {
                            isUserInteracting = true;
                            inputDetected = true;
                            return; // Stop here; resume will be handled on touch end
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
            if (speedSlider == null) return false;
            
            RectTransform sliderRect = speedSlider.GetComponent<RectTransform>();
            if (sliderRect == null) return false;
            
            Canvas canvas = speedSlider.GetComponentInParent<Canvas>();
            if (canvas == null) return false;
            
            // Handle different canvas render modes
            Camera cam = null;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            {
                cam = canvas.worldCamera;
                if (cam == null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    cam = Camera.main;
                }
            }
            
            return RectTransformUtility.RectangleContainsScreenPoint(sliderRect, screenPoint, cam);
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
        
        /// <summary>
        /// Called when user starts interacting with slider
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            isUserInteracting = true;
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
        }
        
        /// <summary>
        /// Called when slider value changes
        /// </summary>
        public void OnSpeedChanged(float newSpeed)
        {
            currentSpeed = Mathf.RoundToInt(newSpeed); // Convert to integer
            
            // If value changed and we're not setting it programmatically, user is interacting
            if (!isProgrammaticallyChangingValue && enableAutoMovement)
            {
                isUserInteracting = true;
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
                Debug.Log($"🎯 SPEED CONTROLLER: Calling SetGlobalBallSpeed({currentSpeed}) on ballSettingsSO");
                ballSettingsSO.SetGlobalBallSpeed(currentSpeed);
                Debug.Log($"🎯 SPEED CONTROLLER: Updated ball settings speed to {currentSpeed} m/s");
            }
            else
            {
                Debug.LogError("🚨 SPEED CONTROLLER: ballSettingsSO is null! Please assign it in the Inspector.");
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
    }
}
