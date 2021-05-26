using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class TabHeaderButton : MonoBehaviour
	{
		public int Value { get; set; }

		private Button Button
		{
			get
			{
				if (button == false)
				{
					button = GetComponent<Button>();
				}
				return button;
			}
		}
		private Button button;
		private bool mouseOver = false;

		public Color selectedColor;
		public Color unselectColor;

		public void Select()
		{
			Button.image.color = selectedColor;
			if (mouseOver)
			{
				// Tab roll out
				if (ControlTabs.Instance.rolledOut == false)
				{
					ControlTabs.Instance.OpenTabWindow();
				}
			}
		}

		public void PointerEnter()
		{
			mouseOver = true;
		}

		public void PointerExit()
		{
			mouseOver = false;
		}

		public void Unselect()
		{
			Button.image.color = unselectColor;
		}

		[HideInInspector]
		public IntEvent Method; // don't touch this

		private void OnDisable()
		{
			Method.RemoveAllListeners();
		}

		private void OnEnable()
		{
			Method.AddListener(i => GetComponentInParent<ControlTabs>()?.SelectTab(i));
		}

		public void ClickExecute()
		{
			Method.Invoke(Value);
		}
	}

	/// <inheritdoc />
	/// "If you wish to use a generic UnityEvent type you must override the class type."
	[Serializable]
	public class IntEvent : UnityEvent<int> { }
}
