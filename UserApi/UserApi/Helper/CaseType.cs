using System.ComponentModel.DataAnnotations;

namespace UserApi.Helper
{
    public enum CaseType
    {
        [Display(Name = "Money claims")] MoneyClaims = 1,
        [Display(Name = "Financial remedy ")] FinancialRemedy = 2
    }
}