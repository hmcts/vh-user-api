using FluentValidation;
using UserApi.Contract.Requests;

namespace UserApi.Validations
{
    public class AddUserToGroupRequestValidation : AbstractValidator<AddUserToGroupRequest>
    {
        public static string MissingUserIdErrorMessage => "Require a UserId";
        public static string MissingGroupNameErrorMessage => "Require a GroupName";
        
        public AddUserToGroupRequestValidation()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage(MissingUserIdErrorMessage);
            RuleFor(x => x.GroupName).NotEmpty().WithMessage(MissingGroupNameErrorMessage);
        }
    }
}