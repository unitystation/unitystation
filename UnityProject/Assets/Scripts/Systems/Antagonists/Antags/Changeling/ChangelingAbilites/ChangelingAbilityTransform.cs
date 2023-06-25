using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Abilities/Transform")]
	public class ChangelingAbilityTransform : ChangelingData
	{
		public override bool PerfomAbility(ChangelingMain changeling, dynamic objToPerfom)
		{
			changeling.Ui.OpenTransformUI(changeling, this);
			return true;
		}

		public void TransformTo(dynamic data)
		{

		}
	}
}