using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

namespace CricketGame.Core
{
    /// <summary>
    /// Centralised graphics manager for runtime fog configuration.
    /// Captures the original project values and allows quick tweaks/reset.
    /// </summary>
    public class GraphicsQualityManager : MonoBehaviour
    {
        [Header("RenderSettings Fog")]
        [Tooltip("Enable to let this component control the built-in RenderSettings fog values.")]
        [SerializeField] private bool manageRenderSettingsFog = true;
        [SerializeField] private bool fogEnabled = true;
        [SerializeField] private FogMode fogMode = FogMode.Linear;
        [SerializeField] private Color fogColor = Color.gray;
        [Tooltip("Fog density used when FogMode is Linear the value is clamped by start/end distances.")]
        [SerializeField] private float fogDensity = 0.01f;
        [Tooltip("Distance from camera where fog begins (Linear mode).")]
        [SerializeField] private float fogStartDistance = 0f;
        [Tooltip("Distance from camera where fog is fully opaque (Linear mode).")]
        [SerializeField] private float fogEndDistance = 300f;

        [Header("URP Volume Fog (Optional)")]
        [Tooltip("Volume that contains a URP Fog override to be controlled by this manager.")]
        [SerializeField] private Volume volumeWithFog;
        [Tooltip("Enable to override the URP fog settings when applying changes.")]
        [SerializeField] private bool manageVolumeFog = true;
        [SerializeField] private bool volumeFogEnabled = true;
        [Tooltip("Mean free path controls how far the eye can see – higher values result in clearer scenes.")]
        [SerializeField] private float volumeMeanFreePath = 300f;
        [Tooltip("Controls the height fog starts from. Increase to lift fog from the ground.")]
        [SerializeField] private float volumeBaseHeight = 0f;
        [Tooltip("Controls the maximum height affected by fog.")]
        [SerializeField] private float volumeMaximumHeight = 100f;
        [SerializeField] private Color volumeAlbedo = Color.white;

        private FogState originalRenderSettings;
        private VolumeFogState originalVolumeSettings;
        private VolumeComponent cachedFogComponent;
        private bool hasOriginalRenderSettings;
        private bool hasOriginalVolumeSettings;

        private void Awake()
        {
            CacheOriginalSettings();
            ApplySettings();
        }

        /// <summary>
        /// Cache the current project fog configuration so that ResetSettings can restore it.
        /// </summary>
        public void CacheOriginalSettings()
        {
            if (!hasOriginalRenderSettings)
            {
                originalRenderSettings = new FogState(RenderSettings.fog,
                                                      RenderSettings.fogMode,
                                                      RenderSettings.fogColor,
                                                      RenderSettings.fogDensity,
                                                      RenderSettings.fogStartDistance,
                                                      RenderSettings.fogEndDistance);
                hasOriginalRenderSettings = true;
            }

            var fogComponent = FindFogComponent();
            if (fogComponent != null)
            {
                if (!hasOriginalVolumeSettings)
                {
                    originalVolumeSettings = new VolumeFogState(fogComponent.active,
                                                                GetParameterValue<float>(fogComponent, "meanFreePath", volumeMeanFreePath),
                                                                GetParameterValue<float>(fogComponent, "baseHeight", volumeBaseHeight),
                                                                GetParameterValue<float>(fogComponent, "maximumHeight", volumeMaximumHeight),
                                                                GetParameterValue<Color>(fogComponent, "albedo", volumeAlbedo));
                    hasOriginalVolumeSettings = true;
                }
            }
        }

        /// <summary>
        /// Apply the values currently defined in the inspector.
        /// </summary>
        [ContextMenu("Apply Settings")]
        public void ApplySettings()
        {
            if (manageRenderSettingsFog)
            {
                RenderSettings.fog = fogEnabled;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = Mathf.Max(0f, fogDensity);
                RenderSettings.fogStartDistance = Mathf.Max(0f, fogStartDistance);
                RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogStartDistance + 1f, fogEndDistance);
            }

            if (manageVolumeFog)
            {
                var fogComponent = FindFogComponent();
                if (fogComponent != null)
                {
                    fogComponent.active = volumeFogEnabled;
                    SetParameterValue(fogComponent, "meanFreePath", Mathf.Max(1f, volumeMeanFreePath));
                    SetParameterValue(fogComponent, "baseHeight", volumeBaseHeight);
                    SetParameterValue(fogComponent, "maximumHeight", Mathf.Max(volumeBaseHeight, volumeMaximumHeight));
                    SetParameterValue(fogComponent, "albedo", volumeAlbedo);
                }
            }
        }

