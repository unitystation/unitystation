using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterObject : RegisterTile
    {
        [HideInInspector]
        public Vector3Int Offset = Vector3Int.zero;

        public bool Passable = true;
        public bool AtmosPassable = true;

        public override bool IsPassable()
        {
            return Passable;
        }

        public override bool IsAtmosPassable()
        {
            return AtmosPassable;
        }

        #region UI MouseActions
        public void OnMouseEnter()
        {
            if (this.name.ToString().Contains("Door"))
            {
                return;
            }
            switch (this.name)
            {


                case "LightSwitch":
                    UI.UIManager.SetToolTip = "Light Switch";
                    return;

                case "LightTube":
                    UI.UIManager.SetToolTip = "Light Tube";
                    return;
                case "LightBulb":
                    UI.UIManager.SetToolTip = "Light Bulb";
                    return;
                case "FireAlarm":
                    UI.UIManager.SetToolTip = "Fire Alarm";
                    return;
                case "MonitorAir":
                    UI.UIManager.SetToolTip = "Air Monitor";
                    return;
                case "FireExtinguisherCabinet":
                    UI.UIManager.SetToolTip = "Fire Extinguisher Cabinet";
                    return;
                case "StatusDisplay_News":
                    UI.UIManager.SetToolTip = "Status Display: News";
                    return;
                case "CellTimer":
                    UI.UIManager.SetToolTip = "Cell Timer";
                    return;
                case "Metal_Chair":
                    UI.UIManager.SetToolTip = "Metal Chair";
                    return;
                case "DisposalsChute":
                    UI.UIManager.SetToolTip = "Disposals Chute";
                    return;
                case "Closet_Oxygen":
                    UI.UIManager.SetToolTip = "Oxygen Closet";
                    return;

                case "FireDoor":
                    UI.UIManager.SetToolTip = "Fire Door";
                    return;
                case "AirVent":
                    UI.UIManager.SetToolTip = "Air Vent";
                    return;
            }
            UI.UIManager.SetToolTip = this.name;
        }
        public void OnMouseExit()
        {
            UI.UIManager.SetToolTip = "";
        }
        #endregion

    }
}