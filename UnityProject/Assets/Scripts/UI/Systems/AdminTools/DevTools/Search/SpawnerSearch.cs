
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Logs;
using Mirror;
using UI.Systems.AdminTools.DevTools.Search;
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
	public readonly DevSpawnerDocument[] documents;

	public readonly DevSpawnerDocument[] DEBUG_documents;

	private SpawnerSearch(DevSpawnerDocument[] documents, DevSpawnerDocument[] DEBUG_documents)
	{
		this.documents = documents;
		this.DEBUG_documents = DEBUG_documents;
	}

	/// <summary>
	/// Create a spawner search which provides search capabilities over the indicated prefabs.
	/// </summary>
	/// <param name="prefabs"></param>
	/// <returns></returns>
	public static SpawnerSearch ForPrefabs(IEnumerable<GameObject> prefabs)
	{
		List<DevSpawnerDocument> documents = new List<DevSpawnerDocument>();
		List<DevSpawnerDocument> DeBugs = new List<DevSpawnerDocument>();
		foreach (var prefab in prefabs)
		{
			if (prefab.GetComponent<NetworkIdentity>() == null)
			{
				Loggy.LogTraceFormat("{0} omitted from dev spawner because it has no network identity. Only" +
				                      " networked prefabs can be spawned.", Category.Admin);
				continue;
			}

			var newEntry = DevSpawnerDocument.ForPrefab(prefab);

			if (newEntry.Value.IsDEBUG == false)
			{
				documents.Add( (DevSpawnerDocument) newEntry );
			}
			DeBugs.Add((DevSpawnerDocument) newEntry );
		}

		return new SpawnerSearch(documents.OrderBy(doc => doc.SearchableName[0]).ToArray(), DeBugs.OrderBy(doc => doc.SearchableName[0]).ToArray());
	}

	/// <summary>
	/// Return documents matching the search.
	/// </summary>
	/// <param name="rawSearch">raw search query</param>
	/// <returns></returns>
	public IEnumerable<DevSpawnerDocument> Search(string rawSearch, bool DEBUG = false)
	{
		string standardizedSearch = Standardize(rawSearch);

		var ToUse = DEBUG ? DEBUG_documents : documents;

		// Linq expression that handles grabbing multiple names from a prefab.
		// it grabs all prefabs in documents then loops through all prefabs and grabs all searchable names.
		// if the searchable name contains a substring that the user is searching it will return it.
		var docs = (from doc in ToUse from prefabNames in doc.SearchableName
			where prefabNames.Contains(standardizedSearch) select doc).ToList();

		return docs;
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
