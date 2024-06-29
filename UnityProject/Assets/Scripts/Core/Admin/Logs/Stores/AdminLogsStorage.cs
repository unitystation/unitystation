using System.Threading.Tasks;
using Core.Admin.Logs.Interfaces;
using UnityEngine;

namespace Core.Admin.Logs.Stores
{
	public class AdminLogsStorage : MonoBehaviour, IAdminStorage
	{
		public async Task Store(LogEntry entry)
		{

		}
	}
}