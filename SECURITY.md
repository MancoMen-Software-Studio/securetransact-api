# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | YES|
| < 1.0   | NO             |

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability, please report it responsibly.

### How to Report

**Do NOT create a public GitHub issue for security vulnerabilities.**

Instead, please email us at: **security@mancomen.com**

Include the following information:

- Type of vulnerability (e.g., SQL injection, XSS, authentication bypass)
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Resolution Target**: Within 30 days (depending on complexity)

### What to Expect

1. We will acknowledge your report within 48 hours
2. We will investigate and keep you informed of our progress
3. We will work with you to understand and resolve the issue
4. Once resolved, we will publicly acknowledge your contribution (unless you prefer to remain anonymous)

## Security Measures

This project implements several security measures:

- **Cryptographic Signing**: All transactions are signed with HMAC-SHA256
- **Secure Token Generation**: CSPRNG-based token generation
- **Hash Chaining**: Event store integrity verification
- **Secret Management**: All secrets stored in Azure Key Vault
- **Dependency Scanning**: Automated via Dependabot and CodeQL
- **Authentication**: JWT with RS256 signing

## Best Practices for Users

- Never commit secrets or API keys to the repository
- Use environment variables or Azure Key Vault for sensitive configuration
- Keep dependencies updated
- Enable branch protection rules
- Review security advisories regularly

## Security Updates

Security updates will be released as patch versions and announced via:

- GitHub Security Advisories
- Release notes in CHANGELOG.md

Thank you for helping keep SecureTransact and our users safe!
