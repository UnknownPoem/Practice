#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEditor;
using System.IO;

namespace CustomSVC
{
    public class ShaderBuildProcessor : IPreprocessShaders
    {
        public int callbackOrder { get { return 0; } }

        static Dictionary<Shader, Dictionary<PassType, List<string>>> _shaderCollectionMapping;

        public static Dictionary<Shader, Dictionary<PassType, List<string>>> ShaderCollectionMappings
        {
            get
            {
                if (_shaderCollectionMapping == null)
                {
                    var settings = ShaderVariantCollectionToolSetting.GetAllSettings();
                    if (settings.Count != 0)
                    {
                        List<ShaderVariantCollection> svcs = new List<ShaderVariantCollection>();
                        foreach (var setting in settings)
                        {
                            svcs.Add(setting.m_ShaderVariantCollection);
                        }

                        _shaderCollectionMapping = ShaderVariantCollectionToolSetting.GetShaderVariantCollectionData(svcs);
                    }
                    else
                    {
                        _shaderCollectionMapping = new Dictionary<Shader, Dictionary<PassType, List<string>>>();
                    }
                }

                return _shaderCollectionMapping;
            }
        }

        public static int totalVariantCount = 0;
        public static int keepVariantCount = 0;
        public static int totalShaderCount = 0;
        public static int keepShaderCount = 0;
        public static Dictionary<Shader, Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>>> ShaderAllVariantMapping = new Dictionary<Shader, Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>>>();
        public static Dictionary<Shader, Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>>> ShaderKeepVariantMapping = new Dictionary<Shader, Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>>>();
        public static void SetShaderCollectionMappingNull()
        {
            _shaderCollectionMapping = null;
            totalVariantCount = 0;
            keepVariantCount = 0;
            totalShaderCount = 0;
            keepShaderCount = 0;

            if (ShaderVariantCollectionToolSetting.b_debug)
            {
                ShaderAllVariantMapping.Clear();
                ShaderKeepVariantMapping.Clear();
            }
        }

