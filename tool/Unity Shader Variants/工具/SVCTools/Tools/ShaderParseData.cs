#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace CustomSVC
{
    public class ShaderParseData
    {
        #region Inner Class

        public class Pass
        {
            #region Fields

            public int m_PassID;
            public PassType m_PassType;
            public List<List<string>> m_Keywords = new List<List<string>>();
            public List<string> m_BuiltinDeclaration = new List<string>();
            public List<string> m_MaterialValidKWS = new List<string>();
            public List<string[]> m_CombineValidKWS = new List<string[]>();

            #endregion

            public Pass(int passID)
            {
                m_PassID = passID;
            }

            #region Public Methods

            public void AddKeywords(List<string> keywords)
            {
                if (keywords != null)
                {
                    m_Keywords.Add(keywords);
                }
            }

            public void AddMaterialVariantKeywordGroup(string keywordGroup)
            {
                if (string.IsNullOrEmpty(keywordGroup))
                {
                    keywordGroup = string.Empty;
                }

                if (!m_MaterialValidKWS.Contains(keywordGroup))
                {
                    m_MaterialValidKWS.Add(keywordGroup);
                }
            }

            public void GenerateKWCombination(List<string> dynamicKWS)
            {
                List<List<string>> keywordsCombinationList = new List<List<string>>();
                keywordsCombinationList.Add(new List<string>());

                // 计算静态关键字组合
                List<string> allKeywords = new List<string>();
                foreach (var keywords in m_Keywords)
                {
                    allKeywords.AddRange(keywords);
                }
                foreach (var keywordGroup in m_MaterialValidKWS)
                {
                    string[] splitedArr = keywordGroup.Split('|');
                    List<string> validKWS = new List<string>();
                    foreach (var kw in splitedArr)
                    {
                        if (allKeywords.Contains(kw))
                            validKWS.Add(kw);
                    }
                    keywordsCombinationList.Add(validKWS);
                }

                // 计算动态关键字组合
                List<List<string>> validKeywords = new List<List<string>>();
                foreach (var keywords in m_Keywords)
                {
                    List<string> temp = new List<string>();
                    // 保留动态关键字和默认关键字
                    temp.Add(keywords[0]);
                    for (int i = 1; i < keywords.Count; ++i)
                    {
                        if (dynamicKWS.Contains(keywords[i]))
                        {
                            temp.Add(keywords[i]);
                        }
                    }
                    validKeywords.Add(temp);
                }

                // 组合动态关键字
                for (int i = 0; i < validKeywords.Count; ++i)
                {
                    keywordsCombinationList = CombinationKeywords(keywordsCombinationList, validKeywords[i], m_Keywords[i]);
                }

                // 去重
                keywordsCombinationList = keywordsCombinationList.Select(list => { list.Sort(); return string.Join("|", list); })
                    .Distinct()
                    .Select(str => { return str.Split(new char[] { '|' }).ToList(); })
                    .ToList();

                foreach (var dynamicKWCombination in keywordsCombinationList)
                {
                    m_CombineValidKWS.Add(dynamicKWCombination.ToArray());
                }
            }

            public void CombineBuiltinKWVariant(Dictionary<Scene, SceneInfo> sceneInfoList)
            {
                List<string> buildinKWGroup = new List<string>();
                string fogKW = string.Empty;
                string instancingKW = string.Empty;

                foreach (var sceneInfo in sceneInfoList.Values)
                {
                    foreach (string buildinDeclaration in m_BuiltinDeclaration)
                    {
                        switch (buildinDeclaration)
                        {
                            case "multi_compile_fwdbase":
                                {
                                    string tempStr = "DIRECTIONAL";
                                    if (sceneInfo.enabledBakedGI && !sceneInfo.isLightingDataAssetNull && sceneInfo.hasContributeGITrue)
                                    {// Mixed Lighting
                                        tempStr += " LIGHTMAP_ON";
                                        if (sceneInfo.lightmapDirectionalMode == LightmapsMode.CombinedDirectional)
                                        {
                                            tempStr += " DIRLIGHTMAP_COMBINED";
                                        }

                                        if (sceneInfo.mixedLightingMode == MixedLightingMode.IndirectOnly)
                                        {// Baked Indirect
                                            if (sceneInfo.hasDirLightWithoutShadow || !sceneInfo.hasDirLight)
                                            {
                                                if (!buildinKWGroup.Contains(tempStr))
                                                {
                                                    buildinKWGroup.Add(tempStr);
                                                }
                                            }

                                            if (sceneInfo.hasDirLightWithShadow)
                                            {
                                                tempStr += " SHADOWS_SCREEN";
                                                if (!buildinKWGroup.Contains(tempStr))
                                                {
                                                    buildinKWGroup.Add(tempStr);
                                                }
                                            }
                                        }
                                        else if (sceneInfo.mixedLightingMode == MixedLightingMode.Shadowmask)
                                        {// Shadowmask
                                            if (!sceneInfo.hasDirLight)
                                            {
                                                if (!buildinKWGroup.Contains(tempStr))
                                                {
                                                    buildinKWGroup.Add(tempStr);
                                                }
                                            }
                                            else
                                            {
                                                tempStr += " SHADOWS_SHADOWMASK";

                                                { // ShadowmaskMode.ShadowMask
                                                    var tempStr2 = tempStr + " LIGHTMAP_SHADOW_MIXING";
                                                    if (!buildinKWGroup.Contains(tempStr2))
                                                    {
                                                        buildinKWGroup.Add(tempStr2);
                                                    }
                                                }
                                                { // ShadowmaskMode.DistanceShadowmask
                                                    var tempStr2 = tempStr;
                                                    if (sceneInfo.hasDirLightWithShadow)
                                                    {
                                                        tempStr2 += " SHADOWS_SCREEN";
                                                    }
                                                    if (!buildinKWGroup.Contains(tempStr2))
                                                    {
                                                        buildinKWGroup.Add(tempStr2);
                                                    }
                                                }

                                            }
                                        }
                                        else if (sceneInfo.mixedLightingMode == MixedLightingMode.Subtractive)
                                        {// Subtractive

                                            if (sceneInfo.hasDirLightBakeTypeMixedTrue)
                                            {
                                                if (!buildinKWGroup.Contains(tempStr + " LIGHTMAP_SHADOW_MIXING"))
                                                {
                                                    buildinKWGroup.Add(tempStr + " LIGHTMAP_SHADOW_MIXING");
                                                }
                                            }
                                            if (sceneInfo.hasDirLightBakeTypeMixedFalse)
                                            {
                                                if (!buildinKWGroup.Contains(tempStr))
                                                {
                                                    buildinKWGroup.Add(tempStr);
                                                }
                                            }
                                        }
                                    }

                                    tempStr = "DIRECTIONAL";
                                    if (!sceneInfo.enabledBakedGI || sceneInfo.isLightingDataAssetNull || sceneInfo.hasContributeGIFalse)
                                    {// Realtime
                                        tempStr += " LIGHTPROBE_SH";
                                        if (!sceneInfo.hasDirLight || sceneInfo.hasDirLightWithoutShadow)
                                        {
                                            if (!buildinKWGroup.Contains(tempStr))
                                            {
                                                buildinKWGroup.Add(tempStr);
                                            }
                                        }

                                        if (sceneInfo.hasDirLightWithShadow)
                                        {
                                            tempStr += " SHADOWS_SCREEN";
                                            if (!buildinKWGroup.Contains(tempStr))
                                            {
                                                buildinKWGroup.Add(tempStr);
                                            }
                                        }
                                    }

                                    break;
                                }
                            case "multi_compile_fwdadd_fullshadows":
                                {
                                    string tempStr = "POINT";

                                    if (sceneInfo.hasPointLightWithShadow)
                                    {
                                        tempStr += " SHADOWS_CUBE";
                                    }

                                    if (!buildinKWGroup.Contains(tempStr))
                                    {
                                        buildinKWGroup.Add(tempStr);
                                    }

                                    break;
                                }
                            case "multi_compile_shadowcaster":
                                {
                                    if (!(sceneInfo.enabledBakedGI && !sceneInfo.isLightingDataAssetNull && sceneInfo.hasContributeGITrue
                                        && sceneInfo.mixedLightingMode == MixedLightingMode.Subtractive))
                                    {// 不是Subtractive

                                        if (sceneInfo.hasDirLightWithShadow && !buildinKWGroup.Contains("SHADOWS_DEPTH"))
                                        {
                                            buildinKWGroup.Add("SHADOWS_DEPTH");
                                        }
                                    }

                                    if (sceneInfo.hasPointLightWithShadow && !buildinKWGroup.Contains("SHADOWS_CUBE"))
                                    {
                                        buildinKWGroup.Add("SHADOWS_CUBE");
                                    }

                                    break;
                                }
                            case "multi_compile_fog":
                                {
                                    if (sceneInfo.enableFog)
                                    {
                                        if (sceneInfo.fogMode == FogMode.Linear)
                                        {
                                            fogKW = " FOG_LINEAR";
                                        }
                                        else if (sceneInfo.fogMode == FogMode.Exponential)
                                        {
                                            fogKW = " FOG_EXP";
                                        }
                                        else if (sceneInfo.fogMode == FogMode.Exponential)
                                        {
                                            fogKW = " FOG_EXP2";
                                        }
                                    }

                                    break;
                                }
                            case "multi_compile_instancing":
                                {
                                    if (sceneInfo.enableInstancing)
                                    {
                                        instancingKW = " INSTANCING_ON";
                                    }
                                    break;
                                }
                            default: break;
                        }
                    }
                }

                if (sceneInfoList.Count == 0)
                {// 按RealTime情况打入可能的变体
                    if (m_PassType == PassType.ForwardBase)
                    {
                        buildinKWGroup.Add("DIRECTIONAL LIGHTPROBE_SH SHADOWS_SCREEN");
                        buildinKWGroup.Add("DIRECTIONAL LIGHTPROBE_SH");
                    }
                    else if (m_PassType == PassType.ForwardAdd)
                    {
                        buildinKWGroup.Add("POINT SHADOWS_CUBE");
                        buildinKWGroup.Add("POINT");
                    }
                    else if (m_PassType == PassType.ShadowCaster)
                    {
                        buildinKWGroup.Add("SHADOWS_DEPTH");
                        buildinKWGroup.Add("SHADOWS_CUBE");
                    }
                }

                if (m_BuiltinDeclaration.Count == 0)
                {
                    buildinKWGroup.Add("");
                }

                if (!string.IsNullOrEmpty(fogKW))
                {
                    int length = buildinKWGroup.Count;
                    for (int i = 0; i < length; ++i)
                    {
                        buildinKWGroup.Add(buildinKWGroup[i] + fogKW);
                    }
                }

                if (!string.IsNullOrEmpty(instancingKW))
                {
                    int length = buildinKWGroup.Count;
                    for (int i = 0; i < length; ++i)
                    {
                        buildinKWGroup.Add(buildinKWGroup[i] + instancingKW);
                    }
                }

                CombineBuiltinKWVariantInternal(buildinKWGroup.ToArray());
            }
            #endregion

            #region Private Methods
            private void CombineBuiltinKWVariantInternal(string[] builtinKWGroups)
            {
                if (builtinKWGroups == null || builtinKWGroups.Length == 0) return;

                List<string[]> finalGroup = new List<string[]>();
                foreach (var builtinKWGroup in builtinKWGroups)
                {
                    List<string> builtinKWs = builtinKWGroup.Split(' ').ToList();
                    foreach (var cKW in m_CombineValidKWS)
                    {
                        List<string> kwList = new List<string>(cKW);
                        // 由于把内置关键字配进了动态关键字里，所以这里需要做一次去重。
                        builtinKWs.RemoveAll(str => kwList.Contains(str));
                        kwList.AddRange(builtinKWs);
                        finalGroup.Add(kwList.Distinct().ToArray());
                    }
                }

                if (finalGroup.Count > 0)
                    m_CombineValidKWS = finalGroup;
            }

            private List<List<string>> CombinationKeywords(List<List<string>> preKWCombination, List<string> appendKeywords, List<string> keywords)
            {
                List<List<string>> ret = new List<List<string>>();
                foreach (var pre in preKWCombination)
                {
                    // 检查是否已添加过该行（静态关键字组合）
                    bool isIntersect = false;
                    foreach (var kw in keywords)
                    {
                        if (!string.IsNullOrEmpty(kw) && pre.Contains(kw))
                        {
                            isIntersect = true;
                            break;
                        }
                    }
                    if (isIntersect)
                    {
                        ret.Add(pre);
                        continue;
                    }

                    foreach (var kw in appendKeywords)
                    {
                        List<string> temp = new List<string>();
                        temp.AddRange(pre);
                        temp.Add(kw);
                        ret.Add(temp);
                    }
                }
                return ret;
            }
            #endregion
        }

        public class SubShader
        {
            #region Fields

            public int m_SubShaderID;
            public Dictionary<int, Pass> m_DataMapping = new Dictionary<int, Pass>();
            public PassType m_PassType = PassType.Normal;

            #endregion

            public SubShader(int subshaderID)
            {
                m_SubShaderID = subshaderID;
            }

            #region Public Methods

            public void AddPass(Pass pass)
            {
                if (!m_DataMapping.ContainsKey(pass.m_PassID))
                    m_DataMapping.Add(pass.m_PassID, pass);
            }

            public void AddMaterialVariantKeywordGroup(string keywordGroup)
            {
                foreach (var pass in m_DataMapping.Values)
                {
                    pass.AddMaterialVariantKeywordGroup(keywordGroup);
                }
            }

            public void GenerateKWCombination(List<string> dynamicKWS)
            {
                foreach (var pass in m_DataMapping.Values)
                {
                    pass.GenerateKWCombination(dynamicKWS);
                }
            }

            public void CombineBuiltinKWVariant(Dictionary<Scene, SceneInfo> sceneInfoList)
            {
                foreach (var pass in m_DataMapping.Values)
                {
                    pass.CombineBuiltinKWVariant(sceneInfoList);
                }
            }

            #endregion
        }

        public class SceneInfo
        {
            public bool enabledBakedGI = false;
            public bool hasPointLight = false;
            public bool hasPointLightWithShadow = false;
            public bool hasDirLight = false;
            public bool hasDirLightWithShadow = false;
            public bool hasDirLightWithoutShadow = false;
            public bool hasDirLightBakeTypeMixedTrue = false;
            public bool hasDirLightBakeTypeMixedFalse = false;
            public bool isLightingDataAssetNull = false;
            public MixedLightingMode mixedLightingMode = MixedLightingMode.IndirectOnly;
            public LightmapsMode lightmapDirectionalMode = LightmapsMode.NonDirectional;
            public bool hasContributeGITrue = false;
            public bool hasContributeGIFalse = false;
            public bool enableFog = false;
            public FogMode fogMode = FogMode.Linear;
            public bool enableInstancing = false;

            public SceneInfo()
            {
                // lights
                var lights = Object.FindObjectsOfType<Light>().ToList();
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        hasDirLight = true;
                        if (light.shadows != LightShadows.None)
                        {
                            hasDirLightWithShadow = true;
                        }
                        else
                        {
                            hasDirLightWithoutShadow = true;
                        }

                        if (light.lightmapBakeType == LightmapBakeType.Mixed)
                        {
                            hasDirLightBakeTypeMixedTrue = true;
                        }
                        else
                        {
                            hasDirLightBakeTypeMixedFalse = true;
                        }
                    }
                    else if (light.type == LightType.Point)
                    {
                        hasPointLight = true;
                        if (light.shadows != LightShadows.None)
                        {
                            hasPointLightWithShadow = true;
                        }
                    }
                }

                MethodInfo GetLightmapSettings = typeof(LightmapEditorSettings).GetMethod("GetLightmapSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                UnityEngine.Object m_LightmapSettings = GetLightmapSettings.Invoke(null, null) as UnityEngine.Object;
                SerializedObject m_LightmapSettingsSO = new SerializedObject(m_LightmapSettings);

                // enabledBakedGI
                enabledBakedGI = Lightmapping.bakedGI;

                // isLightingDataAssetNull
                isLightingDataAssetNull = Lightmapping.lightingDataAsset == null;

                // mixedLightingMode
                var m_MixedBakeMode = m_LightmapSettingsSO.FindProperty("m_LightmapEditorSettings.m_MixedBakeMode");
                mixedLightingMode = (MixedLightingMode)m_MixedBakeMode.intValue;

                // lightmapDirectionalMode
                var m_LightmapDirectionalMode = m_LightmapSettingsSO.FindProperty("m_LightmapEditorSettings.m_LightmapsBakeMode");
                lightmapDirectionalMode = (LightmapsMode)m_LightmapDirectionalMode.intValue;

                // Fog Mode
                enableFog = RenderSettings.fog;
                fogMode = RenderSettings.fogMode;
            }
        }

        #endregion

        #region Fields

        static readonly string SUBSHADER_FALAG = "SubShader";
        static readonly string PASS_FLAG = "Pass";
        static readonly string PASS_TYPE = "\"LightMode\"";
        static readonly string PASS_TYPE_REPLACE_FLAG = "=\"";
        static readonly char PASS_TYPE_SPLIT_FLAG = '\"';
        static readonly string LINE_COMMENT_FLAG = "//";
        static readonly string LINE_COMMENT_INVALID_START_FLAG = "/*";
        static readonly string LINE_COMMENT_INVALID_END_FLAG = "*/";
        static readonly char PASS_KW_STANDARAD = '_';

        static readonly List<string> KW_FLAGS = new List<string> {
            "shader_feature",
            "shader_feature_local",
            "multi_compile",
            "multi_compile_local",
        };

        static readonly List<string> BUILTIN_FLAGS = new List<string>{
            "multi_compile_fwdbase",
            "multi_compile_fwdadd_fullshadows",
            "multi_compile_fog",
            "multi_compile_instancing",
            "multi_compile_shadowcaster",
        };

        static readonly List<string> BUILDIN_KWS = new List<string>
        {
            "DIRECTIONAL",
            "LIGHTMAP_ON",
            "DIRLIGHTMAP_COMBINED",
            "SHADOWS_SCREEN",
            "SHADOWS_SHADOWMASK",
            "LIGHTMAP_SHADOW_MIXING",
            "LIGHTPROBE_SH",
            "POINT",
            "SHADOWS_CUBE",
            "SHADOWS_DEPTH",
            "FOG_LINEAR",
            "FOG_EXP",
            "FOG_EXP2",
            "INSTANCING_ON",
        };

        private Shader m_Shader;

        public Dictionary<int, SubShader> m_DataMapping = new Dictionary<int, SubShader>();

        #endregion

        public ShaderParseData(Shader shader)
        {
            this.m_Shader = shader;
            DoParse();
        }

        public ShaderParseData(Shader shaderClone, ShaderParseData ori)
        {
            this.m_Shader = shaderClone;
            this.m_DataMapping = ori.m_DataMapping;
        }

        #region Private Methods

        string GetKWFlag(string[] lineSplitedArray)
        {
            foreach (var lineStr in lineSplitedArray)
            {
                foreach (var flag in KW_FLAGS)
                {
                    if (lineStr == flag)
                    {
                        return lineStr;
                    }
                }
            }

            return string.Empty;
        }

        string GetBuiltinFlag(string[] lineSplitedArray)
        {
            foreach (var lineStr in lineSplitedArray)
            {
                foreach (var flag in BUILTIN_FLAGS)
                {
                    if (lineStr == flag)
                    {
                        return lineStr;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 检查字符串是否全由下划线组成
        /// </summary>
        /// <param name="kwStr">输入字符串</param>
        /// <returns>返回true，表示字符串完全由下划线组成。</returns>
        bool IsNoKeywordFlag(string kwStr)
        {
            if (string.IsNullOrEmpty(kwStr)) return false;

            char[] arr = kwStr.ToCharArray();

            foreach (var c in arr)
            {
                if (c != PASS_KW_STANDARAD)
                {
                    return false;
                }
            }

            return true;
        }

        void DoParse()
        {
            m_DataMapping.Clear();

            if (this.m_Shader == null) return;

            string path = AssetDatabase.GetAssetPath(m_Shader);
            if (!File.Exists(path)) return;

            StreamReader sr = new StreamReader(path);
            string lineStr = sr.ReadLine();
            int subshaderIndex = 0;
            int passIndex = 0;
            SubShader tempSubShader = null;
            Pass tempPass = null;
            PassType tempPassType = PassType.Normal;
            while (lineStr != null)
            {
                string noSpaceLineStr = lineStr.Replace(" ", "");
                noSpaceLineStr = noSpaceLineStr.Replace("\t", "");
                if (noSpaceLineStr.StartsWith(LINE_COMMENT_FLAG))
                {
                    lineStr = sr.ReadLine();
                    continue;
                }

                if (tempPass != null)
                {
                    //Keywords
                    string[] arr = lineStr.Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
                    string kwFlag = GetKWFlag(arr);
                    if (!string.IsNullOrEmpty(kwFlag))
                    {
                        arr = lineStr.Split(new string[] { kwFlag }, System.StringSplitOptions.None);
                        arr = arr[1].Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries);
                        bool hasNoKeywordVariant = false;
                        List<string> finalKWS = new List<string>();

                        // "shader_feature A" 这种情况，实质上等价于 "shader_feature _ A".
                        if ((kwFlag == "shader_feature" || kwFlag == "shader_feature_local") && arr.Length == 1)
                        {
                            finalKWS.Add("");
                            hasNoKeywordVariant = true;
                        }

                        foreach (var kw in arr)
                        {
                            if (IsNoKeywordFlag(kw))
                            {
                                if (!hasNoKeywordVariant)
                                {
                                    finalKWS.Add("");
                                    hasNoKeywordVariant = true;
                                }
                            }
                            else
                            {
                                finalKWS.Add(kw);
                            }
                        }

                        tempPass.AddKeywords(finalKWS);
                    }

                    // Builtin Declartion
                    string builtinFlag = GetBuiltinFlag(arr);
                    if (!string.IsNullOrEmpty(builtinFlag) && !tempPass.m_BuiltinDeclaration.Contains(builtinFlag))
                    {
                        tempPass.m_BuiltinDeclaration.Add(builtinFlag);
                    }

                    // Pass Type
                    if (noSpaceLineStr.Contains(PASS_TYPE))
                    {
                        arr = noSpaceLineStr.Split(new string[] { PASS_TYPE }, System.StringSplitOptions.None);
                        string tempStr = arr[1].Replace(PASS_TYPE_REPLACE_FLAG, "");
                        arr = tempStr.Split(PASS_TYPE_SPLIT_FLAG);
                        tempStr = arr[0];

                        try
                        {
                            if (tempStr == "Always")
                            {
                                tempStr = "Normal";
                            }

                            if (!string.IsNullOrEmpty(tempStr))
                            {
                                tempPassType = (PassType)System.Enum.Parse(typeof(PassType), tempStr);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Unknown LightMode in {m_Shader.name}. lineStr = {lineStr} , {e.Message}");
                        }

                        tempPass.m_PassType = tempPassType;
                    }
                }

                if (tempSubShader != null)
                {
                    // Pass Start
                    string[] tempSplitStr = noSpaceLineStr.Split('{');
                    if (tempSplitStr[0] == PASS_FLAG)
                    {
                        tempPass = new Pass(passIndex);
                        tempSubShader.AddPass(tempPass);

                        tempPass.m_PassType = tempSubShader.m_PassType;
                        passIndex++;
                    }

                    // SubShader Pass Type
                    if (passIndex == 0 && noSpaceLineStr.Contains(PASS_TYPE))
                    {
                        string[] arr = noSpaceLineStr.Split(new string[] { PASS_TYPE }, System.StringSplitOptions.None);
                        string tempStr = arr[1].Replace(PASS_TYPE_REPLACE_FLAG, "");
                        arr = tempStr.Split(PASS_TYPE_SPLIT_FLAG);
                        tempStr = arr[0];

                        try
                        {
                            if (tempStr == "Always")
                            {
                                tempStr = "Normal";
                            }

                            if (!string.IsNullOrEmpty(tempStr))
                            {
                                tempPassType = (PassType)System.Enum.Parse(typeof(PassType), tempStr);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Unknown LightMode in {m_Shader.name}. lineStr = {lineStr} , {e.Message}");
                        }

                        tempSubShader.m_PassType = tempPassType;
                    }
                }

                // SubShader Start
                if (noSpaceLineStr.StartsWith(SUBSHADER_FALAG))
                {
                    tempSubShader = new SubShader(subshaderIndex);
                    m_DataMapping[subshaderIndex] = tempSubShader;

                    subshaderIndex++;
                    passIndex = 0;
                }

                lineStr = sr.ReadLine();
            }

            sr.Close();
            sr.Dispose();
        }
        #endregion

        #region Public Methods

        public void AddMaterialVariantKeywordGroup(string keywordGroup)
        {
            foreach (var subshader in m_DataMapping.Values)
            {
                subshader.AddMaterialVariantKeywordGroup(keywordGroup);
            }
        }

        public void GenerateKWCombination(List<string> dynamicKWS)
        {
            foreach (var subshader in m_DataMapping.Values)
            {
                subshader.GenerateKWCombination(dynamicKWS);
            }
        }

        public void CombineBuiltinKWVariant(Dictionary<Scene, SceneInfo> sceneInfoList)
        {
            foreach (var subshader in m_DataMapping.Values)
            {
                subshader.CombineBuiltinKWVariant(sceneInfoList);
            }
        }

        #endregion

        #region Static Methods

        [MenuItem("CustomSVC/Check Shader Standard")]
        static bool CheckShaderStandard()
        {
            /// 规则1： SubShader Pass "LightMode" 不在同一行
            string error1 = "SubShader、Pass、\"LightMode\"任意2个或3个不能在同一行";
            /// 规则2：代码注释不允许使用"/*"和"*/"组合，只能"//"
            string error2 = "代码注释不允许使用\"/*\"和\"*/\"组合，只能\"//\"";
            /// 规则3：所有"#pragma"声明只能单独一行
            string error3 = "#pragma声明必须单独一行";
            string[] splitArray = new string[] { "#pragma" };
            /// 规则4：#pragma声明同一行后不能有注释
            string error4 = "#pragma声明同一行后不能有注释";

            ///// 规则4：动态关键字声明格式必须为 "#pragma multi_compile _ A"
            //string error4 = "动态关键字声明格式必须为 \"#pragma multi_compile _ A\"";
            ///// 正则表达式模式，(#pragma)(任意数量空格)(multi_compile|multi_compile_local)(任意字符直至行尾)
            //string pattern41 = @"(#pragma)(\s+)(multi_compile|multi_compile_local)(.*\z)";
            //string pattern42 = @"^[_]+$"; // 正则表达式模式，只包含下划线

            var toolSetting = ShaderVariantCollectionToolSetting.GetAllSettings()[0];
            string[] dynamicKeywords = toolSetting != null ? toolSetting.m_DynamicKWArray.ToArray() : new string[0];

            string[] shaderAssets = AssetDatabase.FindAssets("t:Shader", new string[] { "Assets" });
            if (shaderAssets == null || shaderAssets.Length == 0)
            {
                return true;
            }

            Dictionary<string, Dictionary<int, List<string>>> errorMapping = new Dictionary<string, Dictionary<int, List<string>>>();
            Dictionary<int, List<string>> tempDic = null;
            List<string> tempList = null;
            for (int i = 0; i < shaderAssets.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shaderAssets[i]);
                EditorUtility.DisplayProgressBar("Check Shader Standard", "Check..." + path, (i + 1) * 1.0f / shaderAssets.Length);

                StreamReader sr = new StreamReader(path);

                string lineStr = sr.ReadLine();
                int lineIndex = 1;
                while (lineStr != null)
                {
                    // Error 1
                    if ((lineStr.Contains(SUBSHADER_FALAG) && lineStr.Contains(PASS_FLAG) && lineStr.Contains(PASS_TYPE)) ||
                       (lineStr.Contains(SUBSHADER_FALAG) && lineStr.Contains(PASS_FLAG)) ||
                       (lineStr.Contains(PASS_FLAG) && lineStr.Contains(PASS_TYPE)) ||
                       (lineStr.Contains(SUBSHADER_FALAG) && lineStr.Contains(PASS_TYPE)))
                    {
                        if (!errorMapping.TryGetValue(path, out tempDic))
                        {
                            tempDic = new Dictionary<int, List<string>>();
                            errorMapping[path] = tempDic;
                        }

                        if (!tempDic.TryGetValue(lineIndex, out tempList))
                        {
                            tempList = new List<string>();
                            tempDic[lineIndex] = tempList;
                        }

                        tempList.Add(error1);
                    }

                    // Error 2
                    if (lineStr.Contains(LINE_COMMENT_INVALID_START_FLAG) || lineStr.Contains(LINE_COMMENT_INVALID_END_FLAG))
                    {
                        if (!errorMapping.TryGetValue(path, out tempDic))
                        {
                            tempDic = new Dictionary<int, List<string>>();
                            errorMapping[path] = tempDic;
                        }

                        if (!tempDic.TryGetValue(lineIndex, out tempList))
                        {
                            tempList = new List<string>();
                            tempDic[lineIndex] = tempList;
                        }

                        tempList.Add(error2);
                    }

                    // Error 3
                    string[] arr = lineStr.Split(splitArray, System.StringSplitOptions.None);
                    if (arr != null && arr.Length > 2)
                    {
                        if (!errorMapping.TryGetValue(path, out tempDic))
                        {
                            tempDic = new Dictionary<int, List<string>>();
                            errorMapping[path] = tempDic;
                        }

                        if (!tempDic.TryGetValue(lineIndex, out tempList))
                        {
                            tempList = new List<string>();
                            tempDic[lineIndex] = tempList;
                        }

                        tempList.Add(error3);
                    }

                    // Error 4
                    if (lineStr.Contains("#pragma") && lineStr.Contains("//"))
                    {
                        int pragmaIndex = lineStr.IndexOf("#pragma");
                        int annotationIndex = lineStr.IndexOf("//");

                        if (pragmaIndex < annotationIndex)
                        {
                            if (!errorMapping.TryGetValue(path, out tempDic))
                            {
                                tempDic = new Dictionary<int, List<string>>();
                                errorMapping[path] = tempDic;
                            }

                            if (!tempDic.TryGetValue(lineIndex, out tempList))
                            {
                                tempList = new List<string>();
                                tempDic[lineIndex] = tempList;
                            }

                            tempList.Add(error4);
                        }
                    }

                    //// Error 4
                    //if (Regex.IsMatch(lineStr, pattern41))
                    //{
                    //    Match match = Regex.Match(lineStr, pattern41);

                    //    string backStr = match.Groups[4].Value;
                    //    string strCleaned = Regex.Replace(backStr.Trim(), @" {2,}", " ");
                    //    string[] keywords = strCleaned.Split(' ');
                    //    var dynamicKW = keywords.Intersect(dynamicKeywords);
                    //    if (dynamicKW.Count() > 0)
                    //    {
                    //        if (keywords.Length == 2 && (
                    //            (Regex.IsMatch(keywords[0], pattern42) && !Regex.IsMatch(keywords[1], pattern42)) ||
                    //            (!Regex.IsMatch(keywords[0], pattern42) && Regex.IsMatch(keywords[1], pattern42))
                    //            ))
                    //        {// 即要求必须声明的是两个关键字，有且仅有一个关键字全由下划线组成。除此以外的情况均报错。
                    //        }
                    //        else
                    //        {
                    //            if (!errorMapping.TryGetValue(path, out tempDic))
                    //            {
                    //                tempDic = new Dictionary<int, List<string>>();
                    //                errorMapping[path] = tempDic;
                    //            }

                    //            if (!tempDic.TryGetValue(lineIndex, out tempList))
                    //            {
                    //                tempList = new List<string>();
                    //                tempDic[lineIndex] = tempList;
                    //            }

                    //            tempList.Add(error4);
                    //        }
                    //    }
                    //}

                    lineStr = sr.ReadLine();
                    lineIndex++;
                }


                sr.Close();
                sr.Dispose();
            }

            EditorUtility.ClearProgressBar();

            string outputStr = "";
            foreach (var data in errorMapping)
            {
                string shaderPath = data.Key;
                outputStr += "Error Shader：" + shaderPath + "\n";
                foreach (var childData in data.Value)
                {
                    foreach (var error in childData.Value)
                    {
                        outputStr += "\t第" + childData.Key + "行：" + error + "\n";
                    }
                }
            }

            DirectoryInfo dInfo = new DirectoryInfo("ShaderErrorList.txt");
            StreamWriter sw = new StreamWriter(dInfo.FullName, false, System.Text.Encoding.UTF8);
            sw.Write(outputStr);
            sw.Flush();
            sw.Close();
            sw.Dispose();

            System.Diagnostics.Process.Start(dInfo.FullName);

            return string.IsNullOrEmpty(outputStr);
        }
        [MenuItem("CustomSVC/Normalized Shader Note(规范单行注释)")]
        static void NormalizedShaderNote()
        {
            string[] shaderAssets = AssetDatabase.FindAssets("t:Shader", new string[] { "Assets" });
            if (shaderAssets == null || shaderAssets.Length == 0) return;

            for (int i = 0; i < shaderAssets.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(shaderAssets[i]);
                EditorUtility.DisplayProgressBar("Check Shader Standard", "Check..." + path, (i + 1) * 1.0f / shaderAssets.Length);

                string[] lines = File.ReadAllLines(path);

                for (int j = 0; j < lines.Length; j++)
                {
                    if (lines[j].Contains("/*") && lines[j].Contains("*/"))
                    {
                        Debug.Log($"shader:{path}---- before:{lines[j]} line:{j + 1}");
                        lines[j] = lines[j].Replace("/*", "//").Replace("*/", " ");
                        Debug.Log($"shader:{path}---- after:{lines[j]} line:{j + 1}");
                    }
                }

                File.WriteAllLines(path, lines);
            }
            EditorUtility.ClearProgressBar();
        }

        public static void DoCollection(ShaderVariantCollectionToolSetting setting)
        {
            if (setting == null)
            {
                return;
            }

            if (!CheckShaderStandard())
            {
                return;
            }

            // ### 001: 遍历材质，按shader分类 ############################################################################################
            Dictionary<Shader, List<Material>> shaderToMaterialMapping = new Dictionary<Shader, List<Material>>();
            var folds = new List<string>();
            foreach (var item in setting.m_MatPathArray)
            {
                if (Directory.Exists(item))
                {
                    folds.Add(item);
                }
                else
                {
                    Debug.LogError($"Path: {item} not exist");
                }
            }
            var allMaterialGUIDs = AssetDatabase.FindAssets("t:Material", folds.ToArray());
            var allCount = allMaterialGUIDs.Length;
            for (int i = 0; i < allCount; i++)
            {
                var guid = allMaterialGUIDs[i];
                Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m.shader.name != "Hidden/InternalErrorShader" && !setting.isShaderClone(m.shader))
                {
                    if (shaderToMaterialMapping.ContainsKey(m.shader))
                    {
                        shaderToMaterialMapping[m.shader].Add(m);
                    }
                    else
                    {
                        shaderToMaterialMapping.Add(m.shader, new List<Material>() { m });
                    }
                }
                float progress = (float)i / (float)allCount;
                string des = i.ToString() + "/" + allCount.ToString();
                EditorUtility.DisplayProgressBar("Collecting Materials:", des, progress);
            }
            EditorUtility.DisplayProgressBar("Collecting Shaders:", "", 0.2f);
            // ### 002: Per SubShader & Per Pass 收集材质中Keyword组合 ######################################################################
            Dictionary<Shader, ShaderParseData> shaderParseDataMapping = new Dictionary<Shader, ShaderParseData>();
            foreach (var data in shaderToMaterialMapping)
            {
                Shader s = data.Key;
                ShaderParseData spData = setting.GetShaderParseData(s);
                shaderParseDataMapping.Add(s, spData);

                List<string> shaderAllKWS = setting.GetShaderAllKeywords(s);
                foreach (var mat in data.Value)
                {
                    List<string> materialKWS = new List<string>(mat.shaderKeywords);
                    // 根据当前Shader关键字，剔除不合法的关键字
                    for (int i = materialKWS.Count - 1; i >= 0; i--)
                    {
                        //var matPath = AssetDatabase.GetAssetPath(mat);
                        //if ((materialKWS[i]== "_DISSOLVEUVSPACETYPE_UV2") && matPath.Contains("R00"))
                        //{
                        //    Debug.LogError($"mat path :{matPath}");
                        //}
                        if (!shaderAllKWS.Contains(materialKWS[i]))
                        {
                            materialKWS.RemoveAt(i);
                        }
                    }
                    // 根据配置得动态KW，如果存在则剔除关键字
                    if (materialKWS.Count > 0 && setting.m_DynamicKWArray.Count > 0)
                    {
                        for (int i = materialKWS.Count - 1; i >= 0; i--)
                        {
                            if (setting.m_DynamicKWArray.Contains(materialKWS[i]))
                            {
                                materialKWS.RemoveAt(i);
                            }
                        }
                    }
                    // Collect Material Keyword
                    materialKWS.Sort();
                    string combineStr = "";
                    foreach (var kw in materialKWS)
                    {
                        combineStr += kw + "|";
                    }
                    if (!string.IsNullOrEmpty(combineStr))
                        combineStr = combineStr.Substring(0, combineStr.Length - 1);
                    spData.AddMaterialVariantKeywordGroup(combineStr);
                }
            }
            EditorUtility.DisplayProgressBar("Collecting Dynamic Keywords:", "", 0.35f);

            // ### 003: Per SubShader & Per Pass 收集动态Keyword(配置)组合，如果没有配置则认为不收集相关变体，并与静态Keyword组合 #########
            if (setting.m_DynamicKWArray.Count > 0)
            {
                // 校验配置中动态关键字(setting.DYNAMIC_KW)是否重复，剔除重复关键字
                List<string> finalDynamicKWS = setting.m_DynamicKWArray.Where((x, i) => setting.m_DynamicKWArray.FindIndex(z => z == x) == i).ToList();
                finalDynamicKWS.AddRange(BUILDIN_KWS);

                // 收集配置中动态Keyword
                foreach (var spData in shaderParseDataMapping.Values)
                {
                    spData.GenerateKWCombination(finalDynamicKWS);
                }
            }
            EditorUtility.DisplayProgressBar("Collecting Scene Info:", "", 0.55f);

            // ### 004: 遍历场景，按shader分类记录信息 ###################################################################
            Dictionary<Shader, Dictionary<Scene, SceneInfo>> shaderToSceneInfoMapping = new Dictionary<Shader, Dictionary<Scene, SceneInfo>>();
            string activeSceneBeforePath = EditorSceneManager.GetActiveScene().path;

            setting.m_SceneArray = setting.m_SceneArray.Where(s => s != null).Distinct().ToList();
            foreach (var sceneAsset in setting.m_SceneArray)
            {
                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                try
                {
                    Scene nowScene = EditorSceneManager.OpenScene(scenePath);
                    var renderers = nowScene.GetRootGameObjects().SelectMany(gameObject => gameObject.GetComponentsInChildren<Renderer>())
                    .Distinct().ToArray();

                    foreach (var renderer in renderers)
                    {
                        foreach (var m in renderer.sharedMaterials)
                        {
                            var shader = m.shader;

                            if (!shaderToSceneInfoMapping.ContainsKey(shader))
                            {
                                shaderToSceneInfoMapping.Add(shader, new Dictionary<Scene, SceneInfo>());
                            }

                            if (!shaderToSceneInfoMapping[shader].ContainsKey(nowScene))
                            {
                                shaderToSceneInfoMapping[shader].Add(nowScene, new SceneInfo());
                            }

                            if ((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI) != 0)
                            {
                                shaderToSceneInfoMapping[shader][nowScene].hasContributeGITrue = true;
                            }
                            else
                            {
                                shaderToSceneInfoMapping[shader][nowScene].hasContributeGIFalse = true;
                            }

                            if (m.enableInstancing)
                            {
                                shaderToSceneInfoMapping[shader][nowScene].enableInstancing = true;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    Debug.LogError($"Open the scene path \"{scenePath}\" fails.");
                    continue;
                }
            }
            EditorSceneManager.OpenScene(activeSceneBeforePath);
            EditorUtility.DisplayProgressBar("Combine Builtin Keywords:", "", 0.8f);

            // ### 005: Per SubShader & Per Pass 收集、合并Unity内置Keyword组合 #############################################################
            foreach (var data in shaderParseDataMapping)
            {
                var sceneInfo = shaderToSceneInfoMapping.ContainsKey(data.Key) ?
                    shaderToSceneInfoMapping[data.Key] : new Dictionary<Scene, SceneInfo>();
                data.Value.CombineBuiltinKWVariant(sceneInfo);
            }
            EditorUtility.DisplayProgressBar("Add Shader Clone:", "", 0.85f);

            // ### 006: 添加shader clone #############################################################
            foreach (var shaderCloneInfo in setting.m_ShaderCloneInfoArray)
            {
                Shader shaderClone = shaderCloneInfo.m_ShaderClone;
                Shader shaderOri = shaderCloneInfo.m_ShaderOri;
                if (shaderClone && shaderOri && shaderParseDataMapping.ContainsKey(shaderOri))
                {
                    var spDataOri = shaderParseDataMapping[shaderOri];
                    ShaderParseData spDataClone = new ShaderParseData(shaderClone, spDataOri);
                    shaderParseDataMapping.Add(shaderClone, spDataClone);
                }
            }

            EditorUtility.DisplayProgressBar("Output to SVC:", "", 0.90f);
            // ### 007: Output to SVC #######################################################################################################
            ShaderVariantCollection svc = setting.m_ShaderVariantCollection;
            svc.Clear();
            foreach (var splitSVC in setting.m_SplitSVCInfoArray)
            {
                if (splitSVC.m_SVC != null)
                {
                    splitSVC.m_SVC.Clear();
                }
            }

            foreach (var data in shaderParseDataMapping)
            {
                Shader s = data.Key;
                string shaderPath = AssetDatabase.GetAssetPath(s);

                foreach (var subshader in data.Value.m_DataMapping.Values)
                {
                    foreach (var pass in subshader.m_DataMapping.Values)
                    {
                        if (pass.m_PassType == PassType.Vertex || pass.m_PassType == PassType.VertexLM || pass.m_PassType == PassType.VertexLMRGBM ||
                            pass.m_PassType == PassType.LightPrePassBase || pass.m_PassType == PassType.LightPrePassFinal ||
                            pass.m_PassType == PassType.Deferred || pass.m_PassType == PassType.Meta || pass.m_PassType == PassType.MotionVectors ||
                            pass.m_PassType == PassType.ScriptableRenderPipeline || pass.m_PassType == PassType.ScriptableRenderPipelineDefaultUnlit)
                        {
                            continue;
                        }

                        foreach (var variant in pass.m_CombineValidKWS)
                        {
                            // 按pass 排除非法组合（2021版本提供了按pass获取keyword接口方法）
                            try
                            {
                                ShaderVariantCollection.ShaderVariant sv = new ShaderVariantCollection.ShaderVariant(s, pass.m_PassType, variant);
                                if (!svc.Contains(sv))
                                {
                                    svc.Add(sv);
                                    foreach (var splitSVCInfo in setting.m_SplitSVCInfoArray)
                                    {
                                        if (!string.IsNullOrEmpty(splitSVCInfo.m_ShaderPath) && splitSVCInfo.m_SVC != null
                                            && shaderPath.Contains(splitSVCInfo.m_ShaderPath))
                                        {
                                            splitSVCInfo.m_SVC.Add(sv);
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (System.Exception e)
                            {
                                //string debugStr = "";
                                //foreach (var kw in variant)
                                //{
                                //    debugStr += kw + " ";
                                //}
                                //debugStr = debugStr.Substring(0, debugStr.Length - 1);
                                //Debug.LogError(pass.m_PassType + "|" + debugStr + "\n" + e.ToString());
                            }
                        }

                    }
                }
            }
            EditorUtility.DisplayProgressBar("Save Assets:", "", 0.95f);

            string collectionResult = $"Collection of shader variants completed.\ntotal svc: {svc.shaderCount} shader, {svc.variantCount} variants.\n";
            foreach (var splitSVCInfo in setting.m_SplitSVCInfoArray)
            {
                if (splitSVCInfo.m_SVC != null)
                {
                    EditorUtility.SetDirty(splitSVCInfo.m_SVC);
                    collectionResult += $"{splitSVCInfo.m_ShaderPath}: {splitSVCInfo.m_SVC.shaderCount} shader, {splitSVCInfo.m_SVC.variantCount} variants.\n";
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(collectionResult);
            EditorUtility.ClearProgressBar();
        }
        #endregion
    }
}


//public class MathCombineUtil
//{
//    public static List<string[]> GetCombination(string[] t)
//    {
//        List<string[]> totalCombineList = new List<string[]>();
//        for (int i = 0; i < t.Length; i++)
//        {
//            int combineCount = i + 1;
//            int[] temp = new int[combineCount];
//            GetCombination(ref totalCombineList, t, t.Length, combineCount, temp, combineCount);
//        }

//        return totalCombineList;
//    }


//    static void GetCombination(ref List<string[]> list, string[] source, int totalCount, int requiredCount, int[] indexList, int M)
//    {
//        for (int i = totalCount; i >= requiredCount; i--)
//        {
//            indexList[requiredCount - 1] = i - 1;
//            if (requiredCount > 1)
//            {
//                GetCombination(ref list, source, i - 1, requiredCount - 1, indexList, M);
//            }
//            else
//            {
//                if (list == null)
//                {
//                    list = new List<string[]>();
//                }
//                string[] temp = new string[M];
//                for (int j = 0; j < indexList.Length; j++)
//                {
//                    temp[j] = source[indexList[j]];
//                }
//                list.Add(temp);
//            }
//        }
//    }

//}

#endif
