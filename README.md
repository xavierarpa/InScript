![InScript](https://img.shields.io/badge/InScript-Scripting%20for%20Unity-purple?style=for-the-badge&logo=unity)

InScript - Lightweight Scripting for Unity
===

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black.svg)](https://unity3d.com/pt/get-unity/download/archive)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-blueviolet)](https://makeapullrequest.com)

A lightweight, designer-friendly scripting system for Unity. Define game logic using a simple custom syntax with real-time syntax highlighting in the Inspector.

## ✨ Features

- 📝 **Simple Syntax** - Easy-to-learn DSL designed for game designers
- 🎨 **Syntax Highlighting** - Real-time colored preview in the Unity Inspector
- 🔢 **Line Numbers** - Visual Studio-like line number gutter
- 🎯 **Attribute-Based Binding** - Connect scripts to your code with simple attributes
- 📦 **Inline or Asset** - Use scripts embedded in components or as reusable ScriptableObjects
- ✅ **Live Validation** - Instant error detection as you type
- 🔧 **Zero Dependencies** - Pure C#, no external libraries required

## 📦 Installation

### Via Git URL (Package Manager)

1. Open Package Manager in Unity (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL...`
3. Enter the following URL:

```bash
https://github.com/xavierarpa/InScript.git
```

### Manual Installation

1. Download or clone this repository
2. Copy the `InScript` folder into your `Assets/Plugins` folder

## 🚀 Quick Start

### 1. Create a Script Context

Add attributes to expose fields and methods to scripts:

```csharp
using UnityEngine;
using InScript;

public class Character : MonoBehaviour
{
    [SerializeField, InScript] private float hp = 100;
    [SerializeField, InScript] private float maxHp = 100;
    [SerializeField, InScript] private float attack = 10;
    
    [SerializeField, InScript("Target")] private Character target;
    
    [InScript]
    private void Heal(float amount)
    {
        hp = Mathf.Min(hp + amount, maxHp);
    }
    
    [InScript]
    private void Log(string message, float value)
    {
        Debug.Log($"{message}: {value}");
    }
}
```

### 2. Write a Script

Create a `ScriptAsset` or use an inline `Script` field:

```
@main
    $damage = attack * 1.5
    
    ? hp < maxHp * 0.5
        Log("Low HP!", hp)
        Heal(20)
    ;
    
    #Target.TakeDamage($damage)
;
```

### 3. Execute

```csharp
[SerializeField] private Script script;

void Start()
{
    script.ExecuteBlock("main", this);
}
```

## 📖 Syntax Reference

### Blocks
```
@blockName          // Start a named block
    // code here
;                   // End block
```

### Variables
```
$localVar = 10              // Local variable (script scope)
$localVar += 5              // Compound assignment (+=, -=, *=, /=)
contextVar = value          // Context variable (from your code)
```

### Selectors
```
#Target.hp                  // Access selector property
#Target.TakeDamage(10)      // Call selector method
```

### Conditionals
```
? condition                 // If
    // code
:? otherCondition           // Else if
    // code
:                           // Else
    // code
;                           // End if
```

### Built-in Functions

#### Math Functions
| Function | Description | Example |
|----------|-------------|---------|
| `min(a, b)` | Returns the smaller of two values | `min(hp, maxHp)` → `50` if hp=50, maxHp=100 |
| `max(a, b)` | Returns the larger of two values | `max(0, damage - armor)` → prevents negative |
| `clamp(value, min, max)` | Constrains a value between min and max | `clamp(hp, 0, maxHp)` → keeps hp in valid range |
| `abs(x)` | Returns the absolute (positive) value | `abs(-5)` → `5` |
| `sign(x)` | Returns -1, 0, or 1 based on sign | `sign(-10)` → `-1` |

#### Rounding Functions
| Function | Description | Example |
|----------|-------------|---------|
| `floor(x)` | Rounds down to nearest integer | `floor(3.7)` → `3` |
| `ceil(x)` | Rounds up to nearest integer | `ceil(3.2)` → `4` |
| `round(x)` | Rounds to nearest integer | `round(3.5)` → `4` |

#### Advanced Functions
| Function | Description | Example |
|----------|-------------|---------|
| `sqrt(x)` | Square root | `sqrt(16)` → `4` |
| `pow(base, exp)` | Power/exponent | `pow(2, 3)` → `8` |
| `lerp(a, b, t)` | Linear interpolation between a and b | `lerp(0, 100, 0.5)` → `50` |
| `random(min, max)` | Random float between min and max | `random(1, 10)` → `5.7` (varies) |
| `random()` | Random float between 0 and 1 | `random()` → `0.42` (varies) |

### Operators
| Type | Operators |
|------|-----------|
| Arithmetic | `+` `-` `*` `/` |
| Comparison | `==` `!=` `<` `>` `<=` `>=` |

## 🏗️ Architecture

```
InScript/
├── Runtime/
│   ├── Script.cs              # Serializable script container
│   ├── ScriptAsset.cs         # ScriptableObject wrapper
│   ├── ScriptRunner.cs        # Script execution engine
│   ├── IScriptContext.cs      # Context interface
│   ├── ReflectionContext.cs   # Attribute-based context
│   └── Attributes/
│       └── InScriptAttribute.cs # Unified attribute for values/selectors/methods
└── Editor/
    ├── ScriptDrawer.cs           # PropertyDrawer with syntax highlighting
    ├── ScriptAssetEditor.cs      # Custom editor for ScriptAsset
    ├── ScriptDebugWindow.cs      # Debug panel for testing scripts
    └── SyntaxReferenceWindow.cs  # Dockable syntax documentation
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## 👤 Author

**Xavier Arpa** - [@xavierarpa](https://github.com/xavierarpa)
