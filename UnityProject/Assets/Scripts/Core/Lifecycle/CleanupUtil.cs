using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UI.Action;
using Audio.Containers;
using Logs;

public static class CleanupUtil
{
	/// <summary>
	/// this methods scans the provided list and removes from it all elements, that are not marked as DontDestroyOnLoad. Will keep elements that are equal to null
	/// <paramref name="targetExtractor"/> is just used to get the MonoBehaviour that is then being used to figure out whether or not element should be removed
	/// </summary>
	/// <param name="listInQuestion"> is a list that should be altered according to the rules provided by the method</param>
	/// <param name="targetExtractor"> is a delegate that is transforming <typeparamref name="T"/> into a <c>UnityEngine.MonoBehaviour</c> that is later then removed from the <paramref name="listInQuestion"> if it's not marked as DontDestroyOnLoad </param>
	public static int RidListOfSoonToBeDeadElements<T>(IList<T> listInQuestion, Func<T, UnityEngine.MonoBehaviour> targetExtractor)
	{
		if (listInQuestion == null)
		{
			return -1;
		}

		List<T> survivorList = new List<T>();

		for (int i = 0, max = listInQuestion.Count; i < max; i++)
		{
			MonoBehaviour target = targetExtractor(listInQuestion[i]);

			if (listInQuestion[i] != null && (target == null || target.gameObject.scene.buildIndex == -1))
			{
				survivorList.Add(listInQuestion[i]);
			}
		}

		int res = listInQuestion.Count - survivorList.Count;
		listInQuestion.Clear();

		for (int i = 0, max = survivorList.Count; i < max; i++)
		{
			listInQuestion.Add(survivorList[i]);
		}

		return res;
	}

