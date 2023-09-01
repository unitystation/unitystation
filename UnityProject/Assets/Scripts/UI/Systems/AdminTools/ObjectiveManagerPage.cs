using AdminTools;
using Antagonists;
using Messages.Client;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveManagerPage : MonoBehaviour
{
	[Space(10)]
	[SerializeField] private GUI_AdminTools mainPage;
	public GUI_AdminTools MainPage => mainPage;
	[SerializeField] private ObjectiveEntry objectiveEntry;
	[SerializeField] private ObjectiveEntry addEntry;
	[SerializeField] private GameObject contentArea;
	[SerializeField] private Dropdown dropdown;
	private readonly Dictionary<int, Objective> objectives = new Dictionary<int, Objective>();
	private Objective selectedObjective = null;
	private readonly List<ObjectiveEntry> addedEntries = new List<ObjectiveEntry>();

	[SerializeField] private ObjectiveAttributesEntry attributesEntry;
	[SerializeField] private GameObject attributesContentArea;
	[SerializeField] private GameObject attributesList;
	private readonly List<ObjectiveAttributesEntry> addedAttributesEntries = new List<ObjectiveAttributesEntry>();

	private AdminPlayerEntry player;

	internal void Init(AdminPlayerEntry playerEntry)
	{
		objectiveEntry.SetActive(false);
		addEntry.Init(this);
		player = playerEntry;
		foreach (var x in addedEntries)
		{
			Destroy(x.gameObject);
		}
		addedEntries.Clear();

		RequestAdminObjectiveRefreshMessage.Send(player.PlayerData.uid);
	}


	internal void RemoveEntry(ObjectiveEntry objectiveEntry)
	{
		addedEntries.Remove(objectiveEntry);
		Destroy(objectiveEntry.gameObject);
	}

	public void RefreshInformation(AntagonistInfo antagInfo)
	{
		foreach (var x in antagInfo.Objectives)
		{
			objectiveEntry.SetActive(true);
			var newEntry = Instantiate(objectiveEntry, contentArea.transform).GetComponent<ObjectiveEntry>();
			objectiveEntry.SetActive(false);
			newEntry.Init(this, x);
			addedEntries.Add(newEntry);
		}
		// need for viewing addEntry as last in list
		addEntry.transform.SetAsLastSibling();
		LoadObjectives(antagInfo.antagID);
		OnSelectedObjectiveChange();
	}

	private void LoadObjectives(short antagID)
	{
		Antagonist antag = AntagData.Instance.FromIndexAntag(antagID);
		List<Objective> objs = new List<Objective>();
		if (antag == null)
		{
			objs.AddRange(AntagData.Instance.GetAllBasicObjectives());
		} else
		{
			objs.AddRange(AntagData.Instance.GetAllPosibleObjectives(antag));
		}

		dropdown.ClearOptions();
		var newOptions = new List<Dropdown.OptionData>();
		objectives.Clear();
		selectedObjective = null;
		for (int i = 0; i < objs.Count; i++)
		{
			Objective obj = objs[i];
			newOptions.Add(new Dropdown.OptionData(obj.name));
			objectives.Add(i, obj);
		}
		dropdown.value = 0;
		dropdown.AddOptions(newOptions);
	}

	public void OnSelectedObjectiveChange()
	{
		selectedObjective = objectives[dropdown.value];

		attributesContentArea.SetActive(selectedObjective.attributes.Count != 0);

		foreach (var x in addedAttributesEntries)
		{
			Destroy(x.gameObject);
		}
		addedAttributesEntries.Clear();

		foreach (var x in selectedObjective.attributes)
		{
			attributesEntry.SetActive(true);
			var newEntry = Instantiate(attributesEntry, attributesContentArea.transform).GetComponent<ObjectiveAttributesEntry>();
			attributesEntry.SetActive(false);
			newEntry.Init(this, x, selectedObjective);
			addedAttributesEntries.Add(newEntry);
		}

		attributesEntry.SetActive(false);
	}

	public void AddObjectiveEntry()
	{
		var addedAtributes = new List<ObjectiveAttribute>();
		foreach (var x in addedAttributesEntries)
		{
			addedAtributes.Add(x.Attribute);
		}

		var info = new ObjectiveInfo
		{
			PrefabID = AntagData.Instance.GetIndexObj(selectedObjective),
			Description = selectedObjective.GetDescription(),
			IsNew = true,
			attributes = addedAtributes
		};

		objectiveEntry.SetActive(true);
		var newEntry = Instantiate(objectiveEntry, contentArea.transform).GetComponent<ObjectiveEntry>();
		objectiveEntry.SetActive(false);
		newEntry.Init(this, info);
		addedEntries.Add(newEntry);
		addEntry.transform.SetAsLastSibling();

		dropdown.value = 0;
		OnSelectedObjectiveChange();
	}

	public void AddEntry(ObjectiveInfo info)
	{
		objectiveEntry.SetActive(true);
		var newEntry = Instantiate(objectiveEntry, contentArea.transform).GetComponent<ObjectiveEntry>();
		objectiveEntry.SetActive(false);
		newEntry.Init(this, info);
		addedEntries.Add(newEntry);
		addEntry.transform.SetAsLastSibling();
	}

	public void FinishEditing()
	{
		var toSend = new List<ObjectiveInfo>();
		foreach (var x in addedEntries)
		{
			toSend.Add(x.RelatedObjective);
		}

		RequestAdminObjectiveUpdateMessage.Send(player.PlayerData.uid, toSend);

		Init(player);
	}

	#region ServerPart

	[Server]
	public static void ProceedServerObjectivesUpdate(List<ObjectiveInfo> info, Mind playerMind)
	{
		bool updated = false;
		foreach (var x in info)
		{
			if (x.IsNew)
			{
				try
				{
					if (x.IsCustom)
					{
						AddCustomObjective(playerMind, CustomObjective.Create(x));
					}
					else
					{
						AddObjective(playerMind, x);
					}
					updated = true;
				}
				catch (Exception ex)
				{
					Logger.LogError($"[ObjectiveManagerPage/ProceedServerObjectivesUpdate] Failed to process objective {x.ID}\n {x.Description}\n {ex}");
				}
			} else
			{
				try
				{
					var obj = GetObjective(playerMind, x.ID);
					if (obj != null)
					{
						if (x.toDelete)
						{
							RemoveObjective(playerMind, obj.ID);
							updated = true;
						}
						else if (x.IsCustom)
						{
							if (obj is CustomObjective custom && x.IsDifferent(custom))
							{
								custom.Set(x);
								updated = true;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Logger.LogError($"[ObjectiveManagerPage/ProceedServerObjectivesUpdate] Failed to process objective {x.ID}\n {x.Description}\n {ex}");
				}
			}
		}
		//if objectives updated - why not show them to player?
		if (updated == true)
		{
			playerMind.UpdateAntagButtons();
			playerMind.ShowObjectives();
		}
	}

	private static Objective GetObjective(Mind player, string ID)
	{
		return player.AntagPublic.Objectives.Where(o => o.ID == ID)?.ElementAt(0);
	}

	private static CustomObjective CreateCustomObjective(string desctiption)
	{
		var customObj = CustomObjective.Create(desctiption);

		return customObj;
	}

	private static void AddObjective(Mind player, ObjectiveInfo info)
	{
		if (player.Body == null)
			return;
		var playerScript = player.Body;
		if (playerScript.Mind == null)
			return;


		var antag = playerScript.Mind.AntagPublic;
		Objective obj = AntagData.Instance.FromIndexObj(info.PrefabID);
		if (obj == null)
		{
			return;
		}
		foreach (var attr in info.attributes)
		{
			var attribute = obj.attributes[attr.index];
			if (attr is ObjectiveAttributeItem itemSet && attribute is ObjectiveAttributeItem item)
			{
				item.itemID = itemSet.itemID;
			} else if (attr is ObjectiveAttributeNumber numbSet && attribute is ObjectiveAttributeNumber numb)
			{
				numb.number = numbSet.number;
			}
			else if (attr is ObjectiveAttributePlayer plSet && attribute is ObjectiveAttributePlayer pl)
			{
				pl.playerID = plSet.playerID;
			}
		}
		obj.DoSetupInGame(player);

		antag.Objectives = antag.Objectives.Concat(new[] { obj });
	}

	private static CustomObjective AddCustomObjective(Mind player, CustomObjective customObj)
	{
		var playerScript = player.Body;
		if (playerScript.Mind == null)
			return null;


		var antag = playerScript.Mind.AntagPublic;
		customObj.DoSetupInGame(player);

		antag.Objectives = antag.Objectives.Concat(new[] { customObj });

		return customObj;
	}

	private static void RemoveObjective(Mind player, Objective objective)
	{
		var playerScript = player.Body;
		if (playerScript.Mind == null)
			return;

		player.AntagPublic.Objectives = player.AntagPublic.Objectives.Where(o => o != objective);
	}

	private static void RemoveObjective(Mind player, string ID)
	{
		var playerScript = player.Body;
		if (playerScript.Mind == null)
			return;

		player.AntagPublic.Objectives = player.AntagPublic.Objectives.Where(o => o.ID != ID);
	}

	private static void ChangeCustomObjectiveState(Mind player, CustomObjective objective, bool state)
	{
		var playerScript = player.Body;
		if (playerScript.Mind == null)
			return;

		objective.SetStatus(state);
	}
	#endregion
}


public class RequestAdminObjectiveUpdateMessage : ClientMessage<RequestAdminObjectiveUpdateMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string playerForRequestID;
		public string json;
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin())
		{
			var info = JsonConvert.DeserializeObject<AntagonistInfo>(msg.json);
			try
			{
				var player = PlayerList.Instance.GetPlayerByID(msg.playerForRequestID);

				ObjectiveManagerPage.ProceedServerObjectivesUpdate(info.Objectives, player.Mind);
			}
			catch (Exception ex)
			{
				Logger.LogError($"[RequestAdminObjectiveUpdateMessage/Process] Failed to process objective update {ex}");
			}
		}
	}

	public static NetMessage Send(string playerForRequestID, List<ObjectiveInfo> info)
	{
		var objs = new AntagonistInfo()
		{
			Objectives = info
		};

		NetMessage msg = new NetMessage
		{
			playerForRequestID = playerForRequestID,
			json = JsonConvert.SerializeObject(objs)
		};

		Send(msg);
		return msg;
	}
}

