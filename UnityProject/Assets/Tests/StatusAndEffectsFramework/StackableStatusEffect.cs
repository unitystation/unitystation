using US13.Systems.StatusesAndEffects;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace Tests.StatusAndEffectsFramework
{
	public class StackableStatusEffect: StatusEffect, IStackableStatus
	{
		public int InitialStacks { get; set; } = 1;
		public int Stacks { get; set; }

		public void AddStack(int amount)
		{
			Stacks += amount;
		}

		public void RemoveStack(int amount)
		{
			Stacks -= amount;
		}
	}
}
