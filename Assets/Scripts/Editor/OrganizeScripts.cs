using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CricketGame.EditorTools
{
	public static class OrganizeScripts
	{
		private const string TestScenePath = "Assets/test.unity";
		private const string ScriptsRoot = "Assets/Scripts";
		private const string TestFolder = ScriptsRoot + "/TestScene";
		private const string CoreFolder = ScriptsRoot + "/Core";
		private const string OtherFolder = ScriptsRoot + "/Other";

		[MenuItem("Tools/Cricket Game/Organize Scripts (Test vs Others)")]
		public static void Organize()
		{
			EnsureFolders();

			// Collect script GUIDs referenced in the test scene
			var testSceneGuids = CollectScriptGuidsFromScene(TestScenePath);
			if (testSceneGuids.Count == 0)
			{
				return;
			}

			// Map GUIDs to asset paths that are C# scripts beneath Assets/Scripts
			var testSceneScriptPaths = testSceneGuids
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => !string.IsNullOrEmpty(p) && p.EndsWith(".cs") && p.StartsWith(ScriptsRoot))
				.Distinct()
				.ToList();

			// Move those to TestScene folder (preserves .meta GUIDs)
			foreach (var scriptPath in testSceneScriptPaths)
			{
				MoveAssetIfNeeded(scriptPath, TestFolder);
			}

			// Move remaining scripts under ScriptsRoot that are not in TestScene folder to Core/Other
			var allScripts = AssetDatabase.FindAssets("t:MonoScript", new[] { ScriptsRoot })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(p => p.EndsWith(".cs"))
				.ToList();

			foreach (var path in allScripts)
			{
				if (path.StartsWith(TestFolder)) continue; // already placed

				// Keep high-level systems in Core, utilities/markdown etc in Other
				var fileName = Path.GetFileName(path);
				bool shouldBeCore = fileName.Contains("Cricket") ||
					fileName.Contains("DeliverySystem") || fileName.Contains("BallSettings") ||
					fileName.Contains("BowlingMachine") || fileName.Contains("ContinuousBowlingTest_") == false && fileName.Contains("Test") == false;

				MoveAssetIfNeeded(path, shouldBeCore ? CoreFolder : OtherFolder);
			}

			AssetDatabase.Refresh();
		}

		[MenuItem("Tools/Cricket Game/Rename In/Out Swing → Seam In/Out")]
		public static void RenameSwingScriptsToSeam()
		{
			var renameMap = new Dictionary<string, string>
			{
				{"InSwing.cs", "SeamIn.cs"},
				{"OutSwing.cs", "SeamOut.cs"},
				{"InSwingDelivery.cs", "SeamInDelivery.cs"},
				{"OutSwingDelivery.cs", "SeamOutDelivery.cs"}
			};

			int renamed = 0;
			foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var fileName = Path.GetFileName(path);
				if (renameMap.TryGetValue(fileName, out var newFileName))
				{
					var newNameNoExt = Path.GetFileNameWithoutExtension(newFileName);
					var error = AssetDatabase.RenameAsset(path, newNameNoExt);
					if (string.IsNullOrEmpty(error))
					{
						renamed++;
					}
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		[MenuItem("Tools/Cricket Game/Rename Bowling Script → BowlingController.cs")]
		public static void RenameBowlingScriptFile()
		{
			// Find the script file anywhere in Assets
			var guids = AssetDatabase.FindAssets("ContinuousBowlingTest_WithBounce t:MonoScript", new[] { "Assets" });
			int count = 0;
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.EndsWith("ContinuousBowlingTest_WithBounce.cs")) continue;
				var error = AssetDatabase.RenameAsset(path, "BowlingController");
				if (string.IsNullOrEmpty(error))
				{
					count++;
				}
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private static void EnsureFolders()
		{
			CreateFolderIfMissing("Assets", "Editor");
			CreateFolderIfMissing(ScriptsRoot, "TestScene");
			CreateFolderIfMissing(ScriptsRoot, "Core");
			CreateFolderIfMissing(ScriptsRoot, "Other");
		}

		private static void CreateFolderIfMissing(string parent, string child)
		{
			var full = Path.Combine(parent, child).Replace('\\', '/');
			if (!AssetDatabase.IsValidFolder(full))
			{
				AssetDatabase.CreateFolder(parent, child);
			}
		}

		private static void MoveAssetIfNeeded(string assetPath, string targetFolder)
		{
			var fileName = Path.GetFileName(assetPath);
			var targetPath = Path.Combine(targetFolder, fileName).Replace('\\', '/');
			if (assetPath.Equals(targetPath)) return;

			var result = AssetDatabase.MoveAsset(assetPath, targetPath);
			if (!string.IsNullOrEmpty(result))
			{
				// keep failures silent to avoid spam; return to avoid endless logging
			}
		}

		private static HashSet<string> CollectScriptGuidsFromScene(string scenePath)
		{
			var guids = new HashSet<string>();
			if (!File.Exists(scenePath)) return guids;

			// Parse the YAML scene and collect 'm_Script: {fileID: 11500000, guid: XXXXX, type: 3}' entries
			foreach (var line in File.ReadLines(scenePath))
			{
				var idx = line.IndexOf("guid:");
				if (idx < 0) continue;
				var guid = line.Substring(idx + 5).Trim();
				guid = guid.Split(',')[0].Trim();
				if (guid.Length == 32) guids.Add(guid);
			}

			return guids;
		}
	}
}


