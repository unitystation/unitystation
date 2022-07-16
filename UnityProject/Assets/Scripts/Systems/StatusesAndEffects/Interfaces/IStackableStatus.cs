namespace Systems.StatusesAndEffects.Interfaces
{
	public interface IStackableStatus
	{
		/// <summary>
		/// Initial amount of stacks for this stackable status. If added and there is a previous existing stack, it
		/// will be added to the existing stack.
		/// </summary>
		public int InitialStacks { get; set; }

		/// <summary>
		/// Amount of stacks this status has.
		/// </summary>
		int Stacks { get; set; }

		/// <summary>
		/// This is what the manager calls when adding stacks to an active status
		/// </summary>
		void AddStack(int amount)
		{
			Stacks += amount;
		}

		/// <summary>
		/// This is what the manager calls when removing stacks to an active status
		/// </summary>
		void RemoveStack(int amount)
		{
			Stacks -= amount;
		}
	}
}