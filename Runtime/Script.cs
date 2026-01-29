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
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// Serializable script container with code and execution methods.
    /// Can be embedded inline in components or used inside ScriptAsset.
    /// </summary>
    [Serializable]
    public class Script
    {
        private const string DefaultCode = @"@main
    Log(""Hello from InScript!"")
;
";
        
        [TextArea(5, 30)]
        [Tooltip("Script code")]
        [SerializeField] private string code = DefaultCode;
        
        /// <summary>
        /// The script source code.
        /// </summary>
        public string Code
        {
            get => code;
            set => code = value;
        }
        
        /// <summary>
        /// Returns true if the script has valid code.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(code);
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Script() { }
        
        /// <summary>
        /// Constructor with initial code.
        /// </summary>
        public Script(string initialCode)
        {
            code = initialCode;
        }
        
        /// <summary>
        /// Executes the entire script with the given context object.
        /// Uses reflection to read [InScript] attributes.
        /// </summary>
        public void Execute(object target)
        {
            Execute(ReflectionContext.From(target));
        }
        
        /// <summary>
        /// Executes a specific block with the given context object.
        /// Uses reflection to read [InScript] attributes.
        /// </summary>
        public void Execute(string blockName, object target)
        {
            ExecuteBlock(blockName, ReflectionContext.From(target));
        }
        
        /// <summary>
        /// Executes the entire script with the given context.
        /// </summary>
        public void Execute(IScriptContext context)
        {
            if (!IsValid)
            {
                return;
            }
            
            ScriptRunner.Execute(code, context);
        }
        
        /// <summary>
        /// Executes a specific block of the script.
        /// </summary>
        public void ExecuteBlock(string blockName, IScriptContext context)
        {
            ExecuteBlock(blockName, context, null);
        }
        
        /// <summary>
        /// Executes a specific block with pre-initialized local variables.
        /// </summary>
        public void ExecuteBlock(string blockName, IScriptContext context, Dictionary<string, float> initialLocals)
        {
            if (!IsValid)
            {
                return;
            }
            
            ScriptRunner.ExecuteBlock(code, blockName, context, initialLocals);
        }
    }
}
