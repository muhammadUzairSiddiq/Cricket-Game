using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CricketGame.Graphics
{
	/// <summary>
	/// Centralised runtime controller for scene-wide graphics features such as fog, lighting and post-processing.
	/// Captures the original project settings on Awake so they can be restored at any time via <see cref="ResetToOriginalSettings"/>.
	/// </summary>
	[AddComponentMenu("Cricket Game/Graphics/Graphics Settings Manager")]
	[DisallowMultipleComponent]
	public class GraphicsSettingsManager : MonoBehaviour
	{
		[Header("Scene References")]
		[SerializeField, Tooltip("Global volume that contains post-processing overrides for the scene.")]
		private Volume globalVolume;

		[Header("Auto Apply")]
		[SerializeField, Tooltip("Apply the preset fog and post-processing values automatically on Start.")]
		private bool applyPresetOnStart = false;
		[SerializeField] private FogSettings presetFog = FogSettings.DefaultFog;
		[SerializeField] private PostProcessingSettings presetPostFX = PostProcessingSettings.None;
		[SerializeField] private LightingSettings presetLighting = LightingSettings.DefaultLighting;
		[SerializeField, Tooltip("Optional quality level to switch to on Start. -1 keeps the current level.")]
		private int presetQualityLevel = -1;

		// --- Snapshots ---
		private bool fogSnapshotCaptured;
		private FogSnapshot originalFog;
		private LightingSnapshot originalLighting;
		private int originalQualityLevel;
		private VolumeProfile originalVolumeProfileClone;

		private void Awake()
		{
			CaptureOriginalGraphicsState();
		}

		private void Start()
		{
			if (applyPresetOnStart)
			{
				ApplyFogSettings(presetFog);
				ApplyLightingSettings(presetLighting);
				ApplyPostProcessing(presetPostFX);
				if (presetQualityLevel >= 0)
				{
					ApplyQualityLevel(presetQualityLevel);
				}
			}
		}

		/// <summary>
		/// Saves the current fog, lighting, quality level and post-processing profile so they can be restored later.
		/// </summary>
		private void CaptureOriginalGraphicsState()
		{
			originalQualityLevel = QualitySettings.GetQualityLevel();

			originalFog = new FogSnapshot(RenderSettings.fog,
				RenderSettings.fogMode,
				RenderSettings.fogColor,
				RenderSettings.fogDensity,
				RenderSettings.fogStartDistance,
				RenderSettings.fogEndDistance);
			fogSnapshotCaptured = true;

			originalLighting = new LightingSnapshot(RenderSettings.ambientMode,
				RenderSettings.ambientLight,
				RenderSettings.ambientIntensity,
				RenderSettings.skybox);

			if (globalVolume != null && globalVolume.profile != null)
			{
				// Clone the original profile so we can restore it later without mutating the asset.
				originalVolumeProfileClone = Instantiate(globalVolume.profile);
				originalVolumeProfileClone.name = $"{globalVolume.profile.name}_OriginalClone";
			}
		}

		#region Public API

		/// <summary>
		/// Applies custom fog values at runtime.
		/// </summary>
		public void ApplyFogSettings(FogSettings settings)
		{
			RenderSettings.fog = settings.enableFog;
			RenderSettings.fogMode = settings.mode;
			RenderSettings.fogColor = settings.color;
			RenderSettings.fogDensity = Mathf.Max(0f, settings.density);
			RenderSettings.fogStartDistance = Mathf.Max(0f, settings.startDistance);
			RenderSettings.fogEndDistance = Mathf.Max(settings.startDistance, settings.endDistance);
		}

		/// <summary>
		/// Applies ambient lighting and skybox tweaks.
		/// </summary>
		public void ApplyLightingSettings(LightingSettings settings)
		{
			if (settings.overrideAmbientLight)
			{
				RenderSettings.ambientMode = settings.ambientMode;
				RenderSettings.ambientLight = settings.ambientLight;
				RenderSettings.ambientIntensity = Mathf.Max(0f, settings.ambientIntensity);
			}

			if (settings.overrideSkyboxMaterial && settings.skyboxMaterial != null)
			{
				RenderSettings.skybox = settings.skyboxMaterial;
			}
		}

		/// <summary>
		/// Applies post-processing overrides stored in the supplied settings bundle.
		/// </summary>
		public void ApplyPostProcessing(PostProcessingSettings settings)
		{
			if (globalVolume == null || globalVolume.profile == null)
			{
				return;
			}

			if (settings.applyBloom && TryGetVolumeComponent(out Bloom bloom))
			{
				bloom.active = true;
				SetOverride(bloom.intensity, settings.bloomIntensity);
				SetOverride(bloom.threshold, settings.bloomThreshold);
			}
			else if (TryGetVolumeComponent(out Bloom bloomToDisable))
			{
				bloomToDisable.active = false;
			}

			if (settings.applyColorAdjustments && TryGetVolumeComponent(out ColorAdjustments colorAdjustments))
			{
				colorAdjustments.active = true;
				SetOverride(colorAdjustments.postExposure, settings.postExposure);
				SetOverride(colorAdjustments.saturation, settings.saturation);
				SetOverride(colorAdjustments.contrast, settings.contrast);
				colorAdjustments.colorFilter.overrideState = true;
				colorAdjustments.colorFilter.value = settings.colorFilter;
			}
			else if (TryGetVolumeComponent(out ColorAdjustments colourToDisable))
			{
				colourToDisable.active = false;
			}

			if (settings.applyDepthOfField && TryGetVolumeComponent(out DepthOfField depthOfField))
			{
				depthOfField.active = true;
				SetOverride(depthOfField.focusDistance, settings.focusDistance);
				SetOverride(depthOfField.aperture, settings.aperture);
				SetOverride(depthOfField.focalLength, settings.focalLength);
			}
			else if (TryGetVolumeComponent(out DepthOfField dofToDisable))
			{
				dofToDisable.active = false;
			}

			if (settings.applyVignette && TryGetVolumeComponent(out Vignette vignette))
			{
				vignette.active = true;
				SetOverride(vignette.intensity, settings.vignetteIntensity);
				SetOverride(vignette.smoothness, settings.vignetteSmoothness);
				vignette.color.overrideState = true;
				vignette.color.value = settings.vignetteColor;
			}
			else if (TryGetVolumeComponent(out Vignette vignetteToDisable))
			{
				vignetteToDisable.active = false;
			}
		}

		/// <summary>
		/// Wrapper for <see cref="QualitySettings.SetQualityLevel(int)"/> that keeps the stored original level up-to-date.
		/// </summary>
		public void ApplyQualityLevel(int qualityLevel, bool applyExpensiveChanges = true)
		{
			qualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);
			QualitySettings.SetQualityLevel(qualityLevel, applyExpensiveChanges);
		}

		/// <summary>
		/// Restores fog, lighting, quality level and post-processing profile to the state captured on Awake.
		/// </summary>
		[ContextMenu("Reset Graphics To Original")]
		public void ResetToOriginalSettings()
		{
			if (fogSnapshotCaptured)
			{
				RenderSettings.fog = originalFog.Enabled;
				RenderSettings.fogMode = originalFog.Mode;
				RenderSettings.fogColor = originalFog.Color;
				RenderSettings.fogDensity = originalFog.Density;
				RenderSettings.fogStartDistance = originalFog.StartDistance;
				RenderSettings.fogEndDistance = originalFog.EndDistance;
			}

			RenderSettings.ambientMode = originalLighting.AmbientMode;
			RenderSettings.ambientLight = originalLighting.AmbientLight;
			RenderSettings.ambientIntensity = originalLighting.AmbientIntensity;
			RenderSettings.skybox = originalLighting.SkyboxMaterial;

			QualitySettings.SetQualityLevel(originalQualityLevel);

			if (globalVolume != null && originalVolumeProfileClone != null)
			{
				var restoredProfile = Instantiate(originalVolumeProfileClone);
				restoredProfile.name = $"{originalVolumeProfileClone.name}_RestoredRuntimeCopy";
				globalVolume.profile = restoredProfile;
			}
		}

		#endregion

		#region Helper Methods

		private bool TryGetVolumeComponent<T>(out T component) where T : VolumeComponent
		{
			component = null;
			if (globalVolume == null || globalVolume.profile == null)
			{
				return false;
			}
			return globalVolume.profile.TryGet(out component);
		}

		private static void SetOverride(FloatParameter parameter, float value)
		{
			if (parameter == null) return;
			parameter.overrideState = true;
			parameter.value = value;
		}

		private static void SetOverride(ClampedFloatParameter parameter, float value)
		{
			if (parameter == null) return;
			parameter.overrideState = true;
			parameter.value = value;
		}

		#endregion

		#region Nested Types

		[System.Serializable]
		public struct FogSettings
		{
			public bool enableFog;
			public FogMode mode;
			public Color color;
			[Range(0f, 0.2f)] public float density;
			public float startDistance;
			public float endDistance;

			public FogSettings(bool enableFog, FogMode mode, Color color, float density, float startDistance, float endDistance)
			{
				this.enableFog = enableFog;
				this.mode = mode;
				this.color = color;
				this.density = density;
				this.startDistance = startDistance;
				this.endDistance = endDistance;
			}

			public static FogSettings DefaultFog => new FogSettings(true, FogMode.ExponentialSquared, new Color(0.85f, 0.9f, 0.95f), 0.015f, 10f, 120f);
		}

		private readonly struct FogSnapshot
		{
			public readonly bool Enabled;
			public readonly FogMode Mode;
			public readonly Color Color;
			public readonly float Density;
			public readonly float StartDistance;
			public readonly float EndDistance;

			public FogSnapshot(bool enabled, FogMode mode, Color color, float density, float startDistance, float endDistance)
			{
				Enabled = enabled;
				Mode = mode;
				Color = color;
				Density = density;
				StartDistance = startDistance;
				EndDistance = endDistance;
			}
		}

		[System.Serializable]
		public class PostProcessingSettings
		{
			public bool applyBloom;
			public float bloomIntensity = 1.2f;
			public float bloomThreshold = 1f;

			public bool applyColorAdjustments;
			[ColorUsage(false, true)] public Color colorFilter = Color.white;
			public float postExposure = 0f;
			public float saturation = 0f;
			public float contrast = 0f;

			public bool applyDepthOfField;
			public float focusDistance = 12f;
			public float aperture = 5.6f;
			public float focalLength = 50f;

			public bool applyVignette;
			public float vignetteIntensity = 0.3f;
			public float vignetteSmoothness = 0.8f;
			[ColorUsage(false, true)] public Color vignetteColor = Color.black;

			public static PostProcessingSettings None => new PostProcessingSettings();
		}

		[System.Serializable]
		public struct LightingSettings
		{
			public bool overrideAmbientLight;
			public UnityEngine.Rendering.AmbientMode ambientMode;
			public Color ambientLight;
			public float ambientIntensity;

			public bool overrideSkyboxMaterial;
			public Material skyboxMaterial;

			public LightingSettings(bool overrideAmbientLight, UnityEngine.Rendering.AmbientMode ambientMode, Color ambientLight, float ambientIntensity, bool overrideSkyboxMaterial, Material skyboxMaterial)
			{
				this.overrideAmbientLight = overrideAmbientLight;
				this.ambientMode = ambientMode;
				this.ambientLight = ambientLight;
				this.ambientIntensity = ambientIntensity;
				this.overrideSkyboxMaterial = overrideSkyboxMaterial;
				this.skyboxMaterial = skyboxMaterial;
			}

			public static LightingSettings DefaultLighting => new LightingSettings(false, UnityEngine.Rendering.AmbientMode.Skybox, Color.white, 1f, false, null);
		}

		private readonly struct LightingSnapshot
		{
			public readonly UnityEngine.Rendering.AmbientMode AmbientMode;
			public readonly Color AmbientLight;
			public readonly float AmbientIntensity;
			public readonly Material SkyboxMaterial;

			public LightingSnapshot(UnityEngine.Rendering.AmbientMode ambientMode, Color ambientLight, float ambientIntensity, Material skyboxMaterial)
			{
				AmbientMode = ambientMode;
				AmbientLight = ambientLight;
				AmbientIntensity = ambientIntensity;
				SkyboxMaterial = skyboxMaterial;
			}
		}

		#endregion
	}
}

