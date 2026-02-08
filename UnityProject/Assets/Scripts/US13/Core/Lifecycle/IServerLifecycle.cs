
namespace US13.Core.Lifecycle
{
	/// <summary>
	/// Indicates a component which implements all server lifecycle hooks (spawn / despawn)
	/// </summary>
	public interface IServerLifecycle : IServerSpawn, IServerDespawn
	{

	}
}
