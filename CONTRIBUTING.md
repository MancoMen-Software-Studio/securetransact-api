# Contributing to SecureTransact API

Thank you for your interest in contributing to SecureTransact! This document provides guidelines and instructions for contributing.

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## How to Contribute

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When creating a bug report, include:

- A clear, descriptive title
- Steps to reproduce the behavior
- Expected behavior
- Actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant logs or error messages

### Suggesting Features

Feature requests are welcome. Please provide:

- A clear description of the feature
- The problem it solves
- Potential implementation approach (if you have ideas)
- Any alternatives you've considered

### Pull Requests

1. **Fork the repository** and create your branch from `develop`
2. **Follow the branching convention**: `feature/SEC-XXX-description` or `bugfix/SEC-XXX-description`
3. **Write tests** for any new functionality
4. **Ensure all tests pass**: `dotnet test`
5. **Follow code style**: Run `dotnet format` before committing
6. **Write clear commit messages** following [Conventional Commits](https://www.conventionalcommits.org/)
7. **Update documentation** if needed
8. **Submit your PR** against the `develop` branch

## Development Setup

### Prerequisites

- .NET 9 SDK
- Docker (for PostgreSQL and Redis)
- Your preferred IDE (Rider, VS Code, Visual Studio)

### Local Development

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/Secure-Transact-API.git
cd Secure-Transact-API

# Add upstream remote
git remote add upstream https://github.com/MancoMen-Software-Studio/Secure-Transact-API.git

# Start dependencies
docker-compose up -d

# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project src/SecureTransact.Api
```

## Code Standards

### Architecture Rules

1. **Domain Layer has ZERO external dependencies** - Only .NET BCL
2. **Application Layer references only Domain** - Never Infrastructure
3. **All dependencies point inward** - Outer layers depend on inner layers

### Naming Conventions

- **Classes/Interfaces**: PascalCase (`TransactionAggregate`, `IEventStore`)
- **Methods**: PascalCase (`ProcessTransaction`, `GetByIdAsync`)
- **Properties**: PascalCase (`AccountId`, `Amount`)
- **Private fields**: _camelCase (`_repository`, `_logger`)
- **Parameters/locals**: camelCase (`transactionId`, `amount`)

### Code Style

- Use explicit types (no `var` except when type is obvious)
- Use file-scoped namespaces
- One class per file
- XML documentation on all public APIs
- No empty catch blocks
- No TODO comments in PRs to main

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`

Examples:
- `feat(transactions): add transaction reversal capability`
- `fix(crypto): resolve nonce reuse vulnerability`
- `docs(readme): update installation instructions`
- `test(domain): add unit tests for Money value object`

## Testing Requirements

### Coverage Thresholds

| Layer | Minimum |
|-------|---------|
| Domain | 90% |
| Application | 85% |
| Infrastructure | 70% |
| API | 60% |

### Test Types

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions with real dependencies (via Testcontainers)
- **Architecture Tests**: Verify layer dependencies are correct

## Review Process

1. All PRs require at least one approval
2. CI must pass (build, tests, linting)
3. No merge conflicts with target branch
4. Conversations must be resolved

## Questions?

Feel free to open an issue for any questions about contributing.

---

Thank you for contributing to SecureTransact! 🎉
