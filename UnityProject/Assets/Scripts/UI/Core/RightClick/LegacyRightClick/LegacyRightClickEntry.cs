using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Core.RightClick.LegacyRightClick
{
	public class LegacyRightClickEntry : MonoBehaviour
	{
		public RightClickMenuItem Item { get; private set; }

		[SerializeField] private Transform prefabOfSelf;
		[FormerlySerializedAs("subMenusField")] public Transform SubMenusField;
		[SerializeField] private Button subMenusButton;
		[SerializeField] private Button mainButton;
		[SerializeField] private TMP_Text itemName;
		[SerializeField] private Image itemIcon;
		public UnityEvent OnClicked = new UnityEvent();
		public UnityEvent OnOpenedSubmenu = new UnityEvent();
		public InvokeEventOnPointer PointerEvents;
		public LegacyRightClickMenuController Manager;

		private GameObject linkedObject;

		private void OnDestroy()
		{
			OnClicked.RemoveAllListeners();
			OnOpenedSubmenu.RemoveAllListeners();
		}

		public void Setup(RightClickMenuItem newItem, LegacyRightClickMenuController manager, GameObject linked = null)
		{
			Item = newItem;
			itemName.text = Item.Label.Capitalize();
			linkedObject = linked;
			Manager = manager;
			SetupColors();
			SetupIcon();
			SetupSubMenus();
			mainButton.onClick.AddListener(OnClick);
			mainButton.onClick.AddListener(() => _ = SoundManager.Play(CommonSounds.Instance.Click01));
		}

		private bool HasSubMenus()
		{
			return Item.SubMenus != null && Item.SubMenus.Any();
		}

		private void SetupSubMenus()
		{
			var hasMenus = HasSubMenus();
			subMenusButton.SetActive(hasMenus);
			if (hasMenus == false) return;
			subMenusButton.onClick.AddListener(CreateSubMenus);
		}

		private void SetupColors()
		{
			var colorBlock = mainButton.colors;
			colorBlock.normalColor = Item.BackgroundColor;
		}

		private void SetupIcon()
		{
			itemIcon.sprite = Item.IconSprite;
			itemIcon.color = Item.IconColor;
			itemIcon.SetActive(Item.IconSprite != null);
			if (itemIcon.gameObject.activeSelf)
			{
				itemIcon.ApplySpriteScaling(itemIcon.sprite);
			}
		}

		private void OnClick()
		{
			if (Item.gameObject != null && DistanceCheck() == false) return;

			if (HasSubMenus())
			{
				CreateSubMenus();
			}
			else
			{
				Item.Action?.Invoke();
				OnClicked?.Invoke();
			}
		}

		private bool DistanceCheck()
		{
			var distance = Vector3.Distance(Item.gameObject.AssumedWorldPosServer(), PlayerManager.LocalPlayerObject.AssumedWorldPosServer());
			if (distance < 12f) return true;
			Chat.AddExamineMsg(PlayerManager.LocalPlayerObject, "<color=red>You're too far!</color>");
			return false;
		}

		public void SubmenuOpenedEvent()
		{
			if (linkedObject != null) return;
			SubMenusField.SetActive(false);
		}

		public void CreateSubMenus()
		{
			if (SubMenusField.gameObject.activeSelf || HasSubMenus() == false) return;
			OnOpenedSubmenu?.Invoke();
			foreach (Transform child in SubMenusField)
			{
				Destroy(child.gameObject);
			}

			foreach (var entry in Item.SubMenus)
			{
				var obj = Instantiate(Manager.entryPrefab, SubMenusField, false);
				var item = obj.gameObject.GetComponent<LegacyRightClickEntry>();
				item.Setup(entry, Manager, gameObject);

				if (entry.keepMenuOpen == false) item.OnClicked.AddListener(() => OnClicked?.Invoke());
				item.PointerEvents.OnEnter.AddListener(Manager.Focus);
				item.PointerEvents.OnExit.AddListener(Manager.UnFocus);
			}
			SubMenusField.SetActive(true);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}
	}
}