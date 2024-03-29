#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CustomSVC
{

    public class ShaderVariantCollectionToolsWindow : EditorWindow
    {
        private enum FeatureViewState
        {
            CollectionTool,
            ShaderVariantIndex,
        }

        private static Vector2 cMinWindowSize = new Vector2(600, 300);
        private static ShaderVariantCollectionToolsWindow mwindow;
        public static ShaderVariantCollectionToolsWindow Window
        {
            get
            {
                if (mwindow == null)
                {
                    mwindow = EditorWindow.GetWindow<ShaderVariantCollectionToolsWindow>("ShaderVariantCollectionTools");
                    mwindow.minSize = cMinWindowSize;
                }
                return mwindow;
            }
        }

        private ShaderVariantCollection mCollectionFile;
        [SerializeField]
        private ShaderVariantCollectionMapper mCollectionMapper;

        public ShaderVariantCollectionMapper collectionMapper
        {
            get
            {
                if (mCollectionMapper == null || mCollectionMapper.mCollection != mCollectionFile)
                {
                    mCollectionMapper = new ShaderVariantCollectionMapper(mCollectionFile);
                    if (mShaderViewSelectedShader != null)
                        CollectPassKeywordMap(collectionMapper.GetShaderVariants(mShaderViewSelectedShader));
                }

                return mCollectionMapper;
            }
        }

        private ShaderVariantCollectionToolSetting mSetting;

        private FeatureViewState mCurrentFeatureState = FeatureViewState.CollectionTool;
        private Vector2 mFeatureViewScrollViewPos = Vector2.zero;
        private Vector2 mWorkViewScrollViewPos = Vector2.zero;

        #region ShaderVariantIndex

        private Shader mWillInsertShader;
        [SerializeField]
        private Shader mShaderViewSelectedShader;
        private string mFilterShaderName = "";
        [SerializeField]
        private List<Shader> mFilterShaders = new List<Shader>();

        [Serializable]
        private class CachePassData
        {
            public PassType passType;
            public List<SerializableShaderVariant> variants;
            public bool toggleValue;
        }

        [SerializeField]
        private List<CachePassData> mPassVariantCacheData = new List<CachePassData>();

        private void ResetShaderView()
        {
            mShaderViewSelectedShader = null;
            mFeatureViewScrollViewPos = Vector2.zero;
            mWorkViewScrollViewPos = Vector2.zero;
            mPassVariantCacheData.Clear();
        }

        private void CollectPassKeywordMap(IEnumerable<UnityEngine.ShaderVariantCollection.ShaderVariant> variants)
        {
            mPassVariantCacheData.Clear();

            foreach (var variant in variants)
            {
                int findRes = mPassVariantCacheData.FindIndex(data => data.passType == variant.passType);
                CachePassData pass;
                if (findRes < 0)
                {
                    pass = new CachePassData()
                    {
                        passType = variant.passType,
                        variants = new List<SerializableShaderVariant>(),
                        toggleValue = false
                    };
                    mPassVariantCacheData.Add(pass);
                }
                else
                {
                    pass = mPassVariantCacheData[findRes];
                }

                pass.variants.Add(new SerializableShaderVariant(variant));
            }
        }

        //这个方法不会导致Pass重新折叠
        public void RefreshPassKeywordMap(Shader currentShader)
        {
            //如果当前Shader已经变了，则不需要操作
            if (currentShader != mShaderViewSelectedShader)
                return;

            //否则刷新数据
            Dictionary<PassType, bool> toggleData = new Dictionary<PassType, bool>();
            foreach (CachePassData data in mPassVariantCacheData)
            {
                toggleData.Add(data.passType, data.toggleValue);
            }

            CollectPassKeywordMap(collectionMapper.GetShaderVariants(currentShader));

            foreach (CachePassData data in mPassVariantCacheData)
            {
                if (toggleData.TryGetValue(data.passType, out bool toggleValue))
                    data.toggleValue = toggleValue;
            }
        }
        #endregion

        #region CollectionTool

        private Vector2 mCollectionToolPos = new Vector2(0.0f, 0.0f);

        #endregion

        private GUIStyle mBlackStyle, mItemStyle;

        private static int cBorderWidth = 5;
        private static int cLeftWidth = 350;
        private static int cLeftTopHeight = 100;
        private static int cLeftMiddleHeight = 100;
        private static int cMiddleWidth = 10;

        [MenuItem("CustomSVC/OpenWindow")]
        public static void OpenWindow()
        {
            Window.Show();
        }

        public void OnGUI()
        {
            EditorGUILayout.Space(cBorderWidth);
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            #region 左半部分
            EditorGUILayout.BeginVertical(GUILayout.Width(cLeftWidth));

            #region 左上部分 收集文件选择 配置文件选择
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinHeight(cLeftTopHeight));

            EditorGUILayout.LabelField("变体收集文件：");

            Color oriColor = GUI.color;
            if (mCollectionFile == null)
                GUI.color = Color.red;
            ShaderVariantCollection newCollectionFile = EditorGUILayout.ObjectField(mCollectionFile, typeof(ShaderVariantCollection), false) as ShaderVariantCollection;
            GUI.color = oriColor;

            if (newCollectionFile != mCollectionFile)
            {
                SaveObject(mCollectionFile);
                mCollectionFile = newCollectionFile;
                mSetting.m_ShaderVariantCollection = newCollectionFile;

                ResetShaderView();
            }
            if (mCollectionFile != mSetting.m_ShaderVariantCollection)
            {
                SaveObject(mCollectionFile);
                mCollectionFile = mSetting.m_ShaderVariantCollection;
                ResetShaderView();
            }

            EditorGUILayout.LabelField("工具配置文件：");

            var newSetting = EditorGUILayout.ObjectField(mSetting, typeof(ShaderVariantCollectionToolSetting), false) as ShaderVariantCollectionToolSetting;
            if (newSetting != mSetting)
            {
                SaveObject(mSetting);
                mSetting = newSetting;

            }

            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.Space(cBorderWidth);

            #region 左中部分 功能选择
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinHeight(cLeftMiddleHeight));
            EditorGUILayout.LabelField("功能选择");
            if (mCollectionFile != null)
            {
                if (GUILayout.Button(new GUIContent("快速浏览", "快速浏览变体收集文件内容"), GUILayout.ExpandWidth(true)))
                {
                    mCurrentFeatureState = FeatureViewState.ShaderVariantIndex;
                    collectionMapper.Refresh();
                }

                if (GUILayout.Button(new GUIContent("项目收集工具", "自动收集项目打包所需变体"), GUILayout.ExpandWidth(true)))
                {
                    mCurrentFeatureState = FeatureViewState.CollectionTool;
                    mFeatureViewScrollViewPos = Vector2.zero;
                    mWorkViewScrollViewPos = Vector2.zero;
                }
            }
            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.Space(cBorderWidth);

            #region 左下部分 次级选项
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinHeight(position.height - cLeftTopHeight - cLeftMiddleHeight - 4 * cBorderWidth));

            if (mCollectionFile != null)
            {
                if (mCurrentFeatureState == FeatureViewState.ShaderVariantIndex)
                {
                    EditorGUILayout.LabelField("Shader View");

                    EditorGUILayout.BeginHorizontal();
                    mWillInsertShader = EditorGUILayout.ObjectField(mWillInsertShader, typeof(Shader)) as Shader;
                    if (GUILayout.Button("添加"))
                    {
                        if (!collectionMapper.HasShader(mWillInsertShader))
                        {
                            UndoShaderVariantCollectionTool();
                            collectionMapper.AddShader(mWillInsertShader);
                            //添加Shader后，更新Filter列表
                            if (mFilterShaderName != "" &&
                                mWillInsertShader.name.IndexOf(mFilterShaderName, StringComparison.OrdinalIgnoreCase) >= 0 &&
                                !mFilterShaders.Contains(mWillInsertShader))
                            {
                                mFilterShaders.Add(mWillInsertShader);
                            }
                        }
                        else
                            ShowNotification(new GUIContent($"Shader:{mWillInsertShader}已存在于当前变体收集文件"));
                    }
                    EditorGUILayout.EndHorizontal();

                    #region 过滤名称
                    string prevFilterShaderName = mFilterShaderName;
                    mFilterShaderName = EditorGUILayout.TextField("过滤", mFilterShaderName);
                    if (mFilterShaderName == "")
                    {
                        mFilterShaders.Clear();
                    }
                    else if (prevFilterShaderName != mFilterShaderName)
                    {
                        FilterShader();
                    }
                    #endregion

                    if (collectionMapper.shaders.Count > 0 && GUILayout.Button(new GUIContent("Clear", "清空变体收集文件"), GUILayout.Width(cLeftWidth)))
                    {
                        if (EditorUtility.DisplayDialog("确认", "是否确认清空文件", "是", "否"))
                        {
                            ClearSVC();
                        }
                    }

                    //分割线
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                    mFeatureViewScrollViewPos = EditorGUILayout.BeginScrollView(mFeatureViewScrollViewPos);

                    IEnumerable<Shader> displayList =
                        (mFilterShaderName == "" ? (collectionMapper.shaders as IEnumerable<Shader>) : mFilterShaders);

                    Shader removeShader = null;

                    Color oriGUIColor = GUI.color;
                    foreach (var shader in displayList)
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(cLeftWidth));

                        if (shader == mShaderViewSelectedShader)
                            GUI.color = Color.green;

                        if (GUILayout.Button(new GUIContent(shader.name, shader.name),
                                GUILayout.Width(cLeftWidth - 30)))
                        {
                            if (mShaderViewSelectedShader == shader)//选中状态下再次选择,在项目中定位
                            {
                                Selection.activeObject = shader;
                                EditorGUIUtility.PingObject(shader);
                            }

                            mShaderViewSelectedShader = shader;
                            CollectPassKeywordMap(collectionMapper.GetShaderVariants(shader));
                        }
                        GUI.color = oriGUIColor;

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            removeShader = shader;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (removeShader != null)
                    {
                        UndoShaderVariantCollectionTool();
                        collectionMapper.RemoveShader(removeShader);
                        mFilterShaders.Remove(removeShader);

                        if (removeShader == mShaderViewSelectedShader)
                        {
                            mShaderViewSelectedShader = null;
                            mPassVariantCacheData.Clear();
                        }

                    }

                    EditorGUILayout.EndScrollView();
                }

                if (mCurrentFeatureState == FeatureViewState.CollectionTool)
                {
                    EditorGUILayout.LabelField("Collection View");

                    if (GUILayout.Button(new GUIContent("一键收集变体", "一键收集变体"), GUILayout.ExpandWidth(true)))
                    {
                        ClearSVC();
                        ShaderParseData.DoCollection(mSetting);
                        ResetShaderView();
                    }
                }
            }

            EditorGUILayout.EndVertical();
            #endregion

            EditorGUILayout.EndVertical();
            #endregion

            #region 中间分隔线
            EditorGUILayout.BeginVertical(GUILayout.Width(cMiddleWidth));
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
            #endregion

            #region 右半部分
            int rightWidth = (int)(position.width - cLeftWidth - cMiddleWidth - 10);
            EditorGUILayout.BeginVertical(mBlackStyle, GUILayout.MinWidth(rightWidth), GUILayout.MinHeight(position.height - cBorderWidth * 2));

            if (mCollectionFile != null)
            {
                #region 变体浏览
                if (mCurrentFeatureState == FeatureViewState.ShaderVariantIndex && mShaderViewSelectedShader != null)
                {
                    if (GUILayout.Button("+"))
                    {
                        OpenAddVariantWindow();
                    }

                    //if (mPassKeywordsMap.Count == 0)
                    if (mPassVariantCacheData.Count == 0)
                    {
                        EditorGUILayout.LabelField("当前Shader没有变体被收集");
                    }

                    //bool modify = false;
                    //PassType modifyKey = PassType.Normal;
                    //(List<string[]> list, bool toggle) modifyValue = (null, false);

                    int minusWidth = 20;

                    bool removeVariant = false;
                    // PassType removePassType = PassType.Normal;
                    // string[] removeKeywords = null;
                    ShaderVariantCollection.ShaderVariant removedVariant = default;

                    mWorkViewScrollViewPos = EditorGUILayout.BeginScrollView(mWorkViewScrollViewPos);
                    //foreach (KeyValuePair<PassType, (List<string[]> list, bool toggle)> pair in mPassKeywordsMap)
                    foreach (CachePassData cacheData in mPassVariantCacheData)
                    {
                        //var passType = pair.Key;
                        //var keywordsListTuple = pair.Value;

                        cacheData.toggleValue = EditorGUILayout.Foldout(cacheData.toggleValue, $"{cacheData.passType.ToString()}({cacheData.variants.Count})");
                        // if (newToggle != keywordsListTuple.toggle)
                        // {
                        //     modify = true;
                        //     modifyKey = passType;
                        //     modifyValue = (keywordsListTuple.list, newToggle);
                        // }

                        if (cacheData.toggleValue)
                        {
                            foreach (SerializableShaderVariant variant in cacheData.variants)
                            {
                                EditorGUILayout.BeginHorizontal();
                                if (variant.keywords.Length == 0)
                                    EditorGUILayout.LabelField("<no keywords>");
                                else
                                    EditorGUILayout.LabelField(string.Join(", ", variant.keywords));

                                if (GUILayout.Button("-", GUILayout.Width(minusWidth)))
                                {
                                    removeVariant = true;
                                    removedVariant = variant.Deserialize();
                                    // removePassType = cacheData.passType;
                                    // removeKeywords = variant.keywords;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();

                    // if (modify)
                    //     mPassKeywordsMap[modifyKey] = modifyValue;

                    if (removeVariant)
                    {
                        UndoShaderVariantCollectionTool();
                        // collectionMapper.RemoveVariant(
                        //     new ShaderVariantCollection.ShaderVariant(mShaderViewSelectedShader, removePassType,
                        //         removeKeywords));
                        collectionMapper.RemoveVariant(removedVariant);

                        //CollectPassKeywordMap(collectionMapper.GetShaderVariants(mShaderViewSelectedShader));
                        RefreshPassKeywordMap(mShaderViewSelectedShader);
                    }

                }
                #endregion

                #region 项目变体收集工具
                if (mCurrentFeatureState == FeatureViewState.CollectionTool)
                {
                    mCollectionToolPos = GUILayout.BeginScrollView(mCollectionToolPos);
                    var editor = Editor.CreateEditor(mSetting);
                    editor.OnInspectorGUI();
                    GUILayout.EndScrollView();
                }
                #endregion
            }

            EditorGUILayout.EndVertical();
            #endregion

            GUILayout.EndHorizontal();
        }

        private Dictionary<Type, Type[]> mCachedImplements = new Dictionary<Type, Type[]>();

        private void OpenAddVariantWindow()
        {
            var window = ShaderVariantCollectionAddVariantWindow.Window;

            window.Setup(mShaderViewSelectedShader, collectionMapper);
            window.Show();
            ShowNotification(new GUIContent("已打开添加变体窗口，如未发现请检查窗口是否被当前窗口覆盖"));
        }

        private void FilterShader()
        {
            mFilterShaders.Clear();

            if (mFilterShaderName != "")
            {
                foreach (var shader in collectionMapper.shaders)
                {
                    if (shader.name.IndexOf(mFilterShaderName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        mFilterShaders.Add(shader);
                    }
                }
            }
        }

        internal void UndoShaderVariantCollectionTool()
        {
            collectionMapper.SetSerializeFlag(true);
            Undo.RecordObject(mCollectionFile, "Change SVC tool");
            Undo.RegisterCompleteObjectUndo(collectionMapper, "Change SVC tool");
            Undo.RegisterCompleteObjectUndo(this, "Change SVC tool");
            collectionMapper.SetSerializeFlag(false);
            //Undo.FlushUndoRecordObjects();
        }

        private void SaveObject(Object obj)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        private void SetupStyle()
        {
            if (mBlackStyle == null)
            {
                Color backColor = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.7f, 0.7f, 0.7f);
                Texture2D _blackTexture;
                _blackTexture = MakeTex(4, 4, backColor);
                _blackTexture.hideFlags = HideFlags.DontSave;
                mBlackStyle = new GUIStyle();
                mBlackStyle.normal.background = _blackTexture;
            }

            if (mItemStyle == null)
            {
                Color itemColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.9f, 0.9f, 0.9f);
                Texture2D _itemColorTexture;
                _itemColorTexture = MakeTex(4, 4, itemColor);
                _itemColorTexture.hideFlags = HideFlags.DontSave;
                mItemStyle = new GUIStyle();
                mItemStyle.normal.background = _itemColorTexture;
            }
        }

        private void ClearSVC()
        {
            UndoShaderVariantCollectionTool();
            mCollectionFile.Clear();
            collectionMapper.Refresh();
            mShaderViewSelectedShader = null;
            mPassVariantCacheData.Clear();
            mFilterShaders.Clear();
        }

        public void Awake()
        {
            SetupStyle();
            mSetting =ShaderVariantCollectionToolSetting.GetAllSettings()[0] ;
            mCollectionFile = mSetting.m_ShaderVariantCollection;
        }

        public void OnDisable()
        {
            ShaderVariantCollectionAddVariantWindow.Window.Close();
            SaveObject(mSetting);
        }
    }
}
#endif