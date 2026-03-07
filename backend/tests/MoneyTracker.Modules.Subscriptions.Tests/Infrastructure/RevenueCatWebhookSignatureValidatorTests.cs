using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MoneyTracker.Modules.Subscriptions.Infrastructure;

namespace MoneyTracker.Modules.Subscriptions.Tests.Infrastructure;

public sealed class RevenueCatWebhookSignatureValidatorTests
{
    private const string TestSecret = "test-webhook-secret-123";

    private static RevenueCatWebhookSignatureValidator CreateValidator(string secret = TestSecret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RevenueCat:WebhookSecret"] = secret
            })
            .Build();

        return new RevenueCatWebhookSignatureValidator(configuration);
    }

    private static string ComputeSignature(string body, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, bodyBytes);
        return Convert.ToHexStringLower(hash);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WithCorrectSignature_ReturnsTrue()
    {
        // P4-1-INT-01: HMAC-SHA256 signature validation with correct secret
        var validator = CreateValidator();
        var body = "{\"event\":{\"type\":\"INITIAL_PURCHASE\"}}";
        var signature = ComputeSignature(body, TestSecret);

        var result = validator.Validate(signature, body);

        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WithIncorrectSignature_ReturnsFalse()
    {
        // P4-1-INT-02: HMAC-SHA256 signature validation with incorrect secret
        var validator = CreateValidator();
        var body = "{\"event\":{\"type\":\"INITIAL_PURCHASE\"}}";
        var wrongSignature = ComputeSignature(body, "wrong-secret");

        var result = validator.Validate(wrongSignature, body);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WithEmptySignature_ReturnsFalse()
    {
        var validator = CreateValidator();
        var body = "{\"event\":{\"type\":\"INITIAL_PURCHASE\"}}";

        var result = validator.Validate("", body);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WithEmptyBody_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = validator.Validate("some-signature", "");

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WithNullSignature_ReturnsFalse()
    {
        var validator = CreateValidator();
        var body = "{\"event\":{\"type\":\"INITIAL_PURCHASE\"}}";

        var result = validator.Validate(null!, body);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WithTamperedBody_ReturnsFalse()
    {
        var validator = CreateValidator();
        var originalBody = "{\"event\":{\"type\":\"INITIAL_PURCHASE\"}}";
        var signature = ComputeSignature(originalBody, TestSecret);
        var tamperedBody = "{\"event\":{\"type\":\"CANCELLATION\"}}";

        var result = validator.Validate(signature, tamperedBody);

        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_UsesDefaultSecret_WhenNotConfigured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var validator = new RevenueCatWebhookSignatureValidator(configuration);
        var body = "{\"test\":true}";
        var signature = ComputeSignature(body, "default-webhook-secret-for-testing");

        var result = validator.Validate(signature, body);

        Assert.True(result);
    }
}
