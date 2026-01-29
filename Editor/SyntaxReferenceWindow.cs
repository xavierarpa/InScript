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
using UnityEditor;
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// Editor window displaying InScript syntax reference.
    /// </summary>
    internal class SyntaxReferenceWindow : EditorWindow
    {
        // Syntax colors
        private static readonly Color ColorBlock = new(0.6f, 0.4f, 0.8f);
        private static readonly Color ColorKeyword = new(0.8f, 0.4f, 0.6f);
        private static readonly Color ColorLocalVar = new(0.4f, 0.7f, 0.9f);
        private static readonly Color ColorSelector = new(0.9f, 0.7f, 0.3f);
        private static readonly Color ColorMethod = new(0.6f, 0.9f, 0.6f);
        private static readonly Color ColorOperator = new(0.9f, 0.9f, 0.5f);
        private static readonly Color ColorIdentifier = new(0.85f, 0.85f, 0.85f);
        private static readonly Color ColorNumber = new(0.7f, 0.9f, 0.7f);
        private static readonly Color ColorString = new(0.9f, 0.6f, 0.5f);
        
        // UI colors
        private static readonly Color HeaderBgColor = new(0.15f, 0.15f, 0.18f);
        private static readonly Color SectionBgColor = new(0.22f, 0.22f, 0.25f);
        private static readonly Color SectionBgAltColor = new(0.18f, 0.18f, 0.21f);
        private static readonly Color AccentColor = new(0.5f, 0.3f, 0.7f);
        private static readonly Color DescriptionColor = new(0.65f, 0.65f, 0.65f);
        
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _syntaxStyle;
        private GUIStyle _descriptionStyle;
        private GUIStyle _exampleStyle;
        private bool _stylesInitialized;
        
        private string _searchFilter = "";
        private int _sectionIndex;
        
        [MenuItem("Window/InScript/Syntax Reference")]
        public static void ShowWindow()
        {
            var window = GetWindow<SyntaxReferenceWindow>("InScript Syntax");
            window.minSize = new Vector2(480, 550);
            window.Show();
        }
        
        private void InitStyles()
        {
            if (_stylesInitialized)
            {
                return;
            }
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                padding = new RectOffset(0, 0, 8, 8)
            };
            _headerStyle.normal.textColor = Color.white;
            
            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                richText = true,
                padding = new RectOffset(12, 0, 6, 4)
            };
            _sectionTitleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            
            _syntaxStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 12,
                padding = new RectOffset(20, 0, 2, 2),
                fontStyle = FontStyle.Normal
            };
            
            _descriptionStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 11,
                padding = new RectOffset(0, 10, 2, 2),
                wordWrap = true
            };
            _descriptionStyle.normal.textColor = DescriptionColor;
            
            _exampleStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 10,
                fontStyle = FontStyle.Italic,
                padding = new RectOffset(20, 0, 0, 4)
            };
            _exampleStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            
            _stylesInitialized = true;
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            // Header
            DrawHeader();
            
            // Search bar
            DrawSearchBar();
            
            // Content
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _sectionIndex = 0;
            
            DrawSectionBlocks();
            DrawSectionVariables();
            DrawSectionSelectors();
            DrawSectionConditionals();
            DrawSectionOperators();
            DrawSectionMathFunctions();
            DrawSectionRoundingFunctions();
            DrawSectionAdvancedFunctions();
            
            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
            
            // Footer
            DrawFooter();
        }
        
        private void DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, HeaderBgColor);
            
            // Accent line
            Rect accentRect = new(headerRect.x, headerRect.yMax - 3, headerRect.width, 3);
            EditorGUI.DrawRect(accentRect, AccentColor);
            
            GUI.Label(headerRect, $"<color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>InScript</color> Syntax Reference", _headerStyle);
        }
        
        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🔍", GUILayout.Width(20));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("InScript v1.0.0", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private bool BeginSection(string icon, string title)
        {
            if (!MatchesFilter(title))
            {
                return false;
            }
            
            Color bgColor = (_sectionIndex % 2 == 0) ? SectionBgColor : SectionBgAltColor;
            _sectionIndex++;
            
            EditorGUILayout.BeginVertical();
            
            // Section header background
            Rect sectionRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sectionRect, bgColor);
            
            // Icon and title
            GUI.Label(sectionRect, $"  {icon}  <b>{title}</b>", _sectionTitleStyle);
            
            return true;
        }
        
        private void EndSection()
        {
            GUILayout.Space(8);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawItem(string syntax, string description, string example = null)
        {
            if (!string.IsNullOrEmpty(_searchFilter) && !MatchesFilter(syntax) && !MatchesFilter(description))
            {
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            
            // Syntax column (fixed width)
            EditorGUILayout.LabelField(syntax, _syntaxStyle, GUILayout.Width(220));
            
            // Description column (flexible)
            EditorGUILayout.LabelField(description, _descriptionStyle);
            
            EditorGUILayout.EndHorizontal();
            
            // Example (optional)
            if (!string.IsNullOrEmpty(example))
            {
                EditorGUILayout.LabelField($"→ {example}", _exampleStyle);
            }
        }
        
        private bool MatchesFilter(string text)
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                return true;
            }
            return text.ToLower().Contains(_searchFilter.ToLower());
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // SECTIONS
        // ═══════════════════════════════════════════════════════════════════
        
        private void DrawSectionBlocks()
        {
            if (!BeginSection("📦", "Blocks"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorBlock, "@")}blockName",
                "Declares a named block of code",
                "@main, @onHit, @tick"
            );
            DrawItem(
                $"{C(ColorKeyword, ";")}",
                "Ends a block or conditional statement"
            );
            
            EndSection();
        }
        
        private void DrawSectionVariables()
        {
            if (!BeginSection("📝", "Variables"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorLocalVar, "$")}varName {C(ColorOperator, "=")} value",
                "Local variable (exists only during script execution)",
                "$damage = attack * 1.5"
            );
            DrawItem(
                $"{C(ColorLocalVar, "$")}var {C(ColorOperator, "+= -= *= /=")} value",
                "Compound assignment operators",
                "$counter += 1"
            );
            DrawItem(
                $"{C(ColorIdentifier, "identifier")} {C(ColorOperator, "=")} value",
                "Sets a context variable (from your C# code)",
                "hp = hp - damage"
            );
            
            EndSection();
        }
        
        private void DrawSectionSelectors()
        {
            if (!BeginSection("🎯", "Selectors"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorSelector, "#")}Selector{C(ColorIdentifier, ".property")}",
                "Access a property on a selector target",
                "#Target.hp, #Self.attack"
            );
            DrawItem(
                $"{C(ColorSelector, "#")}Selector{C(ColorMethod, ".Method")}()",
                "Call a method on a selector target",
                "#Target.TakeDamage(10)"
            );
            
            EndSection();
        }
        
        private void DrawSectionConditionals()
        {
            if (!BeginSection("🔀", "Conditionals"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorKeyword, "?")} condition",
                "If statement - executes block if condition is true",
                "? hp < maxHp * 0.5"
            );
            DrawItem(
                $"{C(ColorKeyword, ":?")} condition",
                "Else if statement - alternative condition",
                ":? hp < maxHp * 0.25"
            );
            DrawItem(
                $"{C(ColorKeyword, ":")}",
                "Else statement - executes if no conditions matched"
            );
            DrawItem(
                $"{C(ColorKeyword, ";")}",
                "End conditional block"
            );
            
            EndSection();
        }
        
        private void DrawSectionOperators()
        {
            if (!BeginSection("⚡", "Operators"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorOperator, "+")} {C(ColorOperator, "-")} {C(ColorOperator, "*")} {C(ColorOperator, "/")}",
                "Arithmetic: add, subtract, multiply, divide",
                "attack * 1.5 + bonusDamage"
            );
            DrawItem(
                $"{C(ColorOperator, "==")} {C(ColorOperator, "!=")}",
                "Equality: equal, not equal",
                "state == 1"
            );
            DrawItem(
                $"{C(ColorOperator, "<")} {C(ColorOperator, ">")} {C(ColorOperator, "<=")} {C(ColorOperator, ">=")}",
                "Comparison: less, greater, less or equal, greater or equal",
                "hp <= 0"
            );
            
            EndSection();
        }
        
        private void DrawSectionMathFunctions()
        {
            if (!BeginSection("🔢", "Math Functions"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorMethod, "min")}({C(ColorNumber, "a")}, {C(ColorNumber, "b")})",
                "Returns the smaller of two values",
                "min(hp, maxHp) → 50"
            );
            DrawItem(
                $"{C(ColorMethod, "max")}({C(ColorNumber, "a")}, {C(ColorNumber, "b")})",
                "Returns the larger of two values",
                "max(0, damage - armor) → prevents negative"
            );
            DrawItem(
                $"{C(ColorMethod, "clamp")}({C(ColorNumber, "v")}, {C(ColorNumber, "min")}, {C(ColorNumber, "max")})",
                "Constrains value between min and max",
                "clamp(hp, 0, maxHp)"
            );
            DrawItem(
                $"{C(ColorMethod, "abs")}({C(ColorNumber, "x")})",
                "Returns absolute (positive) value",
                "abs(-5) → 5"
            );
            DrawItem(
                $"{C(ColorMethod, "sign")}({C(ColorNumber, "x")})",
                "Returns -1, 0, or 1 based on sign",
                "sign(-10) → -1"
            );
            
            EndSection();
        }
        
        private void DrawSectionRoundingFunctions()
        {
            if (!BeginSection("📐", "Rounding Functions"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorMethod, "floor")}({C(ColorNumber, "x")})",
                "Rounds down to nearest integer",
                "floor(3.7) → 3"
            );
            DrawItem(
                $"{C(ColorMethod, "ceil")}({C(ColorNumber, "x")})",
                "Rounds up to nearest integer",
                "ceil(3.2) → 4"
            );
            DrawItem(
                $"{C(ColorMethod, "round")}({C(ColorNumber, "x")})",
                "Rounds to nearest integer",
                "round(3.5) → 4"
            );
            
            EndSection();
        }
        
        private void DrawSectionAdvancedFunctions()
        {
            if (!BeginSection("🚀", "Advanced Functions"))
            {
                return;
            }
            
            DrawItem(
                $"{C(ColorMethod, "sqrt")}({C(ColorNumber, "x")})",
                "Square root",
                "sqrt(16) → 4"
            );
            DrawItem(
                $"{C(ColorMethod, "pow")}({C(ColorNumber, "base")}, {C(ColorNumber, "exp")})",
                "Power: base raised to exponent",
                "pow(2, 3) → 8"
            );
            DrawItem(
                $"{C(ColorMethod, "lerp")}({C(ColorNumber, "a")}, {C(ColorNumber, "b")}, {C(ColorNumber, "t")})",
                "Linear interpolation (t: 0-1)",
                "lerp(0, 100, 0.5) → 50"
            );
            DrawItem(
                $"{C(ColorMethod, "random")}({C(ColorNumber, "min")}, {C(ColorNumber, "max")})",
                "Random float in range [min, max]",
                "random(1, 10) → 5.7"
            );
            DrawItem(
                $"{C(ColorMethod, "random")}()",
                "Random float in range [0, 1]",
                "random() → 0.42"
            );
            DrawItem(
                $"{C(ColorMethod, "Log")}({C(ColorString, "\"message\"")})",
                "Print message to Unity Console",
                "Log(\"Hello!\") → [InScript] Hello!"
            );
            
            EndSection();
        }
        
        private string C(Color color, string text)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
    }
}
