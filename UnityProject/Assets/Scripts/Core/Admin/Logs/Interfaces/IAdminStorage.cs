using System.Threading.Tasks;

namespace Core.Admin.Logs.Interfaces
{
	public interface IAdminStorage
	{
		public Task Store(LogEntry entry);
	}
}