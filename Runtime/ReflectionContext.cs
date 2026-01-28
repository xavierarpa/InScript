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
using System.Reflection;
using UnityEngine;

namespace InScript
{
    /// <summary>
    /// IScriptContext implementation that uses reflection to read attributes.
    /// Automatically wraps any object with [ScriptValue], [ScriptMethod], [ScriptSelector] attributes.
    /// </summary>
    internal sealed class ReflectionContext : IScriptContext
    {
        private readonly object target;
        private readonly Dictionary<string, MemberInfo> values;
        private readonly Dictionary<string, bool> readOnlyValues;
        private readonly Dictionary<string, MethodInfo> methods;
        private readonly Dictionary<string, MemberInfo> selectors;
        
        private static readonly Dictionary<Type, ReflectionCache> typeCache = new();

        private ReflectionContext(object target, ReflectionCache cache)
        {
            this.target = target;
            this.values = cache.Values;
            this.readOnlyValues = cache.ReadOnlyValues;
            this.methods = cache.Methods;
            this.selectors = cache.Selectors;
        }

        /// <summary>
        /// Creates an IScriptContext from any object using reflection.
        /// If the object already implements IScriptContext, returns it directly.
        /// </summary>
        public static IScriptContext From(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            
            if (obj is IScriptContext context)
            {
                return context;
            }

            var type = obj.GetType();
            if (!typeCache.TryGetValue(type, out var cache))
            {
                cache = BuildCache(type);
                typeCache[type] = cache;
            }

            return new ReflectionContext(obj, cache);
        }

        private static ReflectionCache BuildCache(Type type)
        {
            var cache = new ReflectionCache();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Scan fields
            foreach (var field in type.GetFields(flags))
            {
                var valueAttr = field.GetCustomAttribute<ScriptValueAttribute>();
                if (valueAttr != null)
                {
                    var name = valueAttr.Name ?? field.Name;
                    cache.Values[name] = field;
                    cache.ReadOnlyValues[name] = valueAttr.ReadOnly;
                }

                var selectorAttr = field.GetCustomAttribute<ScriptSelectorAttribute>();
                if (selectorAttr != null)
                {
                    var name = selectorAttr.Name ?? field.Name;
                    cache.Selectors[name] = field;
                }
            }

            // Scan properties
            foreach (var prop in type.GetProperties(flags))
            {
                var valueAttr = prop.GetCustomAttribute<ScriptValueAttribute>();
                if (valueAttr != null)
                {
                    var name = valueAttr.Name ?? prop.Name;
                    cache.Values[name] = prop;
                    cache.ReadOnlyValues[name] = valueAttr.ReadOnly || !prop.CanWrite;
                }

                var selectorAttr = prop.GetCustomAttribute<ScriptSelectorAttribute>();
                if (selectorAttr != null)
                {
                    var name = selectorAttr.Name ?? prop.Name;
                    cache.Selectors[name] = prop;
                }
            }

            // Scan methods
            foreach (var method in type.GetMethods(flags))
            {
                var methodAttr = method.GetCustomAttribute<ScriptMethodAttribute>();
                if (methodAttr != null)
                {
                    var name = methodAttr.Name ?? method.Name;
                    cache.Methods[name] = method;
                }
            }

            return cache;
        }

        bool IScriptContext.TryGetValue(string name, out float value)
        {
            if (values.TryGetValue(name, out var member))
            {
                var rawValue = GetMemberValue(member);
                value = Convert.ToSingle(rawValue);
                return true;
            }

            Debug.LogWarning($"[InScript] Value '{name}' not found on {target.GetType().Name}. Add [ScriptValue] attribute to expose it.");
            value = 0f;
            return false;
        }

        bool IScriptContext.TrySetValue(string name, float value)
        {
            if (!values.TryGetValue(name, out var member))
            {
                return false;
            }

            if (readOnlyValues.TryGetValue(name, out var isReadOnly) && isReadOnly)
            {
                return false;
            }

            SetMemberValue(member, value);
            return true;
        }

        IScriptContext IScriptContext.GetSelector(string name)
        {
            if (!selectors.TryGetValue(name, out var member))
            {
                Debug.LogWarning($"[InScript] Selector '#{name}' not found on {target.GetType().Name}. Add [ScriptSelector] attribute to expose it.");
                return null;
            }

            var obj = GetMemberValue(member);
            if (obj == null)
            {
                Debug.LogWarning($"[InScript] Selector '#{name}' is null on {target.GetType().Name}.");
                return null;
            }
            
            return From(obj);
        }

        object IScriptContext.Invoke(string methodName, params object[] args)
        {
            if (!methods.TryGetValue(methodName, out var method))
            {
                // Return special marker to indicate method not found
                // This allows ScriptRunner to try executing a block with the same name
                return MethodNotFound.Instance;
            }

            var parameters = method.GetParameters();
            
            // Handle params array (last parameter with ParamArrayAttribute)
            if (parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute), false))
            {
                var normalParamCount = parameters.Length - 1;
                var convertedArgs = new object[parameters.Length];
                
                // Convert normal parameters
                for (int i = 0; i < normalParamCount; i++)
                {
                    if (i < args.Length)
                    {
                        convertedArgs[i] = ConvertArg(args[i], parameters[i].ParameterType);
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        convertedArgs[i] = parameters[i].DefaultValue;
                    }
                }
                
                // Pack remaining args into params array
                var paramsType = parameters[^1].ParameterType.GetElementType();
                var paramsCount = Math.Max(0, args.Length - normalParamCount);
                var paramsArray = Array.CreateInstance(paramsType, paramsCount);
                
                for (int i = 0; i < paramsCount; i++)
                {
                    paramsArray.SetValue(ConvertArg(args[normalParamCount + i], paramsType), i);
                }
                
                convertedArgs[^1] = paramsArray;
                return method.Invoke(target, convertedArgs);
            }
            else
            {
                // Normal invocation
                var convertedArgs = new object[parameters.Length];
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < args.Length)
                    {
                        convertedArgs[i] = ConvertArg(args[i], parameters[i].ParameterType);
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        convertedArgs[i] = parameters[i].DefaultValue;
                    }
                }

                return method.Invoke(target, convertedArgs);
            }
        }
        
        private static object ConvertArg(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }
            
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }
            
            if (targetType == typeof(object))
            {
                return value;
            }
            
            return Convert.ChangeType(value, targetType);
        }

        IScriptSchema IScriptContext.GetSchema()
        {
            return null;
        }

        private object GetMemberValue(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.GetValue(target),
                PropertyInfo prop => prop.GetValue(target),
                _ => null
            };
        }

        private void SetMemberValue(MemberInfo member, float value)
        {
            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(target, Convert.ChangeType(value, field.FieldType));
                    break;
                case PropertyInfo prop:
                    prop.SetValue(target, Convert.ChangeType(value, prop.PropertyType));
                    break;
            }
        }

        private sealed class ReflectionCache
        {
            public readonly Dictionary<string, MemberInfo> Values = new();
            public readonly Dictionary<string, bool> ReadOnlyValues = new();
            public readonly Dictionary<string, MethodInfo> Methods = new();
            public readonly Dictionary<string, MemberInfo> Selectors = new();
        }
    }
}
