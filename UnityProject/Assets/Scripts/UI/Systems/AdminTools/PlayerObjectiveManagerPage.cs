using Antagonists;
using Logs;
using Messages.Client.Admin;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class PlayerObjectiveManagerPage : MonoBehaviour
	{
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
		private readonly List<ObjectiveAttributesEntry> addedAttributesEntries = new List<ObjectiveAttributesEntry>();

		private AdminPlayerEntry player;

		/// <summary>
		/// Initialize page
		/// </summary>
		/// <param name="playerEntry"></param>
		public void Init(AdminPlayerEntry playerEntry)
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
		
		/// <summary>
		/// Removes added entry
		/// </summary>
		/// <param name="objectiveEntry"></param>
		public void RemoveEntry(ObjectiveEntry objectiveEntry)
		{
			addedEntries.Remove(objectiveEntry);
			Destroy(objectiveEntry.gameObject);
		}

		/// <summary>
		/// Refreshes information that given by antagInfo
		/// </summary>
		/// <param name="antagInfo"></param>
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
			}
			else
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

		/// <summary>
		/// When current objective is changed
		/// </summary>
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
				newEntry.Init(mainPage, x, selectedObjective);
				addedAttributesEntries.Add(newEntry);
			}

			attributesEntry.SetActive(false);
		}

		/// <summary>
		/// Used to add new objective entry
		/// </summary>
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
				Attributes = addedAtributes
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

		/// <summary>
		/// Used to add new objective entry from given information in info
		/// </summary>
		/// <param name="info"></param>
		public void AddEntry(ObjectiveInfo info)
		{
			objectiveEntry.SetActive(true);
			var newEntry = Instantiate(objectiveEntry, contentArea.transform).GetComponent<ObjectiveEntry>();
			objectiveEntry.SetActive(false);
			newEntry.Init(this, info);
			addedEntries.Add(newEntry);
			addEntry.transform.SetAsLastSibling();
		}

		/// <summary>
		/// Send edited objectives to server and refreshes page
		/// </summary>
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
		/// <summary>
		/// Server side. Proceed given by administrator new objectives for selected player
		/// </summary>
		/// <param name="info"></param>
		/// <param name="playerMind"></param>
		[Server]
		public static void ProceedServerObjectivesUpdate(List<ObjectiveInfo> info, Mind playerMind)
		{
			bool updated = false;
			foreach (var x in info)
			{
				var isNew = true;
				foreach (var y in playerMind.AntagPublic.Objectives)
				{
					if (y.ID == x.ID)
					{
						isNew = false;
					}
				}
				if (isNew == true)
				{
					try
					{
						if (x.IsCustom == true)
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
						Loggy.LogError($"[ObjectiveManagerPage/ProceedServerObjectivesUpdate] Failed to add objective {x.ID}\n {x.Description}\n {ex}");
					}
				}
				else
				{
					try
					{
						var obj = GetObjective(playerMind, x.ID);
						if (obj != null)
						{
							if (x.toDelete == true)
							{
								RemoveObjective(playerMind, obj.ID);
								updated = true;
							}
							else if (x.IsCustom == true)
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
						Loggy.LogError($"[ObjectiveManagerPage/ProceedServerObjectivesUpdate] Failed to update objective {x.ID}\n {x.Description}\n {ex}");
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

		private static void AddObjective(Mind player, ObjectiveInfo info)
		{
			if (player.Body == null)
				return;
			var playerScript = player.Body;
			if (playerScript.Mind == null)
				return;


			var antag = playerScript.Mind.AntagPublic;
			Objective obj = Instantiate(AntagData.Instance.FromIndexObj(info.PrefabID));
			if (obj == null)
			{
				return;
			}
			foreach (var attr in info.Attributes)
			{
				var attribute = obj.attributes[attr.index];
				if (attr.type != attribute.type)
					continue;
				if (attr.type == ObjectiveAttributeType.ObjectiveAttributeItem)
				{
					attribute.ItemID = attr.ItemID;
				}
				else if (attr.type == ObjectiveAttributeType.ObjectiveAttributeNumber)
				{
					attribute.Number = attr.Number;
				}
				else if (attr.type == ObjectiveAttributeType.ObjectiveAttributePlayer)
				{
					attribute.PlayerID = attr.PlayerID;
				}
				else if (attr.type == ObjectiveAttributeType.ObjectiveAttributeItemTrait)
				{
					attribute.ItemTraitIndex = attr.ItemTraitIndex;
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
}