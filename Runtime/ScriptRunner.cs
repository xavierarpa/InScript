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
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// Executes scripts by calling IScriptContext to resolve values.
    /// Knows nothing about the project using it.
    /// </summary>
    internal static class ScriptRunner
    {
        // ═══════════════════════════════════════════════════════════════════
        // REGEX PATTERNS (Pure syntax)
        // ═══════════════════════════════════════════════════════════════════
        
        // Comments
        private static readonly Regex CommentLinePattern = new(@"^\s*//", RegexOptions.Compiled);
        private static readonly Regex CommentBlockStartPattern = new(@"^\s*/\*", RegexOptions.Compiled);
        private static readonly Regex CommentBlockEndPattern = new(@"\*/\s*$", RegexOptions.Compiled);
        
        // Blocks: @name ... ;
        private static readonly Regex BlockHeaderPattern = new(@"^\s*@(\w+)(?:\s+([\d.]+))?\s*$", RegexOptions.Compiled);
        
        // Control de flujo
        private static readonly Regex IfPattern = new(@"^\s*\?\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex ElseIfPattern = new(@"^\s*:\?\s+(.+)$", RegexOptions.Compiled);
        private static readonly Regex ElsePattern = new(@"^\s*:\s*$", RegexOptions.Compiled);
        private static readonly Regex EndPattern = new(@"^\s*;\s*$", RegexOptions.Compiled);
        
        // Local variables
        private static readonly Regex LocalVarAssignPattern = new(@"^\s*\$(\w+)\s*=\s*(.+)$", RegexOptions.Compiled);
        private static readonly Regex LocalVarCompoundPattern = new(@"^\s*\$(\w+)\s*(\+=|-=|\*=|/=)\s*(.+)$", RegexOptions.Compiled);
        
        // Context assignment: identifier = expr
        private static readonly Regex ContextAssignPattern = new(@"^\s*(\w+)\s*=\s*(.+)$", RegexOptions.Compiled);
        
        // Selector with property/method: #Selector.Property or #Selector.Method()
        private static readonly Regex SelectorAccessPattern = new(@"^\s*#(\w+)\.(\w+)(?:\(([^)]*)\))?\s*$", RegexOptions.Compiled);
        
        // Method call: Method(args)
        private static readonly Regex MethodCallPattern = new(@"^\s*(\w+)\(([^)]*)\)\s*$", RegexOptions.Compiled);
        
        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Executes only the code outside of blocks (global code).
        /// Blocks are like methods and must be called explicitly with ExecuteBlock.
        /// </summary>
        public static void Execute(string script, IScriptContext context)
        {
            if (string.IsNullOrWhiteSpace(script) || context == null)
            {
                return;
            }
            
            var globalCode = ExtractGlobalCode(script);
            if (string.IsNullOrWhiteSpace(globalCode))
            {
                return;
            }
            
            var lines = globalCode.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var localVariables = new Dictionary<string, float>();
            
            ExecuteLines(script, lines, 0, lines.Length, context, localVariables);
        }
        
        /// <summary>
        /// Executes a specific block (@name).
        /// </summary>
        public static void ExecuteBlock(string script, string blockName, IScriptContext context)
        {
            ExecuteBlock(script, blockName, context, null);
        }
        
        /// <summary>
        /// Executes a specific block with pre-initialized local variables.
        /// </summary>
        public static void ExecuteBlock(string script, string blockName, IScriptContext context, Dictionary<string, float> initialLocals)
        {
            if (string.IsNullOrWhiteSpace(script) || context == null)
            {
                return;
            }
            
            var blockContent = ExtractBlock(script, blockName);
            if (string.IsNullOrWhiteSpace(blockContent))
            {
                return;
            }
            
            var lines = blockContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var localVariables = initialLocals != null 
                ? new Dictionary<string, float>(initialLocals) 
                : new Dictionary<string, float>();
            
            ExecuteLines(script, lines, 0, lines.Length, context, localVariables);
        }
        
        /// <summary>
        /// Evaluates an expression and returns the result.
        /// </summary>
        public static float EvaluateExpression(string expression, IScriptContext context)
        {
            var localVariables = new Dictionary<string, float>();
            return EvaluateExpr(expression, context, localVariables);
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // BLOCK EXTRACTION
        // ═══════════════════════════════════════════════════════════════════
        
        private static string ExtractBlock(string script, string blockName)
        {
            var lines = script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            bool inBlock = false;
            int depth = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                
                // Find block start
                var blockMatch = BlockHeaderPattern.Match(trimmed);
                if (blockMatch.Success)
                {
                    if (blockMatch.Groups[1].Value.Equals(blockName, StringComparison.OrdinalIgnoreCase))
                    {
                        inBlock = true;
                        depth = 1;
                        continue;
                    }
                    else if (inBlock)
                    {
                        // Another block, stop here
                        break;
                    }
                }
                
                if (!inBlock)
                {
                    continue;
                }
                
                // Contar profundidad
                if (IfPattern.IsMatch(trimmed))
                {
                    depth++;
                }
                
                if (EndPattern.IsMatch(trimmed))
                {
                    depth--;
                    if (depth == 0)
                    {
                        break;
                    }
                }
                
                result.Add(line);
            }
            
            return string.Join("\n", result);
        }
        
        private static string ExtractGlobalCode(string script)
        {
            var lines = script.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            bool inBlock = false;
            int depth = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                
                // Check for block start
                if (BlockHeaderPattern.IsMatch(trimmed))
                {
                    inBlock = true;
                    depth = 1;
                    continue;
                }
                
                if (inBlock)
                {
                    // Count depth for nested structures
                    if (IfPattern.IsMatch(trimmed))
                    {
                        depth++;
                    }
                    
                    if (EndPattern.IsMatch(trimmed))
                    {
                        depth--;
                        if (depth == 0)
                        {
                            inBlock = false;
                        }
                    }
                    continue;
                }
                
                // Global code (outside blocks)
                result.Add(line);
            }
            
            return string.Join("\n", result);
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // LINE EXECUTION
        // ═══════════════════════════════════════════════════════════════════
        
        private static void ExecuteLines(string script, string[] lines, int start, int end, IScriptContext context, Dictionary<string, float> locals)
        {
            int i = start;
            bool inBlockComment = false;
            
            while (i < end)
            {
                string line = lines[i].Trim();
                
                // Block comment
                if (inBlockComment)
                {
                    if (CommentBlockEndPattern.IsMatch(line))
                    {
                        inBlockComment = false;
                    }
                    i++;
                    continue;
                }
                
                if (CommentBlockStartPattern.IsMatch(line))
                {
                    if (!CommentBlockEndPattern.IsMatch(line))
                    {
                        inBlockComment = true;
                    }
                    i++;
                    continue;
                }
                
                // Empty line or comment
                if (string.IsNullOrEmpty(line) || CommentLinePattern.IsMatch(line))
                {
                    i++;
                    continue;
                }
                
                // Block @name (skip, handled by ExecuteBlock)
                if (BlockHeaderPattern.IsMatch(line))
                {
                    i++;
                    continue;
                }
                
                // IF: ? condition
                var ifMatch = IfPattern.Match(line);
                if (ifMatch.Success)
                {
                    i = ExecuteConditional(script, lines, i, end, context, locals);
                    continue;
                }
                
                // :?, :, ; standalone (syntax error)
                if (ElseIfPattern.IsMatch(line) || ElsePattern.IsMatch(line) || EndPattern.IsMatch(line))
                {
                    i++;
                    continue;
                }
                
                // Compound assignment to local: $var += expr
                var compoundMatch = LocalVarCompoundPattern.Match(line);
                if (compoundMatch.Success)
                {
                    ExecuteLocalCompoundAssign(compoundMatch, context, locals);
                    i++;
                    continue;
                }
                
                // Assignment to local: $var = expr
                var localMatch = LocalVarAssignPattern.Match(line);
                if (localMatch.Success)
                {
                    string varName = localMatch.Groups[1].Value;
                    float value = EvaluateExpr(localMatch.Groups[2].Value, context, locals);
                    locals[varName] = value;
                    i++;
                    continue;
                }
                
                // Selector with action: #Selector.Method() or #Selector.Property = value
                var selectorMatch = SelectorAccessPattern.Match(line);
                if (selectorMatch.Success)
                {
                    ExecuteSelectorAccess(selectorMatch, context, locals);
                    i++;
                    continue;
                }
                
                // Method call: Method(args)
                var methodMatch = MethodCallPattern.Match(line);
                if (methodMatch.Success)
                {
                    ExecuteMethodCall(script, methodMatch, context, locals);
                    i++;
                    continue;
                }
                
                // Context assignment: identifier = expr
                var contextMatch = ContextAssignPattern.Match(line);
                if (contextMatch.Success)
                {
                    string name = contextMatch.Groups[1].Value;
                    float value = EvaluateExpr(contextMatch.Groups[2].Value, context, locals);
                    context.TrySetValue(name, value);
                    i++;
                    continue;
                }
                
                // Unrecognized line
                Debug.LogWarning($"[ScriptLab] Unrecognized line: {line}");
                i++;
            }
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // CONDITIONAL EXECUTION
        // ═══════════════════════════════════════════════════════════════════
        
        private static int ExecuteConditional(string script, string[] lines, int ifIndex, int end, IScriptContext context, Dictionary<string, float> locals)
        {
            var branches = FindConditionalBranches(lines, ifIndex, end);
            
            if (branches.endIndex == -1)
            {
                Debug.LogWarning($"[ScriptLab] Missing ';' for '?' at line {ifIndex + 1}");
                return ifIndex + 1;
            }
            
            bool executed = false;
            foreach (var branch in branches.branches)
            {
                if (executed)
                {
                    break;
                }
                
                if (branch.condition == null)
                {
                    // else
                    ExecuteLines(script, lines, branch.startLine, branch.endLine, context, locals);
                    executed = true;
                }
                else
                {
                    bool condResult = EvaluateCondition(branch.condition, context, locals);
                    if (condResult)
                    {
                        ExecuteLines(script, lines, branch.startLine, branch.endLine, context, locals);
                        executed = true;
                    }
                }
            }
            
            return branches.endIndex + 1;
        }
        
        private struct ConditionalBranch
        {
            public string condition;
            public int startLine;
            public int endLine;
        }
        
        private struct ConditionalResult
        {
            public List<ConditionalBranch> branches;
            public int endIndex;
        }
        
        private static ConditionalResult FindConditionalBranches(string[] lines, int ifIndex, int end)
        {
            var result = new ConditionalResult
            {
                branches = new List<ConditionalBranch>(),
                endIndex = -1
            };
            
            int depth = 1;
            int currentBranchStart = ifIndex + 1;
            string currentCondition = IfPattern.Match(lines[ifIndex].Trim()).Groups[1].Value;
            
            for (int i = ifIndex + 1; i < end; i++)
            {
                string line = lines[i].Trim();
                
                if (IfPattern.IsMatch(line))
                {
                    depth++;
                    continue;
                }
                
                if (EndPattern.IsMatch(line))
                {
                    depth--;
                    if (depth == 0)
                    {
                        result.branches.Add(new ConditionalBranch
                        {
                            condition = currentCondition,
                            startLine = currentBranchStart,
                            endLine = i
                        });
                        result.endIndex = i;
                        return result;
                    }
                    continue;
                }
                
                if (depth != 1)
                {
                    continue;
                }
                
                var elseIfMatch = ElseIfPattern.Match(line);
                if (elseIfMatch.Success)
                {
                    result.branches.Add(new ConditionalBranch
                    {
                        condition = currentCondition,
                        startLine = currentBranchStart,
                        endLine = i
                    });
                    
                    currentCondition = elseIfMatch.Groups[1].Value;
                    currentBranchStart = i + 1;
                    continue;
                }
                
                if (ElsePattern.IsMatch(line))
                {
                    result.branches.Add(new ConditionalBranch
                    {
                        condition = currentCondition,
                        startLine = currentBranchStart,
                        endLine = i
                    });
                    
                    currentCondition = null;
                    currentBranchStart = i + 1;
                }
            }
            
            return result;
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // EXPRESSION EVALUATION
        // ═══════════════════════════════════════════════════════════════════
        
        private static bool EvaluateCondition(string condition, IScriptContext context, Dictionary<string, float> locals)
        {
            string[] operators = { "<=", ">=", "==", "!=", "<", ">" };
            
            foreach (var op in operators)
            {
                int opIndex = condition.IndexOf(op, StringComparison.Ordinal);
                if (opIndex != -1)
                {
                    string left = condition.Substring(0, opIndex).Trim();
                    string right = condition.Substring(opIndex + op.Length).Trim();
                    
                    float leftVal = EvaluateExpr(left, context, locals);
                    float rightVal = EvaluateExpr(right, context, locals);
                    
                    return op switch
                    {
                        "==" => Mathf.Approximately(leftVal, rightVal),
                        "!=" => !Mathf.Approximately(leftVal, rightVal),
                        "<" => leftVal < rightVal,
                        "<=" => leftVal <= rightVal,
                        ">" => leftVal > rightVal,
                        ">=" => leftVal >= rightVal,
                        _ => false
                    };
                }
            }
            
            // Sin operador, evaluar como bool (0 = false, != 0 = true)
            return EvaluateExpr(condition, context, locals) != 0;
        }
        
        private static float EvaluateExpr(string expr, IScriptContext context, Dictionary<string, float> locals)
        {
            expr = expr.Trim();
            
            if (string.IsNullOrEmpty(expr))
            {
                return 0;
            }
            
            // Literal number
            if (float.TryParse(expr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out float num))
            {
                return num;
            }
            
            // Variable local ($var)
            if (expr.StartsWith("$") && locals.TryGetValue(expr.Substring(1), out float localVal))
            {
                return localVal;
            }
            
            // Selector.Property (#Selector.Prop)
            if (expr.StartsWith("#"))
            {
                var match = Regex.Match(expr, @"^#(\w+)\.(\w+)$");
                if (match.Success)
                {
                    var selector = context.GetSelector(match.Groups[1].Value);
                    if (selector != null && selector.TryGetValue(match.Groups[2].Value, out float selectorVal))
                    {
                        return selectorVal;
                    }
                }
                return 0;
            }
            
            // Binary operations (simplified, left to right)
            // Find + and - first (lower precedence)
            int parenDepth = 0;
            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == ')')
                {
                    parenDepth++;
                }
                else if (c == '(')
                {
                    parenDepth--;
                }
                else if (parenDepth == 0 && (c == '+' || c == '-') && i > 0)
                {
                    // Find the previous non-space character to check if this is a unary operator
                    int prevIdx = i - 1;
                    while (prevIdx >= 0 && expr[prevIdx] == ' ')
                    {
                        prevIdx--;
                    }
                    
                    if (prevIdx < 0)
                    {
                        continue; // Unary at start
                    }
                    
                    char prev = expr[prevIdx];
                    // If previous char is an operator, this is unary, skip it
                    if (prev == '+' || prev == '-' || prev == '*' || prev == '/' || prev == '(' || prev == '=')
                    {
                        continue;
                    }
                    
                    float left = EvaluateExpr(expr.Substring(0, i), context, locals);
                    float right = EvaluateExpr(expr.Substring(i + 1), context, locals);
                    return c == '+' ? left + right : left - right;
                }
            }
            
            // Find * and /
            parenDepth = 0;
            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == ')')
                {
                    parenDepth++;
                }
                else if (c == '(')
                {
                    parenDepth--;
                }
                else if (parenDepth == 0 && (c == '*' || c == '/'))
                {
                    float left = EvaluateExpr(expr.Substring(0, i), context, locals);
                    float right = EvaluateExpr(expr.Substring(i + 1), context, locals);
                    return c == '*' ? left * right : (right != 0 ? left / right : 0);
                }
            }
            
            // Parentheses
            if (expr.StartsWith("(") && expr.EndsWith(")"))
            {
                return EvaluateExpr(expr.Substring(1, expr.Length - 2), context, locals);
            }
            
            // Funciones built-in: min(a, b), max(a, b), clamp(v, min, max)
            var funcMatch = Regex.Match(expr, @"^(\w+)\((.+)\)$");
            if (funcMatch.Success)
            {
                string funcName = funcMatch.Groups[1].Value.ToLower();
                string argsStr = funcMatch.Groups[2].Value;
                var args = SplitArgs(argsStr);
                
                return funcName switch
                {
                    "min" when args.Count >= 2 => Mathf.Min(
                        EvaluateExpr(args[0], context, locals),
                        EvaluateExpr(args[1], context, locals)),
                    "max" when args.Count >= 2 => Mathf.Max(
                        EvaluateExpr(args[0], context, locals),
                        EvaluateExpr(args[1], context, locals)),
                    "clamp" when args.Count >= 3 => Mathf.Clamp(
                        EvaluateExpr(args[0], context, locals),
                        EvaluateExpr(args[1], context, locals),
                        EvaluateExpr(args[2], context, locals)),
                    "abs" when args.Count >= 1 => Mathf.Abs(EvaluateExpr(args[0], context, locals)),
                    "sign" when args.Count >= 1 => Mathf.Sign(EvaluateExpr(args[0], context, locals)),
                    "floor" when args.Count >= 1 => Mathf.Floor(EvaluateExpr(args[0], context, locals)),
                    "ceil" when args.Count >= 1 => Mathf.Ceil(EvaluateExpr(args[0], context, locals)),
                    "round" when args.Count >= 1 => Mathf.Round(EvaluateExpr(args[0], context, locals)),
                    "sqrt" when args.Count >= 1 => Mathf.Sqrt(EvaluateExpr(args[0], context, locals)),
                    "pow" when args.Count >= 2 => Mathf.Pow(
                        EvaluateExpr(args[0], context, locals),
                        EvaluateExpr(args[1], context, locals)),
                    "lerp" when args.Count >= 3 => Mathf.Lerp(
                        EvaluateExpr(args[0], context, locals),
                        EvaluateExpr(args[1], context, locals),
                        EvaluateExpr(args[2], context, locals)),
                    "random" when args.Count >= 2 => UnityEngine.Random.Range(
                        EvaluateExpr(args[0], context, locals),
                        EvaluateExpr(args[1], context, locals)),
                    "random" when args.Count == 0 => UnityEngine.Random.value,
                    _ => 0
                };
            }
            
            // Identificador del contexto
            if (context.TryGetValue(expr, out float contextVal))
            {
                return contextVal;
            }
            
            return 0;
        }
        
        private static List<string> SplitArgs(string argsStr)
        {
            var result = new List<string>();
            int parenDepth = 0;
            int start = 0;
            
            for (int i = 0; i < argsStr.Length; i++)
            {
                char c = argsStr[i];
                if (c == '(')
                {
                    parenDepth++;
                }
                else if (c == ')')
                {
                    parenDepth--;
                }
                else if (c == ',' && parenDepth == 0)
                {
                    result.Add(argsStr.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            
            if (start < argsStr.Length)
            {
                result.Add(argsStr.Substring(start).Trim());
            }
            
            return result;
        }
        
        // ═══════════════════════════════════════════════════════════════════
        // EXECUTION HELPERS
        // ═══════════════════════════════════════════════════════════════════
        
        private static void ExecuteLocalCompoundAssign(Match match, IScriptContext context, Dictionary<string, float> locals)
        {
            string varName = match.Groups[1].Value;
            string op = match.Groups[2].Value;
            float exprValue = EvaluateExpr(match.Groups[3].Value, context, locals);
            
            float currentValue = locals.TryGetValue(varName, out float v) ? v : 0;
            
            locals[varName] = op switch
            {
                "+=" => currentValue + exprValue,
                "-=" => currentValue - exprValue,
                "*=" => currentValue * exprValue,
                "/=" => exprValue != 0 ? currentValue / exprValue : currentValue,
                _ => currentValue
            };
        }
        
        private static void ExecuteSelectorAccess(Match match, IScriptContext context, Dictionary<string, float> locals)
        {
            string selectorName = match.Groups[1].Value;
            string memberName = match.Groups[2].Value;
            string argsStr = match.Groups[3].Success ? match.Groups[3].Value : null;
            
            var selector = context.GetSelector(selectorName);
            if (selector == null)
            {
                Debug.LogWarning($"[ScriptLab] Selector not found: {selectorName}");
                return;
            }
            
            if (argsStr != null)
            {
                // Method call
                var args = ParseMethodArgs(argsStr, context, locals);
                selector.Invoke(memberName, args);
            }
        }
        
        private static void ExecuteMethodCall(string script, Match match, IScriptContext context, Dictionary<string, float> locals)
        {
            string methodName = match.Groups[1].Value;
            string argsStr = match.Groups[2].Value;
            
            // Built-in functions
            if (methodName.Equals("Log", StringComparison.OrdinalIgnoreCase))
            {
                var args = ParseMethodArgs(argsStr, context, locals);
                string message = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
                Debug.Log($"[InScript] {message}");
                return;
            }
            
            // First try to call method on context
            var methodArgs = ParseMethodArgs(argsStr, context, locals);
            var result = context.Invoke(methodName, methodArgs);
            
            // If method doesn't exist, try to execute as block
            if (result is MethodNotFound)
            {
                var blockContent = ExtractBlock(script, methodName);
                if (!string.IsNullOrWhiteSpace(blockContent))
                {
                    var lines = blockContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    ExecuteLines(script, lines, 0, lines.Length, context, locals);
                }
                else
                {
                    Debug.LogWarning($"[InScript] '{methodName}' is not a method or block.");
                }
            }
        }
        
        private static object[] ParseMethodArgs(string argsStr, IScriptContext context, Dictionary<string, float> locals)
        {
            if (string.IsNullOrWhiteSpace(argsStr))
            {
                return Array.Empty<object>();
            }
            
            var argStrings = SplitArgs(argsStr);
            var result = new object[argStrings.Count];
            
            for (int i = 0; i < argStrings.Count; i++)
            {
                var arg = argStrings[i].Trim();
                
                // String literal
                if (arg.StartsWith("\"") && arg.EndsWith("\""))
                {
                    result[i] = arg.Substring(1, arg.Length - 2);
                }
                else
                {
                    // Evaluate as numeric expression
                    result[i] = EvaluateExpr(arg, context, locals);
                }
            }
            
            return result;
        }
    }
}
