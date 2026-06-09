using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Systems.Faith
{
	[Serializable]
	public class Faith
	{
		[field: SerializeField] public string FaithName { get; set; }
		[field: SerializeField, TextArea] public string FaithDesc { get; set; }
		[field: SerializeField] public Sprite FaithIcon { get; set; }
		[field: SerializeField] public string GodName { get; set; }
		[field: SerializeField, TextArea] public string NanotrasenProgressMessage { get; set; }
		public string ProclamationText { get; set; } = "";
		public string RejectionText { get; set; } = "";
		[field: SerializeField] public ToleranceToOtherFaiths ToleranceToOtherFaiths { get; set; } = ToleranceToOtherFaiths.Neutral;

		[SerializeReference, SelectImplementation(typeof(IFaithProperty))]
		public List<IFaithProperty> FaithProperties = new List<IFaithProperty>();

		[SerializeReference, SelectImplementation(typeof(IFaithMiracle))]
		public List<IFaithMiracle> FaithMiracles = new List<IFaithMiracle>();

		[SerializeReference, SelectImplementation(typeof(IFaithProclamationTextGenerator))]
		public IFaithProclamationTextGenerator ProclamationTextGenerator;
	}

	public enum ToleranceToOtherFaiths
	{
		Accepting,
		Neutral,
		Rejecting,
		Violent,
	}
}