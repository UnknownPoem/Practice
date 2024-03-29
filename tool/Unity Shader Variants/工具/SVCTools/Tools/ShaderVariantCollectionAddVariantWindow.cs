#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSVC
{
    public class ShaderVariantCollectionAddVariantWindow : EditorWindow
    {
        private static ShaderVariantCollectionAddVariantWindow m_window;
        public static ShaderVariantCollectionAddVariantWindow Window
        {
            get
            {
                if (m_window == null)
                {
                    m_window = EditorWindow.GetWindow<ShaderVariantCollectionAddVariantWindow>("AddVariantWindow");
                    m_window.minSize = new Vector2(480, 320);
                }
                return m_window;
            }
        }

        private static readonly int _maxEntries = 4000;

        private Shader _shader;
        private ShaderVariantCollectionMapper _mapper;

        private static MethodInfo _getShaderVariantEntriesFilteredMethod = null;

        private List<string> _selectedKeywords = new List<string>();
        private List<string> _availableKeywords = new List<string>();
        private List<string> _displayStrings = new List<string>();
        private Vector2 _scrollViewPos = new Vector2(0.0f, 0.0f);

        private static void InitGetKeywordMethod()
        {
            if (_getShaderVariantEntriesFilteredMethod == null)
            {
                _getShaderVariantEntriesFilteredMethod = typeof(ShaderUtil).GetMethod("GetShaderVariantEntriesFiltered",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }
        }

        public void Setup(Shader shader, ShaderVariantCollectionMapper mapper)
        {
            _shader = shader;
            _mapper = mapper;

            InitGetKeywordMethod();

            _selectedKeywords.Clear();
            _availableKeywords.Clear();
            _displayStrings.Clear();

            ApplyKeywordFilter();
        }

        public void ApplyKeywordFilter()
        {
            string[] keywordLists = null;
            string[] remainingKeywords = null;
            PassType[] filteredPassTypes = null;
            object[] paramsArray = new object[] { _shader, _maxEntries, _selectedKeywords.ToArray(),
                _mapper.mCollection, filteredPassTypes, keywordLists, remainingKeywords };
            _getShaderVariantEntriesFilteredMethod.Invoke(null, paramsArray);
            filteredPassTypes = (PassType[])paramsArray[4];
            keywordLists = (string[])paramsArray[5];
            remainingKeywords = (string[])paramsArray[6];

            int passTypeCount = filteredPassTypes.Length;
            string[][] filteredKeywords = new string[passTypeCount][];
            for (var i = 0; i < passTypeCount; ++i)
            {
                filteredKeywords[i] = keywordLists[i].Split(' ');
            }

            _displayStrings.Clear();
            for (var i = 0; i < Mathf.Min(filteredPassTypes.Length, keywordLists.Length); ++i)
            {
                var passType = filteredPassTypes[i];
                var keywordString = string.IsNullOrEmpty(filteredKeywords[i][0]) ? "<no_keywords>" : string.Join(" ", filteredKeywords[i]);
                var tempVariant = new ShaderVariantCollection.ShaderVariant(_shader, passType, filteredKeywords[i]);
                if (!_mapper.HasVariant(tempVariant))
                {
                    _displayStrings.Add(passType.ToString() + " " + keywordString);
                }
            }

            _displayStrings.Sort();
            _availableKeywords.Clear();
            _availableKeywords.InsertRange(0, remainingKeywords);
            _availableKeywords.Sort();
        }

        void DrawKeywordsList(List<string> keywords, bool clickingAddsToSelected)
        {
            var displayKeywords = keywords.Select(k => k.ToLowerInvariant()).ToList();
            EditorGUILayout.BeginHorizontal();

            for (var i = 0; i < displayKeywords.Count; ++i)
            {
                if (i != 0 && i % 4 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                if (GUILayout.Button(displayKeywords[i]))
                {
                    if (clickingAddsToSelected)
                    {
                        if (!_selectedKeywords.Contains(keywords[i]))
                        {
                            _selectedKeywords.Add(keywords[i]);
                            _selectedKeywords.Sort();
                            _availableKeywords.Remove(keywords[i]);
                        }
                    }
                    else
                    {
                        _availableKeywords.Add(keywords[i]);
                        _selectedKeywords.Remove(keywords[i]);
                    }
                    ApplyKeywordFilter();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"当前Shader: {_shader}");
            if (GUILayout.Button("Refresh"))
            {
                ApplyKeywordFilter();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            #region 可添加keyword
            EditorGUILayout.LabelField($"Pick shader keywords to narrow down variant list:");
            DrawKeywordsList(_availableKeywords, true);
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region 当前选择的keyword
            EditorGUILayout.LabelField($"Selected keywords:");
            DrawKeywordsList(_selectedKeywords, false);
            #endregion

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            #region 过滤后的组合
            EditorGUILayout.LabelField($"Shader variants with these keywords (click to add):({_displayStrings.Count})");
            _scrollViewPos = GUILayout.BeginScrollView(_scrollViewPos);
            foreach (var displayString in _displayStrings)
            {
                if (GUILayout.Button(displayString))
                {
                    var tempStrs = displayString.Split(' ');

                    PassType passType = (PassType)Enum.Parse(typeof(PassType), tempStrs[0]);
                    ShaderVariantCollection.ShaderVariant addedVariant;
                    if (tempStrs[1] == "<no_keywords>")
                    {
                        addedVariant = new ShaderVariantCollection.ShaderVariant(_shader, passType);
                    }
                    else
                    {
                        string[] variants = new string[tempStrs.Length - 1];
                        Array.Copy(tempStrs, 1, variants, 0, tempStrs.Length - 1);
                        addedVariant = new ShaderVariantCollection.ShaderVariant(_shader, passType, variants);
                    }

                    if (!_mapper.AddVariant(addedVariant))
                    {
                        Debug.LogError($"变体 {displayString} 添加失败");
                    }
                    else
                    {
                        ShaderVariantCollectionToolsWindow.Window.RefreshPassKeywordMap(_shader);
                        ShaderVariantCollectionToolsWindow.Window.Repaint();
                    }
                    ApplyKeywordFilter();
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.EndScrollView();
            #endregion

            EditorGUILayout.EndVertical();
        }
    }
}
#endif