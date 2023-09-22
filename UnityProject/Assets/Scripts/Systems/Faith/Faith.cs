using System;
using System.Collections.Generic;
using Core.Editor.Attributes;
using UnityEngine;

namespace Systems.Faith
{
	[Serializable]
	public class Faith
	{
		[field: SerializeField] public string FaithName { get; set; }
		[field: SerializeField, TextArea] public string FaithDesc { get; set; }
		[field: SerializeField] public Sprite FaithIcon { get; set; }
		[field: SerializeField] public string GodName { get; set; }
		[field: SerializeField, TextArea] public string NanotrasenProgressMessage { get; set; }
		[field: SerializeField] public ToleranceToOtherFaiths ToleranceToOtherFaiths { get; set; } = ToleranceToOtherFaiths.Neutral;

		[SerializeReference, SelectImplementation(typeof(IFaithProperty))]
		public List<IFaithProperty> FaithProperties = new List<IFaithProperty>();
	}

	public enum ToleranceToOtherFaiths
	{
		Accepting,
		Neutral,
		Rejecting,
		Violent,
	}
}