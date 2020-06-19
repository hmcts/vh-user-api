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
  <Namespace>System.Net</Namespace>
</Query>

#load ".\Reference\GraphApiService"
#load "Helpers\WebApi\HttpClientService"

private GraphApiService _graphApiService = new GraphApiService(new HttpClientService());
async Task Main()
{
	var users = new List<string>
	{
		"a.a@hearings.reform.hmcts.net"
	};
	
	foreach (var username in users)
	{
		try
		{
			var user = await _graphApiService.GetUserByFilter($"userPrincipalName  eq '{username}'");

			if (user == null)
			{
				Console.WriteLine($"{username}: Skipped user not found");
				continue;
			}


			foreach (var groupName in VhoGroups.Concat(KinlyGroups))
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
						throw new Exception($"{groupName}: Group not found");
					}

					groupCache.Add(groupName, group);
				}

				var success = await _graphApiService.AddUserToGroup(user, group);

				if (!success)
				{
					Console.WriteLine($"{user.UserPrincipalName} failed to add to group: {groupName}");
				}

				await Task.Delay(100);
			}

			Console.WriteLine($"{user.UserPrincipalName} done!");

		}
		catch (Exception ex)
		{
			Console.WriteLine($"{username}, Error: {ex.Message}");
		}
	}
}