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
namespace InScript
{
    /// <summary>
    /// Interface that projects must implement to connect scripts with real data.
    /// InScript doesn't know what's behind this interface.
    /// </summary>
    public interface IScriptContext
    {
        /// <summary>
        /// Attempts to get the value of an identifier.
        /// <example>
        /// Script syntax: <c>HP</c>, <c>duration</c>, <c>maxEnergy</c>
        /// <code>
        /// // In script: "HP + 10" calls TryGetValue("HP", out value)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="name">Identifier name (e.g., "HP", "duration")</param>
        /// <param name="value">The retrieved value</param>
        /// <returns>true if exists and was retrieved successfully</returns>
        bool TryGetValue(string name, out float value);
        
        /// <summary>
        /// Attempts to set the value of an identifier.
        /// <example>
        /// Script syntax: <c>HP = 100</c>, <c>energy = energy - 10</c>
        /// <code>
        /// // In script: "HP = 50" calls TrySetValue("HP", 50)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="name">Identifier name</param>
        /// <param name="value">Value to set</param>
        /// <returns>true if exists and is writable</returns>
        bool TrySetValue(string name, float value);
        
        /// <summary>
        /// Gets a sub-context through a selector.
        /// <example>
        /// Script syntax: <c>#Target</c>, <c>#Self</c>, <c>#Ally</c>
        /// <code>
        /// // In script: "#Target.HP" calls GetSelector("Target"), then TryGetValue("HP")
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="name">Selector name (e.g., "Target", "Self")</param>
        /// <returns>The selector's context, or null if not found</returns>
        IScriptContext GetSelector(string name);
        
        /// <summary>
        /// Invokes a method registered in the context.
        /// <example>
        /// Script syntax: <c>Heal(50)</c>, <c>DealDamage(10, "fire")</c>
        /// <code>
        /// // In script: "Heal(25)" calls Invoke("Heal", 25f)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="args">Arguments</param>
        /// <returns>Invocation result (may be null)</returns>
        object Invoke(string methodName, params object[] args);
        
        /// <summary>
        /// Gets the schema for editor validation (optional).
        /// </summary>
        /// <returns>The schema or null if no validation is needed</returns>
        IScriptSchema GetSchema();
    }
}
