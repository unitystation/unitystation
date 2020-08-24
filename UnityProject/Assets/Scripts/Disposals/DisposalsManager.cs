using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Disposals
{
	/// <summary>
	/// Creates, updates, and removes all disposal instances.
	/// </summary>
	public class DisposalsManager : MonoBehaviour
	{
		static DisposalsManager instance;
		public static DisposalsManager Instance {
			get {
				if (instance == null)
				{
					instance = FindObjectOfType<DisposalsManager>();
				}

				return instance;
			}
			set { instance = value; }
		}

		[SerializeField]
		[Tooltip("Set the virtual container prefab to be used in disposal instances.")]
		public GameObject VirtualContainerPrefab;
		[SerializeField]
		[Tooltip("Set how many tiles every disposal instance can traverse in one second.")]
		float TileTraversalsPerSecond = 20;

		List<DisposalTraversal> disposalInstances = new List<DisposalTraversal>();

		void Update()
		{
			/*
			 * Allow one disposal instance to traverse per update.
			 * This means that in the (unlikely) event of huge number of disposals, performance
			 * shouldn't be considerably affected - disposal instances would just be delayed
			 * once there are enough instances such that there is more than one instance that
			 * is not CurrentlyDelayed in an update cycle.
			 *
			 * Realistically, it is expected no more than, say, five disposal instances would exist at one time,
			 * so there should be no noticeable throttling.
			*/

			foreach (DisposalTraversal disposal in disposalInstances)
			{
				if (!disposal.CurrentlyDelayed)
				{
					UpdateDisposal(disposal);
					break;
				}
			}
		}

		public static GameObject SpawnVirtualContainer(Vector3Int worldPosition)
		{
			SpawnResult virtualContainerSpawn = Spawn.ServerPrefab(Instance.VirtualContainerPrefab, worldPosition);
			if (!virtualContainerSpawn.Successful)
			{
				Logger.LogError(
						"Failed to spawn disposal virtual container! " +
						$"Is {nameof(DisposalsManager)} missing reference to {nameof(Instance.VirtualContainerPrefab)}?");
				return default;
			}

			return virtualContainerSpawn.GameObject;
		}

		/// <summary>
		/// Create a new disposal instance, which will move its contents along the disposal pipe network.
		/// </summary>
		/// <param name="container">The virtual container holding the entities to be disposed of.</param>
		public void NewDisposal(DisposalVirtualContainer container)
		{
			DisposalTraversal traversal = new DisposalTraversal(container);
			disposalInstances.Add(traversal);
		}

		/// <summary>
		/// Removes the disposal instance, stopping it from attempting to continue traversing the pipe network.
		/// </summary>
		/// <param name="disposal">The disposal instance to remove</param>
		public void FinishDisposal(DisposalTraversal disposal)
		{
			disposalInstances.Remove(disposal);
		}

		void UpdateDisposal(DisposalTraversal disposal)
		{
			if (disposal.TraversalFinished)
			{
				FinishDisposal(disposal);
			}
			else if (disposal.ReadyToTraverse)
			{
				disposal.Traverse();
			}

			disposal.CurrentlyDelayed = true;
			StartCoroutine(DelayTraversal(disposal));
		}

		IEnumerator DelayTraversal(DisposalTraversal disposal)
		{
			yield return WaitFor.Seconds(1 / TileTraversalsPerSecond);
			disposal.CurrentlyDelayed = false;
		}
	}
}
