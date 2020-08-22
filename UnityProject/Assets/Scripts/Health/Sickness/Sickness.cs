using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	public class Sickness : MonoBehaviour
	{
		/// <summary>
		/// Name of the sickness
		/// </summary>
		public string SicknessName;

		/// <summary>
		/// Indicates if the sickness is contagious or not.
		/// </summary>
		public bool Contagious;

		/// <summary>
		/// List of all the stages of a particular sickness
		/// </summary>
		[SerializeField]
		private List<SicknessStage> sicknessStages;

		public Sickness()
		{
			sicknessStages = new List<SicknessStage>();
		}

		/// <summary>
		/// Name of the sickness
		/// </summary>
		public string Name
		{
			get
			{
				if (string.IsNullOrEmpty(name))
					return "<Unnamed>";

				return name;
			}
		}

		/// <summary>
		/// List of all the stages of a particular sickness
		/// </summary>
		public List<SicknessStage> SicknessStages
		{
			get
			{
				return sicknessStages;
			}
		}
	}
}