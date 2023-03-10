namespace Core.Database
{
	public class PolyStore : JsonObject
	{
		public string phrase;
		public string said_by; // (id) // TODO: why do we need this?
	}

	public class PolyPhrase : JsonObject
	{
		public string phrase;
	}
}
