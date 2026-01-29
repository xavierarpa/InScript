/*
Copyright (c) 2026 Xavier Arpa López Thomas Peter ('xavierarpa')

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// Editor window for debugging InScript scripts.
    /// Provides controls for executing blocks, setting variables, and testing selectors.
    /// </summary>
    internal class ScriptDebugWindow : EditorWindow
    {
        // Source info
        private string _sourceObjectName = "";
        private string _sourceFieldName = "";
        
        // Parsed elements
        private List<string> _blocks = new();
        private List<string> _localVariables = new();
        private List<string> _contextVariables = new();
        private List<string> _selectors = new();
        
        // Runtime values
        private Dictionary<string, float> _variableValues = new();
        
        // UI state
        private Vector2 _scrollPosition;
        private string _currentCode = "";
        private UnityEngine.Object _targetContext;
        private bool _stylesInitialized;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _sourceStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _buttonStyle;
        
        // Colors
        private static readonly Color HeaderBgColor = new(0.15f, 0.15f, 0.18f);
        private static readonly Color SourceBgColor = new(0.12f, 0.12f, 0.14f);
        private static readonly Color SectionBgColor = new(0.22f, 0.22f, 0.25f);
        private static readonly Color AccentColor = new(0.3f, 0.6f, 0.4f);
        private static readonly Color BlockColor = new(0.6f, 0.4f, 0.8f);
        private static readonly Color VarColor = new(0.4f, 0.7f, 0.9f);
        private static readonly Color SelectorColor = new(0.9f, 0.7f, 0.3f);
        
        // Regex patterns
        private static readonly Regex BlockPattern = new(@"^\s*@(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex LocalVarPattern = new(@"\$(\w+)\s*=", RegexOptions.Compiled);
        private static readonly Regex ContextVarPattern = new(@"(?<![\$#\w])([a-zA-Z_]\w*)\s*=(?!=)", RegexOptions.Compiled);
        private static readonly Regex SelectorPattern = new(@"#(\w+)", RegexOptions.Compiled);
        
        [MenuItem("Window/InScript/Debug Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptDebugWindow>("InScript Debug");
            window.minSize = new Vector2(350, 300);
            window.Show();
        }
        
        public static void ShowWindow(string code, UnityEngine.Object context, string fieldName)
        {
            var window = GetWindow<ScriptDebugWindow>("InScript Debug");
            window.minSize = new Vector2(350, 300);
            window._currentCode = code;
            window._targetContext = context;
            window._sourceObjectName = context != null ? context.name : "";
            window._sourceFieldName = fieldName;
            window._variableValues.Clear();
            window.ParseScript(code);
            window.Show();
            window.Repaint();
        }
        
        private void InitStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(0, 0, 6, 6)
            };
            _headerStyle.normal.textColor = Color.white;
            
            _sourceStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(0, 0, 2, 2)
            };
            
            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                richText = true,
                padding = new RectOffset(8, 0, 4, 4)
            };
            
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                richText = true,
                fontSize = 11,
                padding = new RectOffset(8, 8, 4, 4)
            };
            
            _stylesInitialized = true;
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            DrawHeader();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (string.IsNullOrEmpty(_currentCode))
            {
                EditorGUILayout.HelpBox("No script loaded.\n\nClick the 🐛 Debug button on any Script field in the Inspector.", MessageType.Info);
            }
            else
            {
                DrawBlocksSection();
                DrawVariablesSection();
                DrawSelectorsSection();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            // Main header
            Rect headerRect = GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, HeaderBgColor);
            
            Rect accentRect = new(headerRect.x, headerRect.yMax - 2, headerRect.width, 2);
            EditorGUI.DrawRect(accentRect, AccentColor);
            
            GUI.Label(headerRect, $"<color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>🐛</color> Script Debug Panel", _headerStyle);
            
            // Source info bar
            if (!string.IsNullOrEmpty(_sourceObjectName) || !string.IsNullOrEmpty(_sourceFieldName))
            {
                Rect sourceRect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(sourceRect, SourceBgColor);
                
                string sourceText = "";
                if (!string.IsNullOrEmpty(_sourceObjectName))
                {
                    sourceText = $"<b>{_sourceObjectName}</b>";
                }
                if (!string.IsNullOrEmpty(_sourceFieldName))
                {
                    if (!string.IsNullOrEmpty(sourceText))
                    {
                        sourceText += " → ";
                    }
                    sourceText += $"<color=#888>{_sourceFieldName}</color>";
                }
                
                GUI.Label(sourceRect, sourceText, _sourceStyle);
            }
            
            // Context selector
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Context:", GUILayout.Width(50));
            
            EditorGUI.BeginChangeCheck();
            _targetContext = EditorGUILayout.ObjectField(_targetContext, typeof(UnityEngine.Object), true);
            if (EditorGUI.EndChangeCheck() && _targetContext != null)
            {
                _sourceObjectName = _targetContext.name;
            }
            
            if (GUILayout.Button("↻", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                TryReloadFromContext();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void TryReloadFromContext()
        {
            if (_targetContext == null)
            {
                return;
            }
            
            // Try to find Script field on context
            if (_targetContext is MonoBehaviour mono)
            {
                var serializedObject = new SerializedObject(mono);
                var iterator = serializedObject.GetIterator();
                
                while (iterator.NextVisible(true))
                {
                    // Match by field name if we have one
                    if (!string.IsNullOrEmpty(_sourceFieldName) && iterator.displayName == _sourceFieldName)
                    {
                        var codeProp = iterator.FindPropertyRelative("code");
                        if (codeProp != null)
                        {
                            _currentCode = codeProp.stringValue;
                            ParseScript(_currentCode);
                            return;
                        }
                    }
                    // Otherwise find first Script type
                    else if (string.IsNullOrEmpty(_sourceFieldName) && iterator.type == "Script")
                    {
                        var codeProp = iterator.FindPropertyRelative("code");
                        if (codeProp != null)
                        {
                            _currentCode = codeProp.stringValue;
                            _sourceFieldName = iterator.displayName;
                            ParseScript(_currentCode);
                            return;
                        }
                    }
                }
            }
            else if (_targetContext is ScriptAsset asset)
            {
                _currentCode = asset.Code;
                _sourceFieldName = "ScriptAsset";
                ParseScript(_currentCode);
            }
        }
        
        private void DrawBlocksSection()
        {
            if (_blocks.Count == 0)
            {
                return;
            }
            
            DrawSectionHeader("📦", "Blocks", BlockColor);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            foreach (var block in _blocks)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(BlockColor)}>@{block}</color>", _sectionStyle, GUILayout.Width(150));
                
                GUI.enabled = _targetContext != null;
                if (GUILayout.Button("▶ Execute", _buttonStyle, GUILayout.Width(80)))
                {
                    ExecuteBlock(block);
                }
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (_targetContext == null)
            {
                EditorGUILayout.HelpBox("Assign a context to execute blocks", MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawVariablesSection()
        {
            bool hasVars = _localVariables.Count > 0 || _contextVariables.Count > 0;
            if (!hasVars)
            {
                return;
            }
            
            DrawSectionHeader("📝", "Variables", VarColor);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Local variables
            if (_localVariables.Count > 0)
            {
                GUILayout.Label("<b>Local Variables</b> <color=#666>(used when executing)</color>", _sectionStyle);
                
                foreach (var varName in _localVariables)
                {
                    DrawLocalVariableField(varName);
                }
                
                GUILayout.Space(5);
            }
            
            // Context variables
            if (_contextVariables.Count > 0)
            {
                GUILayout.Label("<b>Context Variables</b> <color=#666>(from C# object)</color>", _sectionStyle);
                
                foreach (var varName in _contextVariables)
                {
                    DrawContextVariableField(varName);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawLocalVariableField(string varName)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(VarColor)}>$</color>{varName}", _sectionStyle, GUILayout.Width(120));
            
            string key = $"${varName}";
            if (!_variableValues.ContainsKey(key))
            {
                _variableValues[key] = 0f;
            }
            
            _variableValues[key] = EditorGUILayout.FloatField(_variableValues[key], GUILayout.Width(80));
            
            GUILayout.Label("(initial value)", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawContextVariableField(string varName)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(new Color(0.85f, 0.85f, 0.85f))}>{varName}</color>", _sectionStyle, GUILayout.Width(120));
            
            if (!_variableValues.ContainsKey(varName))
            {
                _variableValues[varName] = 0f;
            }
            
            _variableValues[varName] = EditorGUILayout.FloatField(_variableValues[varName], GUILayout.Width(80));
            
            GUI.enabled = _targetContext != null;
            if (GUILayout.Button("Set", EditorStyles.miniButton, GUILayout.Width(40)))
            {
                SetVariableOnContext(varName, _variableValues[varName]);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSelectorsSection()
        {
            if (_selectors.Count == 0)
            {
                return;
            }
            
            DrawSectionHeader("🎯", "Selectors", SelectorColor);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            foreach (var selector in _selectors)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUILayout.Label($"<color=#{ColorUtility.ToHtmlStringRGB(SelectorColor)}>#{selector}</color>", _sectionStyle, GUILayout.Width(120));
                
                GUI.enabled = _targetContext != null;
                if (GUILayout.Button("Inspect", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    InspectSelector(selector);
                }
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSectionHeader(string icon, string title, Color color)
        {
            GUILayout.Space(8);
            
            Rect sectionRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sectionRect, SectionBgColor);
            
            // Left accent
            Rect accentRect = new(sectionRect.x, sectionRect.y, 3, sectionRect.height);
            EditorGUI.DrawRect(accentRect, color);
            
            GUI.Label(sectionRect, $"  {icon}  <b>{title}</b>", _sectionStyle);
        }
        
        private void ParseScript(string code)
        {
            _blocks.Clear();
            _localVariables.Clear();
            _contextVariables.Clear();
            _selectors.Clear();
            
            if (string.IsNullOrEmpty(code))
            {
                return;
            }
            
            // Find blocks
            var blockMatches = BlockPattern.Matches(code);
            foreach (Match match in blockMatches)
            {
                string blockName = match.Groups[1].Value;
                if (!_blocks.Contains(blockName))
                {
                    _blocks.Add(blockName);
                }
            }
            
            // Find local variables ($var)
            var localMatches = LocalVarPattern.Matches(code);
            foreach (Match match in localMatches)
            {
                string varName = match.Groups[1].Value;
                if (!_localVariables.Contains(varName))
                {
                    _localVariables.Add(varName);
                }
            }
            
            // Find context variables (identifier =)
            var contextMatches = ContextVarPattern.Matches(code);
            foreach (Match match in contextMatches)
            {
                string varName = match.Groups[1].Value;
                // Exclude keywords and already found locals
                if (!IsKeyword(varName) && !_localVariables.Contains(varName) && !_contextVariables.Contains(varName))
                {
                    _contextVariables.Add(varName);
                }
            }
            
            // Find selectors (#Name)
            var selectorMatches = SelectorPattern.Matches(code);
            foreach (Match match in selectorMatches)
            {
                string selectorName = match.Groups[1].Value;
                if (!_selectors.Contains(selectorName))
                {
                    _selectors.Add(selectorName);
                }
            }
            
            Repaint();
        }
        
        private bool IsKeyword(string word)
        {
            return word switch
            {
                "if" or "else" or "while" or "for" or "true" or "false" or
                "min" or "max" or "clamp" or "abs" or "floor" or "ceil" or "round" or
                "sqrt" or "pow" or "lerp" or "random" or "sign" => true,
                _ => false
            };
        }
        
        private void ExecuteBlock(string blockName)
        {
            if (_targetContext == null)
            {
                Debug.LogWarning("[InScript] No context assigned");
                return;
            }
            
            try
            {
                // Build initial locals from the debug panel values
                var initialLocals = new Dictionary<string, float>();
                foreach (var varName in _localVariables)
                {
                    string key = $"${varName}";
                    if (_variableValues.TryGetValue(key, out float value))
                    {
                        initialLocals[varName] = value;
                    }
                }
                
                var script = new Script(_currentCode);
                var context = ReflectionContext.From(_targetContext);
                script.ExecuteBlock(blockName, context, initialLocals);
                Debug.Log($"[InScript] ✓ Executed @{blockName} on {_targetContext.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[InScript] ✗ Error executing @{blockName}: {e.Message}");
            }
        }
        
        private void SetVariableOnContext(string varName, float value)
        {
            if (_targetContext == null)
            {
                return;
            }
            
            try
            {
                var context = ReflectionContext.From(_targetContext);
                if (context.TrySetValue(varName, value))
                {
                    Debug.Log($"[InScript] ✓ Set {varName} = {value} on {_targetContext.name}");
                    
                    // Mark dirty for undo/save
                    EditorUtility.SetDirty(_targetContext);
                }
                else
                {
                    Debug.LogWarning($"[InScript] Could not set {varName} - not found or read-only");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[InScript] ✗ Error setting {varName}: {e.Message}");
            }
        }
        
        private void InspectSelector(string selectorName)
        {
            if (_targetContext == null)
            {
                return;
            }
            
            try
            {
                var context = ReflectionContext.From(_targetContext);
                var selector = context.GetSelector(selectorName);
                
                if (selector != null)
                {
                    Debug.Log($"[InScript] Selector #{selectorName} is valid");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[InScript] ✗ Error inspecting #{selectorName}: {e.Message}");
            }
        }
    }
}
