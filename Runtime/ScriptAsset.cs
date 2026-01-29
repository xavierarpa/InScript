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
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// ScriptableObject containing a Script.
    /// Provides a reusable asset wrapper around the Script class.
    /// </summary>
    [CreateAssetMenu(fileName = "New Script Asset", menuName = "InScript/Script Asset", order = 99999999)]
    public class ScriptAsset : ScriptableObject
    {
        [SerializeField] private Script script = new();
        
        /// <summary>
        /// The underlying Script instance.
        /// </summary>
        public Script Script => script;
        
        /// <summary>
        /// The script source code.
        /// </summary>
        public string Code => script.Code;
        
        /// <summary>
        /// Executes the entire script with the given context object.
        /// </summary>
        public void Execute(object target) => script.Execute(target);
        
        /// <summary>
        /// Executes a specific block with the given context object.
        /// </summary>
        public void Execute(string blockName, object target) => script.Execute(blockName, target);
        
        /// <summary>
        /// Executes the entire script with the given context.
        /// </summary>
        public void Execute(IScriptContext context) => script.Execute(context);
        
        /// <summary>
        /// Executes a specific block of the script.
        /// </summary>
        public void Execute(string blockName, IScriptContext context) => script.Execute(blockName, context);
    }
}
