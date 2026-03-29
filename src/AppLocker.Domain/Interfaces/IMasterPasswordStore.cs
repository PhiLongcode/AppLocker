namespace AppLocker.Domain.Interfaces;

/// <summary>
/// Lưu hash mật khẩu master (SHA256 hex) để không mất sau khi đóng app.
/// </summary>
public interface IMasterPasswordStore
{
    /// <summary>Hash chữ thường 64 ký tự hex, hoặc null nếu chưa đặt.</summary>
    string? LoadMasterPasswordHash();

    void SaveMasterPasswordHash(string sha256HexLower);
}
