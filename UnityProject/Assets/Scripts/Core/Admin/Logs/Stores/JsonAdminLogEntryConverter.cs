using Core.Admin.Logs.Interfaces;
using Newtonsoft.Json;

namespace Core.Admin.Logs.Stores
{
	public class JsonAdminLogEntryConverter : IAdminLogEntryConverter<string>
	{
		public string Convert(LogEntry entry)
		{
			return JsonConvert.SerializeObject(entry);
		}
	}
}