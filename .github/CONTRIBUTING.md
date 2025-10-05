# Contributing to CleanBin

Thank you for your interest in contributing to CleanBin! This document provides guidelines and information for contributors.

## Development Setup

### Prerequisites
- .NET 8 SDK or later
- Visual Studio 2022 or VS Code
- Git

### Getting Started
1. Fork the repository
2. Clone your fork locally
3. Create a feature branch
4. Make your changes
5. Run tests locally
6. Submit a pull request

### Building the Project
```bash
# Restore dependencies
dotnet restore Utilites.sln

# Build the solution
dotnet build Utilites.sln --configuration Release

# Run tests
dotnet test Utilites.sln --configuration Release
```

## Code Style Guidelines

### C# Coding Standards
- Use PascalCase for public members
- Use camelCase for private fields and local variables
- Use meaningful names for variables and methods
- Add XML documentation for public APIs
- Follow the existing code style in the project

### Code Formatting
- Use 4 spaces for indentation
- Use meaningful variable and method names
- Add comments for complex logic
- Keep methods small and focused

## Testing Guidelines

### Unit Tests
- Write unit tests for all public methods
- Aim for high code coverage (>80%)
- Use descriptive test names
- Follow the Arrange-Act-Assert pattern

### Integration Tests
- Test complete workflows
- Use real file system operations where appropriate
- Clean up test data after tests

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "ClassName=PathValidatorTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Pull Request Process

### Before Submitting
1. Ensure all tests pass
2. Update documentation if needed
3. Follow the coding standards
4. Add appropriate tests for new features

### Pull Request Template
Use the provided pull request template to ensure all necessary information is included.

### Review Process
- All pull requests require review
- Address feedback promptly
- Keep pull requests focused and small
- Update documentation as needed

## Issue Reporting

### Bug Reports
Use the bug report template and include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Logs if applicable

### Feature Requests
Use the feature request template and include:
- Clear description of the feature
- Use case and motivation
- Alternative solutions considered
- Priority level

## Release Process

### Version Bumping
- Use semantic versioning (MAJOR.MINOR.PATCH)
- Update version in all project files
- Create appropriate git tags

### Release Notes
- Document new features
- List bug fixes
- Note breaking changes
- Include migration instructions if needed

## CI/CD Pipeline

### Automated Checks
- Code compilation
- Unit and integration tests
- Code coverage analysis
- Security scanning
- Code quality checks

### Manual Triggers
- Release creation
- Version bumping
- Security scans

## Security

### Reporting Security Issues
- Do not create public issues for security vulnerabilities
- Contact maintainers directly
- Provide detailed information about the issue

### Security Best Practices
- Keep dependencies updated
- Follow secure coding practices
- Use appropriate error handling
- Validate all inputs

## Documentation

### Code Documentation
- Add XML documentation for public APIs
- Keep comments up to date
- Explain complex algorithms
- Provide usage examples

### User Documentation
- Update README for new features
- Add examples for new functionality
- Keep installation instructions current
- Document configuration options

## Community Guidelines

### Code of Conduct
- Be respectful and inclusive
- Focus on constructive feedback
- Help others learn and grow
- Follow the project's values

### Communication
- Use clear and concise language
- Provide context for questions
- Be patient with newcomers
- Share knowledge and experience

## Getting Help

### Resources
- Check existing issues and discussions
- Review the documentation
- Ask questions in discussions
- Contact maintainers if needed

### Questions
- Use the discussions section for questions
- Provide context and details
- Search existing discussions first
- Be specific about your issue

Thank you for contributing to CleanBin!