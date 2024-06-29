namespace Core.Admin.Logs.Interfaces
{
	public interface IAdminLogEntryConverter<out T>
	{
		public T Convert(LogEntry entry);
	}
}