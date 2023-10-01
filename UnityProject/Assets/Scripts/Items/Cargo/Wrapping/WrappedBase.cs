using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Logs;
using NaughtyAttributes;
using Objects;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items.Cargo.Wrapping
{
	public abstract class WrappedBase: ObjectContainer
	{
		[SerializeField][Tooltip("When unwrapped, if no content was defined, we will spawn one of these")]
		private List<GameObject> randomContentList;

		[SerializeField]
		private bool RandomPrefab = false;

		[SerializeField] [ShowIf(nameof(RandomPrefab))] private SpawnType OnlySpawn = SpawnType.ItemAndObject;

		private enum SpawnType
		{
			Item,
			Object,
			ItemAndObject,
		}


		[FormerlySerializedAs("unwrapSound")] [SerializeField] [Tooltip("Alternative sound to use when unwrapped")]
		private AddressableAudioSource alternativeUnwrapSound = null;

		[SerializeField][Tooltip("Message you read when you start unwrapping the package. " +
		                         "{0} = this object's name.")]
		protected string originatorUnwrapText = "You start unwrapping the {0}.";

		[SerializeField][Tooltip("Message others read when someone starts unwrapping this package." +
		                         "{0} = performer, {1} = this object's name")]
		protected string othersUnwrapText = "{0} starts unwrapping the {1}.";

		[SerializeField] [Tooltip("Time to unwrap.")]
		private float timeToUnwrap = 2;

		private Attributes attributes;
		protected SpriteHandler spriteHandler;

		private bool ContentGenerated = false;

		protected virtual void OnEnable()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			attributes = GetComponent<Attributes>();
		}

		public void SetContent(GameObject toWrap)
		{
			StoreObject(toWrap);
			var exportCost = toWrap.GetComponent<Attributes>().ExportCost;
			UpdateExportCost(exportCost);
		}

		private void UpdateExportCost(int value)
		{
			attributes.SetExportCost(value);
		}

		protected void PlayUnwrappingSound()
		{
			SoundManager.PlayNetworkedAtPos(alternativeUnwrapSound != null ? alternativeUnwrapSound : CommonSounds.Instance.PosterRipped, gameObject.AssumedWorldPosServer());
		}

		protected void StartUnwrapAction(GameObject performer)
		{
			var cfg = new StandardProgressActionConfig(
				StandardProgressActionType.Restrain);

			Chat.AddActionMsgToChat(
				performer,
				string.Format(originatorUnwrapText, gameObject.ExpensiveName()),
				string.Format(othersUnwrapText, performer.ExpensiveName(), gameObject.ExpensiveName()));

			StandardProgressAction.Create(cfg, UnWrap)
				.ServerStartProgress(ActionTarget.Object(performer.RegisterTile()), timeToUnwrap, performer);
		}

		public abstract void UnWrap();

		/// <summary>
		/// Used to get the content of the current package. If no content was set, then it will try to generate
		/// random content. Useful for mapping!
		/// </summary>
		/// <returns>The game object related to the content of this package</returns>
		public GameObject GetOrGenerateContent()
		{
			GameObject content = null;

			if (ContentGenerated == false)
			{
				ContentGenerated = true;
				if (RandomPrefab)
				{
					int Runs = 0;

					bool runLoop = true;

					while (runLoop)
					{

						Runs++;
						if (Runs > 100)
						{
							Loggy.LogError("Unable to find prefab matching parameters from allSpawnablePrefabs Breaking loop");
							break;
						}

						var Prefab = CustomNetworkManager.Instance.allSpawnablePrefabs.PickRandom();

						if (Prefab.GetComponent<UniversalObjectPhysics>() == null)
						{
							continue;
						}

						if (OnlySpawn == SpawnType.ItemAndObject)
						{
							content  = Spawn.ServerPrefab(Prefab, gameObject.AssumedWorldPosServer()).GameObject;
							return content;
						}
						else if (OnlySpawn == SpawnType.Item)
						{
							if (Prefab.GetComponent<ItemAttributesV2>() == null)
							{
								continue;
							}

							content  = Spawn.ServerPrefab(Prefab, gameObject.AssumedWorldPosServer()).GameObject;
							return content;
						}
						else if (OnlySpawn == SpawnType.Object)
						{
							if (Prefab.GetComponent<ObjectAttributes>() == null)
							{
								continue;
							}
							content  = Spawn.ServerPrefab(Prefab, gameObject.AssumedWorldPosServer()).GameObject;
							return content;
						}
					}
				}

				if (randomContentList.Count > 0)
				{
					content  = Spawn.ServerPrefab(randomContentList.PickRandom(), gameObject.AssumedWorldPosServer()).GameObject;
					return content;
				}
			}

			if (GetStoredObjects() != null)
			{
				content = GetStoredObjects().FirstOrDefault();
			}
			return content;
		}

		protected void MakeContentVisible(GameObject Content = null)
		{
			if (Content == null)
			{
				Content = GetOrGenerateContent();
			}
			var netTransform = Content.gameObject.GetComponent<UniversalObjectPhysics>();
			var pos = gameObject.RegisterTile().WorldPositionServer;
			netTransform.AppearAtWorldPositionServer(pos);
		}
	}
}
