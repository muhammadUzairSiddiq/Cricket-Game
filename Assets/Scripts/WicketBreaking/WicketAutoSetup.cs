using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Auto-setup helper for Wicket Breaking System
    /// Automatically finds wicket components in the scene
    /// </summary>
    public class WicketAutoSetup : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool includeParent = true;
        
        [ContextMenu("Auto-Setup Wicket Components")]
        public void AutoSetup()
        {
            WicketBreakingSystem wicketSystem = GetComponent<WicketBreakingSystem>();
            if (wicketSystem == null)
            {
                Debug.LogError("WicketBreakingSystem component not found!");
                return;
            }
            
            // Use reflection to access private fields
            var stumpField = typeof(WicketBreakingSystem).GetField("wicketStumps", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var bailField = typeof(WicketBreakingSystem).GetField("wicketBails", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Find all child transforms
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            System.Collections.Generic.List<Transform> stumps = new System.Collections.Generic.List<Transform>();
            System.Collections.Generic.List<Transform> bails = new System.Collections.Generic.List<Transform>();
            
            // If parent should be included, add it to stumps
            if (includeParent && transform.childCount > 0)
            {
                // Find the main mesh/cylinder objects
                foreach (Transform child in allChildren)
                {
                    string name = child.name.ToLower();
                    
                    // Check for stump-like objects (cylinders, stumps, vertical pieces)
                    if (name.Contains("cylinder") || 
                        name.Contains("stump") || 
                        name.Contains("wicket") ||
                        name.Contains("post"))
                    {
                        // Exclude bails and small pieces
                        if (!name.Contains("bail") && 
                            !name.Contains("top") && 
                            !name.Contains("cap"))
                        {
                            stumps.Add(child);
                        }
                    }
                    
                    // Check for bail-like objects
                    if (name.Contains("bail") || name.Contains("top"))
                    {
                        bails.Add(child);
                    }
                }
            }
            
            // Set the found components
            if (stumpField != null)
            {
                stumpField.SetValue(wicketSystem, stumps.ToArray());
                Debug.Log($"Auto-setup: Found {stumps.Count} stumps");
            }
            
            if (bailField != null)
            {
                bailField.SetValue(wicketSystem, bails.ToArray());
                Debug.Log($"Auto-setup: Found {bails.Count} bails");
            }
        }
        
        void Start()
        {
            if (autoSetupOnStart)
            {
                AutoSetup();
            }
        }
    }
}

