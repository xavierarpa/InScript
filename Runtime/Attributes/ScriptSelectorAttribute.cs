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

namespace InScript
{
    /// <summary>
    /// Exposes a field as a sub-context selector (accessed with #Name in scripts).
    /// The referenced object will be scanned for [ScriptValue], [ScriptMethod], and [ScriptSelector] attributes.
    /// If the object implements IScriptContext, it will be used directly.
    /// <example>
    /// <code>
    /// [ScriptSelector] private Enemy target;           // Accessed as #target
    /// [ScriptSelector("Self")] private Player player;  // Accessed as #Self
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ScriptSelectorAttribute : Attribute
    {
        /// <summary>
        /// The name exposed to scripts. If null, uses the member name.
        /// </summary>
        public string Name { get; }

        public ScriptSelectorAttribute(string name = null)
        {
            Name = name;
        }
    }
}
