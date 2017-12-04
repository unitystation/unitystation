using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ControlTabs : MonoBehaviour
    {
        public Button statsTab;
        public Button optionsTab;
        public Button moreTab;

        public GameObject panelStats;
        public GameObject panelOptions;
        public GameObject panelMore;

        public Color unselectColor;
        public Color selectedColor;

        private enum WindowSelect
        {
            stats,
            options,
            more
        }

        void Start()
        {
            SelectWindow(WindowSelect.stats);
        }

        private void SelectWindow(WindowSelect winSelect)
        {
            switch (winSelect)
            {
                case WindowSelect.stats:
                    statsTab.image.color = selectedColor;
                    optionsTab.image.color = unselectColor;
                    moreTab.image.color = unselectColor;
                    panelOptions.SetActive(false);
                    panelStats.SetActive(true);
                    panelMore.SetActive(false);

                    break;
                case WindowSelect.options:
                    statsTab.image.color = unselectColor;
                    optionsTab.image.color = selectedColor;
                    moreTab.image.color = unselectColor;
                    panelOptions.SetActive(true);
                    panelStats.SetActive(false);
                    panelMore.SetActive(false);
                    break;
                case WindowSelect.more:
                    statsTab.image.color = unselectColor;
                    optionsTab.image.color = unselectColor;
                    moreTab.image.color = selectedColor;
                    panelOptions.SetActive(false);
                    panelStats.SetActive(false);
                    panelMore.SetActive(true);
                    break;
            }
        }

        public void Button_Stats()
        {
            SelectWindow(WindowSelect.stats);
            SoundManager.Play("Click01");
        }

        public void Button_Options()
        {
            SelectWindow(WindowSelect.options);
            SoundManager.Play("Click01");
        }

        public void Button_More()
        {
            SelectWindow(WindowSelect.more);
            SoundManager.Play("Click01");
        }
    }
}
