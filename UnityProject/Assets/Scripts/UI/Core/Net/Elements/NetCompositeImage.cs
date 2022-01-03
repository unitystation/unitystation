using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

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
		get { return ObjectNetId.ToString(); }
		set {
			externalChange = true;
			//don't update if it's the same sprite
			if ( ObjectNetId.ToString() != value && uint.TryParse( value, out var result ) )
			{
				ObjectNetId = result;

				//Don't need to resolve shit and render images on server
				if ( MasterTab.IsServer )
				{
					externalChange = false;
					return;
				}

				ResolvedObject = null;
				this.StartCoroutine( SetObject(), ref handle );
			} else
			{
				externalChange = false;
			}
		}
	}

	private IEnumerator SetObject()
	{
		yield return WaitForuint( ObjectNetId );
		UpdateCompositeImage();
		externalChange = false;
	}

	private void UpdateCompositeImage()
	{
		var texture = Snapshot.TakeObjectSnapshot( ResolvedObject );
		texture.filterMode = FilterMode;
		Image.sprite = Sprite.Create( texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f) );
	}

	private ObjectImageSnapshot snapshot;
	public ObjectImageSnapshot Snapshot {
		get {
			if ( !snapshot ) {
				snapshot = GetComponent<ObjectImageSnapshot>();
			}
			return snapshot;
		}
	}
	private Image image;
	public Image Image {
		get {
			if ( !image ) {
				image = GetComponent<Image>();
			}
			return image;
		}
	}

	private uint ObjectNetId;
	private GameObject ResolvedObject;
	private Coroutine handle;

	protected IEnumerator WaitForuint(uint id)
	{
		if (id == NetId.Empty)
		{
			Logger.LogWarningFormat( "{0} tried to wait on an empty (0) id", Category.Server, this.GetType().Name );
			yield break;
		}

		int tries = 0;
		while (!NetworkIdentity.spawned.ContainsKey(id))
		{
			if (tries++ > 10)
			{
				Logger.LogWarningFormat( "{0} could not find object with id {1}", Category.Server, this.GetType().Name, id );
				yield break;
			}

			yield return WaitFor.EndOfFrame;
		}

		ResolvedObject = NetworkIdentity.spawned[id].gameObject;
	}

	public override void ExecuteServer(ConnectedPlayer subject) {}
}