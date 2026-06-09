using System.Threading.Tasks;

namespace US13.Core.Admin.Logs.Interfaces
{
	public interface IAdminStorage
	{
		public Task Store(object entry);
	}
}