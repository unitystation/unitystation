using System;
using UnityEngine;
using US13.Managers;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;
using US13.UI.Objects.Medical;

namespace US13.UI.Objects.MedBed
{
	public class GUI_ChemicalInjection : DynamicEntry
	{

		public NetText_label CapacityText;

		public NetText_label ReagentText;

		public NetSlider CapacitySlider;

		public GUI_MedBed TParent;

		public US13.Objects.Medical.MedBed.ReagentReGenAndCap ReagentReGenAndCap;

		public void SetUp(US13.Objects.Medical.MedBed.ReagentReGenAndCap ReagentReGenAndCap, GUI_MedBed TParent)
		{
			this.ReagentReGenAndCap = ReagentReGenAndCap;
			this.TParent = TParent;
			UpdateVariables();
		}

		public void UpdateVariables()
		{
			CapacityText.MasterSetValue(Math.Round(ReagentReGenAndCap.CurrentReagents,1) + "u");
			ReagentText.MasterSetValue(string.IsNullOrEmpty(ReagentReGenAndCap?.Reagent?.Name) ? "Custom" :  ReagentReGenAndCap.Reagent.Name );
			CapacitySlider.MasterSetValue( Mathf.Round(( (ReagentReGenAndCap.CurrentReagents == 0 ? 1 : ReagentReGenAndCap.CurrentReagents) /ReagentReGenAndCap.ReagentCap)*100).ToString() );
		}

		public void Inject10(PlayerInfo subject)
		{

			if (TParent.MedBed.LivingHealthMasterBase?.gameObject == subject.GameObject) return;
			TParent.MedBed.InjectReagent(ReagentReGenAndCap, 10);
		}


		public void Inject5(PlayerInfo subject)
		{
			if (TParent.MedBed.LivingHealthMasterBase?.gameObject == subject.GameObject) return;
			TParent.MedBed.InjectReagent(ReagentReGenAndCap, 5);
		}


		public void Inject2(PlayerInfo subject)
		{
			if (TParent.MedBed.LivingHealthMasterBase?.gameObject == subject.GameObject) return;
			TParent.MedBed.InjectReagent(ReagentReGenAndCap, 2);
		}
	}
}
