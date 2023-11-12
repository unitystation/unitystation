using System;
using System.Collections;
using System.Collections.Generic;
using _3D;
using Messages.Server;
using Mirror;
using ScriptableObjects;
using SecureStuff;
using TileManagement;
using UI.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = System.Random;

public class Manager3D : MonoBehaviour
{
	public static Manager3D Instance;
	public void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this);
		}
	}


	public static bool Is3D = false;

	public GameObject objectToSpawn;

	public void PlayerLoadedIn(NetworkConnectionToClient Player)
	{
		//TODO Action system Lazy for now

		if (Is3D)
		{
			Activate3DMode.SendTo(Player);
		}
	}

	public void PromptConvertTo3D()
	{
		if (Is3D) return;

		DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Doom? **Hold !!!TAB to use mouse, will freeze for a few seconds", " would you like to activate DOOM mode?, is a WIP so is buggy will freeze for a few seconds *Hold !TAB! to use mouse!*, Would you also like music to accompany it? ",
			new List<DynamicUIChoiceEntryData>()
			{
				new DynamicUIChoiceEntryData()
				{
					ChoiceAction =  Ceilings,
					Text = " Give me DOOM with ceilings! ( Performance heavy )"
				},
				new DynamicUIChoiceEntryData()
				{
					ChoiceAction =  ConvertTo3DWithMusic,
					Text = " Give me music and DOOM! ( Opens YouTube in browser )  "
				},
				new DynamicUIChoiceEntryData()
				{
					ChoiceAction =  NoCeilings,
					Text = " Give me DOOM! "
				},
				new DynamicUIChoiceEntryData()
				{
					ChoiceAction = null,
					Text = " No, thank you. "
				}
			});
	}


	public void ConvertTo3DWithMusic()
	{
		Random rand = new Random();
		int randomNumber = rand.Next(701);
		char[] charArray = randomNumber.ToString().ToCharArray();
		Array.Reverse(charArray);
		string reversedString = new string(charArray);

		if (reversedString == "007") //This never happened to the other fella
		{
			SafeURL.Open("https://youtu.be/AFaJWqVcv8k?t=11");
		}
		else
		{
			SafeURL.Open("https://youtu.be/0gEkNVq1ct0?t=8");
		}

		ConvertTo3D();
	}

	private void Ceilings()
	{
		ConvertTo3D(true);
	}

	private void NoCeilings()
	{
		ConvertTo3D();
	}


	[NaughtyAttributes.Button()]
	public void ConvertTo3D(bool addCeilings = false)
	{
		Is3D = true;

		if (CustomNetworkManager.IsServer)
		{
			Activate3DMode.SendToEveryone();
		}

		if(GameData.IsHeadlessServer) return;

		// Get the specific scene by name or index
		Scene targetScene = SceneManager.GetSceneByName("OnlineScene");
		// Alternatively, you can use the scene index: SceneManager.GetSceneByIndex(0);

		// Check if the target scene is valid and loaded
		if (targetScene.IsValid() && targetScene.isLoaded)
		{
			// Instantiate the object to spawn at the root of the scene
			GameObject spawnedObject = Instantiate(objectToSpawn, Vector3.zero, Quaternion.identity, targetScene.GetRootGameObjects()[0].transform);

			spawnedObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);

			// Set the object to parent to the spawned object
			Camera.main.transform.parent = spawnedObject.transform;

			Camera.main.gameObject.AddComponent<FirstPersonCamera>();
			spawnedObject.transform.parent = null;
		}

		//Camera.main.GetComponent<LightingSystem>().enabled = false;

		Camera.main.orthographic = false;


		var Follow = Camera.main.GetComponent<Camera2DFollow>();
		Follow.offsetZ = 0;
		Follow.yOffSet = 0;


		UIManager.Instance.OrNull()?.panelHudBottomController.OrNull()?.ClickOnSelfUI.OrNull()?.gameObject.OrNull()?.SetActive(true);


		var Tiles = FindObjectsOfType<RegisterTile>();

		foreach (var Tile in Tiles)
		{
			if (Tile.GetComponent<ConvertTo3D>() == null)
			{
				Tile.gameObject.AddComponent<ConvertTo3D>();
			}
		}

		var To3D = FindObjectsOfType<ConvertTo3D>();

		foreach (var I3D in To3D)
		{
			I3D.DoConvertTo3D();
		}

		var maps = FindObjectsOfType<MetaTileMap>();


		foreach (var map in maps)
		{
			var PresentTiles = map.PresentTilesNeedsLock;
			if (PresentTiles != null)
			{
				lock (PresentTiles)
				{
					foreach (var Layer in PresentTiles)
					{
						if (Layer.Key == null || Layer.Value == null) continue;
						if (Layer.Key.LayerType == LayerType.Walls || Layer.Key.LayerType == LayerType.Windows)
						{
							foreach (var TileInfo in Layer.Value)
							{
								var Sprite3D = Instantiate(CommonPrefabs.Instance.Cube3D,
									TileInfo.Key + new Vector3(0.5f, 0.5f, 0), new Quaternion(),
									Layer.Key.transform).GetComponent<SetCubeSprite>();

								Sprite3D.gameObject.transform.localPosition = TileInfo.Key + new Vector3(0.5f, 0.5f, 0);

								if (TileInfo.Value?.layerTile?.PreviewSprite != null)
								{
									Sprite3D.SetSprite(TileInfo.Value.layerTile.PreviewSprite);
								}

								if (TileInfo.Value != null)
								{
									TileInfo.Value.AssociatedSetCubeSprite = Sprite3D;
								}
								else
								{
									Destroy(Sprite3D.gameObject);
								}
							}
							var Renderer = Layer.Key.GetComponent<TilemapRenderer>();
							if (Renderer != null)
							{
								Renderer.enabled = false;
							}
						}
						else if (Layer.Key.LayerType == LayerType.Floors && addCeilings)
						{
							foreach (var TileInfo in Layer.Value)
							{
								var Sprite3D = Instantiate(CommonPrefabs.Instance.Cube3D,
									TileInfo.Key + new Vector3(0.5f, 0.5f, -5), new Quaternion(),
									Layer.Key.transform).GetComponent<SetCubeSprite>();

								Sprite3D.gameObject.transform.localPosition = TileInfo.Key +  new Vector3(0.5f, 0.5f, -1.1f);

								TileInfo.Value.AssociatedSetCubeSprite = Sprite3D;
								Sprite3D.SetSprite(TileInfo.Value.layerTile.PreviewSprite);
							}
						}
						else if (Layer.Key.LayerType != LayerType.Objects)
						{
							Layer.Key.gameObject.transform.localPosition = new Vector3(0, 0, 0.5f);
						}
					}
				}
			}


			var MultilayerPresentTiles = map.MultilayerPresentTilesNeedsLock;
			if (MultilayerPresentTiles != null)
			{
				lock (MultilayerPresentTiles)
				{
					foreach (var Layer in MultilayerPresentTiles)
					{
						if (Layer.Key.LayerType != LayerType.Effects)
						{
							Layer.Key.gameObject.transform.localPosition = new Vector3(0, 0, 0.5f);
						}
					}
				}
			}
		}


		var ParallaxControllers = FindObjectsOfType<ParallaxController>();

		foreach (var ParallaxController in ParallaxControllers)
		{
			foreach (var Column in ParallaxController.backgroundTiles)
			{
				foreach (var Tile in Column.rows)
				{
					Tile.gameObject.SetActive(false);
				}
			}
		}

	}
}
