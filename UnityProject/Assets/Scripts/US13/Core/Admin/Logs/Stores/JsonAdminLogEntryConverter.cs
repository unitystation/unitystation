using Newtonsoft.Json;
using US13.Core.Admin.Logs.Interfaces;

namespace US13.Core.Admin.Logs.Stores
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

		public StoredLogEntry ConvertBackSingle(object entry)
		{
			return JsonConvert.DeserializeObject<StoredLogEntry>(entry.ToString(), _settings);
		}
	}
}