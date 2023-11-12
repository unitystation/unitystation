using Changeling;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changeling
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