        /// <summary>
        /// 清理shader.bundle，重新触发bundle构建，之后进到变体剥离逻辑
        /// </summary>
        public static void ClearShaderBundle()
        {
            var array = new string[] {
                "Assets/StreamingAssets/resources/fonts.assetbundle",
                "Assets/StreamingAssets/resources/shaders.assetbundle",
                "Assets/StreamingAssets/resources/shaders_optional.assetbundle",
            };

            foreach (var a in array)
            {
                if (File.Exists(a)) File.Delete(a);
                if (File.Exists($"{a}.meta")) File.Delete($"{a}.meta");
                if (File.Exists($"{a}.manifest")) File.Delete($"{a}.manifest");
                if (File.Exists($"{a}.manifest.meta")) File.Delete($"{a}.manifest.meta");
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {

            if (ShaderVariantCollectionToolSetting.b_debug)
            {
                DebugAllInfo(shader, snippet, data); // debug
            }
            totalVariantCount += data.Count;
            ++totalShaderCount;
            bool isShaderKeep = false;

            for (int i = data.Count - 1; i >= 0; --i)
            {
                if (data[i].shaderCompilerPlatform == ShaderCompilerPlatform.GLES20)
                {
                    data.RemoveAt(i);
                }
            }

            Dictionary<PassType, List<string>> tempDic = null;
            if (ShaderCollectionMappings.TryGetValue(shader, out tempDic))
            {

                List<string> svcKWList = null;
                tempDic.TryGetValue(snippet.passType, out svcKWList);
                if (svcKWList != null && svcKWList.Count > 0)
                {
                    for (int i = data.Count - 1; i >= 0; --i)
                    {
                        ShaderKeyword[] keywords = data[i].shaderKeywordSet.GetShaderKeywords();

                        List<string> totalKWList = new List<string>();
                        foreach (var kw in keywords)
                            totalKWList.Add(ShaderKeyword.GetKeywordName(shader, kw));

                        totalKWList.Sort();
                        string fullKW = "";
                        foreach (var kw in totalKWList)
                            fullKW += kw + "|";
                        if (!string.IsNullOrEmpty(fullKW))
                            fullKW = fullKW.Substring(0, fullKW.Length - 1);

                        if (!svcKWList.Contains(fullKW))
                        {
                            data.RemoveAt(i);
                        }
                        else
                        {
                            isShaderKeep = true;
                            ++keepVariantCount;
                            if (ShaderVariantCollectionToolSetting.b_debug)
                            {
                                DebugKeepInfo(shader, snippet, data[i], fullKW);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = data.Count - 1; i >= 0; --i)
                    {
                        data.RemoveAt(i);
                    }
                }
            }
            else
            {// SVC中未包含的shader
                isShaderKeep = true;
                keepVariantCount += data.Count;
                //for (int i = data.Count - 1; i >= 0; --i)
                //{
                //    if (data[i].shaderCompilerPlatform != ShaderCompilerPlatform.GLES3x
                //        /* || shader.name == "Spine/Sprite/Pixel Lit" || shader.name == "Spine/Outline/Sprite/Pixel Lit"*/)
                //        data.RemoveAt(i);
                //}
            }

            if (isShaderKeep)
            {
                ++keepShaderCount;
            }
        }

        #region Debug

        public static void DebugKeepInfo(Shader shader, ShaderSnippetData snippet, ShaderCompilerData scd, string fullKW)
        {
            if (ShaderVariantCollectionToolSetting.b_debug == false)
            {
                return;
            }

            Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>> tempGTDic = null;
            Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>> tempSPDic = null;
            Dictionary<ShaderType, List<string>> temmpSTDic = null;
            List<string> tempKWList = null;

            if (!ShaderKeepVariantMapping.TryGetValue(shader, out tempGTDic))
            {
                tempGTDic = new Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>>();
                ShaderKeepVariantMapping[shader] = tempGTDic;
            }

            if (!tempGTDic.TryGetValue(scd.graphicsTier, out tempSPDic))
            {
                tempSPDic = new Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>();
                tempGTDic[scd.graphicsTier] = tempSPDic;
            }

            if (!tempSPDic.TryGetValue(scd.shaderCompilerPlatform, out temmpSTDic))
            {
                temmpSTDic = new Dictionary<ShaderType, List<string>>();
                tempSPDic[scd.shaderCompilerPlatform] = temmpSTDic;
            }

            if (!temmpSTDic.TryGetValue(snippet.shaderType, out tempKWList))
            {
                tempKWList = new List<string>();
                temmpSTDic[snippet.shaderType] = tempKWList;
            }

            string finalKW = snippet.passType + "|" + fullKW;
            if (!tempKWList.Contains(finalKW))
                tempKWList.Add(finalKW);
        }

        public static void DebugAllInfo(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (ShaderVariantCollectionToolSetting.b_debug == false)
            {
                return;
            }

            Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>> tempGTDic = null;
            Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>> tempSPDic = null;
            Dictionary<ShaderType, List<string>> temmpSTDic = null;
            List<string> tempKWList = null;

            if (!ShaderAllVariantMapping.TryGetValue(shader, out tempGTDic))
            {
                tempGTDic = new Dictionary<GraphicsTier, Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>>();
                ShaderAllVariantMapping[shader] = tempGTDic;
            }

            for (int i = 0; i < data.Count; i++)
            {
                ShaderCompilerData scd = data[i];

                if (!tempGTDic.TryGetValue(scd.graphicsTier, out tempSPDic))
                {
                    tempSPDic = new Dictionary<ShaderCompilerPlatform, Dictionary<ShaderType, List<string>>>();
                    tempGTDic[scd.graphicsTier] = tempSPDic;
                }

                if (!tempSPDic.TryGetValue(scd.shaderCompilerPlatform, out temmpSTDic))
                {
                    temmpSTDic = new Dictionary<ShaderType, List<string>>();
                    tempSPDic[scd.shaderCompilerPlatform] = temmpSTDic;
                }

                if (!temmpSTDic.TryGetValue(snippet.shaderType, out tempKWList))
                {
                    tempKWList = new List<string>();
                    temmpSTDic[snippet.shaderType] = tempKWList;
                }

                ShaderKeyword[] keywords = scd.shaderKeywordSet.GetShaderKeywords();
                List<string> kwList = new List<string>();
                foreach (var kw in keywords)
                {
                    kwList.Add(ShaderKeyword.GetKeywordName(shader, kw));
                }
                kwList.Sort();
                string combineStr = snippet.passType + "|";
                foreach (var kw in kwList)
                {
                    combineStr += kw + "|";
                }

                combineStr = combineStr.Substring(0, combineStr.Length - 1);

                if (!tempKWList.Contains(combineStr))
                    tempKWList.Add(combineStr);
            }
        }

        public static void OutputDebugInfo()
        {
            Debug.Log($"[ShaderBuildProcessor] TotalShader = {totalShaderCount}, KeepShader = {keepShaderCount}, " +
                $"totalVariant = {totalVariantCount}, keepVariant = {keepVariantCount}");

            if (CustomSVC.ShaderVariantCollectionToolSetting.b_debug)
            {
                // All Variant Info
                string debugShaderVariantFolder = "DebugCustomSVC/SVC_ALL/";
                if (Directory.Exists(debugShaderVariantFolder))
                {
                    string[] files = Directory.GetFiles(debugShaderVariantFolder, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                        File.Delete(file);
                }
                else
                    Directory.CreateDirectory(debugShaderVariantFolder);

                foreach (var data in ShaderAllVariantMapping)
                {
                    Shader s = data.Key;
                    var gtMapping = data.Value;
                    foreach (var gt in gtMapping)
                    {
                        var platformMapping = gt.Value;
                        foreach (var pm in platformMapping)
                        {
                            string name = s.name + "_" + gt.Key + "_" + pm.Key + ".txt";
                            name = name.Replace('/', '_');
                            string outputPath = debugShaderVariantFolder + name;
                            string outputStr = "";
                            var shaderTypeMapping = pm.Value;
                            foreach (var st in shaderTypeMapping)
                            {
                                outputStr += st.Key + "\n";
                                foreach (var kw in st.Value)
                                {
                                    outputStr += "\t" + kw.Replace("|", " ") + "\n";
                                }
                            }

                            //if (File.Exists(outputPath))
                            //{
                            //    File.Delete(outputPath);
                            //}
                            //File.Create(outputPath).Close();

                            StreamWriter sw = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                            sw.Write(outputStr);
                            sw.Flush();
                            sw.Close();
                            sw.Dispose();
                        }
                    }
                }

                // Keep Variant Info
                string debugKeepShaderVariantFolder = "DebugCustomSVC/SVC_KEEP/";
                if (Directory.Exists(debugKeepShaderVariantFolder))
                {
                    string[] files = Directory.GetFiles(debugKeepShaderVariantFolder, "*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                        File.Delete(file);
                }
                else
                    Directory.CreateDirectory(debugKeepShaderVariantFolder);

                foreach (var data in ShaderKeepVariantMapping)
                {
                    Shader s = data.Key;
                    var gtMapping = data.Value;
                    foreach (var gt in gtMapping)
                    {
                        var platformMapping = gt.Value;
                        foreach (var pm in platformMapping)
                        {
                            string name = s.name + "_" + gt.Key + "_" + pm.Key + ".txt";
                            name = name.Replace('/', '_');
                            string outputPath = debugKeepShaderVariantFolder + name;
                            string outputStr = "";
                            var shaderTypeMapping = pm.Value;
                            foreach (var st in shaderTypeMapping)
                            {
                                outputStr += st.Key + "\n";
                                foreach (var kw in st.Value)
                                {
                                    outputStr += "\t" + kw.Replace("|", " ") + "\n";
                                }
                            }

                            StreamWriter sw = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                            sw.Write(outputStr);
                            sw.Flush();
                            sw.Close();
                            sw.Dispose();
                        }
                    }
                }
            }
        }

        #endregion

    }
}

#endif
