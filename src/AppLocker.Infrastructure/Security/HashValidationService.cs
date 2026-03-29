using System.Security.Cryptography;

namespace AppLocker.Infrastructure.Security;

/// <summary>
/// Xác thực file thực thi bằng SHA256 nhằm chống đổi tên file (anti-bypass).
/// </summary>
public class HashValidationService
{
    /// <summary>Tính toán SHA256 hash của một file. Trả về empty string nếu file không tồn tại/lỗi.</summary>
    public string ComputeHash(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;

        try
        {
            using var stream = File.OpenRead(filePath);
            var hashBytes = SHA256.HashData(stream);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Kiểm tra xem file có khớp với mã băm đã lưu trước đó không.</summary>
    public bool ValidateHash(string filePath, string expectedHash)
    {
        var actualHash = ComputeHash(filePath);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
