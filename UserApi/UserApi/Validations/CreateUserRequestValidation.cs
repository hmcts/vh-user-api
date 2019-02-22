using FluentValidation;
using UserApi.Contract.Requests;

namespace UserApi.Validations
{
    public class CreateUserRequestValidation : AbstractValidator<CreateUserRequest>
    {
        public static string MissingFirstNameErrorMessage => "Require a FirstName";
        public static string MissingLastNameErrorMessage => "Require a LastName";
        public static string MissingEmailErrorMessage => "Require a RecoveryEmail";
        
        public CreateUserRequestValidation()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(MissingFirstNameErrorMessage);
            RuleFor(x => x.LastName).NotEmpty().WithMessage(MissingLastNameErrorMessage);
            RuleFor(x => x.RecoveryEmail).NotEmpty().WithMessage(MissingEmailErrorMessage);
        }
    }
}