using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Systems.Explosions;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Items.Others
{
	[RequireComponent(typeof(Integrity))]
	[RequireComponent(typeof(Pickupable))]
	public class Gibtonite : MonoBehaviour, ICheckedInteractable<HandApply>, IHoverTooltip
	{
		private enum GibState
		{
			ACTIVE,
			INACTIVE,
			FUSED
		}

		private Pickupable pickupable;
		private Integrity integrity;
		[SerializeField] private SpriteHandler spritehandler;

		private GibState state = GibState.FUSED;
		[SerializeField] private float explosionStrength = 120f;
		[SerializeField] private float fullDifuselTime = 12f;
		[SerializeField] private int fuseTime = 2750;
		[SerializeField] private SpriteDataSO spriteActive;
		[SerializeField] private SpriteDataSO spriteInActive;
		[SerializeField] private SpriteDataSO spriteFused;
		[SerializeField] private ItemTrait miningScanner;
		[SerializeField] private ItemTrait pickaxe;
		private bool willExpload = false;


		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			integrity = GetComponent<Integrity>();
		}

		private void Start()
		{
			integrity.OnApplyDamage.AddListener(OnDamageTaken);
			// If this ore starts in storage already, then assume it is safe.
			SetState(pickupable.ItemSlot != null ? GibState.INACTIVE : GibState.FUSED);
		}

		private void SetState(GibState newState)
		{
			state = newState;
			switch (state)
			{
				case GibState.ACTIVE:
					spritehandler.SetSpriteSO(spriteActive);
					StopFuse();
					break;
				case GibState.INACTIVE:
					spritehandler.SetSpriteSO(spriteInActive);
					StopFuse();
					break;
				case GibState.FUSED:
					spritehandler.SetSpriteSO(spriteFused);
					_ = Fuse();
					break;
			}
		}

		private void OnDamageTaken(DamageInfo damageInfo)
		{
			if ( damageInfo.Damage == 0 ) return;
			if ( damageInfo.DamageType == DamageType.Radiation ) return;
			switch (state)
			{
				case GibState.INACTIVE:
					if(damageInfo.DamageType == DamageType.Burn || damageInfo.AttackType == AttackType.Energy || damageInfo.AttackType == AttackType.Fire)
						SetState(GibState.ACTIVE);
					return;
				case GibState.ACTIVE:
					SetState(GibState.FUSED);
					return;
				default:
					Expload();
					break;
			}
		}

		private async Task Fuse()
		{
			willExpload = true;
			await Task.Delay(fuseTime);
			if (willExpload) Expload();
		}

		private void StopFuse()
		{
			willExpload = false;
		}

		private void Expload()
		{
			var pos = gameObject.AssumedWorldPosServer().CutToInt();
			_ = Despawn.ServerSingle(gameObject);
			Explosion.StartExplosion(pos, explosionStrength);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandSlot.ItemAttributes.GetTraits().Contains(miningScanner) && state == GibState.FUSED)
			{
				StopFuse();
				SetState(GibState.ACTIVE);
				return;
			}

			if (interaction.HandSlot.ItemAttributes.GetTraits().Contains(pickaxe) && state == GibState.ACTIVE)
			{
				StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.Craft)
					, ()=> SetState(GibState.INACTIVE)).ServerStartProgress(interaction.Performer.RegisterTile(),
					fullDifuselTime, interaction.Performer);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return interaction.HandSlot != null
			       && interaction.HandSlot.ItemAttributes != null
			       && DefaultWillInteract.Default(interaction, side);
		}

		public string HoverTip()
		{
			return state switch
			{
				GibState.ACTIVE => "It is active! It might explode if it receives any damage.",
				GibState.INACTIVE => "It is inactive and relatively safe for now.",
				GibState.FUSED => "OHGODOHFUCK",
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		public string CustomTitle() => null;

		public Sprite CustomIcon() => null;

		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings()
		{
			return new List<TextColor>()
			{
				new TextColor() { Color = Color.green, Text = "Use a drill or a pickaxe to disable it completely." },
				new TextColor() { Color = Color.red, Text = "If you see it flashing, either run or quickly click on it with a mining scanner." },
			};
		}
	}
}