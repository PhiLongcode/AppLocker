using AppLocker.Application.Services;
using AppLocker.Domain.Interfaces;
using FluentAssertions;

namespace AppLocker.Tests.Application;

/// <summary>
/// TDD Red: Tests cho PasswordService TRƯỚC KHI implement.
/// </summary>
public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void SetPassword_ShouldNotThrow()
    {
        var act = () => _service.SetPassword("MySecret123");
        act.Should().NotThrow();
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        _service.SetPassword("MySecret123");

        _service.Verify("MySecret123").Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        _service.SetPassword("MySecret123");

        _service.Verify("WrongPassword").Should().BeFalse();
    }

    [Fact]
    public void Verify_CaseSensitive_ShouldReturnFalse()
    {
        _service.SetPassword("MySecret123");

        _service.Verify("mysecret123").Should().BeFalse();
    }

    [Fact]
    public void Verify_WhenNoPasswordSet_ShouldReturnTrue()
    {
        // Nếu chưa set password thì không bị chặn
        _service.Verify("any").Should().BeTrue();
    }

    [Fact]
    public void IsPasswordSet_WhenSet_ShouldReturnTrue()
    {
        _service.SetPassword("secret");
        _service.IsPasswordSet.Should().BeTrue();
    }

    [Fact]
    public void IsPasswordSet_WhenNotSet_ShouldReturnFalse()
    {
        _service.IsPasswordSet.Should().BeFalse();
    }

    [Fact]
    public void Hash_ShouldNotStoreRawPassword()
    {
        _service.SetPassword("MySecret123");

        // Hash không được bằng raw password
        _service.StoredHash.Should().NotBe("MySecret123");
        _service.StoredHash.Should().HaveLength(64); // SHA256 hex = 64 chars
    }

    [Fact]
    public void SetPassword_Twice_ShouldUpdateHash()
    {
        _service.SetPassword("first");
        var hash1 = _service.StoredHash;

        _service.SetPassword("second");
        var hash2 = _service.StoredHash;

        hash1.Should().NotBe(hash2);
    }

    private sealed class MemoryPasswordStore : IMasterPasswordStore
    {
        public string? Hash { get; set; }
        public string? LoadMasterPasswordHash() => Hash;
        public void SaveMasterPasswordHash(string sha256HexLower) => Hash = sha256HexLower;
    }

    [Fact]
    public void SetPassword_WithStore_ShouldAllowNewInstanceToVerify()
    {
        var mem = new MemoryPasswordStore();
        var first = new PasswordService(mem);
        first.SetPassword("persist-me");

        var second = new PasswordService(mem);
        second.IsPasswordSet.Should().BeTrue();
        second.Verify("persist-me").Should().BeTrue();
        second.Verify("wrong").Should().BeFalse();
    }
}
