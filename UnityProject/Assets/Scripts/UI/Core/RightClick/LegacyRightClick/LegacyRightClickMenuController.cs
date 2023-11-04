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

		[SerializeField] public GameObject entryPrefab;
		[SerializeField] private Transform entries;

		private bool isFocused = false;


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

			if (Input.GetMouseButtonDown(0))
			{
				if(isFocused ) return;
				HideSelf();
			}
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
				newEntry.Setup(menuItem, this);
				if (menuItem.keepMenuOpen == false)
				{
					newEntry.OnClicked.AddListener(HideSelf);
				}
				newEntry.OnOpenedSubmenu.AddListener(HideActiveSubMenu);
				newEntry.PointerEvents.OnEnter.AddListener(Focus);
				newEntry.PointerEvents.OnExit.AddListener(UnFocus);
				newEntry.SetActive(true);
			}
		}

		private void HideSelf()
		{
			self.SetActive(false);
			entries.gameObject.SetActive(false);
			entries.localPosition = new Vector3(0, 0, 0);
			foreach (Transform child in entries)
			{
				Destroy(child.gameObject);
			}
		}

		private void HideActiveSubMenu()
		{
			foreach (Transform child in entries)
			{
				child.GetComponent<LegacyRightClickEntry>()?.SubmenuOpenedEvent();
			}
		}

		public void Focus()
		{
			isFocused = true;
		}
		public void UnFocus()
		{
			isFocused = false;
		}
	}
}