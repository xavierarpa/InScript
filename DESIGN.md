# ScriptLab - Sistema de Scripting Genérico para Unity

## 🎯 Objetivo

Crear un **plugin de scripting genérico** que permita escribir lógica en texto plano desde ScriptableObjects, completamente **desacoplado de cualquier proyecto específico**.

---

## 🧱 Principio Fundamental

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ScriptLab (Plugin)                          │
│                                                                      │
│   • NO conoce ningún proyecto específico                            │
│   • Solo entiende SINTAXIS (bloques, variables, expresiones)        │
│   • Recibe un CONTEXTO en runtime para resolver valores             │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
                                  ↓
                    El proyecto (WW, otro juego, etc.)
                    INYECTA el contexto con sus datos
```

---

## 📊 Flujo General

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   ScriptAsset   │  →   │   ScriptLab     │  →   │  IScriptContext │
│   (solo texto)  │      │   (parser)      │      │  (lo inyecta    │
│                 │      │                 │      │   el proyecto)  │
└─────────────────┘      └─────────────────┘      └─────────────────┘
        │                        │                        │
   Solo sintaxis           Parsea y ejecuta         Resuelve valores
   No sabe qué es          instrucciones            reales del proyecto
   "HP" o "Buff"           genéricas
```

---

## 🔗 Concepto de Binding

Un **Binding** es la conexión entre:
- **Identificador en texto** → `@start`, `duration`, `Target.HP`
- **Código C# real** → método, propiedad, evento, variable

### Tipos de Bindings

| Tipo | En Script | En C# | Ejemplo |
|------|-----------|-------|---------|
| **Block/Event** | `@nombre` | `Action` o método | `@start` → `OnStart()` |
| **Variable Read** | `identifier` | `Func<T>` o propiedad | `duration` → `buff.Duration` |
| **Variable Write** | `identifier =` | `Action<T>` o setter | `duration = 5` → `buff.Duration = 5` |
| **Method** | `Nombre()` | `Func<args, T>` | `Heal(50)` → `unit.Heal(50)` |
| **Selector** | `#Target` | Resolver de entidad | `#Target` → `context.SelectedTarget` |

---

## 📝 Arquitectura del Plugin

### Lo que ScriptLab SÍ tiene (genérico)

```csharp
// ═══════════════════════════════════════════════════════════════════
// SCRIPTLAB - INTERFACES CORE (no conocen ningún proyecto)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Contrato que debe implementar quien quiera ejecutar scripts.
/// ScriptLab NO sabe qué hay detrás, solo llama estos métodos.
/// </summary>
public interface IScriptContext
{
    // Resolver un identificador (variable del contexto)
    bool TryGetValue(string name, out float value);
    bool TrySetValue(string name, float value);
    
    // Resolver un selector (#Target, #Self, etc.)
    IScriptContext GetSelector(string name);
    
    // Invocar un método
    object Invoke(string methodName, params object[] args);
    
    // (Opcional) Para validación en editor
    IScriptSchema GetSchema();
}

/// <summary>
/// Describe qué está disponible en un contexto (para editor/validación).
/// </summary>
public interface IScriptSchema
{
    IEnumerable<string> GetBlocks();           // @start, @tick, etc.
    IEnumerable<string> GetVariables();        // duration, HP, etc.
    IEnumerable<string> GetMethods();          // Heal, Damage, etc.
    IEnumerable<string> GetSelectors();        // Target, Self, etc.
    
    bool IsVariableWritable(string name);
    Type GetMethodReturnType(string name);
    Type[] GetMethodParameters(string name);
}
```

### Lo que ScriptLab NO sabe

```csharp
// ❌ ScriptLab NO tiene esto:
public interface IUnit { }           // Concepto de WW
public interface IBattleContext { }  // Concepto de WW
public class Buff { }                // Concepto de WW

// ScriptLab solo ve:
// - "HP" → un string que el contexto debe resolver
// - "Heal(50)" → una llamada que el contexto debe ejecutar
// - "#Target" → un selector que el contexto debe devolver
```

