using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using UI.Objects.Shuttles;
using HealthV2;
using Logs;

namespace UI
{
	public class GUI_Spawner : NetTab
	{
		private ItemList entryList;
		private ItemList PrefabEntryList => entryList ??= this["EntryList"] as ItemList;

		private SpawnedObjectList spawnedObjectList;
		private SpawnedObjectList SpawnedObjectList => spawnedObjectList ??= this["MobList"] as SpawnedObjectList;

		private NetUIElement<string> infoDisplay;
		private NetUIElement<string> InfoDisplay => infoDisplay ??= (NetUIElement<string>)this["RandomText"];

		private NetUIElement<string> nestedPageName;
		private NetUIElement<string> NestedPageName => nestedPageName ??= (NetUIElement<string>)this["NestedPageName"];

		protected override void InitServer()
		{
			//Init fields from pages/subpages that you want to access later
			//They're visible during InitServer() but might become invisible again afterwards, so get references to them now
			InfoDisplay?.Init();
			NestedPageName?.Init();
			SpawnedObjectList?.Init();
			// -- we don't actually need to call Init(), it's just to call /something/ so that lazy loading would happen

			//Logic for updating mob entry's internal values to reflect set mob gameObject info
			SpawnedObjectList.OnObjectChange.AddListener((newObject, elementName, element) =>
		   {
			   var netElement = (NetUIElement<string>)element;
			   switch (elementName)
			   {
				   case "MobName":
					   netElement.MasterSetValue(newObject.ExpensiveName());
					   break;
				   case "MobIcon":
					   netElement.MasterSetValue(newObject.NetId().ToString());
					   break;
				   default:
					//Don't need to change netElement.Value for other elements
					break;
			   }
		   });

		}

		private IEnumerator Start()
		{
			yield return WaitFor.EndOfFrame;

			if (IsMasterTab)
			{
				//Storytelling
				tgtMode = true;
				StartCoroutine(ToggleStory(0));

				// Add items from InitialContents list
				List<GameObject> initList = Provider.GetComponent<Spawner>().InitialContents;
				foreach (GameObject item in initList)
				{
					PrefabEntryList.AddItem(item);
				}

				SpawnedObjectList.AddObjects(GUI_ShuttleControl.GetObjectsOf<LivingHealthMasterBase>());

				// Done via editor in this example, but can be done via code as well, like this:
				// NestedSwitcher.OnPageChange.AddListener(RefreshSubpageLabel);
			}
		}

		public void RefreshSubpageLabel(NetPage oldPage, NetPage newPage)
		{
			NestedPageName.MasterSetValue(newPage.name);
		}

		private static string[] tgt = ("One day while Andy was toggling, " +
				"Toggle got toggled. He could no longer help himself! " +
				"He watched as Andy stroked his juicy kawaii toggle.").Split(' ');

		private bool tgtMode;
		private IEnumerator ToggleStory(int word)
		{
			InfoDisplay.MasterSetValue(tgt.Wrap(word));
			yield return WaitFor.Seconds(2);
			if (tgtMode)
			{
				StartCoroutine(ToggleStory(++word));
			}
		}

		public void AddItem(string prefabName)
		{
			PrefabEntryList.AddItem(prefabName);
		}

		public void MasterRemoveItem(string prefabName)
		{
			PrefabEntryList.MasterRemoveItem(prefabName);
		}

		public void SpawnItemByIndex(string index)
		{
			ItemEntry item = GetItemFromIndex(index);

			if (item == null)
			{
				return;
			}

			Vector3 originPos = Provider.AssumedWorldPosServer();
			Vector3 nearestPlayerPos = GetNearestPlayerPos(originPos);

			if (nearestPlayerPos == TransformState.HiddenPos)
			{
				return;
			}

			var spawnedItem = Spawn.ServerPrefab(item.Prefab, originPos, Provider.transform.parent).GameObject;
			spawnedItem.GetComponent<UniversalObjectPhysics>()?.NewtonianPush(nearestPlayerPos - originPos, 1,
				inThrownBy: Provider, inAim: BodyPartType.Chest);
		}

		///Tries to get nearest player's position within range, and returns HiddenPos if it fails
		///could be moved to some util class, gonna be useful
		private Vector3 GetNearestPlayerPos(Vector3 originPos, int maxRange = 10)
		{
			float smallestDistance = float.MaxValue;
			Vector3 nearestPosSoFar = TransformState.HiddenPos;

			for (var i = 0; i < PlayerList.Instance.InGamePlayers.Count; i++)
			{
				PlayerInfo player = PlayerList.Instance.InGamePlayers[i];
				float curDistance = Vector3.Distance(originPos, player.Script.WorldPos);

				if (curDistance < smallestDistance)
				{
					smallestDistance = curDistance;
					nearestPosSoFar = player.Script.WorldPos;
				}
			}

			if (smallestDistance <= maxRange)
			{
				return nearestPosSoFar;
			}
			return TransformState.HiddenPos;
		}

		private bool firingMode;

		public void ToggleFire()
		{
			firingMode = !firingMode;
			if (firingMode)
			{
				StartCoroutine(KeepFiring(0));
			}
		}

		private IEnumerator KeepFiring(int shot)
		{
			var strings = PrefabEntryList.Value;
			if (strings.Length > 0)
			{
				//See, this is pretty cool
				string s = strings.Wrap(shot);
				//fire
				SpawnItemByIndex(s);
			}
			yield return WaitFor.Seconds(1.5f);
			if (firingMode)
			{
				StartCoroutine(KeepFiring(++shot));
			}
		}

		public void MasterRemoveItemByIndex(string index)
		{
			MasterRemoveItem(GetItemFromIndex(index)?.Prefab.name);
		}

		private ItemEntry GetItemFromIndex(string index)
		{
			var entryCatalog = PrefabEntryList.EntryIndex;
			if (entryCatalog.ContainsKey(index))
			{
				return entryCatalog[index] as ItemEntry;
			}
			Loggy.LogErrorFormat("'{0}' spawner tab: item with index {1} not found in the list, might be hidden/destroyed", Category.NetUI, gameObject.name, index);
			return null;
		}

		public void HugMobByIndex(string index)
		{
			var mob = GetMob(index);
			if (mob)
			{
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Notice1, Provider.transform.position);
				//Get mob's gameobject and do something good to it
				Chat.AddActionMsgToChat(mob.TrackedObject, $"You hugged {mob.name}", $"{mob.TrackedObject.name} hugged {mob.name}");
			}
		}

		public void RemoveMobByIndex(string index)
		{
			var mob = GetMob(index);
			if (mob)
			{
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Notice1, Provider.transform.position);

				//Get mob's gameobject and do something bad to it
				mob.TrackedObject.GetComponent<LivingHealthBehaviour>().ApplyDamageToBodyPart(null, 500, AttackType.Internal, DamageType.Brute, BodyPartType.Head);
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Smash, mob.TrackedObject.transform.position);

				SpawnedObjectList.Remove(index);
			}
		}

		//TODO: generify: integrate into list functionality
		private SpawnedObjectEntry GetMob(string index)
		{
			var entryCatalog = SpawnedObjectList.EntryIndex;
			if (entryCatalog.ContainsKey(index))
			{
				return entryCatalog[index] as SpawnedObjectEntry;
			}
			return null;
		}
	}
}
