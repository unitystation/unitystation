using System.Linq;
using Core;
using Logs;
using Managers;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Germaphobe : IFaithProperty
	{
		[SerializeField] private string faithPropertyName = "Germaphobe";
		[SerializeField] private string faithPropertyDesc = "People of this faith cannot stand filth and trash.";
		[SerializeField] private Sprite propertyIcon;
		[SerializeField] private ItemTrait filthTrait;

		string IFaithProperty.FaithPropertyName
		{
			get => faithPropertyName;
			set => faithPropertyName = value;
		}

		string IFaithProperty.FaithPropertyDesc
		{
			get => faithPropertyDesc;
			set => faithPropertyDesc = value;
		}

		Sprite IFaithProperty.PropertyIcon
		{
			get => propertyIcon;
			set => propertyIcon = value;
		}

		public FaithData AssociatedFaith { get; set; }

		private int FilthScore = 0;

		public void Setup(FaithData data)
		{
			throw new System.NotImplementedException();
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			throw new System.NotImplementedException();
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			throw new System.NotImplementedException();
		}

		public void RandomEvent()
		{
			throw new System.NotImplementedException();
		}

		private void CheckFilthLevels()
		{
			var total = ComponentsTracker<Attributes>.Instances.Count(x => x.InitialTraits.Contains(filthTrait)
			                                                               && x.gameObject.RegisterTile().Matrix == MatrixManager.MainStationMatrix.Matrix);
			if (FilthScore == 0)
			{
				FilthScore = total;
				return;
			}
			if (total <= FilthScore) return;
			NergulEvent(total);
			FilthScore = total;
		}

		private void NergulEvent(int score)
		{
			Loggy.Log($"[Germaphobe/RegulEvent] - {score}");
			if (score > FilthScore)
			{

			}
		}
	}
}