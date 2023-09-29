using Objects.Lighting;
using ScriptableObjects;
using UnityEngine;

namespace Systems.Faith.FaithProperties
{
	public class Darkness : IFaithProperty
	{
		[SerializeField] private string faithPropertyName = "Darkness";
		[SerializeField] private string faithPropertyDesc = "People of this faith lurk in the darkness.";
		[SerializeField] private Sprite propertyIcon;

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

		public void Setup()
		{
			FaithManager.Instance.FaithPropertiesEventUpdate.Add(CheckNearbyLights);
		}

		private void CheckNearbyLights()
		{
			foreach (var member in FaithManager.Instance.FaithMembers)
			{
				if (member.IsDeadOrGhost) continue;
				var overlapBox = Physics2D.OverlapBoxAll(member.gameObject.AssumedWorldPosServer(), new Vector2(6, 6), 0);
				foreach (var collider in overlapBox)
				{
					if (collider.TryGetComponent<LightSource>(out var lightSource) == false) continue;
					if (MatrixManager.Linecast(member.AssumedWorldPos,
						    LayerTypeSelection.Walls, LayerMask.GetMask("Walls"),
						    collider.gameObject.AssumedWorldPosServer()).ItHit == false) continue;
					if (lightSource.MountState == LightMountState.On)
					{
						if (lightSource.CurrentOnColor.a < 0.5f)
						{
							FaithManager.AwardPoints(15);
							continue;
						}
						else
						{
							FaithManager.TakePoints(25);
							Chat.AddExamineMsg(member.GameObject, $"The nearby {collider.gameObject.ExpensiveName()} is too bright..");
							continue;
						}
					}
					FaithManager.AwardPoints(25);
				}
			}
		}

		public void OnJoinFaith(PlayerScript newMember)
		{
			Chat.AddExamineMsg(newMember.GameObject, $"Your eyes grow your discomfort when standing next to bright lights now..");
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			Chat.AddExamineMsg(member.GameObject, $"Your eyes are unbothered by bright lights now.");
		}


		public void RandomEvent()
		{

		}
	}
}