<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference>Microsoft.IdentityModel.Clients.ActiveDirectory</NuGetReference>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory</Namespace>
  <RemoveNamespace>System.Data</RemoveNamespace>
  <RemoveNamespace>System.Diagnostics</RemoveNamespace>
  <RemoveNamespace>System.IO</RemoveNamespace>
  <RemoveNamespace>System.Linq.Expressions</RemoveNamespace>
  <RemoveNamespace>System.Reflection</RemoveNamespace>
  <RemoveNamespace>System.Text</RemoveNamespace>
  <RemoveNamespace>System.Text.RegularExpressions</RemoveNamespace>
  <RemoveNamespace>System.Threading</RemoveNamespace>
  <RemoveNamespace>System.Transactions</RemoveNamespace>
  <RemoveNamespace>System.Xml</RemoveNamespace>
  <RemoveNamespace>System.Xml.Linq</RemoveNamespace>
  <RemoveNamespace>System.Xml.XPath</RemoveNamespace>
</Query>

#load ".\Reference\GraphApiService"
#load "Helpers\WebApi\HttpClientService"

private GraphApiService _graphApiService = new GraphApiService(new HttpClientService());

async Task Main()
{
	var users = new List<string>
	{
		"user01@email.com"
	};

	foreach (var username in users)
	{
		try
		{
			//var user = await _graphApiService.GetUserByFilter($"userPrincipalName  eq '{username}'");
			//var user = await graphApiService.GetUserByFilter($"startswith(userPrincipalName,'{username}')").Dump();
			//var user = await graphApiService.GetUsersByFilter($"otherMails/any(c:c eq '{username}')").Dump();
			
			var changed = new
			{
				displayName = "Manual01 Changed",//user.DisplayName.Replace("a", "changed"),
				givenName = "Manual01 Changed",
				//surname = request.LastName,
				//mailNickname = request.MailNickname,
				//otherMails = new List<string> { request.RecoveryEmail },
				//accountEnabled = request.AccountEnabled,
				userPrincipalName = username,
				//mobilePhone = request.MobilePhone ?? " ",
				//businessPhones = new List<string> { request.BusinessPhone ?? " " },
				//passwordProfile = new
				//{
				//	forceChangePasswordNextSignIn = false,
				//	password = "qwerty"
				//}
			};
			
			await _graphApiService.UpdateUserAsync(username, changed);
			Console.WriteLine(changed.userPrincipalName);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"{username}, Error: {ex.Message}");
		}
	}
}