public class RequestAdminObjectiveRefreshMessage: ClientMessage<RequestAdminObjectiveRefreshMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string playerForRequestID;
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin())
		{
			ObjectiveRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.UserId, msg.playerForRequestID);
		}
	}

	public static NetMessage Send(string playerForRequestID)
	{
		NetMessage msg = new NetMessage
		{
			playerForRequestID = playerForRequestID
		};

		Send(msg);
		return msg;
	}
}

public class ObjectiveRefreshMessage : ServerMessage<ObjectiveRefreshMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string JsonData;
		public uint Recipient;
	}

	public override void Process(NetMessage msg)
	{
		LoadNetworkObject(msg.Recipient);
		var info = JsonConvert.DeserializeObject<AntagonistInfo>(msg.JsonData);

		var page = UnityEngine.Object.FindFirstObjectByType<ObjectiveManagerPage>();
		page.RefreshInformation(info);
	}

	public static NetMessage Send(GameObject recipient, string adminID, string playerForRequestID)
	{
		//Gather the data
		var objectivesInfo = new AntagonistInfo();
		var player = PlayerList.Instance.GetPlayerByID(playerForRequestID);
		if (player.Mind.AntagPublic.Antagonist != null)
		{
			objectivesInfo.antagID = AntagData.Instance.GetIndexAntag(player.Mind.AntagPublic.Antagonist);
		}

		for (int i = 0; i < player.Mind.AntagPublic.Objectives.Count(); i++)
		{
			var x = player.Mind.AntagPublic.Objectives.ElementAt(i);
			var objInfo = new ObjectiveInfo();

			objInfo.Status = x.IsComplete();
			objInfo.Description = x.Description;
			objInfo.Name = x.name;
			objInfo.ID = x.ID;
			objInfo.IsCustom = x is CustomObjective;
			objInfo.PrefabID = AntagData.Instance.GetIndexObj(x);

			objectivesInfo.Objectives.Add(objInfo);
		}

		var data = JsonConvert.SerializeObject(objectivesInfo);

		NetMessage msg =
			new NetMessage { Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data };

		SendTo(recipient, msg);
		return msg;
	}
}