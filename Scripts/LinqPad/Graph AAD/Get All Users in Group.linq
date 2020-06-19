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
	const string groupName = "VirtualRoomJudge";
	try
	{
		var group = await _graphApiService.GetGroupByName(groupName);

		if (group == null)
		{
			Console.WriteLine($"{groupName}: Skipped group not found");
			return;
		}
		
		await foreach (var users in _graphApiService.GetUsersByGroupAsync(group))
		{
			users.Select(x => new UserResponse
			{
				FirstName = x.GivenName,
				LastName = x.Surname,
				DisplayName = x.DisplayName,
				Email = x.UserPrincipalName
			})
			.Where(u => !string.IsNullOrWhiteSpace(u.FirstName) && u.FirstName.StartsWith("Manchester"))
			.OrderBy(x => x.Email)
			.Dump();
		}

	}
	catch (Exception ex)
	{
		Console.WriteLine($"{ex.Message}");
	}
}