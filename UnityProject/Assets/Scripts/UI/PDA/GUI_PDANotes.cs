using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDANotes : NetPage
	{
		[SerializeField]
		private GUI_PDA controller;

		[SerializeField]
		private NetLabel noteText; //Holds a reference to the textbox's netlable

		//private string noteHolder; //Holds the textbox's string just in case

		private void Start()
		{
			UplinkUpdate();
		}
		// might use later
		/*
		public void TextUpdate()
		{
			noteHolder = noteText.Value;
		}
		*/
		//Checks to see if the PDA is an antag PDA by seeing if the uplinkstring is not null
		private void UplinkUpdate()
		{
			if (controller.Pda.uplinkString == null) return;
			string uplinkString = controller.Pda.uplinkString;
			noteText.Value = $"Uplink code: {uplinkString}";
		}
	}
}
