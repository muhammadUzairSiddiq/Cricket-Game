using UnityEngine;
using UnityEditor;
using CricketGame;

[CustomEditor(typeof(BowlingController))]
public class BowlerSelectionEditor : Editor
{
    private BowlingController bowlingController;
    private string[] bowlerNames;
    private int selectedIndex = -1;

    void OnEnable()
    {
        bowlingController = (BowlingController)target;
        UpdateBowlerNames();
    }

    void UpdateBowlerNames()
    {
        if (bowlingController.availableBowlerPrefabs != null)
        {
            bowlerNames = new string[bowlingController.availableBowlerPrefabs.Length + 1];
            bowlerNames[0] = "None";
            
            for (int i = 0; i < bowlingController.availableBowlerPrefabs.Length; i++)
            {
                if (bowlingController.availableBowlerPrefabs[i] != null)
                {
                    bowlerNames[i + 1] = bowlingController.availableBowlerPrefabs[i].name;
                }
                else
                {
                    bowlerNames[i + 1] = "NULL";
                }
            }
        }
        else
        {
            bowlerNames = new string[] { "None" };
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bowler Selection", EditorStyles.boldLabel);

        // Update bowler names if the array has changed
        if (bowlingController.availableBowlerPrefabs != null)
        {
            UpdateBowlerNames();
        }

        // Find current selection index
        selectedIndex = -1;
        if (bowlingController.selectedBowlerPrefab != null && bowlingController.availableBowlerPrefabs != null)
        {
            for (int i = 0; i < bowlingController.availableBowlerPrefabs.Length; i++)
            {
                if (bowlingController.availableBowlerPrefabs[i] == bowlingController.selectedBowlerPrefab)
                {
                    selectedIndex = i + 1; // +1 because index 0 is "None"
                    break;
                }
            }
        }
        else
        {
            selectedIndex = 0; // None
        }

        // Draw dropdown
        int newIndex = EditorGUILayout.Popup("Select Bowler", selectedIndex, bowlerNames);
        
        if (newIndex != selectedIndex)
        {
            if (newIndex == 0)
            {
                bowlingController.selectedBowlerPrefab = null;
            }
            else
            {
                bowlingController.selectedBowlerPrefab = bowlingController.availableBowlerPrefabs[newIndex - 1];
            }
            
            EditorUtility.SetDirty(bowlingController);
        }

        EditorGUILayout.Space();
        
        // Show current selection info
        if (bowlingController.selectedBowlerPrefab != null)
        {
            EditorGUILayout.LabelField("Current Selection:", bowlingController.selectedBowlerPrefab.name);
            
        // Show BowlerProfile info if available
        BowlerProfile profile = bowlingController.selectedBowlerPrefab.GetComponent<BowlerProfile>();
        if (profile != null)
        {
            EditorGUILayout.LabelField("Default Delivery:", profile.GetDefaultDeliveryType().ToString());
            
            var allowedDeliveries = profile.GetAllowedDeliveryTypes();
            if (allowedDeliveries.Count > 0)
            {
                string deliveries = string.Join(", ", allowedDeliveries);
                EditorGUILayout.LabelField("Allowed Deliveries:", deliveries);
            }
        }
        
        // Show spawn mapping info if available
        if (bowlingController.bowlerSpawnMappings != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Mapping:", EditorStyles.boldLabel);
            
            // Find the mapping for the selected bowler
            BowlerSpawnMapping mapping = null;
            foreach (var map in bowlingController.bowlerSpawnMappings)
            {
                if (map != null && map.bowlerPrefab == bowlingController.selectedBowlerPrefab)
                {
                    mapping = map;
                    break;
                }
            }
            
            if (mapping != null)
            {
                EditorGUILayout.LabelField("Spawn01:", mapping.spawn01 != null ? mapping.spawn01.name : "Not Set");
                EditorGUILayout.LabelField("Spawn02:", mapping.spawn02 != null ? mapping.spawn02.name : "Not Set");
                EditorGUILayout.LabelField("Current:", mapping.useSpawn01 ? "Spawn01" : "Spawn02");
            }
            else
            {
                EditorGUILayout.LabelField("No spawn mapping found for this bowler");
            }
        }
        }
        else
        {
            EditorGUILayout.LabelField("Current Selection:", "None");
        }

        EditorGUILayout.Space();
        
        // Quick action buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Instantiate Selected"))
        {
            bowlingController.InstantiateSelectedBowler();
        }
        
        if (GUILayout.Button("Destroy Current"))
        {
            bowlingController.DestroyCurrentBowlerInstance();
        }
        
        EditorGUILayout.EndHorizontal();
    }
}
