using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterCloset : RegisterObject {

		public bool PassableWhileOpen;
		
		private bool isClosed = true;
		public bool IsClosed {
			set {
				isClosed = value;
				Passable = PassableWhileOpen && !isClosed;
			}
			get { return isClosed; }
		}
	}
}