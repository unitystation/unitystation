using System;

namespace NaughtyAttributes
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class DisableIfAttribute : EnableIfAttributeBase
	{
		public DisableIfAttribute(string condition)
			: base(condition)
		{
			Inverted = true;
		}

		public DisableIfAttribute(EConditionOperator conditionOperator, params string[] conditions)
			: base(conditionOperator, conditions)
		{
			Inverted = true;
		}
	}
}
