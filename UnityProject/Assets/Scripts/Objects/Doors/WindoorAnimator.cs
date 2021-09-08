using System;
using System.Collections;
using System.Collections.Generic;
using TileManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

namespace Doors
{
	/// <summary>
	/// Used for Windoors
	/// Windoor animator.
	/// Remember to name at least one of the child sprite renderers as 'DoorBase'
	/// </summary>
	public class WindoorAnimator : DoorAnimator
	{
		[Tooltip("The resource path to the sprite sheet. i.e: icons/obj/doors/windoor")]
		public string WspritePath;

		[Tooltip("A list of frame numbers for the open/close animation, not including the openFrame and closeFrame")]
		public int[] WanimFrames;

		private int WanimLength;
		public int WcloseFrame;
		public int WdeniedFrame;
		public int WopenFrame;

		public bool IncludeAccessDeniedAnim;
		[Tooltip("Only check if the door changes appearance (smooths) based on nearby walls")]
		public bool Smoothed;
		private Tilemap tilemap;
		private MetaTileMap metaTileMap;
		private TileChangeManager tileChangeManager;
		private SpriteRenderer doorbase;
		private Sprite[] sprites;
		private static readonly int[] map =
		{
			0, 2, 4, 8, 1, 255, 3, 6, 12, 9, 10, 5, 7, 14, 13, 11, 15, 19, 38, 76, 137, 23, 39, 46, 78, 77, 141, 139, 27, 31, 47, 79, 143, 63, 111, 207, 159,
			191, 127, 239, 223, 55, 110, 205, 155, 175, 95
		};

		public void Awake()
		{
			WanimLength = WanimFrames.Length;
			sprites = Resources.LoadAll<Sprite>(WspritePath);
			foreach (Transform child in transform)
			{
				var cn = child.name.ToUpper();
				if (cn.Contains("DOORBASE")) doorbase = child.gameObject.GetComponent<SpriteRenderer>();
			}
			tileChangeManager = GetComponentInParent<TileChangeManager>();
		}

		public void Start()
		{
			//Call doorController after awake so it has a chance to init
			if (doorController.IsClosed)
			{
				doorbase.sprite = sprites[WcloseFrame];
			}
			else
			{
				doorbase.sprite = sprites[WopenFrame];
			}
		}

		public override void OpenDoor(bool skipAnimation)
		{
			if (skipAnimation == false)
			{
				doorController.isPerformingAction = true;
				doorController.PlayOpenSound();
				doorController.isPerformingAction = false;
			}

			StartCoroutine(PlayOpenAnim(skipAnimation));
		}

		public override void CloseDoor(bool skipAnimation)
		{
			if (skipAnimation == false)
			{
				doorController.isPerformingAction = true;
				doorController.PlayCloseSound();
			}

			StartCoroutine(PlayCloseAnim(skipAnimation));
		}

		public override void AccessDenied(bool skipAnimation)
		{
			if (skipAnimation || IncludeAccessDeniedAnim == false)
			{
				return;
			}

			doorController.isPerformingAction = true;
			_ = SoundManager.PlayAtPosition( CommonSounds.Instance.AccessDenied, transform.position, gameObject);
			StartCoroutine(PlayDeniedAnim());
		}

		/// <summary>
		/// Not implemented
		/// For doors that aren't airlocks: WinDoors, shutters, etc.
		/// Not needed for what's currently available but this may change in the future.
		/// </summary>
		/// <param name="skipAnimation"></param>
		public override void PressureWarn(bool skipAnimation)
		{
			return;
		}

		private IEnumerator Delay()
		{
			yield return WaitFor.Seconds(0.3f);
			doorController.isPerformingAction = false;
		}

		private bool HasWall(Vector3Int position, Vector3Int direction, Quaternion rotation, Tilemap tilemap)
		{
			TileBase tile = tilemap.GetTile(position + (rotation * direction).RoundToInt());
			ConnectedTile t = tile as ConnectedTile;
			return t != null && t.connectCategory == ConnectCategory.Walls;
		}

