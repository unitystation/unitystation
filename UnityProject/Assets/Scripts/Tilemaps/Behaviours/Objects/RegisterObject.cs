using System.Text.RegularExpressions;
using UI;
using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterObject : RegisterTile
	{
		public bool AtmosPassable = true;
		[HideInInspector] public Vector3Int Offset = Vector3Int.zero;

		public bool Passable = true;

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
			if (name.Contains("Door"))
			{
				//door names are bade, so we bail out because we do that in door controller
				return;
			}
			//thanks stack overflow!
			Regex r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

			string tmp = r.Replace(name, " ");
			UIManager.SetToolTip = tmp;
		}

		public void OnMouseExit()
		{
			UIManager.SetToolTip = "";
		}

		#endregion
	}
}