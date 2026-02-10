namespace US13.Tilemaps.Behaviours.Meta
{
	public interface IInitialiseSystem
	{
		public int Priority { get; }
		public void Initialize();
	}
}
