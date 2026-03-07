using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MoneyTracker.Modules.BankConnections.Domain;

namespace MoneyTracker.Modules.BankConnections.Infrastructure;

public sealed class BasiqWebhookSignatureValidator : IWebhookSignatureValidator
{
    private readonly byte[] _secretKey;

    public BasiqWebhookSignatureValidator(IConfiguration configuration)
    {
        var secret = configuration["Basiq:WebhookSecret"] ?? "default-webhook-secret-for-testing";
        _secretKey = Encoding.UTF8.GetBytes(secret);
    }

    public bool Validate(string signature, string rawBody)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(rawBody))
        {
            return false;
        }

        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var expectedHash = HMACSHA256.HashData(_secretKey, bodyBytes);
        var expectedSignature = Convert.ToHexStringLower(expectedHash);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expectedSignature));
    }
}
