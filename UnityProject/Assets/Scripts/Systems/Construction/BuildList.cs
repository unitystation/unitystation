using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;

namespace Construction
{
	/// <summary>
	/// Defines which objects are possible to build using a given material in hand.
	/// </summary>
	[CreateAssetMenu(fileName = "BuildList", menuName = "Construction/BuildList")]
	public class BuildList : ScriptableObject
	{

		[Tooltip("Possible things which can be built. It will be automatically sorted by name" +
		         " in game.")]
		[SerializeField]
		[ArrayElementTitle("name", "unnamed")]
		private List<Entry> entries = null;

		/// <summary>
		/// Entries in this list, sorted alphabetically by name.
		/// </summary>
		public IEnumerable<Entry> Entries => entries.OrderBy(ent => ent.Name);

		[Serializable]
		public class Entry
		{
			[Tooltip("Name to show to the player for this entry.")]
			[SerializeField]
			private string name = null;

			/// <summary>
			/// Name to show to the player for this entry.
			/// </summary>
			public string Name => name;

			[Tooltip("Prefab of object which will be spawned.")]
			[SerializeField]
			private GameObject prefab = null;
			/// <summary>
			/// Prefab of object which will be spawned.
			/// </summary>
			public GameObject Prefab => prefab;

			[Tooltip("How much of the material it costs to build this.")]
			[SerializeField]
			private int cost = 1;

			/// <summary>
			/// How much of the material it costs to build this.
			/// </summary>
			public int Cost => cost;

			[Tooltip("Time it takes (in seconds) to build.")]
			[SerializeField]
			private float buildTime = 1;

			/// <summary>
			/// Time it takes (in seconds) to build.
			/// </summary>
			public float BuildTime => buildTime;

			[Tooltip("Number of instances of the prefab that will be spawned.")]
			[SerializeField]
			private int spawnAmount = 1;

			/// <summary>
			/// Number of instances of the prefab that will be spawned.
			/// </summary>
			public int SpawnAmount => spawnAmount;

			[Tooltip("Is only one allowed to be built on a given tile?")]
			[SerializeField]
			private bool onePerTile = true;
			/// <summary>
			/// Is only one allowed to be built on a given tile?
			/// </summary>
			public bool OnePerTile => onePerTile;

			[SerializeField]
			[Tooltip("When constructed will face the player")]
			private bool facePlayerDirectionOnConstruction = false;
			public bool FacePlayerDirectionOnConstruction => facePlayerDirectionOnConstruction;

			/// <summary>
			/// build this at the indicated location.
			/// </summary>
			/// <param name="at"></param>
			/// <param name="buildingMaterial">object being used in hand to build this.</param>
			/// <returns>true game object if successful</returns>
			public GameObject ServerBuild(SpawnDestination at, BuildingMaterial buildingMaterial)
			{
				var stackable = buildingMaterial.GetComponent<Stackable>();
				if (stackable != null)
				{
					if (stackable.Amount < cost)
					{
						Loggy.LogWarningFormat("Server logic error. " +
						                        "Tried building {0} with insufficient materials in hand ({1})." +
						                        " Build will not be performed.", Category.Construction, name,
							buildingMaterial);
						return null;
					}
					stackable.ServerConsume(cost);
				}
				else
				{
					if (cost > 1)
					{
						Loggy.LogWarningFormat("Server logic error. " +
						                        "Tried building {0} with insufficient materials in hand ({1})." +
						                        " Build will not be performed.", Category.Construction, name,
							buildingMaterial);
						return null;
					}

					Inventory.ServerDespawn(buildingMaterial.GetComponent<Pickupable>().ItemSlot);
				}

				return Spawn.ServerPrefab(prefab, at, spawnAmount)?.GameObject;
			}

			/// <summary>
			/// returns true iff this entry can be built with the buildingMaterial
			/// </summary>
			/// <param name="buildingMaterial"></param>
			/// <returns></returns>
			public bool CanBuildWith(BuildingMaterial buildingMaterial)
			{
				int heldAmount = 0;
                var stackable = buildingMaterial.GetComponent<Stackable>();
                if (stackable != null)
                {
                	heldAmount = stackable.Amount;
                }
                else
                {
                	heldAmount = 1;
                }

                return heldAmount >= Cost;
			}
		}

	}
}
