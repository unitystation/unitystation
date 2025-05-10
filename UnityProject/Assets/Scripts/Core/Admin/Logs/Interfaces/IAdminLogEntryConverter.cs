using System.Collections.Generic;

namespace Core.Admin.Logs.Interfaces
{
	public interface IAdminLogEntryConverter<out T>
	{
		public T Convert(object entry);
		public LongTermLogEntry ConvertBackSingle(object entry);
	}
}