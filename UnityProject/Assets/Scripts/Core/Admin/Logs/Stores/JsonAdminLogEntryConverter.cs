using Core.Admin.Logs.Interfaces;
using Newtonsoft.Json;

namespace Core.Admin.Logs.Stores
{
	public class JsonAdminLogEntryConverter : IAdminLogEntryConverter<string>
	{
		private JsonSerializerSettings _settings = new JsonSerializerSettings()
		{
			NullValueHandling = NullValueHandling.Include, MissingMemberHandling = MissingMemberHandling.Ignore
		};

		public string Convert(object entry)
		{
			return JsonConvert.SerializeObject(entry, _settings) + "\n";
		}
	}
}