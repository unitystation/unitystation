using Logs;
using US13.Systems.Electricity.Electrical_processes;

namespace US13.Systems.Electricity.FunctionsAndClasses
{

	public class WrapCurrent
	{
		public bool inPool;
		public Current Current;
		public double Strength;

		public void CombineCurrent(WrapCurrent addSendingCurrent)
		{
			if (Current == addSendingCurrent.Current)
			{
				Strength = Strength + addSendingCurrent.Strength;
			}
			else
			{
				Loggy.Info("Tried to combine two currents, but they were not equal", Category.Electrical);
			}
		}

		public void Multiply(float Multiply)
		{
			Strength = Strength * Multiply;
		}

		public void SetUp(WrapCurrent _Current)
		{
			Current = _Current.Current;
			Strength = _Current.Strength;
		}

		public double GetCurrent()
		{
			return Current.current * Strength;
		}

		public override string ToString()
		{
			return string.Format("(" + Current.current + "*" + Strength + ")");
		}

		public void Pool()
		{
			if (inPool == false)
			{
				Current = null;
				Strength = 1;
				ElectricalPool.PooledWrapCurrent.Add(this);
				inPool = true;
			}
		}
	}
}