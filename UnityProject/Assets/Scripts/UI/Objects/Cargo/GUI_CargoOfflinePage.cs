using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;
using UI.Objects.Cargo;


namespace UI.Objects.Cargo
{
    public class GUI_CargoOfflinePage : GUI_CargoPage
    {
        public Text errorText;

        public override void OpenTab()
        {
            base.OpenTab();
            StartCoroutine(AnimateText());
        }

        private IEnumerator AnimateText()
        {
            while(gameObject.activeSelf)
            {
	            errorText.color = new Color(errorText.color.r, errorText.color.g, 0);
	            yield return WaitFor.Seconds(0.2f);
	            errorText.color = new Color(errorText.color.r, errorText.color.g, 1);
	            yield return WaitFor.Seconds(0.3f);
            }
        }
    }
}