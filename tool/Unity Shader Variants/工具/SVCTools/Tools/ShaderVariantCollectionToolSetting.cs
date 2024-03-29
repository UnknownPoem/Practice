#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace CustomSVC
{
    [CreateAssetMenu(fileName = "ShaderVariantCollectionToolSetting", menuName = "引擎工具[新]/变体收集器配置")]
    public class ShaderVariantCollectionToolSetting : ScriptableObject
    {
        [System.Serializable]
        public class SplitSVCInfo
        {
            public string m_ShaderPath = string.Empty;
            public ShaderVariantCollection m_SVC = null;
        }

        [System.Serializable]
        public class ShaderCloneInfo
        {
            public Shader m_ShaderClone = null;
            public Shader m_ShaderOri = null;
        }

        #region Fields
        public static bool b_debug = false;
        [Tooltip("变体收集文件")]
        public ShaderVariantCollection m_ShaderVariantCollection = null;
        [Tooltip("收集shader用到的材质路径")]
        public string[] m_MatPathArray = new string[] { "Assets/Res" };
        [Tooltip("会在游戏运行时设置的动态关键字")]
        public List<string> m_DynamicKWArray = new List<string>() { };
        [Tooltip("收集内置关键字用到的场景")]
        public List<SceneAsset> m_SceneArray = new List<SceneAsset>();
        [Tooltip("需要拆分的SVC，按照shader路径划分")]
        public List<SplitSVCInfo> m_SplitSVCInfoArray = new List<SplitSVCInfo>();
        [Tooltip("shader克隆，shaderClone的关键字组合将被设置为完全与shaderOri完全相同")]
        public List<ShaderCloneInfo> m_ShaderCloneInfoArray = new List<ShaderCloneInfo>();

        //[Tooltip("要收集的Shader路径")]
        //public string[] m_ShaderValidPathArray = new string[] { "Assets/Shader/" };
        [Tooltip("清空当前收集到的变体")]
        private string m_ClearSVCMethodName = "ClearCurrentShaderVariantCollection";
        [Tooltip("保存当前收集到变体")]
        private string m_SaveSVCMethodName = "SaveCurrentShaderVariantCollection";
        [Tooltip("变体过滤")]
        private string m_GetShaderVariantEntriesFilteredMethodName = "GetShaderVariantEntriesFiltered";
        [Tooltip("获取全局KeyWords")]
        private string m_GetGlobalKeywordsMethodName = "GetShaderGlobalKeywords";
        [Tooltip("获取local KeyWords")]
        private string m_GetShaderLocalKeywordsMethodName = "GetShaderLocalKeywords";
        [Tooltip("获取Subshader数量")]
        private string m_GetShaderSubshaderCountName = "GetShaderSubshaderCount";
        [Tooltip("ShaderVariantCollection-Item属性")]
        private static string m_ShaderVariantCollectionItemName = "m_Shaders";
        [Tooltip("ShaderVariantCollection-Shader属性")]
        private static string m_ShaderVariantCollectionShaderName = "first";
        [Tooltip("ShaderVariantCollection-ShaderVariant-Shader属性")]
        private static string m_ShaderVariantCollectionShaderVariantName = "second.variants";
        [Tooltip("ShaderVariantCollection-ShaderVariant-Keywords属性")]
        private static string m_ShaderVariantCollectionShaderVariantKeyName = "keywords";
        [Tooltip("ShaderVariantCollection-ShaderVariant-PassType属性")]
        private static string m_ShaderVariantCollectionShaderVariantPassTypeName = "passType";


        ////[Tooltip("SVC Camera Parent")]
        ////public string m_SVCCameraRoot = "SVCCameraRoot";
        //[Tooltip("Single Keywords")]
        //public string m_SearchSingleKeywords = "";
        //[Tooltip("Full Keywords")]
        //public string m_SearchFullKeywords = "";

        //public string m_UnityEditorPath
        //{
        //    get
        //    {
        //        string applicationPath = EditorApplication.applicationPath;

        //        if (applicationPath.EndsWith("Unity.exe"))
        //        {
        //            return applicationPath.Replace("Unity.exe", @"Data\Managed\UnityEditor.dll");
        //        }
        //        else
        //        {
        //            applicationPath += "/Contents/Managed/UnityEditor.dll";
        //            return applicationPath;
        //        }
        //    }
        //}

        #endregion

        #region Public Methods

        public static List<ShaderVariantCollectionToolSetting> GetAllSettings()
        {
            MonoScript ms = MonoScript.FromScriptableObject(new ShaderVariantCollectionToolSetting());
            string scriptFilePath = AssetDatabase.GetAssetPath(ms);
            string scriptDirectoryPath = System.IO.Path.GetDirectoryName(scriptFilePath);
            string[] findResultGUID = AssetDatabase.FindAssets("t:ShaderVariantCollectionToolSetting", new string[] { scriptDirectoryPath });

            List<ShaderVariantCollectionToolSetting> res = new List<ShaderVariantCollectionToolSetting>();

            if (findResultGUID.Length == 0)
            {
                ShaderVariantCollectionToolSetting newSetting = ScriptableObject.CreateInstance<ShaderVariantCollectionToolSetting>();

                AssetDatabase.CreateAsset(newSetting, scriptDirectoryPath + "\\ShaderVariantCollectionToolSetting.asset");
                AssetDatabase.SaveAssets();

                res.Add(newSetting);
            }
            else
            {
                foreach (var guid in findResultGUID)
                {
                    res.Add(AssetDatabase.LoadAssetAtPath<ShaderVariantCollectionToolSetting>(
                    AssetDatabase.GUIDToAssetPath(guid)));
                }
            }

            return res;
        }

        /// <summary>
        /// 清空已经收集的变体
        /// </summary>
        /// <returns></returns>
        public bool ClearCurrentShaderVariantCollection()
        {
            MethodInfo mInfo = GetMethodInfo<ShaderUtil>(m_ClearSVCMethodName);
            if (mInfo == null) return false;

            mInfo.Invoke(null, null);

            return true;
        }

        /// <summary>
        /// 保存当前已经收集的变体
        /// </summary>
        /// <param name="savePath"></param>
        /// <returns></returns>
        public bool SaveCurrentShaderVariantCollection(string savePath)
        {
            MethodInfo mInfo = GetMethodInfo<ShaderUtil>(m_SaveSVCMethodName);
            if (mInfo == null) return false;

            mInfo.Invoke(null, new object[] { savePath });

            return true;
        }

        /// <summary>
        /// 获取全局Keywords
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public string[] GetShaderGlobalKeywords(Shader shader)
        {
            if (shader == null) return null;

            MethodInfo mInfo = GetMethodInfo<ShaderUtil>(m_GetGlobalKeywordsMethodName);
            if (mInfo == null) return null;
            string[] keywords = mInfo.Invoke(null, new object[] { shader }) as string[];

            return keywords;
        }

        /// <summary>
        /// 获取本地Keywords
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public string[] GetShaderLocalKeywords(Shader shader)
        {
            if (shader == null) return null;

            MethodInfo mInfo = GetMethodInfo<ShaderUtil>(m_GetShaderLocalKeywordsMethodName);
            if (mInfo == null) return null;
            string[] keywords = mInfo.Invoke(null, new object[] { shader }) as string[];

            return keywords;
        }

        /// <summary>
        /// 获取所有Keywords
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public List<string> GetShaderAllKeywords(Shader shader)
        {
            if (shader == null) return null;

            string[] localkeywords = GetShaderLocalKeywords(shader);
            string[] globalkeywords = GetShaderGlobalKeywords(shader);

            List<string> keywords = new List<string>(localkeywords);
            keywords.AddRange(globalkeywords);

            return keywords;
        }

        public ShaderParseData GetShaderParseData(Shader shader)
        {
            return new ShaderParseData(shader);
        }

        public static Dictionary<Shader, Dictionary<PassType, List<string>>> GetShaderVariantCollectionData(List<ShaderVariantCollection> svcList)
        {
            Dictionary<Shader, Dictionary<PassType, List<string>>> mapping = new Dictionary<Shader, Dictionary<PassType, List<string>>>();
            Dictionary<PassType, List<string>> tempDic = null;
            List<string> tempList = null;
            string[] splitArr = new string[] { " " };
            if (svcList != null && svcList.Count > 0)
            {
                foreach (var svc in svcList)
                {
                    SerializedObject so = new SerializedObject(svc);
                    SerializedProperty childCollectionSP = so.FindProperty(m_ShaderVariantCollectionItemName);
                    if (childCollectionSP != null)
                    {
                        for (int k = 0; k < childCollectionSP.arraySize; k++)
                        {
                            SerializedProperty sp = childCollectionSP.GetArrayElementAtIndex(k);

                            SerializedProperty shaderSP = sp.FindPropertyRelative(m_ShaderVariantCollectionShaderName);

                            Shader shader = (Shader)shaderSP.objectReferenceValue;
                            if (shader == null) continue;

                            string shaderPath = AssetDatabase.GetAssetPath(shader);
                            if (!System.IO.File.Exists(shaderPath)) continue;

                            if (!mapping.TryGetValue(shader, out tempDic))
                            {
                                tempDic = new Dictionary<PassType, List<string>>();
                                mapping[shader] = tempDic;
                            }

                            SerializedProperty shaderVariantSP = sp.FindPropertyRelative(m_ShaderVariantCollectionShaderVariantName);
                            for (int j = 0; j < shaderVariantSP.arraySize; j++)
                            {
                                SerializedProperty itemSP = shaderVariantSP.GetArrayElementAtIndex(j);
                                SerializedProperty keywordsSP = itemSP.FindPropertyRelative(m_ShaderVariantCollectionShaderVariantKeyName);
                                SerializedProperty passTypeSP = itemSP.FindPropertyRelative(m_ShaderVariantCollectionShaderVariantPassTypeName);

                                string keywordsStr = keywordsSP.stringValue;
                                List<string> keywords = new List<string>(keywordsStr.Split(splitArr, System.StringSplitOptions.RemoveEmptyEntries));
                                keywords.Sort();
                                string combineStr = "";
                                foreach (var kw in keywords)
                                {
                                    combineStr += kw + "|";
                                }
                                if (!string.IsNullOrEmpty(combineStr))
                                    combineStr = combineStr.Substring(0, combineStr.Length - 1);

                                PassType passType = (PassType)passTypeSP.intValue;
                                if (!tempDic.TryGetValue(passType, out tempList))
                                {
                                    tempList = new List<string>();
                                    tempDic[passType] = tempList;
                                }

                                tempList.Add(combineStr);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError(svc + ":Not find " + m_ShaderVariantCollectionItemName);
                    }
                }
            }

            return mapping;
        }

        /// <summary>
        /// 获取Subshader数量
        /// </summary>
        /// <param name="shader"></param>
        /// <returns></returns>
        public int GetShaderSubshaderCount(Shader shader)
        {
            if (shader == null) return -1;

            MethodInfo mInfo = GetMethodInfo<ShaderUtil>(m_GetShaderSubshaderCountName);
            if (mInfo == null) return -1;
            int count = (int)mInfo.Invoke(null, new object[] { shader });

            return count;
        }

        public void GetShaderVariantEntriesFiltered(Shader shader, string[] filterKeywords, ShaderVariantCollection excludeCollection, out int[] passTypes, out string[] keywordLists, out string[] remainingKeywords)
        {
            passTypes = null;
            keywordLists = null;
            remainingKeywords = null;
            MethodInfo mInfo = GetMethodInfo<ShaderUtil>(m_GetShaderVariantEntriesFilteredMethodName);
            object[] paramsArray = new object[] { shader, 100000, filterKeywords, excludeCollection, passTypes, keywordLists, remainingKeywords };
            mInfo.Invoke(null, paramsArray);

            passTypes = (int[])paramsArray[4];
            keywordLists = (string[])paramsArray[5];
            remainingKeywords = (string[])paramsArray[6];
        }

        public List<PassType> GetActiveSubshaderPassTypeList(Shader shader)
        {
            string[] passTypeNames = System.Enum.GetNames(typeof(PassType));
            int[] passTypeValues = System.Enum.GetValues(typeof(PassType)) as int[];
            Dictionary<string, PassType> mapping = new Dictionary<string, PassType>();
            for (int i = 0; i < passTypeNames.Length; i++)
            {
                string passTypeNameUpper = passTypeNames[i].ToUpper();
                string key = passTypeNameUpper == "NORMAL" ? "" : passTypeNameUpper;

                mapping[key] = (PassType)passTypeValues[i];
            }

            List<PassType> passTypeList = new List<PassType>();
            for (int i = 0; i < shader.passCount; i++)
            {
                string shaderTag = shader.FindPassTagValue(i, new ShaderTagId("LightMode")).name;
                if (mapping.ContainsKey(shaderTag))
                {
                    PassType pType = mapping[shaderTag];
                    passTypeList.Add(pType);
                    Debug.LogWarning(pType);
                }
            }

            return passTypeList;
        }

        public bool isShaderClone(Shader shader)
        {
            bool ret = false;
            foreach (var shaderCloneInfo in m_ShaderCloneInfoArray)
            {
                if (shaderCloneInfo.m_ShaderClone == shader)
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        //public bool ShaderPathIsValid(string path)
        //{
        //    if (m_ShaderValidPathArray == null || m_ShaderValidPathArray.Length == 0) return true;

        //    foreach (var p in m_ShaderValidPathArray)
        //    {
        //        if (path.StartsWith(p))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        #endregion

        #region Private Methods

        MethodInfo GetMethodInfo<T>(string methodName)
        {
            MethodInfo mInfo = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            return mInfo;
        }

        #endregion

        #region Demo Test

        [System.Serializable]
        public class PassTypeKW
        {
            public bool m_Enabled = true;
            public PassType m_PassType;
            public List<string> m_Keywords;
        }

        [System.Serializable]
        public class BuiltinShaderVariantKW
        {
            public Shader m_Shader;
            public List<PassTypeKW> m_PassTypeKWMapping;
        }

        //public List<BuiltinShaderVariantKW> BUILTIN_KW = new List<BuiltinShaderVariantKW>() { };

        //public Dictionary<Shader, BuiltinShaderVariantKW> GetShaderBuiltinConfig()
        //{
        //    Dictionary<Shader, BuiltinShaderVariantKW> mapping = new Dictionary<Shader, BuiltinShaderVariantKW>();

        //    foreach (var config in BUILTIN_KW)
        //    {
        //        mapping[config.m_Shader] = config;
        //    }

        //    return mapping;
        //}


        #endregion
    }

    //[CustomEditor(typeof(ShaderVariantCollectionToolSetting))]
    //public class ShaderVariantCollectionToolSettingInspector : Editor
    //{
    //    //private SerializedProperty m_ShaderVariantCollection;
    //    private SerializedProperty m_MatPathArray;
    //    private SerializedProperty m_DynamicKWArray;
    //    private SerializedProperty m_SceneArray;
    //    private SerializedProperty m_SplitSVCArray;
    //    private SerializedProperty m_ShaderCloneArray;

    //    private ShaderVariantCollectionToolSetting m_Setting;

    //    void OnEnable()
    //    {
    //        m_MatPathArray = serializedObject.FindProperty("m_MatPathArray");
    //        m_DynamicKWArray = serializedObject.FindProperty("m_DynamicKWArray");
    //        m_SceneArray = serializedObject.FindProperty("m_SceneArray");
    //        m_SplitSVCArray = serializedObject.FindProperty("m_SplitSVCArray");
    //        m_ShaderCloneArray = serializedObject.FindProperty("m_ShaderCloneArray");

    //        m_Setting = target as ShaderVariantCollectionToolSetting;
    //    }

    //    public override void OnInspectorGUI()
    //    {
    //        base.OnInspectorGUI();

    //        serializedObject.Update();

    //        EditorGUILayout.IntField(1);
    //        EditorGUILayout.IntField(1);
    //        EditorGUILayout.IntField(1);
    //        EditorGUILayout.IntField(1);
    //        EditorGUILayout.PropertyField(m_MatPathArray);
    //        EditorGUILayout.PropertyField(m_DynamicKWArray);
    //        EditorGUILayout.PropertyField(m_SceneArray);
    //        EditorGUILayout.PropertyField(m_SplitSVCArray);

    //        serializedObject.ApplyModifiedProperties();
    //    }
    //}
}

#endif