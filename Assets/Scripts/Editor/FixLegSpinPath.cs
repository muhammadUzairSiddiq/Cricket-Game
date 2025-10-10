using UnityEngine;
using UnityEditor;

namespace CricketGame
{
    /// <summary>
    /// Editor utility to fix LegSpinDelivery curved path settings
    /// </summary>
    public class FixLegSpinPath : Editor
    {
        [MenuItem("Cricket Game/Fix Leg Spin - Disable Curved Path")]
        static void DisableLegSpinCurvedPath()
        {
            // Find all LegSpinDelivery components in the scene
            LegSpinDelivery[] allLegSpinDeliveries = FindObjectsOfType<LegSpinDelivery>(true);
            
            if (allLegSpinDeliveries.Length == 0)
            {
                Debug.LogWarning("⚠️ No LegSpinDelivery components found in the scene!");
                EditorUtility.DisplayDialog(
                    "No Components Found", 
                    "No LegSpinDelivery components found in the current scene.\n\nMake sure your scene is loaded and has a LegSpinDelivery component.", 
                    "OK"
                );
                return;
            }
            
            int fixedCount = 0;
            foreach (LegSpinDelivery legSpin in allLegSpinDeliveries)
            {
                if (legSpin.enableCurvedPath)
                {
                    // Mark for undo
                    Undo.RecordObject(legSpin, "Disable Leg Spin Curved Path");
                    
                    legSpin.enableCurvedPath = false;
                    EditorUtility.SetDirty(legSpin);
                    
                    Debug.Log($"✅ Fixed: {legSpin.gameObject.name} - Curved path DISABLED");
                    fixedCount++;
                }
                else
                {
                    Debug.Log($"✓ Already correct: {legSpin.gameObject.name} - Curved path already disabled");
                }
            }
            
            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog(
                    "Leg Spin Fixed!", 
                    $"Successfully disabled curved path for {fixedCount} LegSpinDelivery component(s)!\n\nLeg Spin will now use STRAIGHT PATH.", 
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Already Correct", 
                    $"All {allLegSpinDeliveries.Length} LegSpinDelivery component(s) already have curved path disabled.\n\nIf you're still seeing curved path, check the console for more details.", 
                    "OK"
                );
            }
            
            // Show summary
            Debug.Log($"🎯 LEG SPIN PATH FIX COMPLETE:");
            Debug.Log($"   - Found: {allLegSpinDeliveries.Length} LegSpinDelivery component(s)");
            Debug.Log($"   - Fixed: {fixedCount} component(s)");
            Debug.Log($"   - Already correct: {allLegSpinDeliveries.Length - fixedCount} component(s)");
        }
        
        [MenuItem("Cricket Game/Check Leg Spin Path Mode")]
        static void CheckLegSpinPathMode()
        {
            LegSpinDelivery[] allLegSpinDeliveries = FindObjectsOfType<LegSpinDelivery>(true);
            
            if (allLegSpinDeliveries.Length == 0)
            {
                Debug.LogWarning("⚠️ No LegSpinDelivery components found in the scene!");
                EditorUtility.DisplayDialog(
                    "No Components Found", 
                    "No LegSpinDelivery components found in the current scene.", 
                    "OK"
                );
                return;
            }
            
            Debug.Log($"🎯 LEG SPIN PATH MODE CHECK - Found {allLegSpinDeliveries.Length} component(s):");
            Debug.Log("═══════════════════════════════════════════════════════");
            
            string message = "";
            foreach (LegSpinDelivery legSpin in allLegSpinDeliveries)
            {
                string pathMode = legSpin.IsCurvedPathEnabled() ? "CURVED PATH ❌" : "STRAIGHT PATH ✅";
                string status = $"GameObject: {legSpin.gameObject.name}\n" +
                               $"  - Enable Leg Spin: {legSpin.enableLegSpin}\n" +
                               $"  - Enable Curved Path: {legSpin.enableCurvedPath}\n" +
                               $"  - Path Mode: {pathMode}\n";
                
                Debug.Log(status);
                message += status + "\n";
            }
            
            Debug.Log("═══════════════════════════════════════════════════════");
            
            EditorUtility.DisplayDialog(
                "Leg Spin Path Mode", 
                message, 
                "OK"
            );
        }
    }
}

