using System;
using UnityEngine;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

public class GUI_ChemicalInjection : DynamicEntry
{

	public NetText_label CapacityText;

	public NetText_label ReagentText;

	public NetSlider CapacitySlider;

	public GUI_MedBed TParent;

	public MedBed.ReagentReGenAndCap ReagentReGenAndCap;

	public void SetUp(MedBed.ReagentReGenAndCap ReagentReGenAndCap, GUI_MedBed TParent)
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

	public void Inject10()
	{
		TParent.MedBed.InjectReagent(ReagentReGenAndCap, 10f);
	}

	public void Inject5()
	{
		TParent.MedBed.InjectReagent(ReagentReGenAndCap, 5f);
	}

	public void Inject2()
	{
		TParent.MedBed.InjectReagent(ReagentReGenAndCap, 2f);
	}
}
