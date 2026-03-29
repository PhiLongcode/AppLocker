using AppLocker.Infrastructure.Security;
using FluentAssertions;

namespace AppLocker.Tests.Infrastructure;

/// <summary>
/// TDD Red: Tests cho HashValidationService (Epic 4) TRƯỚC KHI implement.
/// </summary>
public class HashValidationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly HashValidationService _service = new();

    public HashValidationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"hash_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string CreateTempFile(string content = "hello world")
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.tmp");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ComputeHash_ShouldReturn64CharHex()
    {
        var file = CreateTempFile("test content");

        var hash = _service.ComputeHash(file);

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void ComputeHash_SameContent_ShouldReturnSameHash()
    {
        var file1 = CreateTempFile("identical content");
        var file2 = CreateTempFile("identical content");

        _service.ComputeHash(file1).Should().Be(_service.ComputeHash(file2));
    }

    [Fact]
    public void ComputeHash_DifferentContent_ShouldReturnDifferentHash()
    {
        var file1 = CreateTempFile("content A");
        var file2 = CreateTempFile("content B");

        _service.ComputeHash(file1).Should().NotBe(_service.ComputeHash(file2));
    }

    [Fact]
    public void ValidateHash_WithStoredHash_ShouldReturnTrue()
    {
        var file = CreateTempFile("appdata");
        var storedHash = _service.ComputeHash(file);

        _service.ValidateHash(file, storedHash).Should().BeTrue();
    }

    [Fact]
    public void ValidateHash_WhenFileChanged_ShouldReturnFalse()
    {
        var file = CreateTempFile("original");
        var storedHash = _service.ComputeHash(file);

        // Thay đổi file
        File.WriteAllText(file, "modified content");

        _service.ValidateHash(file, storedHash).Should().BeFalse();
    }

    [Fact]
    public void ComputeHash_WhenFileNotExist_ShouldReturnEmpty()
    {
        var hash = _service.ComputeHash(@"C:\nonexistent\file.exe");

        hash.Should().BeEmpty();
    }
}

/// <summary>
/// TDD Red: Tests cho StartupService (Registry autostart) - Epic 4.
/// </summary>
public class StartupServiceTests
{
    // Dùng Registry key test riêng biệt để không ảnh hưởng hệ thống
    private const string TestAppName = "AppLock_UnitTest_" + nameof(StartupServiceTests);
    private readonly AppLocker.Infrastructure.Services.StartupService _service =
        new(TestAppName);

    [Fact]
    public void IsRegistered_Initially_ShouldBeFalse()
    {
        // Cleanup trước
        _service.Unregister();
        _service.IsRegistered().Should().BeFalse();
    }

    [Fact]
    public void Register_ShouldMakeIsRegisteredTrue()
    {
        try
        {
            _service.Register(@"C:\dummy\applocker.exe");
            _service.IsRegistered().Should().BeTrue();
        }
        finally
        {
            _service.Unregister();
        }
    }

    [Fact]
    public void Unregister_AfterRegister_ShouldMakeIsRegisteredFalse()
    {
        _service.Register(@"C:\dummy\applocker.exe");
        _service.Unregister();

        _service.IsRegistered().Should().BeFalse();
    }
}
