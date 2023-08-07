using System.Collections.Generic;
using UnityEngine;

namespace UI.Core.RightClick.LegacyRightClick
{
	public class LegacyRightClickMenuController : MonoBehaviour, IRightClickMenu
	{
		private GameObject self;
		GameObject IRightClickMenu.Self
		{
			get => self == null ? gameObject : self;
			set => self = value;
		}

		public List<RightClickMenuItem> Items { get; set; }

		private Canvas canvas;
		public Canvas Canvas => this.GetComponentByRef(ref canvas);

		[SerializeField] private Transform entryPrefab;
		[SerializeField] private Transform entries;


		private void Awake()
		{
			self = gameObject;
			if (canvas == null) canvas = FindObjectOfType<Canvas>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			CheckForInput();
		}

		private void CheckForInput()
		{
			if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
			{
				HideSelf();
				return;
			}

			if (Input.GetMouseButtonDown(0) == false) return;
			CheckMouseClickInBounds();
		}

		/// <summary>
		/// Checks if a mouse click occurred within the bounds of the current game object's RectTransform.
		/// If the mouse click is outside of the bounds, deactivates the game object.
		/// </summary>
		private void CheckMouseClickInBounds()
		{
			//BUG: rect transform extends way beyond the bounds of the UI for some reason when resizing the game's screen or UI scale.
			//This is not a major issue. But it can be annoying trying to click away, and the menu decides to never go away.
			if (RectTransformUtility.RectangleContainsScreenPoint(gameObject.GetComponent<RectTransform>(),
				    Input.mousePosition)) return;
			HideSelf();
		}


		public void SetupMenu(List<RightClickMenuItem> items, IRadialPosition radialPosition, RightClickRadialOptions radialOptions)
		{
			transform.localScale = new Vector3(1, 0, 1);
			Items = items;
			self.transform.position = radialPosition.GetPositionIn(Camera.main, Canvas);
			self.transform.rotation = Quaternion.identity;
			SpawnEntries();
			this.SetActive(true);
			entries.gameObject.SetActive(true);
			LeanTween.scale(self, new Vector3(1, 1, 1), 0.15f).setEase(LeanTweenType.easeInOutQuad);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		private void SpawnEntries()
		{
			foreach (Transform child in entries)
			{
				Destroy(child.gameObject);
			}

			foreach (var menuItem in Items)
			{
				var entry = Instantiate(entryPrefab, entries, false);
				if (entry.gameObject.TryGetComponent<LegacyRightClickEntry>(out var newEntry) == false) continue;
				newEntry.Setup(menuItem);
				if (menuItem.keepMenuOpen == false)
				{
					newEntry.OnClicked.AddListener(HideSelf);
				}
				newEntry.OnOpenedSubmenu.AddListener(HideActiveSubMenu);
				newEntry.SetActive(true);
			}
		}

		private void HideSelf()
		{
			self.SetActive(false);
			entries.gameObject.SetActive(false);
		}

		private void HideActiveSubMenu()
		{
			foreach (Transform child in entries)
			{
				child.GetComponent<LegacyRightClickEntry>()?.SubmenuOpenedEvent();
			}
		}
	}
}