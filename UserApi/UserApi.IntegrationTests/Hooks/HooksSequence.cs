namespace UserApi.IntegrationTests.Hooks
{ 
    internal enum HooksSequence
    {
        ConfigHooks = 1,
        RegisterApisHooks = 2,
        HealthcheckHooks = 3,
        CheckGroupsHooks = 4,
        RemoveGroup = 5
    }
}
