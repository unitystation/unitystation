using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UI.Action;
using Audio.Containers;

public static class CleanupUtil
{

	public static int RidListOfSoonToBeDeadElements<T>(IList<T> list_in_question, Func<T, UnityEngine.MonoBehaviour> target_extractor)
	{
		if (list_in_question == null)
		{
			return -1;
		}

		List<T> survivor_list = new List<T>();

		for (int i = 0, max = list_in_question.Count; i < max; i++)
		{
			MonoBehaviour target = target_extractor(list_in_question[i]);

			if (list_in_question[i] != null && (target == null || target.gameObject.scene.buildIndex == -1))
			{
				survivor_list.Add(list_in_question[i]);
			}
		}

		int res = list_in_question.Count - survivor_list.Count;
		list_in_question.Clear();

		for (int i = 0, max = survivor_list.Count; i < max; i++)
		{
			list_in_question.Add(survivor_list[i]);
		}

		return res;
	}

	public static int RidListOfDeadElements<T>(IList<T> list_in_question, Func<T, UnityEngine.MonoBehaviour> target_extractor)
	{
		if (list_in_question == null)
		{
			return -1;
		}

		List<T> survivor_list = new List<T>();

		for (int i = 0, max = list_in_question.Count; i < max; i++)
		{
			MonoBehaviour target = target_extractor(list_in_question[i]);

			if (list_in_question[i] != null && target == null)
			{
				survivor_list.Add(list_in_question[i]);
			}
		}

		int res = list_in_question.Count - survivor_list.Count;
		list_in_question.Clear();

		for (int i = 0, max = survivor_list.Count; i < max; i++)
		{
			list_in_question.Add(survivor_list[i]);
		}

		return res;
	}

	public static int RidListOfDeadElements(IList<Action> list_in_question)
	{
		if (list_in_question == null)
		{
			return -1;
		}

		List<Action> survivor_list = new List<Action>();

		for (int i = 0, max = list_in_question.Count; i < max; i++)
		{
			if (list_in_question[i] != null && list_in_question[i].Target == null)
			{
				survivor_list.Add(list_in_question[i]);
			}
		}

		int res = list_in_question.Count - survivor_list.Count;
		list_in_question.Clear();

		for (int i = 0, max = survivor_list.Count; i < max; i++)
		{
			list_in_question.Add(survivor_list[i]);
		}

		return res;
	}

	public static int RidListOfDeadElements<T>(IList<T> list_in_question) where T: MonoBehaviour
	{
		if (list_in_question == null)
		{
			return -1;
		}
		List<T> survivor_list = new List<T>();
		
		for (int i = 0, max = list_in_question.Count; i < max; i++)
		{
			if (list_in_question[i] != null)
			{
				survivor_list.Add(list_in_question[i]);
			}
		}

		int res = list_in_question.Count - survivor_list.Count;
		list_in_question.Clear();

		for (int i = 0, max = survivor_list.Count; i < max; i++)
		{
			list_in_question.Add(survivor_list[i]);
		}

		return res;
	}

	public static int RidDictionaryOfDeadElements<TKey, TValue>(IDictionary<TKey, TValue> dict_in_question, Func<TKey, TValue, bool> condition )
	{
		if (dict_in_question == null)
		{
			return -1;
		}

		List<KeyValuePair<TKey, TValue>> survivor_list = new List<KeyValuePair<TKey, TValue>>();

		foreach (var a in dict_in_question)
		{
			if (condition(a.Key, a.Value))
			{
				survivor_list.Add(a);
			}
		}

		int res = dict_in_question.Count - survivor_list.Count;
		dict_in_question.Clear();

		for (int i = 0, max = survivor_list.Count; i < max; i++)
		{
			dict_in_question.Add(survivor_list[i].Key, survivor_list[i].Value);
		}

		return res;
	}

	public static int RidDictionaryOfDeadElements<TKey, TValue>(IDictionary<TKey, TValue> dict_in_question)
	{
		if (dict_in_question == null)
		{
			return -1;
		}

		List<KeyValuePair<TKey, TValue>> survivor_list = new List<KeyValuePair<TKey, TValue>>();
		
		foreach(var a in dict_in_question)
		{
			if (a.Key != null && a.Value != null)
			{
				survivor_list.Add(a);
			}
		}

		int res = dict_in_question.Count - survivor_list.Count;
		dict_in_question.Clear();

		for (int i = 0, max = survivor_list.Count; i < max; i++)
		{
			dict_in_question.Add(survivor_list[i].Key, survivor_list[i].Value);
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
