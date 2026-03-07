using MoneyTracker.Modules.SharedKernel.Presentation;

namespace MoneyTracker.Api.Tests.Unit.Security;

public sealed class InputValidationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void RejectControlCharacters_RejectsNullByte()
    {
        var result = EndpointHelpers.RejectControlCharacters("hello\x00world");

        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RejectControlCharacters_RejectsBellCharacter()
    {
        var result = EndpointHelpers.RejectControlCharacters("hello\x07world");

        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RejectControlCharacters_AllowsTab()
    {
        var result = EndpointHelpers.RejectControlCharacters("hello\tworld");

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RejectControlCharacters_AllowsNewline()
    {
        var result = EndpointHelpers.RejectControlCharacters("hello\nworld");

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RejectControlCharacters_AllowsCarriageReturn()
    {
        var result = EndpointHelpers.RejectControlCharacters("hello\rworld");

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RejectControlCharacters_AllowsNormalText()
    {
        var result = EndpointHelpers.RejectControlCharacters("Hello, World! 123");

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateStringInput_ReturnsNull_ForValidInput()
    {
        var result = EndpointHelpers.ValidateStringInput("valid input", 500, "testParam");

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateStringInput_ReturnsNull_ForNullInput()
    {
        var result = EndpointHelpers.ValidateStringInput(null, 500, "testParam");

        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateStringInput_RejectsExceedingMaxLength()
    {
        var longInput = new string('a', 501);

        var result = EndpointHelpers.ValidateStringInput(longInput, 500, "testParam");

        Assert.NotNull(result);
        Assert.Contains("maximum length", result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateStringInput_RejectsControlCharacters()
    {
        var result = EndpointHelpers.ValidateStringInput("hello\x00world", 500, "testParam");

        Assert.NotNull(result);
        Assert.Contains("control characters", result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateStringInput_UsesDefaultMaxLength_WhenZeroProvided()
    {
        var longInput = new string('a', 501);

        var result = EndpointHelpers.ValidateStringInput(longInput, 0, "testParam");

        Assert.NotNull(result);
        Assert.Contains("500", result);
    }
}
