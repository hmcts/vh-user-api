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
	var usersToCreate = CreateIncrementingCreateUserRequests();
	var groupsToAdd = RepresentativeGroups.Concat(TestAccountsGroups);
	
	string.Join(", ", groupsToAdd).Dump("Groups to add to");
	
	foreach (var userToCreate in usersToCreate)
	{
		//Console.WriteLine($"{userToCreate.UserPrincipalName} - {userToCreate.FirstName} - {userToCreate.LastName} - {userToCreate.RecoveryEmail}"); continue;
		
		//if(await UserAlreadyExists(userToCreate)) break;
		
		var user = await _graphApiService.CreateUserAsync(userToCreate);

		foreach (var groupName in groupsToAdd)
		{
			Microsoft.Graph.Group group;
			
			if(groupCache.ContainsKey(groupName))
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
			
			await Task.Delay(60);
		}

		Console.WriteLine($"{user.UserPrincipalName}");
	}
}

public IEnumerable<CreateUserRequest> CreateIncrementingCreateUserRequests()
{
	var totalPadLength = 4;
	
	foreach (var index in Enumerable.Range(1, 150))
	{
		int decimalLength = index.ToString("D").Length + (totalPadLength - index.ToString().Length);
		var cuurentIndexWithPadding = index.ToString("D" + decimalLength.ToString());
		
		foreach (var subIndex in Enumerable.Range(3, 5))
		{
			var firstName = "TP";
			var lastName = $"Representative{cuurentIndexWithPadding}_{subIndex}";
			var displayName = $"{firstName}{lastName}";
			
			yield return new CreateUserRequest
			{
				FirstName = firstName,
				LastName = lastName,
				DisplayName = displayName,
				UserPrincipalName = $"{displayName}@email.com",
				RecoveryEmail = $"{displayName}@email.com",
				MailNickname = $"{displayName}".ToLower(),
				AccountEnabled = true,
				ForceChangePasswordNextSignIn = false,
				Password = "qwerty"
			};
		}	
	}
}

private async Task<bool> UserAlreadyExists(CreateUserRequest userToCreate)
{
	if(await _graphApiService.GetUserByFilter($"userPrincipalName  eq '{userToCreate.RecoveryEmail}'") != null)
	{
		Console.WriteLine($"{userToCreate.RecoveryEmail}: User already exists");
		
		return true;
	}
	
	return false;
}