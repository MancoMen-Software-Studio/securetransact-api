# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure with Clean Architecture
- Domain layer with core aggregates and value objects
- Application layer with CQRS handlers
- Infrastructure layer with Event Store implementation
- API layer with Minimal API endpoints
- GitHub Actions CI/CD pipeline
- Docker development environment
- Documentation and architecture guides

### Security
- HMAC-SHA256 transaction signing
- CSPRNG-based secure token generation
- Hash-chained event store for tamper detection
- JWT authentication with RS256

---

## Version History

### [0.1.0] - Unreleased

Initial release with core functionality.

[Unreleased]: https://github.com/MancoMen-Software-Studio/Secure-Transact-API/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/MancoMen-Software-Studio/Secure-Transact-API/releases/tag/v0.1.0
