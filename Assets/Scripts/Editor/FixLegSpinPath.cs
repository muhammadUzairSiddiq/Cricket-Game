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
                    fixedCount++;
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
            
        }
        
        [MenuItem("Cricket Game/Check Leg Spin Path Mode")]
        static void CheckLegSpinPathMode()
        {
            LegSpinDelivery[] allLegSpinDeliveries = FindObjectsOfType<LegSpinDelivery>(true);
            
            if (allLegSpinDeliveries.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Components Found", 
                    "No LegSpinDelivery components found in the current scene.", 
                    "OK"
                );
                return;
            }
            
            string message = "";
            foreach (LegSpinDelivery legSpin in allLegSpinDeliveries)
            {
                string pathMode = legSpin.IsCurvedPathEnabled() ? "CURVED PATH ❌" : "STRAIGHT PATH ✅";
                string status = $"GameObject: {legSpin.gameObject.name}\n" +
                               $"  - Enable Leg Spin: {legSpin.enableLegSpin}\n" +
                               $"  - Enable Curved Path: {legSpin.enableCurvedPath}\n" +
                               $"  - Path Mode: {pathMode}\n";
                
                message += status + "\n";
            }
            
            EditorUtility.DisplayDialog(
                "Leg Spin Path Mode", 
                message, 
                "OK"
            );
        }
    }
}

