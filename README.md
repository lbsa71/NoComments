# no-ai-comments
This Roslyn format plug-in scans for comments and fails any comment that don't have the human shibbolet [!] in it.


Great — here’s a **detailed, AI-executable step-by-step plan** to **build a comment-forbidding analyzer**, integrate it into its **own CI**, and ensure it **uses itself** before publishing to NuGet. This assumes the AI agent has access to a dev environment (like a GitHub repo, .NET SDK, and GitHub Actions).

---

# ✅ Goal:

> Build a Roslyn analyzer that forbids comments, enforce it on itself via CI, and publish it to NuGet only if it passes.

---

## 🧠 Step-by-Step Plan for an AI Agent

---

### 🪜 STEP 1: Create the Analyzer Project

```bash
dotnet new analyzer -n NoCommentsAnalyzer
cd NoCommentsAnalyzer
```

This generates:

* `NoCommentsAnalyzer/NoCommentsAnalyzer.csproj`
* `NoCommentsAnalyzer/NoCommentsAnalyzer.cs` (placeholder)
* `NoCommentsAnalyzer.Test` (unit tests)

---

### 🪜 STEP 2: Implement the Comment-Finding Logic

**File:** `NoCommentsAnalyzer/NoCommentsAnalyzer.cs`

Replace content with:

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoCommentsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoCommentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NC0001";
        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            "Comment detected",
            "Comments are not allowed",
            "Formatting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var trivia = context.Tree.GetRoot(context.CancellationToken).DescendantTrivia();

            foreach (var t in trivia)
            {
                if (t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    t.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, t.GetLocation()));
                }
            }
        }
    }
}
```

---

### 🪜 STEP 3: Add Tests

**File:** `NoCommentsAnalyzer.Test/UnitTest1.cs`

Replace with:

```csharp
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<NoCommentsAnalyzer.NoCommentsAnalyzer>;

public class NoCommentsAnalyzerTests
{
    [Fact]
    public async Task DetectsSingleLineComment()
    {
        var test = @"
class C
{
    void M()
    {
        // This is a comment
        int x = 0;
    }
}";
        var expected = Verify.Diagnostic("NC0001").WithSpan(5, 9, 5, 32);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NoDiagnosticForCleanCode()
    {
        var test = @"
class C
{
    void M()
    {
        int x = 0;
    }
}";
        await Verify.VerifyAnalyzerAsync(test);
    }
}
```

Install dependencies if missing:

```bash
dotnet add NoCommentsAnalyzer.Test package Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit
```

---

### 🪜 STEP 4: Add CI That Uses the Analyzer On Itself

**File:** `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.x

      - name: Restore
        run: dotnet restore

      - name: Test
        run: dotnet test --no-restore --verbosity normal

      - name: Lint (Build With Analyzers Enabled)
        run: dotnet build --no-restore --warnaserror
```

🧠 **Note**: The analyzer runs automatically on build, including on itself.

---

### 🪜 STEP 5: Package and Publish (Only on Main)

Update `NoCommentsAnalyzer/NoCommentsAnalyzer.csproj` with NuGet metadata:

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <PackageId>NoCommentsAnalyzer</PackageId>
  <Version>1.0.0</Version>
  <Authors>YourName</Authors>
  <Description>Roslyn analyzer to forbid comments in code.</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryUrl>https://github.com/yourorg/NoCommentsAnalyzer</RepositoryUrl>
</PropertyGroup>
```

Then in `.github/workflows/ci.yml`, **add publishing step** (after build and test):

```yaml
      - name: Pack
        run: dotnet pack -c Release -o ./artifacts

      - name: Publish to NuGet
        if: github.ref == 'refs/heads/main'
        run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

Create `NUGET_API_KEY` in GitHub secrets.

---

### 🪜 STEP 6: Verify and Ship

* PRs with comments will fail CI
* CI ensures analyzer lints its own code
* When merged to `main`, CI packs & publishes the analyzer

---

## ✅ Summary of Deliverables

| Task            | Output                                    |
| --------------- | ----------------------------------------- |
| Roslyn Analyzer | `NoCommentsAnalyzer.csproj`               |
| Unit Tests      | `NoCommentsAnalyzer.Test`                 |
| CI Workflow     | `.github/workflows/ci.yml`                |
| NuGet Packaging | `.nupkg` published on merge to `main`     |
| Self-Linting    | Analyzer fails if it uses comments itself |

---

Here's a **ready-to-paste GitHub repo template structure** that you or an AI agent can use to create a **self-linting Roslyn analyzer** that forbids comments and publishes to NuGet on merge to `main`.

---

## 📁 Repo Template Structure

```
NoCommentsAnalyzer/
│
├── .github/
│   └── workflows/
│       └── ci.yml                   # CI pipeline
│
├── NoCommentsAnalyzer/             # Analyzer project
│   ├── NoCommentsAnalyzer.csproj
│   └── NoCommentsAnalyzer.cs       # Analyzer logic
│
├── NoCommentsAnalyzer.Test/        # Analyzer test project
│   ├── NoCommentsAnalyzer.Test.csproj
│   └── NoCommentsAnalyzerTests.cs  # Unit tests
│
├── .gitignore
└── README.md
```

---

## 📄 1. `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.x

