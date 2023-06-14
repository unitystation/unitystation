using System;
using Light2D;
using Mirror;
using UnityEngine;

namespace Core.Lighting
{
	public class LightsHolder : NetworkBehaviour
	{
		public SyncList<LightData> Lights { get; private set; } = new SyncList<LightData>();

		[SerializeField] private Transform lightsParent;
		[SerializeField] private GameObject lightSpriteObject;
		[SerializeField] private Rotatable rotatable;

		private void Start()
		{
			rotatable ??= GetComponent<Rotatable>();
			UpdateLights();
			rotatable.OnRotationChange.AddListener(OnRotate);
		}

		private void OnDestroy()
		{
			rotatable.OnRotationChange.RemoveListener(OnRotate);
		}

		private void OnRotate(OrientationEnum orientation)
		{
			//We add 90 to deal with the texture's own rotation.
			Quaternion rotation = Quaternion.Euler(0f, 0f, (float)orientation * 90 + 90);
			foreach (Transform child in lightsParent)
			{
				child.rotation = rotation;
			}
		}

		public int AddLight(LightData data)
		{
			Lights.Add(data);
			UpdateLights();
			return Lights.Count - 1;
		}

		public void RemoveLight(LightData data, int index)
		{
			Lights.Remove(data);
			Despawn.ClientSingle(lightsParent.GetChild(index).gameObject);
			UpdateLights();
		}

		public void RemoveLight(int data)
		{
			//BUG: Need to update the lights to remove the correct ID
			if (Lights.Contains(Lights[data]) == false)
			{
				Logger.LogError("Could not find correct light source to remove.");
				ClearHeldLights();
				UpdateLights();
				return;
			}

			RemoveLight(Lights[data], data);
			UpdateLights();
		}

		[ClientRpc]
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
			lightSprite.Sprite = data.lightSprite;
			lightSprite.transform.localScale = new Vector3(data.size, data.size, data.size);
		}

		[ClientRpc]
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
		public Color lightColor;
		public Light2D.LightSprite.LightShape lightShape;
		public Sprite lightSprite;
		public float size;
	}
}

