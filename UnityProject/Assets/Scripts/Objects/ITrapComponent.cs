namespace Objects
{
	/// <summary>
	/// Used to call logic for when activating traps from things like the mouse trap item.
	/// </summary>
	public interface ITrapComponent
	{
		public void TriggerTrap();
	}
}