<Query Kind="Program">
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference>Microsoft.IdentityModel.Clients.ActiveDirectory</NuGetReference>
  <Namespace>Microsoft.Graph</Namespace>
  <Namespace>Microsoft.Graph.Extensions</Namespace>
  <Namespace>Microsoft.Identity.Core.Cache</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory.Extensibility</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory.Internal</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Bson</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load ".\Reference\GraphApiService"
#load "Helpers\WebApi\HttpClientService"

private GraphApiService _graphApiService = new GraphApiService(new HttpClientService());

async Task Main()
{
	var users = new List<CreateUserRequest>
	{
		CreateRequest("Judge", "Test3"),
		CreateRequest("Judge", "Test4"),
	};

	foreach (var userToCreate in users)
	{
		if (await _graphApiService.GetUserByFilter($"userPrincipalName  eq '{userToCreate.RecoveryEmail}'") != null)
		{
			Console.WriteLine($"{userToCreate.RecoveryEmail}: Skipped user was found");
			break;
		}
		
		var user = await _graphApiService.CreateUserAsync(userToCreate);

		foreach (var groupName in JudgeGroups.Concat(KinlyGroups))
		{
			Microsoft.Graph.Group group;

			if (groupCache.ContainsKey(groupName))
			{
				group = groupCache[groupName];
			}
			else
			{
				group = await _graphApiService.GetGroupByName(groupName);

				if (group == null)
				{
					throw new Exception($"{groupName}: Skipped group not found");
				}

				groupCache.Add(groupName, group);
			}

			var success = await _graphApiService.AddUserToGroup(user, group);

			if (!success) throw new Exception($"Failed to add {user.UserPrincipalName} to group: {groupName}");
			
			await Task.Delay(100);
		}

		Console.WriteLine($"{user.UserPrincipalName}");
	}
}

public CreateUserRequest CreateRequest(string firstname, string lastname)
{
	var displayName = $"{firstname}.{lastname}";

	return new CreateUserRequest
	{
		FirstName = firstname,
		LastName = lastname,
		DisplayName = displayName,
		UserPrincipalName = $"{displayName}@email.com",
		RecoveryEmail = $"{displayName}@email.com",
		MailNickname = $"{displayName}".ToLower(),
		AccountEnabled = true,
		ForceChangePasswordNextSignIn = true,
		Password = "qwerty"
	};
}