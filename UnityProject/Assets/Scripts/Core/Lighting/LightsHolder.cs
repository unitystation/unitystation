using System;
using System.Linq;
using Light2D;
using Logs;
using Mirror;
using UnityEngine;

namespace Core.Lighting
{
	public class LightsHolder : NetworkBehaviour
	{
		public readonly SyncList<LightData> Lights = new SyncList<LightData>();

		[SerializeField] private Transform lightsParent;
		[SerializeField] private GameObject lightSpriteObject;
		[SerializeField] private Rotatable rotatable;

		private OrientationEnum currentOrientation = OrientationEnum.Default;

		public override void OnStartClient()
		{
			base.OnStartClient();
			Lights.Callback += OnLightsListChange;
		}

		public override void OnStopClient()
		{
			base.OnStopClient();
			Lights.Callback -= OnLightsListChange;
		}

		private void OnDisable()
		{
			Lights.Callback -= OnLightsListChange;
		}

		private void Start()
		{
			rotatable ??= GetComponent<Rotatable>();
			UpdateLights(); //you still need to update on start because Mirror does not invoke the OnLightsListChanged event when the list gets first populated.
			rotatable.OnRotationChange.AddListener(OnRotate);
		}

		private void OnDestroy()
		{
			rotatable.OnRotationChange.RemoveListener(OnRotate);
		}

		private void OnRotate(OrientationEnum orientation)
		{
			currentOrientation = orientation;
			//We add 90 to deal with the texture's own origin/pivot.
			var rotation = Quaternion.Euler(0f, 0f, (float)orientation * 90 + 90);
			foreach (Transform child in lightsParent)
			{
				if (CustomNetworkManager.IsHeadless)
				{
					child.rotation = rotation;
				}
				else
				{
					LeanTween.rotate(child.gameObject, rotation.eulerAngles, 0.24f).
						setEaseInCubic().setEase(LeanTweenType.easeSpring);
				}
			}
		}

		public void AddLight(LightData data)
		{
			if (Lights.Any(lightSprite => lightSprite.Id == data.Id))
			{
				return;
			}
			Lights.Add(data);
			netIdentity.isDirty = true;
		}

		public void RemoveLight(LightData data)
		{
			if (Lights.Contains(data) == false)
			{
				Loggy.LogError("Could not find correct light source to remove.");
				ClearHeldLights();
				UpdateLights();
				return;
			}

			//this gets called on the server
			RemoveLightObject(data.Id);

			Lights.Remove(data);
			netIdentity.isDirty = true;
		}

		private void RemoveLightObject(int id)
		{
			foreach (var sprite in lightsParent.GetComponentsInChildren<LightSprite>())
			{
				if (sprite.GivenID == id) Despawn.ClientSingle(sprite.gameObject);
			}
		}

		private void OnLightsListChange(SyncList<LightData>.Operation op, int index, LightData oldItem,
			LightData newItem)
		{
			switch (op)
			{
				case SyncList<LightData>.Operation.OP_CLEAR:
					ClearHeldLights();
					break;
				case SyncList<LightData>.Operation.OP_REMOVEAT:
					//this gets called on the client
					RemoveLightObject(oldItem.Id);
					break;
				default:
					UpdateLights();
					break;
			}
		}

		public void UpdateLights()
		{
			for (int i = 0; i < Lights.Count; i++)
			{
				if (lightsParent.childCount <= i || lightsParent.GetChild(i) == null)
				{
					var newLight = Spawn.ClientPrefab(lightSpriteObject, parent: lightsParent);
					// Add the LightSprite component to the new game object
					LightSprite lightSprite = newLight.GameObject.GetComponent<LightSprite>();
					// Set the properties of the LightSprite component based on the corresponding data in the Lights list
					SetLightData(Lights[i], lightSprite);
					lightSprite.transform.localPosition = Vector3.zero;
					// Set the correct facing direction so that the light sprite doesn't look in a different direction when it gets added to the player.
					lightSprite.transform.rotation = Quaternion.Euler(0f, 0f, (float)currentOrientation * 90 + 90);
				}
				else
				{
					LightSprite lightSprite = lightsParent.GetChild(i).gameObject.GetComponent<LightSprite>();
					SetLightData(Lights[i], lightSprite);
				}
			}
		}

		private void SetLightData(LightData data, LightSprite lightSprite)
		{
			lightSprite.Color = data.lightColor;
			lightSprite.Shape = data.lightShape;
			if (data.lightSpriteObject != null)
			{
				lightSprite.Sprite = data.lightSpriteObject.GetComponent<ItemLightControl>()?.ObjectLightSprite.Sprite;
			}
			lightSprite.transform.localScale = new Vector3(data.size, data.size, data.size);
			if (data.Id != 0)
			{
				lightSprite.GivenID = data.Id;
				return;
			}
			Loggy.LogWarning("No id was given to lightSprite, assigning random one.", Category.Lighting);
			var newId = Guid.NewGuid().GetHashCode();
			data.Id = newId;
			lightSprite.GivenID = newId;
		}

		private void ClearHeldLights()
		{
			foreach (var sprite in lightsParent.GetComponentsInChildren<LightSprite>())
			{
				Despawn.ClientSingle(sprite.gameObject);
			}
		}
	}

	public struct LightData
	{
		public int Id;
		public Color lightColor;
		public Light2D.LightSprite.LightShape lightShape;
		public NetworkIdentity lightSpriteObject;
		public float size;
	}
}

