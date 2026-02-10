namespace US13.Core.Database
{
	public partial class ServerData
	{
		// public static async Task<bool> UpdateCharacterProfile(CharacterSheet updateSettings)
		// {
		// 	if (FirebaseAuth.DefaultInstance.CurrentUser == null)
		// 	{
		// 		Loggy.LogWarning("User is not logged in! Skipping character upload.", Category.DatabaseAPI);
		// 		return false;
		// 	}
		// 	var jsonSettings = JsonConvert.SerializeObject(updateSettings);
		// 	var payload = JsonConvert.SerializeObject(new
		// 	{
		// 		fields = new
		// 		{
		// 			character = new { stringValue = jsonSettings }
		// 		}
		// 	});
		//
		//
		// 	HttpRequestMessage r = new HttpRequestMessage(HttpMethod.Put,
		// 		UserFirestoreURL + "/?updateMask.fieldPaths=character");
		// 	r.Method = new HttpMethod("PATCH");
		// 	var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
		// 	r.Headers.Add("Authorization", $"Bearer {IdToken}");
		// 	r.Content = httpContent;
		//
		// 	try
		// 	{
		// 		await SafeHttpRequest.SendAsync(r);
		// 	}
		// 	catch (Exception e)
		// 	{
		// 		Loggy.LogError($"Error occured when uploading character: {e.Message}", Category.DatabaseAPI);
		// 		return false;
		// 	}
		//
		// 	return true;
		// }
	}
}
