#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Stonehold.Editor
{
    public static class MobileQualitySetupTools
    {
        private const string SettingsFolder = "Assets/Settings";

        [MenuItem("Stonehold/Graphics/Setup 3 Mobile Quality Tiers")]
        public static void SetupQualityTiers()
        {
            Debug.Log("[MobileQualitySetup] Starting 3 Mobile Quality Tiers Configuration...");

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            // 1. Create or Load Renderers
            string lowRendererPath = Path.Combine(SettingsFolder, "Low_Renderer.asset");
            string medRendererPath = Path.Combine(SettingsFolder, "Medium_Renderer.asset");
            string highRendererPath = Path.Combine(SettingsFolder, "High_Renderer.asset");

            UniversalRendererData lowRenderer = GetOrCreateRendererData(lowRendererPath, "Low_Renderer");
            UniversalRendererData medRenderer = GetOrCreateRendererData(medRendererPath, "Medium_Renderer");
            UniversalRendererData highRenderer = GetOrCreateRendererData(highRendererPath, "High_Renderer");

            // 2. Create or Configure RP Assets
            string lowRPPath = Path.Combine(SettingsFolder, "Low_RPAsset.asset");
            string medRPPath = Path.Combine(SettingsFolder, "Medium_RPAsset.asset");
            string highRPPath = Path.Combine(SettingsFolder, "High_RPAsset.asset");

            UniversalRenderPipelineAsset lowAsset = GetOrCreateRPAsset(lowRPPath, lowRenderer, "Low_RPAsset");
            UniversalRenderPipelineAsset medAsset = GetOrCreateRPAsset(medRPPath, medRenderer, "Medium_RPAsset");
            UniversalRenderPipelineAsset highAsset = GetOrCreateRPAsset(highRPPath, highRenderer, "High_RPAsset");

            // Configure Low Tier (Battery Saver)
            ConfigureLowTier(lowAsset);

            // Configure Medium Tier (Balanced 60 FPS)
            ConfigureMediumTier(medAsset);

            // Configure High Tier (Crisp 4x MSAA)
            ConfigureHighTier(highAsset);

            EditorUtility.SetDirty(lowAsset);
            EditorUtility.SetDirty(medAsset);
            EditorUtility.SetDirty(highAsset);
            AssetDatabase.SaveAssets();

            // 3. Configure ProjectSettings/QualitySettings.asset
            ConfigureQualitySettingsAsset(lowAsset, medAsset, highAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MobileQualitySetup] Successfully configured 3 Mobile Quality Tiers (Low, Medium, High)!");
        }

        private static UniversalRendererData GetOrCreateRendererData(string path, string name)
        {
            UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UniversalRendererData>();
                data.name = name;
                AssetDatabase.CreateAsset(data, path);
            }
            return data;
        }

        private static UniversalRenderPipelineAsset GetOrCreateRPAsset(string path, UniversalRendererData rendererData, string name)
        {
            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create(rendererData);
                asset.name = name;
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void ConfigureLowTier(UniversalRenderPipelineAsset asset)
        {
            SerializedObject so = new SerializedObject(asset);
            SetProp(so, "m_RenderScale", 0.85f);
            SetProp(so, "m_MSAA", 1); // 1 = Off
            SetProp(so, "m_SupportsHDR", false);
            SetProp(so, "m_MainLightRenderingMode", 1); // PerPixel
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_MainLightShadowmapResolution", 512);
            SetProp(so, "m_AdditionalLightsRenderingMode", 0); // Disabled for draw calls
            SetProp(so, "m_AdditionalLightShadowsSupported", false);
            SetProp(so, "m_ShadowDistance", 25f);
            SetProp(so, "m_ShadowCascadeCount", 1);
            SetProp(so, "m_SoftShadowsSupported", false);
            SetProp(so, "m_UseSRPBatcher", true);
            SetProp(so, "m_UseFastSRGBLinearConversion", true);
            so.ApplyModifiedProperties();
        }

        private static void ConfigureMediumTier(UniversalRenderPipelineAsset asset)
        {
            SerializedObject so = new SerializedObject(asset);
            SetProp(so, "m_RenderScale", 1.0f);
            SetProp(so, "m_MSAA", 2); // 2x MSAA
            SetProp(so, "m_SupportsHDR", true);
            SetProp(so, "m_MainLightRenderingMode", 1);
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_MainLightShadowmapResolution", 1024);
            SetProp(so, "m_AdditionalLightsRenderingMode", 1);
            SetProp(so, "m_AdditionalLightsPerObjectLimit", 2);
            SetProp(so, "m_AdditionalLightShadowsSupported", false);
            SetProp(so, "m_ShadowDistance", 40f);
            SetProp(so, "m_ShadowCascadeCount", 2);
            SetProp(so, "m_SoftShadowsSupported", true);
            SetProp(so, "m_SoftShadowQuality", 2); // Medium Soft
            SetProp(so, "m_UseSRPBatcher", true);
            SetProp(so, "m_UseFastSRGBLinearConversion", true);
            so.ApplyModifiedProperties();
        }

        private static void ConfigureHighTier(UniversalRenderPipelineAsset asset)
        {
            SerializedObject so = new SerializedObject(asset);
            SetProp(so, "m_RenderScale", 1.0f);
            SetProp(so, "m_MSAA", 4); // 4x MSAA
            SetProp(so, "m_SupportsHDR", true);
            SetProp(so, "m_MainLightRenderingMode", 1);
            SetProp(so, "m_MainLightShadowsSupported", true);
            SetProp(so, "m_MainLightShadowmapResolution", 2048);
            SetProp(so, "m_AdditionalLightsRenderingMode", 1);
            SetProp(so, "m_AdditionalLightsPerObjectLimit", 4);
            SetProp(so, "m_AdditionalLightShadowsSupported", true);
            SetProp(so, "m_AdditionalLightsShadowmapResolution", 1024);
            SetProp(so, "m_ShadowDistance", 55f);
            SetProp(so, "m_ShadowCascadeCount", 3);
            SetProp(so, "m_SoftShadowsSupported", true);
            SetProp(so, "m_SoftShadowQuality", 3); // High Soft
            SetProp(so, "m_UseSRPBatcher", true);
            SetProp(so, "m_UseFastSRGBLinearConversion", true);
            so.ApplyModifiedProperties();
        }

        private static void ConfigureQualitySettingsAsset(UniversalRenderPipelineAsset lowAsset, UniversalRenderPipelineAsset medAsset, UniversalRenderPipelineAsset highAsset)
        {
            UnityEngine.Object[] qualityAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (qualityAssets == null || qualityAssets.Length == 0)
            {
                Debug.LogWarning("[MobileQualitySetup] Unable to load QualitySettings.asset");
                return;
            }

            SerializedObject qs = new SerializedObject(qualityAssets[0]);
            SerializedProperty qualitySettingsProp = qs.FindProperty("m_QualitySettings");
            if (qualitySettingsProp == null || !qualitySettingsProp.isArray)
            {
                Debug.LogWarning("[MobileQualitySetup] m_QualitySettings property not found.");
                return;
            }

            // Ensure exactly 3 levels
            qualitySettingsProp.arraySize = 3;

            // 0: Low (Battery Saver)
            SerializedProperty lowTier = qualitySettingsProp.GetArrayElementAtIndex(0);
            lowTier.FindPropertyRelative("name").stringValue = "Low";
            lowTier.FindPropertyRelative("pixelLightCount").intValue = 1;
            lowTier.FindPropertyRelative("shadows").intValue = 1; // Hard shadows only
            lowTier.FindPropertyRelative("shadowResolution").intValue = 0; // Low
            lowTier.FindPropertyRelative("shadowCascades").intValue = 1;
            lowTier.FindPropertyRelative("shadowDistance").floatValue = 25f;
            lowTier.FindPropertyRelative("skinWeights").intValue = 2;
            lowTier.FindPropertyRelative("anisotropicTextures").intValue = 0; // Disabled
            lowTier.FindPropertyRelative("antiAliasing").intValue = 0;
            lowTier.FindPropertyRelative("softParticles").intValue = 0;
            lowTier.FindPropertyRelative("lodBias").floatValue = 0.8f;
            lowTier.FindPropertyRelative("customRenderPipeline").objectReferenceValue = lowAsset;

            // 1: Medium (Balanced)
            SerializedProperty medTier = qualitySettingsProp.GetArrayElementAtIndex(1);
            medTier.FindPropertyRelative("name").stringValue = "Medium";
            medTier.FindPropertyRelative("pixelLightCount").intValue = 2;
            medTier.FindPropertyRelative("shadows").intValue = 2; // All / Soft
            medTier.FindPropertyRelative("shadowResolution").intValue = 1; // Medium
            medTier.FindPropertyRelative("shadowCascades").intValue = 2;
            medTier.FindPropertyRelative("shadowDistance").floatValue = 40f;
            medTier.FindPropertyRelative("skinWeights").intValue = 2;
            medTier.FindPropertyRelative("anisotropicTextures").intValue = 1; // Supported
            medTier.FindPropertyRelative("antiAliasing").intValue = 2; // 2x MSAA
            medTier.FindPropertyRelative("softParticles").intValue = 0;
            medTier.FindPropertyRelative("lodBias").floatValue = 1.2f;
            medTier.FindPropertyRelative("customRenderPipeline").objectReferenceValue = medAsset;

            // 2: High (Crisp)
            SerializedProperty highTier = qualitySettingsProp.GetArrayElementAtIndex(2);
            highTier.FindPropertyRelative("name").stringValue = "High";
            highTier.FindPropertyRelative("pixelLightCount").intValue = 4;
            highTier.FindPropertyRelative("shadows").intValue = 2; // All / Soft
            highTier.FindPropertyRelative("shadowResolution").intValue = 2; // High
            highTier.FindPropertyRelative("shadowCascades").intValue = 4;
            highTier.FindPropertyRelative("shadowDistance").floatValue = 55f;
            highTier.FindPropertyRelative("skinWeights").intValue = 4; // 4 bones
            highTier.FindPropertyRelative("anisotropicTextures").intValue = 2; // Forced On
            highTier.FindPropertyRelative("antiAliasing").intValue = 4; // 4x MSAA
            highTier.FindPropertyRelative("softParticles").intValue = 1;
            highTier.FindPropertyRelative("lodBias").floatValue = 2.0f;
            highTier.FindPropertyRelative("customRenderPipeline").objectReferenceValue = highAsset;

            // Default level = Medium (1)
            SerializedProperty currentQualityProp = qs.FindProperty("m_CurrentQuality");
            if (currentQualityProp != null)
            {
                currentQualityProp.intValue = 1;
            }

            qs.ApplyModifiedProperties();
        }

        private static void SetProp(SerializedObject so, string propertyName, object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null) return;

            if (value is float f) prop.floatValue = f;
            else if (value is int i) prop.intValue = i;
            else if (value is bool b) prop.boolValue = b;
        }
    }
}
#endif
