using System;
using System.Threading.Tasks;

namespace Core.Database
{
	public static class PersistenceServer
	{
		public static string Host = "api.deb.local";
		public static UriBuilder UriBuilder = new("http", Host);

		public static Uri GetUri(string endpoint)
		{
			UriBuilder.Path = $"/api/persistence/{endpoint}";
			return UriBuilder.Uri;
		}
	}

	public static class PolyPersist
	{
		public static async Task<JsonObject> StorePhrase(string phrase)
		{
			var requestBody = new PolyStore
			{
				phrase = phrase,
			};

			// TODO: get actual endpoint
			// return JsonObject if response actualy has a body, else, just void, don't need return type
			return await ApiServer.Post<JsonObject>(PersistenceServer.GetUri("polystore"), requestBody);
		}

		public static async Task<PolyPhrase> GetRandomPhrase()
		{
			return await ApiServer.Get<PolyPhrase>(PersistenceServer.GetUri("polysays"));
		}
	}
}