---

## 📜 ScriptAsset (ScriptableObject del Plugin)

```csharp
// Este es el ScriptableObject que vive en ScriptLab
// NO conoce ningún proyecto específico

[CreateAssetMenu(menuName = "ScriptLab/Script")]
public class ScriptAsset : ScriptableObject
{
    [TextArea(5, 20)]
    public string Code = "";
    
    // Ejecutar con un contexto inyectado
    public void Execute(IScriptContext context)
    {
        ScriptRunner.Execute(Code, context);
    }
    
    // Ejecutar un bloque específico
    public void ExecuteBlock(string blockName, IScriptContext context)
    {
        ScriptRunner.ExecuteBlock(Code, blockName, context);
    }
}
```

---

## 🔄 Flujo de Ejecución

### 1. ScriptLab parsea (sin saber qué significa nada)

```csharp
// ScriptLab ve el texto:
@start
    $healPerTick = 5 + BDY * 0.2
    Heal($healPerTick)
;

// Y lo convierte en instrucciones genéricas:
Block("start") {
    SetLocal("$healPerTick", Expression(5 + Identifier("BDY") * 0.2))
    Call("Heal", LocalVar("$healPerTick"))
}
```

### 2. El proyecto inyecta el contexto

```csharp
// En Wonder Wilds (o cualquier proyecto):
var context = new MiContextoPersonalizado(misObjetos);
scriptAsset.ExecuteBlock("start", context);
```

### 3. ScriptLab ejecuta, preguntando al contexto

```csharp
// Internamente ScriptLab hace:

// Para "BDY":
context.TryGetValue("BDY", out float bdy);  // → 15.0

// Para "Heal($healPerTick)":
context.Invoke("Heal", healPerTick);  // El contexto ejecuta su lógica
```

---

## 🏷️ Sintaxis (Lo único que ScriptLab conoce)

### Prefijos y su significado

| Prefijo | Significado | Resuelto por |
|---------|-------------|--------------|
| `@block` | Inicio de bloque | ScriptLab (estructura) |
| `$var` | Variable local del script | ScriptLab (memoria local) |
| `identifier` | Identificador externo | `context.TryGetValue()` |
| `#Selector` | Selector de entidad | `context.GetSelector()` |
| `Method()` | Llamada a método | `context.Invoke()` |
| `;` | Fin de bloque | ScriptLab (estructura) |
| `?` `:?` `:` | Condicional if/elseif/else | ScriptLab (flujo) |

### Ejemplo de Script (sintaxis pura, sin significado semántico)

```javascript
// ScriptLab solo ve estructura, no sabe qué es "HP" o "Heal"

@start                          // Bloque "start"
    $localVar = 5 + foo * 0.2   // $localVar = local, "foo" = pregunta al contexto
    DoSomething($localVar)      // Invocar "DoSomething" en contexto
;                               // Fin del bloque

@tick                           // Otro bloque
    ? bar < baz * 0.5           // Condición: contexto resuelve "bar" y "baz"
        #Entity.Action(10)      // Selector "Entity", invocar "Action"
    ;
;
```

### Acceso a propiedades de selectores

```javascript
#Selector.property        // context.GetSelector("Selector").TryGetValue("property")
#Selector.Method(args)    // context.GetSelector("Selector").Invoke("Method", args)
```

### Bloques y flujo

```javascript
// Bloque simple
@start
    // código
;

// Bloque con parámetro (para hooks con data)
@onDamageTaken($amount)
    ? $amount > 50
        RemoveSelf()
    ;
;

// Condicional
? HP < MaxHP * 0.5
    // si HP < 50%
:? HP < MaxHP * 0.25
    // else if HP < 25%
:
    // else
;
```

---

## 📐 Diferenciación de Variables (Decisión de Diseño)

### Problema
¿Cómo distinguir variables locales del script vs identificadores del contexto?

### Solución: Prefijo `$` para locales

