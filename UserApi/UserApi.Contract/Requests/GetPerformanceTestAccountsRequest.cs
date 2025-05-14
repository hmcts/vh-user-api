namespace UserApi.Contract.Requests;

public class GetPerformanceTestAccountsRequest
{
    public PerformanceTestGroup Group { get; set; } = PerformanceTestGroup.TestAccounts;
}

public enum PerformanceTestGroup
{
    TestAccounts = 0,
    PanelMemberAccounts = 1,
    JudgeAccounts = 2
}