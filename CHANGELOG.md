# Changelog

All notable changes to this package will be documented in this file.

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
