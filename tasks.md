# Tasks for NoCommentsAnalyzer implementation

This file tracks the progress of implementing the NoCommentsAnalyzer.

## Implementation Tasks

- [x] Create directory structure for the project
- [x] Create .github/workflows/ci.yml file
- [x] Implement NoCommentsAnalyzer project:
  - [x] NoCommentsAnalyzer.csproj
  - [x] NoCommentsAnalyzer.cs (with proper shibboleth check)
- [x] Create NoCommentsAnalyzer.Test project:
  - [x] NoCommentsAnalyzer.Test.csproj 
  - [x] NoCommentsAnalyzerTests.cs
- [x] Create .gitignore file

## Requirements from README.md

1. Build a Roslyn analyzer that forbids comments that don't have the shibbolet string "[!]" in it
2. Allow XML documentation comments
3. Enforce the analyzer on itself via CI
4. Publish to NuGet only if it passes

## What's Done

- Created the basic project structure for the Roslyn analyzer
- Implemented the analyzer to check for comments without the "[!]" shibboleth
- Excluded XML documentation comments from this check
- Set up the CI pipeline with self-linting
- Added basic tests for the analyzer
- Added NuGet package configuration and publishing to NuGet when merged to main