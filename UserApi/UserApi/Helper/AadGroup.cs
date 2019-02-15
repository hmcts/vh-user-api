using System.ComponentModel.DataAnnotations;

namespace UserApi.Helper
{
    public enum AadGroup
    {
        [Display(Name = "Virtual Room Administrator")] VirtualRoomAdministrator = 1,
        [Display(Name = "External")] External = 2,
        [Display(Name = "Internal")] Internal = 3,
        [Display(Name = "Money claims")] MoneyClaims = 4,
        [Display(Name = "Financial remedy")] FinancialRemedy = 5
    }
}
