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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// PropertyDrawer for Script class with syntax highlighting.
    /// </summary>
    [CustomPropertyDrawer(typeof(Script))]
    internal class ScriptDrawer : PropertyDrawer
    {
        private static readonly Regex CommentLinePattern = new(@"^\s*//", RegexOptions.Compiled);
        private static readonly Regex BlockHeaderPattern = new(@"^\s*@(\w+)(?:\s+([\d.]+))?\s*$", RegexOptions.Compiled);
        private static readonly Regex IfPattern = new(@"^\s*\?\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex ElseIfPattern = new(@"^\s*:\?\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex ElsePattern = new(@"^\s*:\s*$", RegexOptions.Compiled);
        private static readonly Regex EndPattern = new(@"^\s*;\s*$", RegexOptions.Compiled);
        private static readonly Regex LocalVarAssignPattern = new(@"^\s*\$(\w+)\s*=\s*(.+)$", RegexOptions.Compiled);
        private static readonly Regex LocalVarCompoundPattern = new(@"^\s*\$(\w+)\s*(\+=|-=|\*=|/=)\s*(.+)$", RegexOptions.Compiled);
        private static readonly Regex SelectorAccessPattern = new(@"^\s*#(\w+)\.(\w+)(?:\(([^)]*)\))?\s*$", RegexOptions.Compiled);
        private static readonly Regex MethodCallPattern = new(@"^\s*(\w+)\(([^)]*)\)\s*$", RegexOptions.Compiled);
        private static readonly Regex ContextAssignPattern = new(@"^\s*(\w+)\s*=\s*(.+)$", RegexOptions.Compiled);
        
        private static readonly Color ColorComment = new(0.5f, 0.5f, 0.5f);
        private static readonly Color ColorBlock = new(0.6f, 0.4f, 0.8f);
        private static readonly Color ColorKeyword = new(0.8f, 0.4f, 0.6f);
        private static readonly Color ColorLocalVar = new(0.4f, 0.7f, 0.9f);
        private static readonly Color ColorSelector = new(0.9f, 0.7f, 0.3f);
        private static readonly Color ColorMethod = new(0.6f, 0.9f, 0.6f);
        private static readonly Color ColorNumber = new(0.7f, 0.9f, 0.7f);
        private static readonly Color ColorString = new(0.9f, 0.6f, 0.5f);
        private static readonly Color ColorOperator = new(0.9f, 0.9f, 0.5f);
        private static readonly Color ColorIdentifier = new(0.85f, 0.85f, 0.85f);
        
        private GUIStyle _codeOverlayStyle;
        private GUIStyle _lineNumberStyle;
        private readonly Dictionary<string, string> _controlNames = new();
        
        private const float CodeLineHeight = 15f;
        private const float CodePadding = 8f;
        private const int MinLines = 1;
        private const float LineNumberWidth = 30f;
        private static readonly Color LineNumberColor = new(0.5f, 0.5f, 0.5f);
        private static readonly Color LineNumberBgColor = new(0.15f, 0.15f, 0.15f, 1f);
        
        private float GetCodeHeight(string code)
        {
            int lineCount = string.IsNullOrEmpty(code) ? MinLines : Mathf.Max(code.Split('\n').Length, MinLines);
            return CodeLineHeight * lineCount + CodePadding;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var codeProp = property.FindPropertyRelative("code");
            string code = codeProp?.stringValue ?? "";
            
            float codeHeight = GetCodeHeight(code);
            float headerHeight = EditorGUIUtility.singleLineHeight + 4;
            float errorsHeight = GetErrorsHeight(code);
            
            return headerHeight + codeHeight + errorsHeight + 10;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var codeProp = property.FindPropertyRelative("code");
            string controlName = GetControlName(property);
            
            Rect currentRect = position;
            currentRect.height = EditorGUIUtility.singleLineHeight;
            
            // Header: Label on left, buttons on right
            EditorGUI.LabelField(currentRect, label, EditorStyles.boldLabel);
            
            // Syntax button
            Rect syntaxButtonRect = currentRect;
            syntaxButtonRect.x = currentRect.xMax - 28;
            syntaxButtonRect.width = 28;
            if (GUI.Button(syntaxButtonRect, "❔", EditorStyles.miniButton))
            {
                SyntaxReferenceWindow.ShowWindow();
            }
            
            // Debug button
            Rect debugButtonRect = currentRect;
            debugButtonRect.x = currentRect.xMax - 58;
            debugButtonRect.width = 28;
            if (GUI.Button(debugButtonRect, "🔧", EditorStyles.miniButton))
            {
                var target = property.serializedObject.targetObject;
                ScriptDebugWindow.ShowWindow(codeProp.stringValue, target, property.displayName);
            }
            
            currentRect.y += EditorGUIUtility.singleLineHeight + 4;
            
            float codeHeight = GetCodeHeight(codeProp.stringValue);
            
            Rect codeRect = currentRect;
            codeRect.height = codeHeight;
            
            DrawCodeEditor(codeRect, codeProp, controlName);
            
            currentRect.y += codeHeight + 4;
            
            float errorsHeight = GetErrorsHeight(codeProp.stringValue);
            if (errorsHeight > 0)
            {
                Rect errorsRect = currentRect;
                errorsRect.height = errorsHeight;
                DrawErrors(errorsRect, codeProp.stringValue);
                currentRect.y += errorsHeight + 2;
            }
            
            EditorGUI.EndProperty();
        }
        
        private string GetControlName(SerializedProperty property)
        {
            string key = property.propertyPath + property.serializedObject.targetObject.GetInstanceID();
            if (!_controlNames.TryGetValue(key, out string name))
            {
                name = "ScriptCode_" + key.GetHashCode();
                _controlNames[key] = name;
            }
            return name;
        }
        
        private void DrawCodeEditor(Rect rect, SerializedProperty codeProp, string controlName)
        {
            if (_codeOverlayStyle == null)
            {
                _codeOverlayStyle = new GUIStyle(EditorStyles.textArea)
                {
                    fontSize = 12,
                    wordWrap = false,
                    richText = true,
                    padding = new RectOffset(6, 6, 4, 4),
                    alignment = TextAnchor.UpperLeft
                };
            }
            
            if (_lineNumberStyle == null)
            {
                _lineNumberStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.UpperRight,
                    padding = new RectOffset(4, 4, 4, 4),
                    richText = false
                };
                _lineNumberStyle.normal.textColor = LineNumberColor;
            }
            
            // Calculate widths - preview and editor share remaining space equally
            float remainingWidth = rect.width - LineNumberWidth;
            float halfWidth = remainingWidth * 0.5f;
            
            // 1. Line numbers column (left)
            Rect lineNumberRect = rect;
            lineNumberRect.width = LineNumberWidth;
            EditorGUI.DrawRect(lineNumberRect, LineNumberBgColor);
            DrawLineNumbers(lineNumberRect, codeProp.stringValue);
            
            // 2. Preview column (center) - always shows colored syntax
            Rect previewRect = rect;
            previewRect.x += LineNumberWidth;
            previewRect.width = halfWidth;
            EditorGUI.DrawRect(previewRect, new Color(0.18f, 0.18f, 0.18f, 1f));
            
            string coloredText = ColorizeScript(codeProp.stringValue);
            GUI.Label(previewRect, coloredText, _codeOverlayStyle);
            
            // 3. Editor column (right) - editable text area
            Rect editorRect = rect;
            editorRect.x += LineNumberWidth + halfWidth;
            editorRect.width = halfWidth;
            EditorGUI.DrawRect(editorRect, new Color(0.14f, 0.14f, 0.14f, 1f));
            
            var editStyle = new GUIStyle(EditorStyles.textArea)
            {
                fontSize = 12,
                wordWrap = false,
                richText = false,
                padding = new RectOffset(6, 6, 4, 4)
            };
            editStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            editStyle.focused.textColor = new Color(0.85f, 0.85f, 0.85f);
            editStyle.normal.background = Texture2D.blackTexture;
            editStyle.focused.background = Texture2D.blackTexture;
            
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(controlName);
            string newCode = EditorGUI.TextArea(editorRect, codeProp.stringValue, editStyle);
            if (EditorGUI.EndChangeCheck())
            {
                codeProp.stringValue = newCode;
            }
            
            EditorGUIUtility.AddCursorRect(editorRect, MouseCursor.Text);
        }
        
        private void DrawLineNumbers(Rect rect, string code)
        {
            int lineCount = string.IsNullOrEmpty(code) ? 1 : code.Split('\n').Length;
            
            var sb = new System.Text.StringBuilder();
            for (int i = 1; i <= lineCount; i++)
            {
                if (i > 1)
                {
                    sb.Append('\n');
                }
                sb.Append(i);
            }
            
            GUI.Label(rect, sb.ToString(), _lineNumberStyle);
        }
        
        private string ColorizeScript(string script)
        {
            if (string.IsNullOrEmpty(script))
            {
                return "";
            }
            
            // Normalize tabs to spaces for consistent rendering
            script = script.Replace("\t", "          ");
            
            var lines = script.Split('\n');
            var result = new System.Text.StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    result.Append('\n');
                }
                result.Append(ColorizeLine(lines[i]));
            }
            
            return result.ToString();
        }
        
        private string ColorizeLine(string line)
        {
            string trimmed = line.Trim();
            
            if (string.IsNullOrEmpty(trimmed))
            {
                return line;
            }
            
            if (CommentLinePattern.IsMatch(trimmed))
            {
                return Colorize(line, ColorComment);
            }
            
            var blockMatch = BlockHeaderPattern.Match(trimmed);
            if (blockMatch.Success)
            {
                string blockName = blockMatch.Groups[1].Value;
                string param = blockMatch.Groups[2].Value;
                string result = Colorize("@" + blockName, ColorBlock);
                if (!string.IsNullOrEmpty(param))
                {
                    result += " " + Colorize(param, ColorNumber);
                }
                return GetIndent(line) + result;
            }
            
            if (IfPattern.IsMatch(trimmed))
            {
                var match = IfPattern.Match(trimmed);
                return GetIndent(line) + Colorize("?", ColorKeyword) + " " + ColorizeExpression(match.Groups[1].Value);
            }
            
            var elseIfMatch = ElseIfPattern.Match(trimmed);
            if (elseIfMatch.Success)
            {
                return GetIndent(line) + Colorize(":?", ColorKeyword) + " " + ColorizeExpression(elseIfMatch.Groups[1].Value);
            }
            
            if (ElsePattern.IsMatch(trimmed))
            {
                return GetIndent(line) + Colorize(":", ColorKeyword);
            }
            
            if (EndPattern.IsMatch(trimmed))
            {
                return GetIndent(line) + Colorize(";", ColorKeyword);
            }
            
            var compoundMatch = LocalVarCompoundPattern.Match(trimmed);
            if (compoundMatch.Success)
            {
                return GetIndent(line) + 
                    Colorize("$" + compoundMatch.Groups[1].Value, ColorLocalVar) + " " +
                    Colorize(compoundMatch.Groups[2].Value, ColorOperator) + " " +
                    ColorizeExpression(compoundMatch.Groups[3].Value);
            }
            
            var localMatch = LocalVarAssignPattern.Match(trimmed);
            if (localMatch.Success)
            {
                return GetIndent(line) + 
                    Colorize("$" + localMatch.Groups[1].Value, ColorLocalVar) + " " +
                    Colorize("=", ColorOperator) + " " +
                    ColorizeExpression(localMatch.Groups[2].Value);
            }
            
            var selectorMatch = SelectorAccessPattern.Match(trimmed);
            if (selectorMatch.Success)
            {
                string result = GetIndent(line) + 
                    Colorize("#" + selectorMatch.Groups[1].Value, ColorSelector) + "." +
                    Colorize(selectorMatch.Groups[2].Value, ColorMethod);
                if (selectorMatch.Groups[3].Success)
                {
                    result += "(" + ColorizeExpression(selectorMatch.Groups[3].Value) + ")";
                }
                return result;
            }
            
            var methodMatch = MethodCallPattern.Match(trimmed);
            if (methodMatch.Success)
            {
                return GetIndent(line) + 
                    Colorize(methodMatch.Groups[1].Value, ColorMethod) + "(" +
                    ColorizeExpression(methodMatch.Groups[2].Value) + ")";
            }
            
            var contextMatch = ContextAssignPattern.Match(trimmed);
            if (contextMatch.Success)
            {
                return GetIndent(line) + 
                    Colorize(contextMatch.Groups[1].Value, ColorIdentifier) + " " +
                    Colorize("=", ColorOperator) + " " +
                    ColorizeExpression(contextMatch.Groups[2].Value);
            }
            
            return GetIndent(line) + ColorizeExpression(trimmed);
        }
        
        private string ColorizeExpression(string expr)
        {
            if (string.IsNullOrEmpty(expr))
            {
                return "";
            }
            
            var result = new System.Text.StringBuilder();
            var tokens = Tokenize(expr);
            
            foreach (var token in tokens)
            {
                if (token.StartsWith("$"))
                {
                    result.Append(Colorize(token, ColorLocalVar));
                }
                else if (token.StartsWith("#"))
                {
                    result.Append(Colorize(token, ColorSelector));
                }
                else if (token.StartsWith("\"") && token.EndsWith("\""))
                {
                    result.Append(Colorize(token, ColorString));
                }
                else if (float.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    result.Append(Colorize(token, ColorNumber));
                }
                else if (IsOperator(token))
                {
                    result.Append(Colorize(token, ColorOperator));
                }
                else if (IsBuiltinFunction(token))
                {
                    result.Append(Colorize(token, ColorMethod));
                }
                else
                {
                    result.Append(Colorize(token, ColorIdentifier));
                }
            }
            
            return result.ToString();
        }
        
        private List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inString = false;
            
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                
                if (c == '"')
                {
                    if (inString)
                    {
                        current.Append(c);
                        tokens.Add(current.ToString());
                        current.Clear();
                        inString = false;
                    }
                    else
                    {
                        if (current.Length > 0)
                        {
                            tokens.Add(current.ToString());
                            current.Clear();
                        }
                        current.Append(c);
                        inString = true;
                    }
                    continue;
                }
                
                if (inString)
                {
                    current.Append(c);
                    continue;
                }
                
                if (char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    tokens.Add(" ");
                    continue;
                }
                
                if (IsOperatorChar(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    
                    if (i + 1 < expr.Length)
                    {
                        string twoChar = c.ToString() + expr[i + 1];
                        if (twoChar is "<=" or ">=" or "==" or "!=" or "+=" or "-=" or "*=" or "/=")
                        {
                            tokens.Add(twoChar);
                            i++;
                            continue;
                        }
                    }
                    
                    tokens.Add(c.ToString());
                    continue;
                }
                
                current.Append(c);
            }
            
            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }
            
            return tokens;
        }
        
        private bool IsOperatorChar(char c)
        {
            return c is '+' or '-' or '*' or '/' or '=' or '<' or '>' or '!' or '(' or ')' or ',' or '.';
        }
        
        private bool IsOperator(string token)
        {
            return token is "+" or "-" or "*" or "/" or "=" or "<" or ">" or 
                "<=" or ">=" or "==" or "!=" or "+=" or "-=" or "*=" or "/=" or
                "(" or ")" or "," or ".";
        }
        
        private bool IsBuiltinFunction(string token)
        {
            return token.ToLower() is "min" or "max" or "clamp" or "abs" or 
                "floor" or "ceil" or "round" or "sqrt" or "random";
        }
        
        private string GetIndent(string line)
        {
            int indent = 0;
            foreach (char c in line)
            {
                if (c == ' ' || c == '\t')
                {
                    indent++;
                }
                else
                {
                    break;
                }
            }
            return new string(' ', indent);
        }
        
        private string Colorize(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{EscapeRichText(text)}</color>";
        }
        
        private string EscapeRichText(string text)
        {
            return text.Replace("<", "＜").Replace(">", "＞");
        }
        
        private float GetErrorsHeight(string code)
        {
            var errors = ValidateScript(code);
            if (errors.Count == 0)
            {
                return 0;
            }
            return EditorGUIUtility.singleLineHeight * errors.Count + 10;
        }
        
        private void DrawErrors(Rect rect, string code)
        {
            var errors = ValidateScript(code);
            if (errors.Count > 0)
            {
                EditorGUI.HelpBox(rect, string.Join("\n", errors), MessageType.Error);
            }
        }
        
        private List<string> ValidateScript(string script)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(script))
            {
                return errors;
            }
            
            var lines = script.Split('\n');
            int blockDepth = 0;
            int ifDepth = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int lineNum = i + 1;
                
                if (string.IsNullOrEmpty(line) || CommentLinePattern.IsMatch(line))
                {
                    continue;
                }
                
                if (BlockHeaderPattern.IsMatch(line))
                {
                    if (blockDepth > 0)
                    {
                        errors.Add($"Line {lineNum}: Nested blocks not allowed");
                    }
                    blockDepth++;
                    continue;
                }
                
                if (IfPattern.IsMatch(line))
                {
                    ifDepth++;
                    continue;
                }
                
                if ((ElseIfPattern.IsMatch(line) || ElsePattern.IsMatch(line)) && ifDepth == 0)
                {
                    errors.Add($"Line {lineNum}: ':' or ':?' without matching '?'");
                    continue;
                }
                
                if (EndPattern.IsMatch(line))
                {
                    if (ifDepth > 0)
                    {
                        ifDepth--;
                    }
                    else if (blockDepth > 0)
                    {
                        blockDepth--;
                    }
                    else
                    {
                        errors.Add($"Line {lineNum}: ';' without open block or conditional");
                    }
                    continue;
                }
            }
            
            if (ifDepth > 0)
            {
                errors.Add($"Missing {ifDepth} ';' to close conditionals");
            }
            
            if (blockDepth > 0)
            {
                errors.Add($"Missing {blockDepth} ';' to close blocks");
            }
            
            return errors;
        }
    }
}
