using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Systems.Clearance;
using Systems.Electricity;
using Systems.Explosions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Objects
{
	[Serializable]
	public class BarSignEntry
	{
		public string name;
		public string description;
	}
	
	[RequireComponent(typeof(ClearanceRestricted))]
	public class BarSign : NetworkBehaviour, ICheckedInteractable<HandApply>, IEmpAble, IAPCPowerable {
		
		private ClearanceRestricted restricted;
		private SpriteHandler spriteHandler;
		private ObjectAttributes attributes;
		private LightEmissionBehaviour emission;
		
		[SerializeField] private bool randomStartingIndex;
		[SerializeField] private int startingIndex = EMPTY_SPRITE;

		private const int EMP_SPRITE = 0;
		private const int EMPTY_SPRITE = 1;
		
		private int signIndex;
		
		[SerializeField] private bool isLocked = true;
		[SerializeField] private bool canLockBeToggled = true;
		private bool isEMPed;
		private bool isPowered;
		private bool isReady;
		
		[SerializeField] private List<BarSignEntry> barSigns = new();
		
		private void Awake()
		{
			restricted = GetComponent<ClearanceRestricted>();
			attributes = GetComponent<ObjectAttributes>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			emission = GetComponentInChildren<LightEmissionBehaviour>();
			signIndex = randomStartingIndex ? Random.Range(EMPTY_SPRITE + 1, spriteHandler.CatalogueCount) : startingIndex;
			isReady = true;
		}
		
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (isEMPed || isPowered == false) return false;
			return true;
		}
		
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (isEMPed || isPowered == false) return;
			if (restricted.HasClearance(interaction.HandObject) && canLockBeToggled)
			{
				isLocked = !isLocked;

				Chat.AddActionMsgToChat(interaction.Performer,
					$"You {(isLocked ? "lock" : "unlock")} the bar sign.",
					$"{interaction.PerformerPlayerScript.visibleName} {(isLocked ? "locks" : "unlocks")} the bar sign.");
			} else if (interaction.HandObject == null && isLocked == false)
			{
				IncrementSign();
			}
		}

		private void IncrementSign()
		{
			var newindex = spriteHandler.CurrentSpriteIndex + 1;
			ChangeSign(newindex > spriteHandler.CatalogueCount - 1 ? EMPTY_SPRITE + 1 : newindex);
		}
		
		private void ChangeSign(int index)
		{
			if (isServer)
			{
				spriteHandler.SetCatalogueIndexSprite(index);
				if (index > EMPTY_SPRITE)
				{
					signIndex = index;
					attributes.ServerSetArticleName($"{barSigns[index - 2].name} Bar Sign");
					attributes.ServerSetArticleDescription(barSigns[index - 2].description);			
				}
				else
				{
					attributes.ServerSetArticleName("Bar Sign");
					attributes.ServerSetArticleDescription("");			
				}
			}
		}

		public void OnEmp(int empStrength)
		{
			if (isEMPed == false)
			{
				StartCoroutine(Emp(empStrength));
			}
		}
		
		public IEnumerator Emp(int empStrength)
		{
			int effectTime = (int)(empStrength * 0.75f);
			isEMPed = true;
			ChangeSign(EMP_SPRITE);
			yield return WaitFor.Seconds(effectTime);
			signIndex = Random.Range(EMPTY_SPRITE + 1, spriteHandler.CatalogueCount);
			ChangeSign(signIndex);
			isEMPed = false;
		}

		public void PowerNetworkUpdate(float voltage)
		{
			//Unused as this code only cares about the current state
		}

		public void StateUpdate(PowerState state)
		{
			//Dont update if emp'd or itll overwrite the emp effect
			if (isEMPed) return;
			switch (state)
			{
				case PowerState.On:
				case PowerState.LowVoltage:
				case PowerState.OverVoltage:
					if (isReady)
					{
						isPowered = true;
						emission.enabled = true;
						ChangeSign(signIndex);
					}
					break;
				case PowerState.Off:
					isPowered = false;
					ChangeSign(EMPTY_SPRITE);
					emission.enabled = false;
					break;
			}
		}
	}	
}
