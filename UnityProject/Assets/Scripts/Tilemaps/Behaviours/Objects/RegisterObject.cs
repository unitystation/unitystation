using System.Text.RegularExpressions;
using UI;
using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterObject : RegisterTile
	{
		public bool AtmosPassable = true;

		public bool Passable = true;
	
		public override bool IsPassable(Vector3Int from)
		{
			return Passable;
		}
		
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