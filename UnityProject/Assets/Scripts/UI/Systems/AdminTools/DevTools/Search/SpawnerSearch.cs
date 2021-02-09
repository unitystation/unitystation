
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using UnityEngine;

/// <summary>
/// Provides the capability to search over the list of spawnable items.
///
/// Keeping it REALLY simple for now, just a simple "contains" based approach against the prefab name
/// since we have a relatively tiny amount of content to search over. We don't really need
/// a super sophisticated search engine.
/// </summary>
public class SpawnerSearch
{
	public static readonly Regex NON_ALPHANUMERIC = new Regex(@"\W", RegexOptions.Compiled);

	// array of structs for fast iteration while searching
	private readonly DevSpawnerDocument[] documents;

	private SpawnerSearch(DevSpawnerDocument[] documents)
	{
		this.documents = documents;
	}

	/// <summary>
	/// Create a spawner search which provides search capabilities over the indicated prefabs.
	/// </summary>
	/// <param name="prefabs"></param>
	/// <returns></returns>
	public static SpawnerSearch ForPrefabs(IEnumerable<GameObject> prefabs)
	{
		List<DevSpawnerDocument> documents = new List<DevSpawnerDocument>();
		foreach (var prefab in prefabs)
		{
			if (prefab.GetComponent<NetworkIdentity>() == null)
			{
				Logger.LogTraceFormat("{0} omitted from dev spawner because it has no network identity. Only" +
				                      " networked prefabs can be spawned.", Category.Admin);
				continue;
			}
			documents.Add(DevSpawnerDocument.ForPrefab(prefab));
		}

		return new SpawnerSearch(documents.OrderBy(doc => doc.SearchableName).ToArray());
	}

	/// <summary>
	/// Return documents matching the search.
	/// </summary>
	/// <param name="rawSearch">raw search query</param>
	/// <returns></returns>
	public IEnumerable<DevSpawnerDocument> Search(string rawSearch)
	{
		string standardizedSearch = Standardize(rawSearch);

		return documents.Where(doc => doc.SearchableName.Contains(standardizedSearch));
	}

	/// <summary>
	/// Standardizes text / search queries so searches will match regardless of things like
	/// case or special characters
	/// </summary>
	/// <param name="raw">raw text</param>
	/// <returns>standardized text</returns>
	public static string Standardize(string raw)
	{
		string result = raw.ToLower();
		//convert non alphanumeric stuff to whitespace - we only care about letters and numbers
		return SpawnerSearch.NON_ALPHANUMERIC.Replace(result, " ");
	}
}
