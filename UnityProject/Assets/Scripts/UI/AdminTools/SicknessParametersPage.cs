using AdminTools;
using Assets.Scripts.Health.Sickness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.AdminTools
{
	public class SicknessParametersPage: AdminPage
	{
		[SerializeField]
		private Dropdown sicknessDropdown = null;

		[SerializeField]
		private InputField NumberOfPlayerInput = null;

		public void Awake()
		{
			sicknessDropdown.ClearOptions();

			List<Dropdown.OptionData> optionDatas = new List<Dropdown.OptionData>();

			foreach (Sickness sickness in SicknessManager.Instance.Sicknesses)
			{
				optionDatas.Add(new Dropdown.OptionData(sickness.SicknessName));
			}

			sicknessDropdown.AddOptions(optionDatas);
		}

		public void StartInfection()
		{

		}
	}
}
