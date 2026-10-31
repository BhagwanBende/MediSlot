using MediSlot.Infrastructure.Services;

namespace MediSlot.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_is_not_the_plain_password()
    {
        var hash = _hasher.Hash("Test@1234");

        Assert.NotEqual("Test@1234", hash);
        Assert.StartsWith("$2", hash);   // BCrypt format
    }

    [Fact]
    public void Verify_returns_true_for_correct_password()
    {
        var hash = _hasher.Hash("Test@1234");

        Assert.True(_hasher.Verify("Test@1234", hash));
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hash = _hasher.Hash("Test@1234");

        Assert.False(_hasher.Verify("Wrong@1234", hash));
    }

    [Fact]
    public void Same_password_gives_different_hashes_because_of_salt()
    {
        var first = _hasher.Hash("Test@1234");
        var second = _hasher.Hash("Test@1234");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("Test@1234", first));
        Assert.True(_hasher.Verify("Test@1234", second));
    }
}