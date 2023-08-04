using System.Linq;
using Audio.Containers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Core.RightClick.LegacyRightClick
{
	public class LegacyRightClickEntry : MonoBehaviour
	{
		public RightClickMenuItem Item { get; private set; }

		[SerializeField] private Transform prefabOfSelf;
		[SerializeField] private Transform subMenusField;
		[SerializeField] private Button subMenusButton;
		[SerializeField] private Button mainButton;
		[SerializeField] private TMP_Text itemName;
		[SerializeField] private Image itemIcon;
		public UnityEvent OnClicked = new UnityEvent();
		public UnityEvent OnOpenedSubmenu = new UnityEvent();

		private void OnDestroy()
		{
			OnClicked.RemoveAllListeners();
			OnOpenedSubmenu.RemoveAllListeners();
		}

		public void Setup(RightClickMenuItem newItem)
		{
			Item = newItem;
			itemName.text = Item.Label;
			SetupColors();
			SetupIcon();
			SetupSubMenus();
			mainButton.onClick.AddListener(OnClick);
			mainButton.onClick.AddListener(() => _ = SoundManager.Play(CommonSounds.Instance.Click01));
		}

		private void SetupSubMenus()
		{
			var hasMenus = Item.SubMenus != null && Item.SubMenus.Any();
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
		}

		private void OnClick()
		{
			if (Item.gameObject != null && DistanceCheck() == false) return;

			if (Item.SubMenus != null && Item.SubMenus.Any())
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
			subMenusField.SetActive(false);
		}

		public void CreateSubMenus()
		{
			if (subMenusField.gameObject.activeSelf) return;
			OnOpenedSubmenu?.Invoke();
			foreach (Transform child in subMenusField)
			{
				Destroy(child.gameObject);
			}

			foreach (var entry in Item.SubMenus)
			{
				var obj = Instantiate(prefabOfSelf, subMenusField, false);
				var item = obj.gameObject.GetComponent<LegacyRightClickEntry>();
				item.Setup(entry);
				if (entry.keepMenuOpen == false) item.OnClicked.AddListener(() => OnClicked?.Invoke());
			}
			subMenusField.SetActive(true);
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}
	}
}