        /// <summary>
        /// Restore the cached project values.
        /// </summary>
        [ContextMenu("Reset Settings")]
        public void ResetSettings()
        {
            if (hasOriginalRenderSettings)
            {
                RenderSettings.fog = originalRenderSettings.Enabled;
                RenderSettings.fogMode = originalRenderSettings.Mode;
                RenderSettings.fogColor = originalRenderSettings.Color;
                RenderSettings.fogDensity = originalRenderSettings.Density;
                RenderSettings.fogStartDistance = originalRenderSettings.StartDistance;
                RenderSettings.fogEndDistance = originalRenderSettings.EndDistance;
            }

            var fogComponent = FindFogComponent();
            if (hasOriginalVolumeSettings && fogComponent != null)
            {
                fogComponent.active = originalVolumeSettings.Enabled;
                SetParameterValue(fogComponent, "meanFreePath", originalVolumeSettings.MeanFreePath);
                SetParameterValue(fogComponent, "baseHeight", originalVolumeSettings.BaseHeight);
                SetParameterValue(fogComponent, "maximumHeight", originalVolumeSettings.MaximumHeight);
                SetParameterValue(fogComponent, "albedo", originalVolumeSettings.Albedo);
            }
        }

        /// <summary>
        /// Update the inspector-managed fields based on the current scene configuration.
        /// </summary>
        [ContextMenu("Sync From Scene")]
        public void SyncFromCurrentScene()
        {
            fogEnabled = RenderSettings.fog;
            fogMode = RenderSettings.fogMode;
            fogColor = RenderSettings.fogColor;
            fogDensity = RenderSettings.fogDensity;
            fogStartDistance = RenderSettings.fogStartDistance;
            fogEndDistance = RenderSettings.fogEndDistance;

            var fogComponent = FindFogComponent();
            if (fogComponent != null)
            {
                volumeFogEnabled = fogComponent.active;
                volumeMeanFreePath = GetParameterValue<float>(fogComponent, "meanFreePath", volumeMeanFreePath);
                volumeBaseHeight = GetParameterValue<float>(fogComponent, "baseHeight", volumeBaseHeight);
                volumeMaximumHeight = GetParameterValue<float>(fogComponent, "maximumHeight", volumeMaximumHeight);
                volumeAlbedo = GetParameterValue<Color>(fogComponent, "albedo", volumeAlbedo);
            }
        }

        private VolumeComponent FindFogComponent()
        {
            if (cachedFogComponent != null)
                return cachedFogComponent;

            if (volumeWithFog == null || volumeWithFog.profile == null)
                return null;

            foreach (var component in volumeWithFog.profile.components)
            {
                if (component == null)
                    continue;

                var typeName = component.GetType().Name;
                if (typeName == "Fog" || typeName == "VolumeFog" || typeName == "VolumetricFog")
                {
                    cachedFogComponent = component;
                    return cachedFogComponent;
                }
            }

            return null;
        }

        private static T GetParameterValue<T>(VolumeComponent component, string fieldName, T fallback)
        {
            if (component == null)
                return fallback;

            var field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return fallback;

            var parameter = field.GetValue(component);
            if (parameter == null)
                return fallback;

            var valueProperty = parameter.GetType().GetProperty("value");
            if (valueProperty != null && valueProperty.GetValue(parameter) is T typedValue)
            {
                return typedValue;
            }

            return fallback;
        }

        private static void SetParameterValue(VolumeComponent component, string fieldName, object newValue)
        {
            if (component == null)
                return;

            var field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return;

            var parameter = field.GetValue(component);
            if (parameter == null)
                return;

            var overrideProperty = parameter.GetType().GetProperty("overrideState");
            overrideProperty?.SetValue(parameter, true);

            var valueProperty = parameter.GetType().GetProperty("value");
            valueProperty?.SetValue(parameter, newValue);
        }

        [System.Serializable]
        private struct FogState
        {
            public readonly bool Enabled;
            public readonly FogMode Mode;
            public readonly Color Color;
            public readonly float Density;
            public readonly float StartDistance;
            public readonly float EndDistance;

            public FogState(bool enabled, FogMode mode, Color color, float density, float start, float end)
            {
                Enabled = enabled;
                Mode = mode;
                Color = color;
                Density = density;
                StartDistance = start;
                EndDistance = end;
            }
        }

        [System.Serializable]
        private struct VolumeFogState
        {
            public readonly bool Enabled;
            public readonly float MeanFreePath;
            public readonly float BaseHeight;
            public readonly float MaximumHeight;
            public readonly Color Albedo;

            public VolumeFogState(bool enabled, float meanFreePath, float baseHeight, float maximumHeight, Color albedo)
            {
                Enabled = enabled;
                MeanFreePath = meanFreePath;
                BaseHeight = baseHeight;
                MaximumHeight = maximumHeight;
                Albedo = albedo;
            }
        }
    }
}
