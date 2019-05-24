using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Renders composite image of gameobject instance that's available on clientside.
/// Value is object's NetId()
/// Rendering settings are in ObjectImageSnapshot
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(ObjectImageSnapshot))]
public class NetCompositeImage : NetUIElement
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
				ObjectNetId = new NetworkInstanceId(result);

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
		yield return WaitFor( ObjectNetId );
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

	private NetworkInstanceId ObjectNetId;
	private GameObject ResolvedObject;
	private Coroutine handle;

	protected IEnumerator WaitFor(NetworkInstanceId id)
	{
		if (id.IsEmpty())
		{
			Logger.LogWarningFormat( "{0} tried to wait on an empty (0) id", Category.NetMessage, this.GetType().Name );
			yield break;
		}

		int tries = 0;
		while ((ResolvedObject = ClientScene.FindLocalObject(id)) == null)
		{
			if (tries++ > 10)
			{
				Logger.LogWarningFormat( "{0} could not find object with id {1}", Category.NetMessage, this.GetType().Name, id );
				yield break;
			}

			yield return YieldHelper.EndOfFrame;
		}
	}

	public override void ExecuteServer() {}
}