```javascript
$myVar = 10          // SIEMPRE local (ScriptLab la maneja)
duration = 5         // SIEMPRE contexto (context.TrySetValue)
foo                  // SIEMPRE contexto (context.TryGetValue)
```

**Reglas claras**:
1. `$nombre` → Variable local, ScriptLab la almacena internamente
2. `nombre` sin prefijo → ScriptLab pregunta al contexto
3. Si el contexto no tiene `nombre` → Error o warning

---

## 🧩 Cómo un Proyecto se Conecta (Ejemplo Abstracto)

El proyecto crea su propio contexto implementando `IScriptContext`:

```csharp
// EN EL PROYECTO (no en ScriptLab)

public class MiContexto : IScriptContext
{
    private Dictionary<string, Func<float>> _getters = new();
    private Dictionary<string, Action<float>> _setters = new();
    private Dictionary<string, Func<object[], object>> _methods = new();
    private Dictionary<string, Func<IScriptContext>> _selectors = new();
    
    public MiContexto()
    {
        // Registrar lo que el script puede acceder
        _getters["duration"] = () => _miObjeto.Duracion;
        _setters["duration"] = (v) => _miObjeto.Duracion = v;
        
        _getters["value1"] = () => _datos.Value1;
        _getters["value2"] = () => _datos.Value2;
        
        _methods["DoAction"] = (args) => { _miObjeto.HacerAlgo((float)args[0]); return null; };
        
        _selectors["Other"] = () => new OtroContexto(_otroObjeto);
    }
    
    public bool TryGetValue(string name, out float value)
    {
        if (_getters.TryGetValue(name, out var getter))
        {
            value = getter();
            return true;
        }
        value = 0;
        return false;
    }
    
    public bool TrySetValue(string name, float value)
    {
        if (_setters.TryGetValue(name, out var setter))
        {
            setter(value);
            return true;
        }
        return false;
    }
    
    public object Invoke(string name, params object[] args)
    {
        if (_methods.TryGetValue(name, out var method))
        {
            return method(args);
        }
        throw new Exception($"Method '{name}' not found");
    }
    
    public IScriptContext GetSelector(string name)
    {
        if (_selectors.TryGetValue(name, out var selector))
        {
            return selector();
        }
        return null;
    }
}
```

### Uso

```csharp
// El proyecto ejecuta el script con su contexto
var script = Resources.Load<ScriptAsset>("MiScript");
var context = new MiContexto(misObjetos);

script.ExecuteBlock("start", context);
```

---

## 🛠️ Schema (Para Validación en Editor)

El Schema es **opcional** y solo sirve para que el editor pueda:
- Autocompletar identificadores válidos
- Validar que los métodos existen
- Mostrar errores de sintaxis vs errores semánticos

```csharp
// El proyecto puede proveer un schema para el editor
public class MiSchema : IScriptSchema
{
    public IEnumerable<string> GetBlocks() => new[] { "start", "tick", "end" };
    public IEnumerable<string> GetVariables() => new[] { "duration", "value1", "value2" };
    public IEnumerable<string> GetMethods() => new[] { "DoAction", "Log" };
    public IEnumerable<string> GetSelectors() => new[] { "Other", "All" };
    
    public bool IsVariableWritable(string name) => name == "duration";
    // etc.
}
```

**Sin schema**: El editor solo valida sintaxis (bloques cerrados, expresiones válidas).
**Con schema**: El editor también valida que los identificadores existan.

---

## 📋 Resumen: Separación de Responsabilidades

