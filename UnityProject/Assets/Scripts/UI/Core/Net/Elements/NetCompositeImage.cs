using System.Collections;
using Logs;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// <summary>
	/// Renders composite image of gameobject instance that's available on clientside.
	/// Value is object's NetId()
	/// Rendering settings are in ObjectImageSnapshot
	/// </summary>
	[RequireComponent(typeof(Image))]
	[RequireComponent(typeof(ObjectImageSnapshot))]
	public class NetCompositeImage : NetUIStringElement
	{
		public override ElementMode InteractionMode => ElementMode.ServerWrite;
		public FilterMode FilterMode = FilterMode.Point;

		public override string Value {
			get => ObjectNetId.ToString();
			protected set {
				//don't update if it's the same sprite
				if (ObjectNetId.ToString() != value && uint.TryParse(value, out var result))
				{
					externalChange = true;
					ObjectNetId = result;

					//Don't need to resolve shit and render images on server
					if (containedInTab.IsMasterTab)
					{
						externalChange = false;
						return;
					}

					ResolvedObject = null;
					this.StartCoroutine(SetObject(), ref handle);
				}
			}
		}

		private IEnumerator SetObject()
		{
			yield return WaitForuint(ObjectNetId);
			UpdateCompositeImage();
			externalChange = false;
		}

		private void UpdateCompositeImage()
		{
			var texture = Snapshot.TakeObjectSnapshot(ResolvedObject);
			texture.filterMode = FilterMode;
			Image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
		}

		private ObjectImageSnapshot snapshot;
		public ObjectImageSnapshot Snapshot => snapshot ??= GetComponent<ObjectImageSnapshot>();

		private Image image;
		public Image Image => image ??= GetComponent<Image>();

		private uint ObjectNetId;
		private GameObject ResolvedObject;
		private Coroutine handle;

		protected IEnumerator WaitForuint(uint id)
		{
			if (id == NetId.Empty)
			{
				Loggy.LogWarningFormat("{0} tried to wait on an empty (0) id", Category.Server, this.GetType().Name);
				yield break;
			}

			var spawned =
				CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

			int tries = 0;
			while (spawned.ContainsKey(id) == false)
			{
				if (tries++ > 10)
				{
					Loggy.LogWarningFormat("{0} could not find object with id {1}", Category.Server, this.GetType().Name, id);
					yield break;
				}

				yield return WaitFor.EndOfFrame;
			}

			ResolvedObject = spawned[id].gameObject;
		}

		public override void ExecuteServer(PlayerInfo subject) { }
	}
}
