<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Web.dll</Reference>
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference>Microsoft.IdentityModel.Clients.ActiveDirectory</NuGetReference>
  <Namespace>Microsoft.IdentityModel.Clients.ActiveDirectory</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
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
  <IncludeAspNet>true</IncludeAspNet>
</Query>

#load ".\Reference\GraphApiService"
#load "Helpers\WebApi\HttpClientService"

private GraphApiService _graphApiService = new GraphApiService(new HttpClientService());

async Task Main()
{
	var users = new List<string>
	{
		"a1.a1@email.com",
		"a2.a2@email.com"
	};

	foreach (var username in users)
	{
		try
		{
			//var user = await graphApiService.GetUserByFilter($"userPrincipalName  eq '{username}'").Dump();
			var user = await _graphApiService.GetUserByFilter($"startswith(userPrincipalName,'{username}')").Dump();
			//var user = await graphApiService.GetUsersByFilter($"otherMails/any(c:c eq '{username}')").Dump();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"{username}, Error: {ex.Message}");
		}
	}
}