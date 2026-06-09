# Rule: Formatting, Syntax, and Coding Style Standards

## Metadata
- **ID**: RULE-001-FORMATTING-SYNTAX
- **Scope**: Entire Solution
- **Target Language**: C# (.NET 10)
- **Status**: Active

## Overview
This rule outlines the syntax, formatting, indentation, and structural conventions of C# files across the solution. It ensures code readability and visual consistency across all layers.

---

## 1. Indentation & Spacing
- **Indentation**: Use **4 spaces** (never use physical tabs).
- **Line Endings**: LF or CRLF (consistent per file). Ensure a single blank line at the end of every file.
- **Max Line Length**: Limit lines to approximately 120 characters where possible.

---

## 2. Braces and Whitespace
- **Brace Style**: Use **Allman style** (braces on new lines) for classes, namespaces, methods, and control statements.
- **Empty Lines**:
  - One blank line between properties, methods, and constructor definitions.
  - No double empty lines inside methods.
  - A blank line after `using` directives.

### Example:
```csharp
namespace Blog.Domain.Entities;

public class Sample
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public void DoSomething()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return;
        }

        // Logic here
    }
}
```

---

## 3. Namespace Style
- Always use **file-scoped namespaces** to reduce nesting depth.
- Example:
  ```csharp
  namespace Blog.Domain.Entities;
  ```

---

## 4. Language Features and Types
- **Nullable Reference Types**: Enable Nullable context globally (`#nullable enable` or `<Nullable>enable</Nullable>` in `.csproj`). All nullable reference types must be explicitly marked with `?`.
- **Records**: Use positional `record` types for immutable Data Transfer Objects (DTOs), requests, commands, and queries.
  ```csharp
  public record UserDto(Guid Id, string Email, string DisplayName);
  ```
- **Target-Typed New**: Use target-typed new expressions (`new()`) when the type is obvious.
  ```csharp
  List<string> tags = new();
  ```
- **Implicit Usings**: Enabled globally. Do not include standard system imports unless required. Maintain using directives at the top of the file, organized alphabetically.

---

## 5. Naming Conventions
- **Classes / Records / Interfaces**: PascalCase. Interfaces must start with `I`.
- **Methods / Properties**: PascalCase.
- **Private Fields**: camelCase with an underscore prefix (`_value`).
- **Local Variables / Parameters**: camelCase.
- **Constants**: UPPER_SNAKE_CASE.
