namespace US13.Core.Admin.Logs.Interfaces
{
	public interface IAdminLogEntryConverter<out T>
	{
		public T Convert(object entry);
		public StoredLogEntry ConvertBackSingle(object entry);
	}
}