using MediSlot.Application.DTOs.Auth;
using MediSlot.Application.Validators;

namespace MediSlot.UnitTests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest ValidRequest() => new(
        Name: "Test Patient",
        Email: "patient@test.com",
        Password: "Test@1234",
        DateOfBirth: new DateOnly(1996, 5, 10),
        Gender: "Male",
        Phone: "9876543210",
        BloodGroup: "O+");

    private bool HasErrorFor(RegisterRequest request, string property)
        => _validator.Validate(request).Errors.Any(e => e.PropertyName == property);

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_name_fails()
    {
        var request = ValidRequest() with { Name = "" };

        Assert.True(HasErrorFor(request, nameof(RegisterRequest.Name)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad")]
    [InlineData("a@")]
    [InlineData("@test.com")]
    public void Invalid_email_fails(string email)
    {
        var request = ValidRequest() with { Email = email };

        Assert.True(HasErrorFor(request, nameof(RegisterRequest.Email)));
    }

    [Theory]
    [InlineData("Short1A")]          // 8 peksha kami
    [InlineData("alllowercase1")]    // uppercase nahi
    [InlineData("ALLUPPERCASE1")]    // lowercase nahi
    [InlineData("NoDigitsHere")]     // digit nahi
    public void Weak_password_fails(string password)
    {
        var request = ValidRequest() with { Password = password };

        Assert.True(HasErrorFor(request, nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void Future_date_of_birth_fails()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var request = ValidRequest() with { DateOfBirth = tomorrow };

        Assert.True(HasErrorFor(request, nameof(RegisterRequest.DateOfBirth)));
    }

    [Fact]
    public void Invalid_phone_fails()
    {
        var request = ValidRequest() with { Phone = "abc" };

        Assert.True(HasErrorFor(request, nameof(RegisterRequest.Phone)));
    }

    [Fact]
    public void Invalid_blood_group_fails()
    {
        var request = ValidRequest() with { BloodGroup = "X+" };

        Assert.True(HasErrorFor(request, nameof(RegisterRequest.BloodGroup)));
    }

    [Fact]
    public void Missing_blood_group_is_allowed()
    {
        var request = ValidRequest() with { BloodGroup = null };

        Assert.True(_validator.Validate(request).IsValid);
    }
}