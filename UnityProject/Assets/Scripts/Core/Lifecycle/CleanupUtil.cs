using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UI.Action;
using Audio.Containers;

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
						Logger.Log("Name of leaked object : " + target.name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Logger.Log("Typename of leaked object : " + target.GetType().Name, Category.MemoryCleanup);
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
						Logger.Log("Name of leaked object : " + (listInQuestion[i].Target as MonoBehaviour).name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Logger.Log("Typename of leaked object : " + listInQuestion[i].Target.GetType().Name, Category.MemoryCleanup);
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
	public static int RidListOfDeadElements<T>(IList<T> listInQuestion, bool verbose = false) where T: MonoBehaviour
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
						Logger.Log("Name of leaked object : " + listInQuestion[i].name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Logger.Log("Typename of leaked object : " + listInQuestion[i].GetType().Name, Category.MemoryCleanup);
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
						Logger.Log("Typename of (possibly) leaked object : " + keyValuePair.Key.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Logger.Log("Typename of (possibly) leaked object : " + keyValuePair.Value.GetType().Name, Category.MemoryCleanup);
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
		
		foreach(var keyValuePair in dictInQuestion)
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
						Logger.Log("Typename of (possibly) leaked object : " + keyValuePair.Key.GetType().Name, Category.MemoryCleanup);
					}
					catch (Exception ee)
					{
						//do nothing
					}

					try
					{
						Logger.Log("Typename of (possibly) leaked object : " + keyValuePair.Value.GetType().Name, Category.MemoryCleanup);
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
	}

	/// <summary>
	/// Should be called right after previous round ended, and some scenes might already be unloaded
	/// </summary>
	public static void CleanupInbetweenScenes()
	{
	}

	/// <summary>
	/// Should be called sometimes after round started and all necessary scenes are loaded
	/// </summary>
	public static void RoundStartCleanup()
	{
	}
}
