using Systems.StatusesAndEffects;
using Systems.StatusesAndEffects.Interfaces;

namespace Tests.StatusAndEffectsFramework
{
	public class MockStatus : StatusEffect
	{

	}

	public class ImmediateStatusEffect : StatusEffect, IImmediateEffect
	{
		public bool DidEffect { get; private set; } = false;

		public override void DoEffect()
		{
			DidEffect = true;
		}
	}

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