using CleanBin;
using FluentAssertions;
using Xunit;

namespace CleanBin.Tests
{
    public class OperationResultTests
    {
        [Fact]
        public void Success_WithValue_ShouldCreateSuccessfulResult()
        {
            // Arrange
            var testValue = "test value";

            // Act
            var result = OperationResult<string>.Success(testValue);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(testValue);
            result.ErrorMessage.Should().BeNull();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void Success_WithoutValue_ShouldCreateSuccessfulResult()
        {
            // Act
            var result = OperationResult.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().BeNull();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void Failure_WithErrorMessage_ShouldCreateFailedResult()
        {
            // Arrange
            var errorMessage = "Test error message";

            // Act
            var result = OperationResult<string>.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be(errorMessage);
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void Failure_WithException_ShouldCreateFailedResult()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = OperationResult<string>.Failure(exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be(exception.Message);
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public void Failure_WithErrorMessageAndException_ShouldCreateFailedResult()
        {
            // Arrange
            var errorMessage = "Custom error message";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = OperationResult<string>.Failure(errorMessage, exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be(errorMessage);
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public void Failure_WithoutValue_WithErrorMessage_ShouldCreateFailedResult()
        {
            // Arrange
            var errorMessage = "Test error message";

            // Act
            var result = OperationResult.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be(errorMessage);
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void Failure_WithoutValue_WithException_ShouldCreateFailedResult()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = OperationResult.Failure(exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be(exception.Message);
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public void Failure_WithoutValue_WithErrorMessageAndException_ShouldCreateFailedResult()
        {
            // Arrange
            var errorMessage = "Custom error message";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = OperationResult.Failure(errorMessage, exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Value.Should().BeNull();
            result.ErrorMessage.Should().Be(errorMessage);
            result.Exception.Should().Be(exception);
        }

        [Theory]
        [InlineData("test value")]
        [InlineData(42)]
        [InlineData(true)]
        [InlineData(null)]
        public void Success_WithDifferentTypes_ShouldCreateSuccessfulResult<T>(T value)
        {
            // Act
            var result = OperationResult<T>.Success(value);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(value);
            result.ErrorMessage.Should().BeNull();
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void OperationResult_WithNullErrorMessage_ShouldBeAllowed()
        {
            // Act
            var result = OperationResult<string>.Failure((string)null!);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void OperationResult_WithNullException_ShouldBeAllowed()
        {
            // Act
            var result = OperationResult<string>.Failure("error", null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Exception.Should().BeNull();
        }
    }
}