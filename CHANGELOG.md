# Changelog

All notable changes to this package will be documented in this file.

## [1.1.0] - 2026-01-29

### Added
- **Debug Panel** (`ScriptDebugWindow`):
  - Visual debugging tool for testing scripts
  - Block execution with pre-initialized local variables
  - Live variable editing (context and local)
  - Selector inspection
  - Works in both Editor and Play mode
  - Script source identification (object name + field name)
  - Refresh button to reload script from context
- **Built-in Log function**: `Log("message")` prints to Unity Console with `[InScript]` prefix
- **Debug button** (🔧) in Script PropertyDrawer header
- **Syntax button** (❔) in Script PropertyDrawer header

### Changed
- **BREAKING**: Unified attributes into single `[InScript]` attribute
  - Removed `[ScriptValue]`, `[ScriptMethod]`, `[ScriptSelector]`
  - Automatic type detection: primitives→values, objects→selectors, methods→methods
  - Simpler API: `[InScript]` or `[InScript("alias")]`
- **BREAKING**: Removed `readOnly` parameter (unnecessary - use C# readonly if needed)
- Improved Script execution to accept initial local variables dictionary
- Optimized PropertyDrawer header buttons to icon size (28px width)
- Enhanced `ReflectionContext` with automatic type detection

### Fixed
- Debug window now correctly passes local variables when executing blocks
- Refresh button properly reloads script from context
- Local variables persist their values between block executions in Debug panel
- Warning messages now reference correct `[InScript]` attribute

## [1.0.0] - 2026-01-28

### Added
- **Script class**: Serializable script container that can be embedded inline or used in ScriptAssets
- **ScriptAsset**: ScriptableObject wrapper for reusable scripts
- **ScriptRunner**: Core execution engine with support for blocks, conditionals, and expressions
- **Attribute-based context binding**:
  - `[ScriptValue]` - Expose fields/properties as script variables
  - `[ScriptMethod]` - Expose methods as callable functions
  - `[ScriptSelector]` - Expose fields as sub-contexts (accessed with #Name)
- **ReflectionContext**: Automatic context creation from attributed objects
- **ScriptDrawer**: PropertyDrawer with:
  - Real-time syntax highlighting
  - Line numbers
  - Split view (preview + editor)
  - Live error validation
  - Syntax Reference button
- **SyntaxReferenceWindow**: Dockable editor window with:
  - Searchable syntax documentation
  - Categorized sections with icons
  - Examples for each function
  - Alternating section backgrounds
- **Built-in functions**:
  - Math: `min`, `max`, `clamp`, `abs`, `sign`
  - Rounding: `floor`, `ceil`, `round`
  - Advanced: `sqrt`, `pow`, `lerp`, `random(min, max)`, `random()`
- **Custom syntax**:
  - `@block` - Named code blocks
  - `$var` - Local variables
  - `#Selector` - Sub-context access
  - `? :? :` - Conditionals
  - `;` - Block/conditional terminator

### Features
- Designer-friendly syntax
- Zero external dependencies
- Works with Unity 2021.3+
- Full MIT license
