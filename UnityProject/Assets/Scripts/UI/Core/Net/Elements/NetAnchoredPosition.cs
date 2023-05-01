using UnityEngine;

namespace UI.Core.NetUI
{
	public class NetAnchoredPosition : NetUIStringElement
	{
		public RectTransform Element => element ??= GetComponent<RectTransform>();
		private RectTransform element;

		private Vector2 position;

		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public override string Value
		{
			get => position.ToString() ?? "-1";
			protected set
			{
				externalChange = true;
				SetPosition(value);
				externalChange = false;
			}
		}

		public void SetPosition(Vector2 anchoredPosition)
		{
			MasterSetValue(anchoredPosition.ToString());
		}

		private void SetPosition(string anchoredPosition)
		{
			anchoredPosition = anchoredPosition.TrimStart('(').TrimEnd(')');
			string[] pos = anchoredPosition.Split(',');

			if (float.TryParse(pos[0], out float x) == false) return;
			if (float.TryParse(pos[1], out float y) == false) return;

			if (Element.anchoredPosition == new Vector2(x, y)) return;

			Element.anchoredPosition = new Vector2(x, y);
			this.position = Element.anchoredPosition;
		}
	}
}