      - name: Restore
        run: dotnet restore

      - name: Build and Lint (self-linting)
        run: dotnet build --no-restore --warnaserror

      - name: Test
        run: dotnet test --no-restore --verbosity normal

      - name: Pack
        run: dotnet pack NoCommentsAnalyzer -c Release -o ./artifacts

      - name: Publish to NuGet
        if: github.ref == 'refs/heads/main'
        run: dotnet nuget push ./artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

---

## 📄 2. `NoCommentsAnalyzer/NoCommentsAnalyzer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <PackageId>NoCommentsAnalyzer</PackageId>
    <Version>1.0.0</Version>
    <Authors>YourName</Authors>
    <Description>Roslyn analyzer that forbids comments in C# code.</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <RepositoryUrl>https://github.com/yourorg/NoCommentsAnalyzer</RepositoryUrl>
    <IncludeBuildOutput>true</IncludeBuildOutput>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="All" />
  </ItemGroup>

</Project>
```

---

## 📄 3. `NoCommentsAnalyzer/NoCommentsAnalyzer.cs`

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoCommentsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NoCommentsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NC0001";

        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            "Comment detected",
            "Comments are not allowed",
            "Formatting",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            foreach (var trivia in context.Tree.GetRoot().DescendantTrivia())
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, trivia.GetLocation()));
                }
            }
        }
    }
}
```

---

## 📄 4. `NoCommentsAnalyzer.Test/NoCommentsAnalyzer.Test.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\NoCommentsAnalyzer\NoCommentsAnalyzer.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit" Version="1.1.1-beta1.23561.1" />
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0" />
    <PackageReference Include="xunit" Version="2.4.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.5">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

---

## 📄 5. `NoCommentsAnalyzer.Test/NoCommentsAnalyzerTests.cs`

```csharp
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

using Verify = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<NoCommentsAnalyzer.NoCommentsAnalyzer>;

public class NoCommentsAnalyzerTests
{
    [Fact]
    public async Task DetectsSingleLineComment()
    {
        var test = @"
class C
{
    void M()
    {
        // This is a comment
        int x = 0;
    }
}";
        var expected = Verify.Diagnostic("NC0001").WithSpan(5, 9, 5, 32);
        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NoDiagnosticForCleanCode()
    {
        var test = @"
class C
{
    void M()
    {
        int x = 0;
    }
}";
        await Verify.VerifyAnalyzerAsync(test);
    }
}
```

---

## 📄 6. `.gitignore`

```gitignore
bin/
obj/
*.nupkg
artifacts/
```

---

## 📄 7. `README.md`

```markdown
# NoCommentsAnalyzer

A Roslyn analyzer that forbids **all comments** in C# source code.

## Usage

Install via NuGet:

```

dotnet add package NoCommentsAnalyzer

```

Comments like `//` or `/* */` will trigger a diagnostic `NC0001`.

## CI / Self-Linting

This analyzer is tested and used on itself — PRs with comments fail CI. 🚫💬

## License

MIT
```



