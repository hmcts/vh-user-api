using FluentValidation;
using UserApi.Contract.Requests;

namespace UserApi.Validations
{
    public class CreateUserRequestValidation : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidation()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(MissingFirstNameErrorMessage);
            RuleFor(x => x.LastName).NotEmpty().WithMessage(MissingLastNameErrorMessage);
            RuleFor(x => x.RecoveryEmail).NotEmpty().WithMessage(MissingEmailErrorMessage);
            RuleFor(x => x.RecoveryEmail).Must(x => x.IsValidEmail()).WithMessage(InvalidEmailErrorMessage);
        }

        public static string MissingFirstNameErrorMessage => "first name cannot be empty";
        public static string MissingLastNameErrorMessage => "last name cannot be empty";
        public static string MissingEmailErrorMessage => "recovery email cannot be empty";
        public static string InvalidEmailErrorMessage => "email has incorrect format";
    }
}