using System.Text.RegularExpressions;
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
        #region UI Mouse Actions
        public void OnMouseEnter()
        {
            if (this.name.ToString().Contains("Door"))
            {
                //door names are bade, so we bail out because we do that in door controller
                return;
            }
            //thanks stack overflow!
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            string tmp = r.Replace(this.name, " ");
            UI.UIManager.SetToolTip = tmp;
        }
        public void OnMouseExit()
        {
            UI.UIManager.SetToolTip = "";
        }
        #endregion

    }
}