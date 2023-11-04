using System.Collections;
using System.Collections.Generic;

namespace Systems.Electricity
{
	public class VIRCurrent
	{
		public bool inPool;
		public List<WrapCurrent> CurrentSources = new List<WrapCurrent>();

		public void addCurrent(WrapCurrent NewWrapCurrent)
		{
			foreach (var wrapCurrent in CurrentSources)
			{
				if (wrapCurrent.Current == NewWrapCurrent.Current)
				{
					wrapCurrent.CombineCurrent(NewWrapCurrent);
					return;
				}
			}
			CurrentSources.Add(NewWrapCurrent);
		}

		public void CombineWith(VIRCurrent NewWrapCurrent)
		{
			if (CurrentSources == NewWrapCurrent.CurrentSources) return;
			CurrentSources.AddRange(NewWrapCurrent.CurrentSources);
		}

		public VIRCurrent SplitCurrent(float Multiplier)
		{
			var newVIRCurrent = ElectricalPool.GetVIRCurrent();
			foreach (var CurrentS in CurrentSources)
			{
				var newWCurrent = ElectricalPool.GetWrapCurrent();
				newWCurrent.SetUp(CurrentS);
				newVIRCurrent.CurrentSources.Add(newWCurrent);
			}

			foreach (var CurrentS in newVIRCurrent.CurrentSources)
			{
				CurrentS.Multiply(Multiplier);
			}
			return newVIRCurrent;
		}

		public double Current()
		{
			double current = 0;

			foreach (var wrapCurrent in CurrentSources)
			{
				current += wrapCurrent.GetCurrent();
			}

			return current;
		}

		public override string ToString()
		{
			return string.Format(Current().ToString() + "[" + string.Join(",", CurrentSources) + "]");
		}

		public void Pool()
		{
			if (inPool == false)
			{
				foreach (var CurrentSource in CurrentSources)
				{
					CurrentSource.Pool();
				}
				CurrentSources.Clear();
				ElectricalPool.PooledVIRCurrent.Add(this);
				inPool = true;
			}
		}
	}
}
