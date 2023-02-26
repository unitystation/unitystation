using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UI.Action;
using Audio.Containers;

public static class CleanupUtil
{

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

	public static void EndRoundCleanup()
	{
	}

	public static void CleanupInbetweenScenes()
	{
	}

	public static void RoundStartCleanup()
	{
	}
}
