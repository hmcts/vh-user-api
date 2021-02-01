using FluentValidation;
using UserApi.Contract.Requests;

namespace UserApi.Validations
{
    public class UpdateUserAccountRequestValidation : AbstractValidator<UpdateUserAccountRequest>
    {
        public UpdateUserAccountRequestValidation()
        {
            RuleFor(x => x.FirstName).NotEmpty().WithMessage(MissingFirstNameErrorMessage);
            RuleFor(x => x.LastName).NotEmpty().WithMessage(MissingLastNameErrorMessage);
        }

        public static string MissingFirstNameErrorMessage => "Require a FirstName";
        public static string MissingLastNameErrorMessage => "Require a LastName";
    }
}