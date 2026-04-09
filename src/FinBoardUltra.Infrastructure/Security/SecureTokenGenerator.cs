using System.Security.Cryptography;
using FinBoardUltra.Domain.Interfaces.Services;

namespace FinBoardUltra.Infrastructure.Security;

public sealed class SecureTokenGenerator : ITokenGenerator
{
    public string Generate()
    {
        // 32 bytes = 256 bits of entropy; base-64 encodes to 44 characters.
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
