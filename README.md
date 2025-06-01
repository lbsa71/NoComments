# NoCommentsAnalyzer

A Roslyn analyzer that enforces intentional commenting practices in C# code by requiring explicit markers for human-written comments.

## What it does

This analyzer scans your C# code for comments and reports a diagnostic `NC0001` for any comment that doesn't meet the refined detection rules. This helps ensure that comments in your codebase are intentionally written by humans and follow established patterns.

## 🔍 Refined Detection Rules

### ✅ Allowed Comments

1. **XML documentation comments** (`///`, `/** */` style) — these are always allowed for API documentation.

2. **File-level banner comments** at the **very top** of the file, **if**:
   - They are multi-line `/* ... */` blocks or consecutive `//` lines
   - Appear before any namespace or using directive
   - Start with standard license phrases like `Copyright`, `Licensed`, `SPDX-License-Identifier`

3. **Explicitly intentional comments**, which include one of these markers:
   - `// HUMAN:` — Clear, semantic indicator of intentional human input
   - `// NOTE:` — Already somewhat idiomatic, allows useful notes
   - `// INTENT:` — Emphasizes deliberation behind the comment
   - `// OK:` — Easy, minimal disruption, assertive marker
   - `// [!]` — Legacy marker (still supported for backward compatibility)

4. **Suppressions or TODOs** using standard patterns:
   - `// TODO: ...`
   - `// HACK: ...`
   - `// FIXME: ...`

### ❌ Flagged Comments

- Any inline or block comment **not matching** the above rules
- Example: `// this is a regular comment` ➜ triggers `NC0001`

## Examples

### ✅ Allowed Comments

```csharp
/*
 * Copyright (c) 2025 Your Company
 * Licensed under MIT License
 */

using System;

namespace MyProject
{
    /// <summary>
    /// This XML documentation is always allowed
    /// </summary>
    public class Example
    {
        public void Method()
        {
            // HUMAN: This comment was deliberately written by a human
            Console.WriteLine("Hello");
            
            // NOTE: This explains an important detail
            var result = Calculate();
            
            // TODO: Implement error handling
            ProcessResult(result);
            
            // OK: Simple acknowledgment comment
            return;
        }
    }
}
```

### ❌ Flagged Comments

```csharp
public void Method()
{
    // This comment will trigger NC0001
    Console.WriteLine("Hello");
    
    /* This comment will also trigger NC0001 */
    var x = 42;
}

## Installation

Install via NuGet:

```bash
dotnet add package NoCommentsAnalyzer
```

## Usage

Once installed, the analyzer will automatically run during compilation and report warnings for any unauthorized comments.

## Configuration

The analyzer runs with default severity of `Warning`. You can configure it in your `.editorconfig` or project file:

```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.NC0001.severity = error

# Enable/disable features (default: all true)
nocomments_analyzer.enable_intentional_markers = true
nocomments_analyzer.enable_suppression_patterns = true
nocomments_analyzer.enable_license_banner = true
nocomments_analyzer.enable_xml_documentation = true

# Disable analyzer for specific files
nocomments_analyzer.disable_for_file = false

# Customize allowed patterns (comma-separated)
nocomments_analyzer.intentional_markers = HUMAN:,NOTE:,INTENT:,OK:,[!]
nocomments_analyzer.suppression_patterns = TODO:,HACK:,FIXME:
nocomments_analyzer.license_patterns = Copyright,Licensed,SPDX-License-Identifier
```

### Feature Configuration

- **`enable_intentional_markers`**: Enable/disable checking for intentional comment markers
- **`enable_suppression_patterns`**: Enable/disable checking for TODO/HACK/FIXME patterns  
- **`enable_license_banner`**: Enable/disable checking for license banner comments
- **`enable_xml_documentation`**: Enable/disable XML documentation comment detection
- **`disable_for_file`**: Disable the analyzer entirely for specific files

### Inline Disabling

You can disable the analyzer for individual comments using the inline directive:

```csharp
// nocomments:disable This comment will be ignored by the analyzer
var x = 42;
```

## License

MIT