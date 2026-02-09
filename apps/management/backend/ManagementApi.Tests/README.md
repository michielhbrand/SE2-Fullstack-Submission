# ManagementApi Tests

This project contains comprehensive unit and integration tests for the Management API.

## Test Structure

```
ManagementApi.Tests/
├── Helpers/                    # Test utilities and helpers
│   ├── TestWebApplicationFactory.cs
│   └── TestDataBuilder.cs
├── UnitTests/                  # Unit tests
│   ├── Validators/            # Validator tests
│   ├── Mappers/               # Mapper tests
│   └── Services/              # Service tests
└── IntegrationTests/          # Integration tests
    ├── AuthEndpointsTests.cs
    ├── OrganizationEndpointsTests.cs
    └── UserEndpointsTests.cs
```

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run with code coverage
```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~CreateUserRequestValidatorTests"
```

### Run tests by category
```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~UnitTests"

# Integration tests only
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## Code Coverage

The project is configured to generate code coverage reports in multiple formats:
- JSON
- Cobertura (XML)
- LCOV
- OpenCover

Coverage reports are generated in the `TestResults` directory after running tests with coverage collection.

### View Coverage Report

After running tests with coverage:
```bash
# Install report generator tool (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:Html

# Open the report
open coveragereport/index.html  # macOS
```

## Test Coverage Goals

- **Minimum Target**: 80% code coverage
- **Focus Areas**:
  - Validators: 100% coverage
  - Mappers: 100% coverage
  - Services: 80%+ coverage
  - Endpoints: 70%+ coverage

## Test Categories

### Unit Tests

#### Validators
- Input validation rules
- Error message verification
- Edge cases and boundary conditions

#### Mappers
- Entity to DTO mapping
- Null handling
- Data transformation accuracy

#### Services
- Business logic
- Database operations
- External service interactions (mocked)

### Integration Tests

#### Endpoints
- HTTP request/response validation
- End-to-end workflows
- Error handling
- Status code verification

## Dependencies

- **xUnit**: Test framework
- **FluentAssertions**: Assertion library
- **Moq**: Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing
- **Coverlet**: Code coverage tool

## Best Practices

1. **Arrange-Act-Assert**: Follow AAA pattern in all tests
2. **Descriptive Names**: Test names should describe what they test
3. **One Assertion Per Test**: Keep tests focused
4. **Test Independence**: Tests should not depend on each other
5. **Clean Up**: Dispose resources properly
6. **Mock External Dependencies**: Use mocks for external services

## Continuous Integration

Tests are designed to run in CI/CD pipelines:
- Fast execution
- No external dependencies
- Deterministic results
- Clear failure messages
