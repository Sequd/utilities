using CleanBin;
using FluentAssertions;
using Xunit;

namespace CleanBin.Tests
{
    public class PathValidatorTests
    {
        [Fact]
        public void ValidateDirectoryPath_WithNullPath_ShouldReturnFailure()
        {
            // Act
            var result = PathValidator.ValidateDirectoryPath(null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Путь не может быть пустым или содержать только пробелы");
        }

        [Fact]
        public void ValidateDirectoryPath_WithEmptyPath_ShouldReturnFailure()
        {
            // Act
            var result = PathValidator.ValidateDirectoryPath("");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Путь не может быть пустым или содержать только пробелы");
        }

        [Fact]
        public void ValidateDirectoryPath_WithWhitespacePath_ShouldReturnFailure()
        {
            // Act
            var result = PathValidator.ValidateDirectoryPath("   ");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Путь не может быть пустым или содержать только пробелы");
        }

        [Fact]
        public void ValidateDirectoryPath_WithTooLongPath_ShouldReturnFailure()
        {
            // Arrange
            var longPath = new string('a', 261); // 261 символов, больше чем MaxPathLength (260)

            // Act
            var result = PathValidator.ValidateDirectoryPath(longPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Путь слишком длинный. Максимальная длина: 260 символов");
        }

        [Fact]
        public void ValidateDirectoryPath_WithInvalidCharacters_ShouldReturnFailure()
        {
            // Arrange
            var invalidPath = "C:\\test<invalid>path";

            // Act
            var result = PathValidator.ValidateDirectoryPath(invalidPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Путь содержит недопустимые символы");
        }

        [Fact]
        public void ValidateDirectoryPath_WithNonExistentPath_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentPath = "C:\\NonExistentDirectory12345";

            // Act
            var result = PathValidator.ValidateDirectoryPath(nonExistentPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Директория не существует");
        }

        [Fact]
        public void ValidateDirectoryPath_WithValidPath_ShouldReturnSuccess()
        {
            // Arrange
            var tempPath = Path.GetTempPath();

            // Act
            var result = PathValidator.ValidateDirectoryPath(tempPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void ValidatePathsArray_WithNullArray_ShouldReturnSuccessWhenAllowEmptyIsTrue()
        {
            // Act
            var result = PathValidator.ValidatePathsArray(null, allowEmpty: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidatePathsArray_WithNullArray_ShouldReturnFailureWhenAllowEmptyIsFalse()
        {
            // Act
            var result = PathValidator.ValidatePathsArray(null, allowEmpty: false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Массив путей не может быть null");
        }

        [Fact]
        public void ValidatePathsArray_WithEmptyArray_ShouldReturnSuccessWhenAllowEmptyIsTrue()
        {
            // Arrange
            var emptyArray = new string[0];

            // Act
            var result = PathValidator.ValidatePathsArray(emptyArray, allowEmpty: true);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void ValidatePathsArray_WithEmptyArray_ShouldReturnFailureWhenAllowEmptyIsFalse()
        {
            // Arrange
            var emptyArray = new string[0];

            // Act
            var result = PathValidator.ValidatePathsArray(emptyArray, allowEmpty: false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Массив путей не может быть пустым");
        }

        [Fact]
        public void ValidatePathsArray_WithNullElement_ShouldReturnFailure()
        {
            // Arrange
            var arrayWithNull = new string[] { "valid", null, "also-valid" };

            // Act
            var result = PathValidator.ValidatePathsArray(arrayWithNull);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Массив содержит пустые или null пути");
        }

        [Fact]
        public void ValidatePathsArray_WithEmptyElement_ShouldReturnFailure()
        {
            // Arrange
            var arrayWithEmpty = new string[] { "valid", "", "also-valid" };

            // Act
            var result = PathValidator.ValidatePathsArray(arrayWithEmpty);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Массив содержит пустые или null пути");
        }

        [Fact]
        public void ValidatePathsArray_WithWhitespaceElement_ShouldReturnFailure()
        {
            // Arrange
            var arrayWithWhitespace = new string[] { "valid", "   ", "also-valid" };

            // Act
            var result = PathValidator.ValidatePathsArray(arrayWithWhitespace);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Массив содержит пустые или null пути");
        }

        [Fact]
        public void ValidatePathsArray_WithValidPaths_ShouldReturnSuccess()
        {
            // Arrange
            var validPaths = new string[] { "C:\\test1", "C:\\test2", "C:\\test3" };

            // Act
            var result = PathValidator.ValidatePathsArray(validPaths);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(".git", true)]
        [InlineData(".vs", true)]
        [InlineData(".vscode", true)]
        [InlineData("bin", false)]
        [InlineData("obj", false)]
        [InlineData("packages", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSystemPath_ShouldReturnCorrectResult(string path, bool expected)
        {
            // Act
            var result = PathValidator.IsSystemPath(path);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("C:\\Windows\\System32", true)]
        [InlineData("C:\\Program Files", true)]
        [InlineData("C:\\Program Files (x86)", true)]
        [InlineData("C:\\Users", true)]
        [InlineData("C:\\Documents and Settings", true)]
        [InlineData("C:\\Boot", true)]
        [InlineData("C:\\Recovery", true)]
        [InlineData("C:\\MyProject\\bin", false)]
        [InlineData("C:\\MyProject\\obj", false)]
        [InlineData("C:\\MyProject\\packages", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsCriticalSystemPath_ShouldReturnCorrectResult(string path, bool expected)
        {
            // Act
            var result = PathValidator.IsCriticalSystemPath(path);

            // Assert
            result.Should().Be(expected);
        }
    }
}