```
┌─────────────────────────────────────────────────────────────────────┐
│                       ScriptLab (Plugin)                            │
│                                                                      │
│  Responsabilidades:                                                  │
│  ✅ Parsear texto → tokens → AST                                    │
│  ✅ Entender sintaxis: @blocks, $locals, expresiones, #selectors    │
│  ✅ Ejecutar instrucciones llamando al IScriptContext               │
│  ✅ Manejar variables locales ($var)                                │
│  ✅ Evaluar expresiones matemáticas                                 │
│  ✅ Control de flujo (?, :?, :, ;)                                  │
│                                                                      │
│  NO sabe:                                                            │
│  ❌ Qué significa "HP", "duration", "Heal"                          │
│  ❌ Qué es un Buff, Unit, Battle                                    │
│  ❌ Nada del proyecto que lo usa                                    │
│                                                                      │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────┐   │
│  │   Parser     │ →  │   AST        │ →  │   Executor           │   │
│  │  (sintaxis)  │    │ (estructura) │    │  (llama contexto)    │   │
│  └──────────────┘    └──────────────┘    └──────────────────────┘   │
│                                                    ↓                 │
│                                          IScriptContext              │
│                                          (interfaz genérica)         │
└─────────────────────────────────────────────────────────────────────┘
                                  ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    Proyecto (Wonder Wilds, etc.)                    │
│                                                                      │
│  Responsabilidades:                                                  │
│  ✅ Implementar IScriptContext con sus datos reales                 │
│  ✅ Definir qué significa cada identificador                        │
│  ✅ Ejecutar la lógica real cuando se invoca un método              │
│  ✅ Proveer IScriptSchema para validación en editor (opcional)      │
│                                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│  │ BuffContext  │  │ SkillContext │  │ OtroContext  │  ...          │
│  │ : IScript    │  │ : IScript    │  │ : IScript    │               │
│  │   Context    │  │   Context    │  │   Context    │               │
│  └──────────────┘  └──────────────┘  └──────────────┘               │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## ❓ Decisiones de Diseño Pendientes

1. **¿Bloques con parámetros?**
   - `@onEvent($param)` → ScriptLab recibiría el valor y lo pondría en `$param`
   - Alternativa: contexto pone valores en variables reservadas

2. **¿Soporte para async/yield?**
   - `Wait(1.0)` → ¿Cómo manejar sin conocer el proyecto?
   - Posible: devolver instrucción especial que el proyecto interpreta

3. **¿Tipos de datos?**
   - Simple: todo es `float` o `string`
   - Complejo: sistema de tipos genérico

4. **¿Funciones built-in?**
   - `min()`, `max()`, `clamp()`, `random()` → ¿Las provee ScriptLab?
   - O todo lo provee el contexto

5. **¿Compilación vs Interpretación?**
   - Interpretar cada vez (simple, flexible)
   - Compilar a bytecode (performance, complejidad)

---

## 🚀 Estructura del Plugin

```
Assets/Plugins/xavierarpa/ScriptLab/
├── Runtime/
│   ├── Core/
│   │   ├── IScriptContext.cs        # Interfaz que implementa el proyecto
│   │   ├── IScriptSchema.cs         # Interfaz para validación (opcional)
│   │   └── ScriptAsset.cs           # ScriptableObject con el texto
│   ├── Parsing/
│   │   ├── Tokenizer.cs             # Texto → Tokens
│   │   ├── Parser.cs                # Tokens → AST
│   │   └── Nodes/                   # Nodos del AST
│   │       ├── BlockNode.cs
│   │       ├── ExpressionNode.cs
│   │       ├── AssignmentNode.cs
│   │       ├── ConditionNode.cs
│   │       └── InvokeNode.cs
│   └── Execution/
│       ├── ScriptRunner.cs          # Ejecuta el AST con un contexto
│       └── ExpressionEvaluator.cs   # Evalúa expresiones matemáticas
├── Editor/
│   ├── ScriptAssetEditor.cs         # Inspector con syntax highlighting
│   ├── SyntaxHighlighter.cs         # Colorea el código
│   └── Validator.cs                 # Valida sintaxis (y semántica con schema)
└── package.json                     # Para distribuir como UPM
```

---

## 🚀 Próximos Pasos

1. [ ] Definir `IScriptContext` e `IScriptSchema`
2. [ ] Crear `ScriptAsset` (ScriptableObject básico)
3. [ ] Implementar Tokenizer
4. [ ] Implementar Parser → AST
5. [ ] Implementar ScriptRunner
6. [ ] Crear Editor con syntax highlighting
7. [ ] Crear ejemplo de uso con contexto mock
