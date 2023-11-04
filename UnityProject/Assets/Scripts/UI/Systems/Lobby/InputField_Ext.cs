using Logs;
using UnityEngine;
using UnityEngine.UI;


	public class InputField_Ext : InputField
	{
		private bool caretFound;
		private ScrollRect chatScrollArea;
		private ContentSizeFitter contentFitter;

		protected override void Start()
		{
			contentFitter = GetComponentInChildren<ContentSizeFitter>();
			chatScrollArea = transform.parent.gameObject.GetComponent<ScrollRect>();

			onValueChanged.AddListener(ResizeInput);
		}

		// Resize input field as new lines get added
		private void ResizeInput(string iText)
		{
			if (!caretFound)
			{
				DetectCaret();
			}

			//Adding 100f to the overall size just forces the contentSizeFitter to react
			float prefHeight = textComponent.rectTransform.sizeDelta.y + 100f;

			textComponent.rectTransform.sizeDelta = new Vector2(textComponent.rectTransform.sizeDelta.x, prefHeight);
			contentFitter.SetLayoutVertical(); // This ensures the content is updated before forcing the scroll area to scroll down (or else it gets jumpy)
			chatScrollArea.verticalNormalizedPosition =
				0f; //TODO: if the user is highlighting or has scrolled up then do not force the position (as it will be annoying a f)
		}

		private void DetectCaret()
		{
			GameObject findCaret = GameObject.Find(gameObject.name + " Input Caret");
			if (findCaret != null)
			{
				findCaret.transform.parent = textComponent.gameObject.transform;
				caretFound = true;
			}
			else
			{
				Loggy.Log("Caret obj not found",Category.UI);
			}
		}
	}
