using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

namespace Systems.Electricity
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
				Loggy.Log("Tried to combine two currents, but they were not equal", Category.Electrical);
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