		private int SmoothFrame()
		{
			metaTileMap = tileChangeManager.MetaTileMap;
			tilemap = metaTileMap.Layers[LayerType.Walls].GetComponent<Tilemap>();
			Vector3Int position = Vector3Int.RoundToInt(transform.localPosition);
			var layer = tilemap.GetComponent<Layer>();
			Quaternion rotation;
			if (layer != null)
			{
				rotation = layer.RotationOffset.QuaternionInverted;
			}
			else
			{
				rotation = Quaternion.identity;
			}
			rotation = layer.RotationOffset.QuaternionInverted;
			int mask = (HasWall(position, Vector3Int.up, rotation, tilemap) ? 1 : 0) + (HasWall(position, Vector3Int.right, rotation, tilemap) ? 2 : 0) +
				   (HasWall(position, Vector3Int.down, rotation, tilemap) ? 4 : 0) + (HasWall(position, Vector3Int.left, rotation, tilemap) ? 8 : 0);
			if ((mask & 3) == 3)
			{
				mask += HasWall(position, Vector3Int.right + Vector3Int.up, rotation, tilemap) ? 16 : 0;
			}
			if ((mask & 6) == 6)
			{
				mask += HasWall(position, Vector3Int.right + Vector3Int.down, rotation, tilemap) ? 32 : 0;
			}
			if ((mask & 12) == 12)
			{
				mask += HasWall(position, Vector3Int.left + Vector3Int.down, rotation, tilemap) ? 64 : 0;
			}
			if ((mask & 9) == 9)
			{
				mask += HasWall(position, Vector3Int.left + Vector3Int.up, rotation, tilemap) ? 128 : 0;
			}
			int i = Array.IndexOf(map, mask);
			i += 6; //The first 6 frames are reserved for the animation
			return i;
		}

		private IEnumerator PlayCloseAnim(bool skipAnimation)
		{
			if (skipAnimation)
			{
				doorController.BoxCollToggleOn();
			}
			else
			{
				var halfway = Mathf.RoundToInt(WanimLength / 2);
				for (int i = WanimLength - 1; i >= 0; i--)
				{
					doorbase.sprite = sprites[WanimFrames[i]];
					//Stop movement half way through door opening to sync up with sortingOrder layer change
					if (i == halfway)
					{
						doorController.BoxCollToggleOn();
					}

					yield return WaitFor.Seconds(0.1f);
				}
			}
			if (Smoothed)
			{
				int i = SmoothFrame();
				doorbase.sprite = sprites[i];
			}
			else
			{
				doorbase.sprite = sprites[WcloseFrame];
			}
			doorController.OnAnimationFinished(isClosing: true);
		}

		private IEnumerator PlayOpenAnim(bool skipAnimation)
		{
			if (skipAnimation)
			{
				doorbase.sprite = sprites[WanimFrames[WanimLength - 1]];
				doorController.BoxCollToggleOff();
			}
			else
			{
				var halfway = Mathf.RoundToInt(WanimLength / 2);
				for (int j = 0; j < WanimLength; j++)
				{
					doorbase.sprite = sprites[WanimFrames[j]];
					//Allow movement half way through door opening to sync up with sortingOrder layer change
					if (j == halfway)
					{
						doorController.BoxCollToggleOff();
					}

					yield return WaitFor.Seconds(0.1f);
				}
			}

			doorbase.sprite = sprites[WopenFrame];
			doorController.OnAnimationFinished();
		}

		private IEnumerator PlayDeniedAnim()
		{
			bool light = false;
			for (int i = 0; i < WanimLength * 2; i++)
			{
				if (light == false)
				{
					doorbase.sprite = sprites[WdeniedFrame];
				}
				else
				{
					doorbase.sprite = sprites[WcloseFrame];
				}

				light = !light;
				yield return WaitFor.Seconds(0.05f);
			}

			doorbase.sprite = sprites[WcloseFrame];
			doorController.OnAnimationFinished();
		}
	}
}