	/// <summary>
	/// this methods scans the provided list and removes from it all elements that are equal to null
	/// <paramref name="targetExtractor"/> is just used to get the MonoBehaviour that is then being used to figure out whether or not element should be removed
	/// </summary>
	/// <param name="listInQuestion"> is a  list that should be altered according to the rules provided by the method</param>
	/// <param name="targetExtractor"> is a delegate that is transforming <typeparamref name="T"/> into a <c>UnityEngine.MonoBehaviour</c> that is later then removed from the <paramref name="listInQuestion"> if it's equal to null </param>
	/// <param name="verbose"> set it to True if you want to allow it to attempt to log typenames or even object names of objects, that would be removed from the list </param>
	public static int RidListOfDeadElements<T>(IList<T> listInQuestion, Func<T, UnityEngine.MonoBehaviour> targetExtractor, bool verbose = false)
	{
		if (listInQuestion == null)
		{
			return -1;
		}

		List<T> survivorList = new List<T>();

		for (int i = 0, max = listInQuestion.Count; i < max; i++)
		{
			MonoBehaviour target = targetExtractor(listInQuestion[i]);

			if (listInQuestion[i] != null && target == null)
			{
				survivorList.Add(listInQuestion[i]);
			}
			else
			{
				if (verbose)
				{
					try
					{
						Loggy.Log("Name of leaked object : " + target.name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Loggy.Log("Typename of leaked object : " + target.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}
				}
			}
		}

		int res = listInQuestion.Count - survivorList.Count;
		listInQuestion.Clear();

		for (int i = 0, max = survivorList.Count; i < max; i++)
		{
			listInQuestion.Add(survivorList[i]);
		}

		return res;
	}
	/// <summary>
	/// this methods scans the provided list and removes from it all instances of <c>System.Action</c> that have targets equal to null
	/// </summary>
	/// <param name="listInQuestion"> is a list that should be altered according to the rules provided by the method</param>
	/// <param name="verbose"> set it to True if you want to allow it to attempt to log typenames or even object names of objects, that would be removed from the list </param>

	public static int RidListOfDeadElements(IList<Action> listInQuestion, bool verbose = false)
	{
		if (listInQuestion == null)
		{
			return -1;
		}

		List<Action> survivorList = new List<Action>();

		for (int i = 0, max = listInQuestion.Count; i < max; i++)
		{
			if (listInQuestion[i] != null && listInQuestion[i].Target == null)
			{
				survivorList.Add(listInQuestion[i]);
			}
			else
			{
				if (verbose)
				{
					try
					{
						Loggy.Log("Name of leaked object : " + (listInQuestion[i].Target as MonoBehaviour).name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Loggy.Log("Typename of leaked object : " + listInQuestion[i].Target.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}
				}
			}
		}

		int res = listInQuestion.Count - survivorList.Count;
		listInQuestion.Clear();

		for (int i = 0, max = survivorList.Count; i < max; i++)
		{
			listInQuestion.Add(survivorList[i]);
		}

		return res;
	}

	/// <summary>
	/// this methods scans the provided list and removes from it all elements that are equal to null
	/// </summary>
	/// <param name="listInQuestion"> is a  list that should be altered according to the rules provided by the method</param>
	/// <param name="verbose"> set it to True if you want to allow it to attempt to log typenames or even object names of objects, that would be removed from the list </param>
	public static int RidListOfDeadElements<T>(IList<T> listInQuestion, bool verbose = false) where T : MonoBehaviour
	{
		if (listInQuestion == null)
		{
			return -1;
		}
		List<T> survivorList = new List<T>();

		for (int i = 0, max = listInQuestion.Count; i < max; i++)
		{
			if (listInQuestion[i] != null)
			{
				survivorList.Add(listInQuestion[i]);
			}
			else
			{
				if (verbose)
				{
					try
					{
						Loggy.Log("Name of leaked object : " + listInQuestion[i].name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Loggy.Log("Typename of leaked object : " + listInQuestion[i].GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}
				}
			}
		}

		int res = listInQuestion.Count - survivorList.Count;
		listInQuestion.Clear();

		for (int i = 0, max = survivorList.Count; i < max; i++)
		{
			listInQuestion.Add(survivorList[i]);
		}

		return res;
	}

	/// <summary>
	/// this methods scans the provided dictionary and removes from it all elements that didn't pass <paramref name="survivalCondition"> curvival condition
	/// <paramref name="survivalCondition"/> should return true of provided pair of key and value is supposed to stay in the dictionary
	/// </summary>
	/// <param name="dictInQuestion"> is a dictionary that should be altered according to the rules provided by the method</param>
	/// <param name="survivalCondition"> is a delegate that is deciding whether or not pair of key and value of the dictionary should stay in the dictionary</param>
	/// <param name="verbose"> set it to True if you want to allow it to attempt to log typenames or even object names of objects, that would be removed from the list </param>
	public static int RidDictionaryOfDeadElements<TKey, TValue>(IDictionary<TKey, TValue> dictInQuestion, Func<TKey, TValue, bool> survivalCondition, bool verbose = false)
	{
		if (dictInQuestion == null)
		{
			return -1;
		}

		List<KeyValuePair<TKey, TValue>> survivorList = new List<KeyValuePair<TKey, TValue>>();

		foreach (var keyValuePair in dictInQuestion)
		{
			if (survivalCondition(keyValuePair.Key, keyValuePair.Value))
			{
				survivorList.Add(keyValuePair);
			}
			else
			{
				if (verbose)
				{
					try
					{
						Loggy.Log("Typename of (possibly) leaked object : " + keyValuePair.Key.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Loggy.Log("Typename of (possibly) leaked object : " + keyValuePair.Value.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}
				}
			}
		}

		int res = dictInQuestion.Count - survivorList.Count;
		dictInQuestion.Clear();

		for (int i = 0, max = survivorList.Count; i < max; i++)
		{
			dictInQuestion.Add(survivorList[i].Key, survivorList[i].Value);
		}

		return res;
	}
	/// <summary>
	/// this methods scans the provided dictionary and removes from it all keyvaluepairs in which either key or value are equal to null
	/// </summary>
	/// <param name="dictInQuestion"> is a dictionary that should be altered according to the rules provided by the method</param>
	/// <param name="verbose"> set it to True if you want to allow it to attempt to log typenames or even object names of objects, that would be removed from the list </param>
	public static int RidDictionaryOfDeadElements<TKey, TValue>(IDictionary<TKey, TValue> dictInQuestion, bool verbose = false)
	{
		if (dictInQuestion == null)
		{
			return -1;
		}

		List<KeyValuePair<TKey, TValue>> survivorList = new List<KeyValuePair<TKey, TValue>>();

		foreach (var keyValuePair in dictInQuestion)
		{
			if (keyValuePair.Key != null && keyValuePair.Value != null)
			{
				survivorList.Add(keyValuePair);
			}
			else
			{
				if (verbose)
				{
					try
					{
						Loggy.Log("Typename of (possibly) leaked object : " + keyValuePair.Key.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Loggy.Log("Typename of (possibly) leaked object : " + keyValuePair.Value.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}
				}
			}
		}

		int res = dictInQuestion.Count - survivorList.Count;
		dictInQuestion.Clear();

		for (int i = 0, max = survivorList.Count; i < max; i++)
		{
			dictInQuestion.Add(survivorList[i].Key, survivorList[i].Value);
		}

		return res;
	}

	/// <summary>
	/// Should be called right when round ended, but before any scene unloaded
	/// </summary>
	public static void EndRoundCleanup()
	{
		PlayerManager.Reset();
		GameManager.Instance.CentComm.Clear();
		GameManager.Instance.SpaceBodies.Clear();
		Items.Weapons.ExplosiveBase.ExplosionEvent = new UnityEngine.Events.UnityEvent<Vector3Int, Items.Weapons.BlastData>();
		Items.TrackingBeacon.Clear();
		SoundManager.Instance.Clear();
		SpriteHandlerManager.Instance.OnRoundRestart(default(UnityEngine.SceneManagement.Scene), default(UnityEngine.SceneManagement.Scene));
		UpdateManager.Instance.Clear();
		AudioManager.Instance.MultiInterestFloat.InterestedParties.Clear();
		SoundManager.Instance.SoundSpawns.Clear();
		SoundManager.Instance.NonplayingSounds.Clear();
		GameManager.Instance.ResetStaticsOnNewRound();
		SpriteHandlerManager.PresentSprites.Clear();
		LandingZoneManager.Instance.landingZones.Clear();
		LandingZoneManager.Instance.spaceTeleportPoints.Clear();
		SpriteHandlerManager.PresentSprites = new Dictionary<Mirror.NetworkIdentity, Dictionary<string, SpriteHandler>>();
		ChatBubbleManager.Instance.Clear();

		foreach (var a in GameObject.FindObjectsOfType<AdminTools.AdminPlayerEntry>(true))
		{
			a.pendingMsgNotification = null;
		}

		foreach (var a in GameObject.FindObjectsOfType<UI_ItemSlot>(true))
		{
			a.Image.ClearAll();
		}

		foreach (var a in GameObject.FindObjectsOfType<UI_SlotManager>(true))
		{
			a.OpenSlots.Clear();
		}
		UI_ItemImage.ImageAndHandler.ClearAll();
	}

	/// <summary>
	/// Should be called right after previous round ended, and some scenes might already be unloaded
	/// </summary>
	public static void CleanupInbetweenScenes()
	{
		MatrixManager.Instance.ResetMatrixManager();
		MatrixManager.IsInitialized = false;
		GameManager.Instance.ResetStaticsOnNewRound();
		Systems.Cargo.CargoManager.Instance.OnRoundRestart();
		Systems.Scenes.LavaLandManager.Instance.Clean();
		ClientSynchronisedEffectsManager.Instance.ClearData();
		TileManager.Instance.Cleanup_between_rounds();
		CleanupUtil.RidListOfDeadElements(GameManager.Instance.SpaceBodies);
		RidDictionaryOfDeadElements(LandingZoneManager.Instance.landingZones, (u, k) => u != null);
	}

	/// <summary>
	/// Should be called sometimes after round started and all necessary scenes are loaded
	/// </summary>
	public static void RoundStartCleanup()
	{
		foreach (var a in UnityEngine.GameObject.FindObjectsOfType<UIAction>(true))
		{
			if ((a.iAction is UI.Action.ItemActionButton) && (a.iAction as UI.Action.ItemActionButton == null || (a.iAction as UI.Action.ItemActionButton).CurrentlyOn == null))
			{
				a.iAction = null;
				UnityEngine.GameObject.Destroy(a.gameObject);
			}
		}

		ComponentManager.ObjectToPhysics.Clear();
		Spawn.Clean();
		MatrixManager.Instance.PostRoundStartCleanup();
		SpriteHandlerManager.Instance.Clean();
		Debug.Log("removed " + RidDictionaryOfDeadElements(Mirror.NetworkClient.spawned, (u,k)=> k != null) + " dead elements from Mirror.NetworkClient.spawned");
		Debug.Log("removed " + RidDictionaryOfDeadElements(SoundManager.Instance.SoundSpawns, (u, k) => k != null) + " dead elements from SoundManager.Instance.SoundSpawns");
		AdminTools.AdminOverlay.Instance?.Clear();
		TileManager.Instance.DeepCleanupTiles();
		CleanupUtil.RidListOfDeadElements(GameManager.Instance.SpaceBodies);
		UI.Core.Action.UIActionManager.Instance.Clear();//maybe it'l work second time?
		SpriteHandlerManager.Instance.Clean();
		Dictionary<UInt64, Mirror.NetworkIdentity > dict = Mirror.NetworkIdentity.sceneIds;
		Debug.Log("removed " + RidDictionaryOfDeadElements(dict, (u, k) => k != null) + " dead elements from Mirror.NetworkIdentity.sceneIds");
		RidDictionaryOfDeadElements(LandingZoneManager.Instance.landingZones, (u, k) => u != null);
		SpriteHandlerManager.Instance.Clean();
		Debug.Log("removed " + RidDictionaryOfDeadElements(SoundManager.Instance.NonplayingSounds, (u, k) => k != null) + " dead elements from SoundManager.Instance.NonplayingSounds");
		RidDictionaryOfDeadElements(SpriteHandlerManager.PresentSprites, (u, k) => u != null && k != null);
		Debug.Log("Finished RoundStartCleanup!");
	}
}
