using System.Collections;
using Logs;
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
		[SerializeField] private float minimumAlphaForDarkness = 0.65f;

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
						if (lightSource.CurrentOnColor.a <= minimumAlphaForDarkness)
						{
							FaithManager.AwardPoints(15);
							if(Application.isEditor) Loggy.Log("Awarded points for having low darkness value.");
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
			Chat.AddExamineMsg(newMember.GameObject, $"Your eyes grow in discomfort when standing next to bright lights now..");
		}

		public void OnLeaveFaith(PlayerScript member)
		{
			Chat.AddExamineMsg(member.GameObject, $"Your eyes are unbothered by bright lights now.");
		}


		public void RandomEvent()
		{
			//Todo: add more events based on points for darkness.
			if (DMMath.Prob(15))
			{
				Chat.AddGameWideSystemMsgToChat("<color=red>An entity is lashing out on station lights..");
				GameManager.Instance.StartCoroutine(KillAllLights());
			}
		}

		private IEnumerator KillAllLights()
		{
			var currentIndex = 0;
			var maximumIndexes = 20;
			foreach (var stationObject in MatrixManager.MainStationMatrix.Objects.GetComponentsInChildren<LightSource>())
			{
				if (currentIndex >= maximumIndexes)
				{
					currentIndex = 0;
					yield return WaitFor.EndOfFrame;
				}
				if (stationObject.CurrentOnColor.a <= minimumAlphaForDarkness) continue;
				if (DMMath.Prob(50)) continue;
				stationObject.Integrity.ForceDestroy();
				currentIndex++;
			}
		}
	}
}