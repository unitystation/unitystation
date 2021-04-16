﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPage : NetPage
	{
		public string DirectoryName = "/.../available_supplies";
		public GUI_Cargo cargoGUI;

		/// <summary>
		/// Method to update tab info on it's opening
		/// Called from GUI_Cargo on SwitchTab()
		/// </summary>
		public virtual void OpenTab()
		{

		}

		public virtual void UpdateTab()
		{

		}

	}
}
