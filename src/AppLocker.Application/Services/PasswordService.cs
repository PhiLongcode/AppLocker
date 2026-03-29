using System.Security.Cryptography;
using System.Text;
using AppLocker.Domain.Interfaces;

namespace AppLocker.Application.Services;

/// <summary>
/// Quản lý mật khẩu bảo vệ AppLocker bằng SHA256 (có thể lưu qua SQLite).
/// </summary>
public class PasswordService
{
    private readonly IMasterPasswordStore? _store;
    private string? _storedHash;

    public PasswordService(IMasterPasswordStore? store = null)
    {
        _store = store;
        _storedHash = store?.LoadMasterPasswordHash();
    }

    /// <summary>Hash SHA256 đang lưu (hex string 64 ký tự). Rỗng nếu chưa set.</summary>
    public string StoredHash => _storedHash ?? string.Empty;

    /// <summary>Kiểm tra xem đã có password được set chưa (kể cả sau khi mở lại app).</summary>
    public bool IsPasswordSet => _storedHash is not null;

    /// <summary>Đặt password mới - lưu dạng SHA256 hash.</summary>
    public void SetPassword(string rawPassword)
    {
        _storedHash = Hash(rawPassword);
        _store?.SaveMasterPasswordHash(_storedHash);
    }

    /// <summary>Kiểm tra password nhập vào. Nếu chưa set password, luôn trả về true.</summary>
    public bool Verify(string rawPassword)
    {
        if (!IsPasswordSet) return true;
        return Hash(rawPassword) == _storedHash;
    }

    private static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
