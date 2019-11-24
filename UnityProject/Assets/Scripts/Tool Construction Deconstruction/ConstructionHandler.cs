using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

public class ConstructionHandler : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public IConstructionHandler RelatedInterface;
	public List<ConstructionStage> ConstructionStages;
	public SpriteRenderer TSpriteRenderer;
	public RegisterObject registerObject;

	public List<KeyValuePair<SpriteRenderer,bool>> otherSpriteRenderer = new List<KeyValuePair<SpriteRenderer, bool>>();
	public Dictionary<int, List<ComponentData>> ContainedObjects = new Dictionary<int, List<ComponentData>>();
	public List<int> ListChance;
	public System.Random random = new System.Random();

	[SyncVar(hook = "ClientGoToStage")]
	public int CurrentStage = 0;
	public bool GenerateComponents = true;

	public GameObject PrefabCircuitBoard;

	[HideInInspector]
	public GameObject CircuitBoard;

	[HideInInspector]
	public List<MonoBehaviour> DisabledMonoBehaviours = new List<MonoBehaviour>();

	public GameObject StandardConstructionComponent;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		return (InteractionCheck(interaction, side));

	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var slot = interaction.HandSlot;

		if (RelatedInterface != null)
		{
			if (!RelatedInterface.InteractionUpdate(interaction, slot, this))
			{
				return;
			}
		}
		if (ContainedObjects[CurrentStage] != null)
		{
			foreach (var _Object in ContainedObjects[CurrentStage]) {
				if (_Object.NumberNeeded > _Object.NumberPresent)
				{
					if (_Object.GameObject != null)
					{
						if (slot.Item.GetComponent(_Object.IdentifyingComponent) != null)
						{
							if (_Object.TimeNeeded > 0)
							{
								var progressFinishAction = new ProgressCompleteAction(() => ExceptItem(slot, interaction));
								UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, _Object.TimeNeeded, progressFinishAction, interaction.Performer);
							}
							else {
								ConstructionStages[CurrentStage].PresentParts.Add(slot.ItemObject);
								//TODO: In need of refactor throughout this component to not use Inventory.Vanish, instead should have an ItemStorage of its own most likely
								Inventory.ServerVanish(slot);
								_Object.NumberPresent++;
							}
						}
					}
					else if (_Object.CType != ConstructionElementType.Null){
						var Item = slot.ItemObject?.GetComponent<ConstructionComponent>();
						if (Item != null) {
							if (Item.CType == _Object.CType && Item.level >= _Object.level) {
								if (_Object.TimeNeeded > 0) {
									var progressFinishAction = new ProgressCompleteAction(() => ExceptItem(slot, interaction));
									UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, _Object.TimeNeeded, progressFinishAction, interaction.Performer);
								}
								else {
									ConstructionStages[CurrentStage].PresentParts.Add(slot.ItemObject);
									Inventory.ServerVanish(slot);
									_Object.NumberPresent++;
								}
							}
						}
					}
				}
			}
		}

		var attrs = slot.Item?.GetComponent<IItemAttributes>();
		var tool = slot.Item?.GetComponent<Tool>();
		if (attrs == null || tool == null)
		{
			return;
		}

		foreach (var trait in attrs.GetTraits())
		{
			if (ConstructionStages[CurrentStage].TraitStage.ContainsKey(trait))
			{
				var Jump = ConstructionStages[CurrentStage].TraitStage[trait];
				if (Jump.ConstructionTime > 0)
				{
					var progressFinishAction = new ProgressCompleteAction(() => JumpLanding(tool, attrs));
					UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer,
						Jump.ConstructionTime / tool.SpeedMultiplier, progressFinishAction, interaction.Performer);
				}
				else
				{
					JumpLanding(tool, attrs);
				}
			}
		}
	}

	public bool InteractionCheck(HandApply interaction, NetworkSide side) {
		if (side == NetworkSide.Client) {
			if (interaction.HandSlot != null) {
				return (true);
			}
		}

		var slot = interaction.HandSlot;

		if (RelatedInterface != null)
		{
			return (RelatedInterface.CanInteraction(interaction, slot, this));
		}
		if (ContainedObjects[CurrentStage] != null)
		{
			foreach (var _Object in ContainedObjects[CurrentStage])
			{
				if (_Object.NumberNeeded > _Object.NumberPresent)
				{
					if (_Object.GameObject != null)
					{
						if (slot.Item.GetComponent(_Object.IdentifyingComponent) != null)
						{
							return (true);
						}
					}
					else if (_Object.CType != ConstructionElementType.Null)
					{
						var Item = slot.Item?.GetComponent<ConstructionComponent>();
						if (Item != null)
						{
							if (Item.CType == _Object.CType && Item.level >= _Object.level)
							{
								return (true);
							}
						}
					}
				}
			}
		}

		var tool = slot.Item?.GetComponent<Tool>();
		var attrs = slot.Item?.GetComponent<IItemAttributes>();
		if (tool == null || attrs == null)
		{
			return (false);
		}

		foreach (var trait in attrs.GetTraits())
		{
			if (ConstructionStages[CurrentStage].TraitStage.ContainsKey(trait))
			{
				return (true);
			}
		}

		return (false);
	}
	public void ExceptItem(ItemSlot slot, HandApply interaction) {
	if (ContainedObjects[CurrentStage] != null)
		{
			foreach (var _Object in ContainedObjects[CurrentStage])
			{
				if (_Object.NumberNeeded > _Object.NumberPresent)
				{
					if (_Object.GameObject != null)
					{
						if (slot.Item.GetComponent(_Object.IdentifyingComponent) != null)
						{
							ConstructionStages[CurrentStage].PresentParts.Add(slot.ItemObject);
							Inventory.ServerVanish(slot);
							_Object.NumberPresent++;
						}
					}
					else if (_Object.CType != ConstructionElementType.Null)
					{
						var Item = slot.Item?.GetComponent<ConstructionComponent>();
						if (Item != null)
						{
							if (Item.CType == _Object.CType && Item.level >= _Object.level)
							{
								ConstructionStages[CurrentStage].PresentParts.Add(slot.ItemObject);
								Inventory.ServerVanish(slot);
								_Object.NumberPresent++;
							}
						}
					}
				}
			}
			//if
		}
	}

	public void JumpLanding(Tool tool, IItemAttributes attrs)
	{
		foreach (var trait in attrs.GetTraits())
		{
			if (ConstructionStages[CurrentStage].TraitStage.ContainsKey(trait))
			{
				var Jump = ConstructionStages[CurrentStage].TraitStage[trait];
				float SuccessChance = (tool.SuccessChance / 100) * Jump.SuccessChance;
				if (!(SuccessChance < ListChance[random.Next(99)]))
				{
					if (Jump.Construction)
					{
						ConstructionStages[CurrentStage].CheckParts();
						if (!ConstructionStages[CurrentStage].MissingParts)
						{
							GoToStage(Jump.JumpToStage);
						}
					}
					else
					{
						SpawnStage(CurrentStage);
						SpawnStage(Jump.JumpToStage);
						GoToStage(Jump.JumpToStage);
					}
				}
				else
				{
					Logger.Log("you Failed!");
					return;
				}
			}
		}
		Logger.Log("you Failed!");
	}

	public void ClientGoToStage(int Stage)
	{
		if (ConstructionStages[CurrentStage].ObjectStateofStage == ObjectState.Normal && ConstructionStages[Stage].ObjectStateofStage == ObjectState.InConstruction)
		{
			SetDefaultState(false);

		}
		CurrentStage = Stage;
		TSpriteRenderer.sprite = ConstructionStages[CurrentStage].StageSprite;
		if (ConstructionStages[CurrentStage].ObjectStateofStage == ObjectState.Normal)
		{
			SetDefaultState(true);
		}
	}

	public void GoToStage(int Stage)
	{
		if (ConstructionStages[CurrentStage].ObjectStateofStage != ConstructionStages[Stage].ObjectStateofStage )
		{
			gameObject.BroadcastMessage("ObjectStateChange", ConstructionStages[Stage].ObjectStateofStage, SendMessageOptions.DontRequireReceiver);
			SetDefaultState(false);

		}
		CurrentStage = Stage;
		TSpriteRenderer.sprite = ConstructionStages[CurrentStage].StageSprite;
		if (ConstructionStages[CurrentStage].ObjectStateofStage == ObjectState.Normal)
		{
			gameObject.BroadcastMessage("ObjectStateChange", ObjectState.Normal, SendMessageOptions.DontRequireReceiver);
			SetDefaultState(true);
		}
		if (ConstructionStages[CurrentStage].FinalDeconstructedResult != null) {

			SpawnStage(CurrentStage);
			var Objecte = Spawn.ServerPrefab(ConstructionStages[CurrentStage].FinalDeconstructedResult, this.transform.position, parent: this.transform.parent);

			if (CircuitBoard != null)
			{
				CustomNetTransform netTransform = CircuitBoard.GetComponent<CustomNetTransform>();
				netTransform.AppearAtPosition(this.transform.position);
				netTransform.AppearAtPositionServer(this.transform.position);
			}
			Despawn.ServerSingle(this.gameObject);
		}
		//ConstructionStages[CurrentStage]
	}
	public void SetDefaultState(bool Toggle) {
		setOtherSprites(Toggle);
	}
	public bool otherSpritesContain(SpriteRenderer SR)
	{
		foreach (var _Sprite in otherSpriteRenderer) {
			if (_Sprite.Key == SR) {
				return (true);
			}
		}
		return (false);
	}

	public void setOtherSprites(bool Toggle) {
		var sp = this.GetComponentsInChildren<SpriteRenderer>();
		foreach (var SR in sp) {
			if (!otherSpritesContain(SR) && (SR != TSpriteRenderer)) {
				var kandV = new KeyValuePair<SpriteRenderer, bool>(SR, SR.enabled);
				otherSpriteRenderer.Add(kandV);
			}

		}
		//if (prefab != PrefabUtility.GetCorrespondingObjectFromSource(selectedObject))
		foreach (var SR in otherSpriteRenderer) {
			if (Toggle)
			{
				SR.Key.gameObject.SetActive(SR.Value);
			}
			else {
				SR.Key.gameObject.SetActive(Toggle);
			}
		}
		TSpriteRenderer.gameObject.SetActive(!Toggle);
	}

	void Start()
	{
		TSpriteRenderer = Instantiate(TSpriteRenderer).GetComponent<SpriteRenderer>();
		TSpriteRenderer.gameObject.transform.SetParent(this.transform, false);
		if (isServer)
		{
			if (RelatedInterface == null)
			{
				RelatedInterface = this.gameObject.GetComponent<IConstructionHandler>();
			}
			if (registerObject == null)
			{
				registerObject = this.gameObject.GetComponent<RegisterObject>();
			}

			if (PrefabCircuitBoard != null && CircuitBoard == null)
			{
				CircuitBoard = Spawn.ServerPrefab(PrefabCircuitBoard, this.transform.position, parent: this.transform.parent).GameObject;
				if (CircuitBoard != null)
				{
					CustomNetTransform netTransform = CircuitBoard.GetComponent<CustomNetTransform>();
					netTransform.DisappearFromWorldServer();
				}
			}
			ListChance = Enumerable.Range(1, 100).ToList();
			int c = 0;
			foreach (var Stage in ConstructionStages)
			{

				foreach (var TOStageAdvance in Stage.StageAdvances)
				{
					Stage.TraitStage[TOStageAdvance.RequiredTrait] = TOStageAdvance;
				}


				GenerateStage(Stage, c);

				if (!ContainedObjects.ContainsKey(c))
				{
					ContainedObjects[c] = new List<ComponentData>();
				}
				c++;

			}

			GoToStage(CurrentStage);
		}
		else
		{
			foreach (var Stage in ConstructionStages)
			{
				foreach (var TOStageAdvance in Stage.StageAdvances)
				{
					Stage.TraitStage[TOStageAdvance.RequiredTrait] = TOStageAdvance;
				}

			}

		}
	}

	public void SpawnStage(int StageNumber) {
		foreach (var _Object in ContainedObjects[StageNumber])
		{
			for (int i = 0; i < ConstructionStages[StageNumber].PresentParts.Count; i++)
			{
				CustomNetTransform netTransform = ConstructionStages[StageNumber].PresentParts[i].GetComponent<CustomNetTransform>();
				netTransform.AppearAtPosition(this.transform.position);
				netTransform.AppearAtPositionServer(this.transform.position);
			}
			_Object.NumberPresent = 0;
		}
		ConstructionStages[StageNumber].PresentParts.Clear();
	}

	public void GenerateStage(ConstructionStage Stage, int StageNumber) {
		if (!ContainedObjects.ContainsKey(StageNumber))
		{
			ContainedObjects[StageNumber] = new List<ComponentData>();
		}
		foreach (var NeededObject in Stage.NeededParts)
		{
			if (Stage.IncludePartsInitialisation && GenerateComponents)
			{
				for (int i = 0; i < NeededObject.NumberNeeded; i++)
				{
					if (NeededObject.GameObject != null)
					{
						var _Object = Spawn.ServerPrefab(NeededObject.GameObject, this.transform.position, parent: this.transform.parent).GameObject;
						CustomNetTransform netTransform = _Object.GetComponent<CustomNetTransform>();
						netTransform.DisappearFromWorldServer();
						Stage.PresentParts.Add(_Object);
					}
					else if (NeededObject.CType != ConstructionElementType.Null)
					{
						var _Object = Spawn.ServerPrefab(StandardConstructionComponent, this.transform.position, parent: this.transform.parent).GameObject;
						CustomNetTransform netTransform = _Object.GetComponent<CustomNetTransform>();
						netTransform.DisappearFromWorldServer();
						_Object.GetComponent<ConstructionComponent>().setTypeLevel(NeededObject.CType, NeededObject.level);
						Stage.PresentParts.Add(_Object);
					}
				}
				NeededObject.NumberPresent = NeededObject.NumberNeeded;
			}
			ContainedObjects[StageNumber].Add(NeededObject);
		}
	}

	public override void OnStartClient()
	{
		ClientGoToStage(this.CurrentStage);
		base.OnStartClient();
	}
	private void OnStartServer()
	{
		ClientGoToStage(this.CurrentStage);

		//if extending another component
		base.OnStartServer();
	}
}
