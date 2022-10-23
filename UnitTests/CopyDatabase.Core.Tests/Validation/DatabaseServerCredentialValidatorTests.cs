namespace CopyDatabase.Core.Validation.Tests;

public class DatabaseServerCredentialValidatorTests {
    private DatabaseServerCredentialValidator sut;

    public DatabaseServerCredentialValidatorTests() {
        this.sut = new DatabaseServerCredentialValidator();
    }

    [Fact()]
    public void ShouldFailValidationWhenSourceIsNotProvided() {
        var result = this.sut.Validate(new DatabaseServerTestCredentials());
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, oo => oo.ErrorMessage.Equals("Source is required"));
    }

    [Fact()]
    public void ShouldFailValiIDatabaseServerCredentialsUsernameIsNotProvided() {
        var result = this.sut.Validate(new DatabaseServerTestCredentials() {
            DataSource = "asdf"
        });
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, oo => oo.ErrorMessage.Equals("Username is required"));
    }

    [Fact()]
    public void ShouldFailValiIDatabaseServerCredentialsPasswordIsNotProvided() {
        var result = this.sut.Validate(new DatabaseServerTestCredentials() {
            DataSource = "asdf",
            UserName = "test"
        });
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, oo => oo.ErrorMessage.Equals("Password is required"));
    }
}