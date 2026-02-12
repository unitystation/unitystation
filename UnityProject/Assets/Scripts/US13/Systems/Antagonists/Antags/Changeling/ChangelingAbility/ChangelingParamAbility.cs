using System.Collections.Generic;

namespace US13.Systems.Antagonists.Antags.Changeling.ChangelingAbility
{
	public class ChangelingParamAbility: ChangelingBaseAbility
	{
		public virtual bool UseAbilityParamClient(ChangelingMain changeling)
		{
			return true;
		}

		public virtual bool UseAbilityParamServer(ChangelingMain changeling, List<string> param)
		{
			return true;
		}
	}
}