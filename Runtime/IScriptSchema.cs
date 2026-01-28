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

namespace InScript
{
    /// <summary>
    /// Schema that describes what is available in a context.
    /// Used for validation and autocompletion in the editor.
    /// </summary>
    public interface IScriptSchema
    {
        /// <summary>
        /// Available block names (e.g.: "start", "tick", "end")
        /// </summary>
        IEnumerable<string> GetBlocks();
        
        /// <summary>
        /// Available variable/identifier names (e.g.: "HP", "duration")
        /// </summary>
        IEnumerable<string> GetVariables();
        
        /// <summary>
        /// Available method names (e.g.: "Heal", "Damage")
        /// </summary>
        IEnumerable<string> GetMethods();
        
        /// <summary>
        /// Available selector names (e.g.: "Target", "Self")
        /// </summary>
        IEnumerable<string> GetSelectors();
        
        /// <summary>
        /// Indicates whether a variable can be modified.
        /// </summary>
        bool IsVariableWritable(string name);
        
        /// <summary>
        /// Gets the schema of a selector (to validate sub-properties).
        /// </summary>
        IScriptSchema GetSelectorSchema(string name);
    }
}
