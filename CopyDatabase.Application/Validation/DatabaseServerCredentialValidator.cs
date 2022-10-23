using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

namespace CopyDatabase.Core.Validation {
    internal class DatabaseServerCredentialValidator : AbstractValidator<IDatabaseServerCredentials> {
        public DatabaseServerCredentialValidator() {
            RuleFor(x => x.DataSource).NotEmpty().WithMessage("Source is required");

            RuleFor(oo => oo.UserName).NotEmpty().When(oo => oo.UseWindowsAuth == false).WithMessage("Username is required"); ;
            RuleFor(oo => oo.Password.Length).GreaterThan(0).When(oo => oo.UseWindowsAuth == false).WithMessage("Password is required");
        }
    }
}
