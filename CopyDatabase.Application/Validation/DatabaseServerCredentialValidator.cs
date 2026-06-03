using FluentValidation;

namespace CopyDatabase.Core.Validation {
    internal sealed class DatabaseServerCredentialValidator : AbstractValidator<IDatabaseServerCredentials> {
        public DatabaseServerCredentialValidator() {
            RuleFor(x => x.DataSource).NotEmpty().WithMessage("Source is required");

            RuleFor(oo => oo.UserName).NotEmpty().When(oo => oo.UseWindowsAuth == false).WithMessage("Username is required"); ;
            RuleFor(oo => oo.Password.Length).GreaterThan(0).When(oo => oo.UseWindowsAuth == false).WithMessage("Password is required");
        }
    }
}
