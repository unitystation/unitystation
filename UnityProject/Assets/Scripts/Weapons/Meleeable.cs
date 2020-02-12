using System.Collections.Generic;
using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object or tiles to be attacked by melee. 
/// Inventory interactable lets knife cut up materials in the target hand. 
/// </summary>
public class Meleeable : MonoBehaviour, IPredictedCheckedInteractable<PositionalHandApply>, ICheckedInteractable<InventoryApply>
{
	[SerializeField]
	private ItemTrait butcherKnifeTrait;

	[SerializeField]
	private static readonly StandardProgressActionConfig ProgressConfig
	= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	[SerializeField]
	private float butcherTime = 2.0f;

	[SerializeField]
	private string butcherSound = "BladeSlice";

	/// <summary>
	/// Which layers are allowed to be attacked on tiles regardless of intent
	/// </summary>
	private static readonly HashSet<LayerType> attackableLayers = new HashSet<LayerType>(
	new[] {
		LayerType.Grills,
		LayerType.Walls,
		LayerType.Windows
	});

	/// <summary>
	/// Which layers are allowed to be attacked on tiles only on harm intent
	/// </summary>
	/// NOTE: Not allowing attacking base or floors now because it's annoying during combat when you misclick
	// private static readonly HashSet<LayerType> harmIntentOnlyAttackableLayers = new HashSet<LayerType>(
	// 	new[] {
	// 		LayerType.Base,
	// 		LayerType.Floors
	// 	});
	private static readonly HashSet<LayerType> harmIntentOnlyAttackableLayers = new HashSet<LayerType>();

	private InteractableTiles interactableTiles;

	private void Start()
	{
		interactableTiles = GetComponent<InteractableTiles>();
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		//are we in range
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//must be targeting us
		if (interaction.TargetObject != gameObject) return false;
		//allowed to attack due to cooldown?
		//note: actual cooldown is started in WeaponNetworkActions melee logic on server side,
		//clientPredictInteraction on clientside
		if (Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Melee, side)))
		{
			interaction = PositionalHandApply.Invalid;
			return true;
		}

		//not punching unless harm intent
		if (interaction.HandObject == null && interaction.Intent != Intent.Harm) return false;

		//if attacking tiles, only some layers are allowed to be attacked
		if (interactableTiles != null)
		{
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, true);

			//Nothing there, could be space?
			if (tileAt == null) return false;

			if (!attackableLayers.Contains(tileAt.LayerType))
			{
				return interaction.Intent == Intent.Harm && harmIntentOnlyAttackableLayers.Contains(tileAt.LayerType);
			}
		}

		return true;
	}


	public void ClientPredictInteraction(PositionalHandApply interaction)
	{
		//start clientside melee cooldown so we don't try to spam melee
		//requests to server
		Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Melee);
	}

	//no rollback logic
	public void ServerRollbackClient(PositionalHandApply interaction) { }

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
		if (interactableTiles != null)
		{
			//attacking tiles
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, true);
			wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, BodyPartType.None, tileAt.LayerType);
		}
		else
		{
			//attacking objects

			//butcher check
			GameObject victim = interaction.TargetObject;
			var healthComponent = victim.GetComponent<LivingHealthBehaviour>();
			if (healthComponent && healthComponent.allowKnifeHarvest && healthComponent.IsDead && Validations.HasItemTrait(interaction.HandObject, butcherKnifeTrait) && interaction.Intent == Intent.Harm)
			{
				GameObject performer = interaction.Performer;

				void ProgressFinishAction()
				{
					LivingHealthBehaviour victimHealth = victim.GetComponent<LivingHealthBehaviour>();
					victimHealth.Harvest();
					SoundManager.PlayNetworkedAtPos(butcherSound, victim.RegisterTile().WorldPositionServer);
				}

				var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
					.ServerStartProgress(victim.RegisterTile(), butcherTime, performer);
			}
			else
			{
				wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, interaction.TargetBodyPart, LayerType.None);
			}
		}
	}

	//check if item is being applied to offhand with cuttable object on it.
	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
	
		//can the player act at all?
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//interaction only occurs if cutting target is on a hand slot.
		if (!interaction.IsToHandSlot) return false;

		//if the item isn't a knife, no go.
		if (!Validations.HasItemTrait(interaction.FromSlot.Item.gameObject, butcherKnifeTrait)) return false;
		
		//ToSlot must not be empty.
		if (interaction.ToSlot == null) return false;

		return true;
	}
	public void ServerPerformInteraction(InventoryApply interaction)
	{

		//is the target item cuttable?
		ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
		Ingredient ingredient = new Ingredient(attr.ArticleName);
		GameObject cut = CraftingManager.Cuts.FindRecipe(new List<Ingredient> { ingredient });
		if (cut)
		{
			Inventory.ServerDespawn(interaction.TargetSlot);

			SpawnResult spwn = Spawn.ServerPrefab(CraftingManager.Cuts.FindOutputMeal(cut.name), 
			SpawnDestination.At(), 1);

			if (spwn.Successful)
			{
				
				//foreach (GameObject obj in spwn.GameObjects)
				//{
				//	Inventory.ServerAdd(obj,interaction.TargetSlot);
				//}

				Inventory.ServerAdd(spwn.GameObject ,interaction.TargetSlot);

			}

		} else {

			Chat.AddExamineMsgFromServer(interaction.Performer, "You can't cut this.");
		}

	}
}