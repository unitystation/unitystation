using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Highlight : MonoBehaviour
{
	public static bool HighlightEnabled;
	public static Highlight instance;

	public SpriteRenderer prefabSpriteRenderer;
	public SpriteRenderer spriteRenderer;
	public Material material;

	 void Start()
	{
		if (PlayerPrefs.HasKey(PlayerPrefKeys.EnableHighlights))
		{
			if (PlayerPrefs.GetInt(PlayerPrefKeys.EnableHighlights) == 1)
			{
				HighlightEnabled = true;
			}
			else
			{
				HighlightEnabled = false;
			}
		}
		else
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.EnableHighlights, 1);
			PlayerPrefs.Save();
		}


	}
	public static void SetPreference(bool preference)
	{
		if (preference)
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.EnableHighlights, 1);
			HighlightEnabled = true;
		}
		else
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.EnableHighlights, 0);
			HighlightEnabled = false;
		}
		PlayerPrefs.Save();
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}
	public static void DeHighlight()
	{
		if (HighlightEnabled)
		{
			if (instance.spriteRenderer == null)
			{
				instance.spriteRenderer = Instantiate(instance.prefabSpriteRenderer);
			}

			Texture2D mainTex = instance.spriteRenderer.sprite.texture;
			Unity.Collections.NativeArray<Color32> data = mainTex.GetRawTextureData<Color32>();
			for (int xy = 0; xy < data.Length; xy++)
			{
				data[xy] = new Color32(0, 0, 0, 0);
			}
			mainTex.Apply();
		}
	}

	public static void HighlightThis(GameObject Highlightobject)
	{
		if (!PlayerManager.LocalPlayerScript.IsGhost && HighlightEnabled)
		{
			ShowHighlight(Highlightobject);
		}
	}

	public static void ShowHighlight(GameObject Highlightobject, bool ignoreHandApply = false)
	{
		if (instance.spriteRenderer == null)
		{
			instance.spriteRenderer = Instantiate(instance.prefabSpriteRenderer);
		}
		instance.spriteRenderer.gameObject.SetActive(true);
		instance.spriteRenderer.enabled = true;
		var SpriteRenderers = Highlightobject.GetComponentsInChildren<SpriteRenderer>();

		instance.spriteRenderer.transform.SetParent(SpriteRenderers[0].transform, true);
		instance.spriteRenderer.transform.localPosition = Vector3.zero;
		instance.spriteRenderer.transform.transform.localRotation = Quaternion.Euler(0,0,0);
		instance.spriteRenderer.sortingLayerID = SpriteRenderers[0].sortingLayerID;

		SpriteRenderers = SpriteRenderers.Where(x => x.sprite != null && x != instance.spriteRenderer).ToArray();
		Texture2D mainTex = instance.spriteRenderer.sprite.texture;
		if (ignoreHandApply || CheckHandApply(Highlightobject))
		{
			if (ignoreHandApply)
			{
				instance.material.SetColor("_OutlineColor", Color.green);
			}

			foreach (var T in SpriteRenderers)
			{
				RecursiveTextureStack(mainTex, T);
			}

			mainTex.Apply();
			instance.spriteRenderer.sprite = Sprite.Create(mainTex, new Rect(0, 0, mainTex.width, mainTex.height), new Vector2(0.5f, 0.5f), 32, 1, SpriteMeshType.FullRect, new Vector4(32, 32, 32, 32));
		}
	}


	static void RecursiveTextureStack(Texture2D mainTex, SpriteRenderer SpriteRenderers)
	{
		int xx = 3;
		int yy = 3;

		for (int x = (int)SpriteRenderers.sprite.textureRect.position.x; x < (int)SpriteRenderers.sprite.textureRect.position.x + SpriteRenderers.sprite.rect.width; x++)
		{
			for (int y = (int)SpriteRenderers.sprite.textureRect.position.y; y < SpriteRenderers.sprite.textureRect.position.y + SpriteRenderers.sprite.rect.height; y++)
			{
				//Logger.Log(yy + " <XX YY> " + xx + "   " +  x + " <X Y> " + y  );
				if (SpriteRenderers.sprite.texture.GetPixel(x, y).a != 0)
				{
					mainTex.SetPixel(xx, yy, SpriteRenderers.sprite.texture.GetPixel(x, y));
				}
				yy = yy + 1;
			}
			yy = 3;
			xx = xx + 1;
		}
	}


	public static bool CheckHandApply(GameObject target)
	{

		//call the used object's handapply interaction methods if it has any, for each object we are applying to
		var handApply = HandApply.ByLocalPlayer(target);
		var posHandApply = PositionalHandApply.ByLocalPlayer(target);

		//if handobj is null, then its an empty hand apply so we only need to check the receiving object
		if (handApply.HandObject != null)
		{
			//get all components that can handapply or PositionalHandApply
			var handAppliables = handApply.HandObject.GetComponents<MonoBehaviour>()
				.Where(c => c != null && c.enabled && (c is IBaseInteractable<HandApply> || c is IBaseInteractable<PositionalHandApply>));
			Logger.LogTraceFormat("Checking HandApply / PositionalHandApply interactions from {0} targeting {1}",
				Category.Interaction, handApply.HandObject.name, target.name);

			foreach (var handAppliable in handAppliables.Reverse())
			{
				if (handAppliable is IBaseInteractable<HandApply>)
				{
					var hap = handAppliable as IBaseInteractable<HandApply>;
					if (CheckInteractInternal(hap, handApply, NetworkSide.Client))
					{
						instance.material.SetColor("_OutlineColor", Color.cyan);
						return true;
					}
				}
				else
				{
					var hap = handAppliable as IBaseInteractable<PositionalHandApply>;
					if (CheckInteractInternal(hap, posHandApply, NetworkSide.Client))
					{
						instance.material.SetColor("_OutlineColor", Color.magenta);
						return true;
					}

				}
			}
		}


		//call the hand apply interaction methods on the target object if it has any
		var targetHandAppliables = handApply.TargetObject.GetComponents<MonoBehaviour>()
			.Where(c => c != null && c.enabled && (c is IBaseInteractable<HandApply> || c is IBaseInteractable<PositionalHandApply>));
		foreach (var targetHandAppliable in targetHandAppliables.Reverse())
		{
			if (targetHandAppliable is IBaseInteractable<HandApply> Hap)
			{
				//var hap = targetHandAppliable as IBaseInteractable<HandApply>;
				if (CheckInteractInternal(Hap, handApply, NetworkSide.Client))
				{
					instance.material.SetColor("_OutlineColor", Color.green);
					return true;
				}
			}
			else
			{
				var hap = targetHandAppliable as IBaseInteractable<PositionalHandApply>;
				if (CheckInteractInternal(hap, posHandApply, NetworkSide.Client))
				{
					instance.material.SetColor("_OutlineColor", new Color(1, 0.647f, 0));
					return true;
				}
			}
		}
		//instance.material.SetColor("_OutlineColor", Color.grey);
		return false;
	}


	private static bool CheckInteractInternal<T>(IBaseInteractable<T> interactable, T interaction,
	NetworkSide side)
	where T : Interaction
	{
		if (Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Interaction, side))) return false;
		var result = false;
		//check if client side interaction should be triggered
		if (side == NetworkSide.Client && interactable is IClientInteractable<T> clientInteractable)
		{
			result = clientInteractable.Interact(interaction);
			if (result)
			{
				Logger.LogTraceFormat("ClientInteractable triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, clientInteractable.GetType().Name,
					(clientInteractable as Component).gameObject.name);
				Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction);
				return true;
			}
		}
		//check other kinds of interactions
		if (interactable is ICheckable<T> checkable)
		{
			result = checkable.WillInteract(interaction, side);
			if (result)
			{
				Logger.LogTraceFormat("WillInteract triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, checkable.GetType().Name,
					(checkable as Component).gameObject.name);
				return true;
			}
		}
		else if (interactable is IInteractable<T>)
		{
			//use default logic
			result = DefaultWillInteract.Default(interaction, side);
			if (result)
			{
				Logger.LogTraceFormat("WillInteract triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, interactable.GetType().Name,
					(interactable as Component).gameObject.name);

				return true;
			}
		}

		Logger.LogTraceFormat("No interaction triggered from {0} on {1} for object {2}", Category.Interaction, typeof(T).Name, interactable.GetType().Name,
			(interactable as Component).gameObject.name);

		return false;
	}
}

