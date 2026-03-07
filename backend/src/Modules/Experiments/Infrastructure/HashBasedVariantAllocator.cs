using System.Security.Cryptography;
using System.Text;
using MoneyTracker.Modules.Experiments.Domain;

namespace MoneyTracker.Modules.Experiments.Infrastructure;

public static class HashBasedVariantAllocator
{
    public static string Allocate(ExperimentId experimentId, Guid userId, IReadOnlyList<ExperimentVariant> variants)
    {
        var input = $"{experimentId.Value}{userId}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Use first 4 bytes to get a stable integer, then mod 100
        var hashValue = Math.Abs(BitConverter.ToInt32(hashBytes, 0)) % 100;

        var cumulative = 0;
        foreach (var variant in variants)
        {
            cumulative += variant.Weight;
            if (hashValue < cumulative)
            {
                return variant.Name;
            }
        }

        // Fallback to last variant (should not reach here if weights sum to 100)
        return variants[^1].Name;
    }
}
