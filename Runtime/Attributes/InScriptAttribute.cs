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
    /// Exposes a member (field, property, or method) to InScript scripts.
    /// The system automatically detects the member type:
    /// - float/int/bool fields/properties → script values (hp, attack, etc.)
    /// - object reference fields/properties → selectors (#Target, #Self)
    /// - methods → callable actions (Heal(), TakeDamage())
    /// <example>
    /// <code>
    /// [InScript] private float hp = 100;              // Value: hp
    /// [InScript("life")] private float hp = 100;      // Value: life
    /// [InScript] private Enemy target;                // Selector: #target
    /// [InScript("Self")] private Player player;       // Selector: #Self
    /// [InScript] private void Heal(float amount) { }  // Method: Heal(10)
    /// [InScript("dmg")] private void TakeDamage() { } // Method: dmg()
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class InScriptAttribute : Attribute
    {
        /// <summary>
        /// The name exposed to scripts. If null, uses the member name.
        /// </summary>
        public string Name { get; }

        public InScriptAttribute(string name = null)
        {
            Name = name;
        }
    }
}