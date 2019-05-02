using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace UserApi.Helper
{
    public enum AadGroup
    {
        [Display(Name = "VirtualRoomAdministrator")]
        VirtualRoomAdministrator = 1,
        [Display(Name = "External")] External = 2,
        [Display(Name = "Internal")] Internal = 3,
        [Display(Name = "MoneyClaims")] MoneyClaims = 4,
        [Display(Name = "FinancialRemedy")] FinancialRemedy = 5,
        [Display(Name = "SSPR Enabled")] SsprEnabled = 6,
        [Display(Name = "VirtualRoomProfessionalUser")]
        VirtualRoomProfessionalUser = 7,
        [Display(Name = "VirtualRoomJudge")]
        VirtualRoomJudge = 8
    }
}