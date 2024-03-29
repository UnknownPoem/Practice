#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildProcessor : IPostprocessBuildWithReport, IPreprocessBuildWithReport
{
    #region field
    public int callbackOrder => 0;

    private static List<string> _changedPropertys = new List<string>();
    private static int _instancingVariants = 0;
    #endregion

    #region public method
    public void OnPreprocessBuild(BuildReport report)
    {
        CustomSVC.ShaderBuildProcessor.SetShaderCollectionMappingNull();
        SetGraphicsSettings();

    }

    public void OnPostprocessBuild(BuildReport report)
    {
        RevertGraphicsSettings();
        CustomSVC.ShaderBuildProcessor.OutputDebugInfo();

    }

    //[MenuItem("CustomSVC/Strip Always Include Shaders")]
    //public static void StripAlwaysIncludeShaders()
    //{
    //    var svc = CustomSVC.ShaderVariantCollectionToolSetting.shaderVariantCollectionToolSetting.m_ShaderVariantCollection;
    //    var mapper = new CustomSVC.ShaderVariantCollectionMapper(svc);
    //    var shaders = mapper.shaders;

    //    MethodInfo GetGraphicsSettings = typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
    //    UnityEngine.Object graphicsSettings = GetGraphicsSettings.Invoke(null, null) as UnityEngine.Object;
    //    SerializedObject so = new SerializedObject(graphicsSettings);

    //    SerializedProperty alwaysIncludedShadersSerializedProperty = so.FindProperty("m_AlwaysIncludedShaders");
    //    for (int i = 0; i < alwaysIncludedShadersSerializedProperty.arraySize;)
    //    {
    //        SerializedProperty shaderSerializedProperty = alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(i);
    //        Shader shader = shaderSerializedProperty.objectReferenceValue as Shader;

    //        if (shader == null || shaders.Contains(shader))
    //        {
    //            alwaysIncludedShadersSerializedProperty.DeleteArrayElementAtIndex(i);
    //        }
    //        else
    //        {
    //            ++i;
    //        }
    //    }

    //    so.ApplyModifiedProperties();
    //    so.Update();
    //}
    #endregion

    #region Private Methods
    public static void SetGraphicsSettings()
    {
        //MethodInfo GetGraphicsSettings = typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        //UnityEngine.Object graphicsSettings = GetGraphicsSettings.Invoke(null, null) as UnityEngine.Object;
        //SerializedObject so = new SerializedObject(graphicsSettings);

        ////_changedPropertys.Clear();
        //string[] boolPropertys = {
        //    // Lightmap Modes
        //    "m_LightmapStripping" , "m_LightmapKeepPlain", "m_LightmapKeepDirCombined", "m_LightmapKeepDynamicPlain",
        //    "m_LightmapKeepDynamicDirCombined", "m_LightmapKeepShadowMask", "m_LightmapKeepSubtractive",
        //    // Fog Modes
        //    "m_FogStripping", "m_FogKeepLinear", "m_FogKeepExp", "m_FogKeepExp2",
        //};
        //foreach (string str in boolPropertys)
        //{
        //    SerializedProperty property = so.FindProperty(str);
        //    if (property.boolValue == false)
        //    {
        //        _changedPropertys.Add(str);
        //    }
        //    property.boolValue = true;
        //}

        ////Instancing Variants  ////////////////////////////////
        //SerializedProperty m_InstancingStripping = so.FindProperty("m_InstancingStripping");
        //_instancingVariants = m_InstancingStripping.intValue;
        //m_InstancingStripping.intValue = 0;

        //// Apply
        //so.ApplyModifiedProperties();
        //so.Update();
        
    }

    public static void RevertGraphicsSettings()
    {
        //MethodInfo GetGraphicsSettings = typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        //UnityEngine.Object graphicsSettings = GetGraphicsSettings.Invoke(null, null) as UnityEngine.Object;
        //SerializedObject so = new SerializedObject(graphicsSettings);

        //foreach (string str in _changedPropertys)
        //{
        //    SerializedProperty property = so.FindProperty(str);
        //    property.boolValue = false;
        //}
        //SerializedProperty m_InstancingStripping = so.FindProperty("m_InstancingStripping");
        //m_InstancingStripping.intValue = _instancingVariants;

        //// Apply
        //so.ApplyModifiedProperties();
        //so.Update();

    }

    #endregion
}
#endif