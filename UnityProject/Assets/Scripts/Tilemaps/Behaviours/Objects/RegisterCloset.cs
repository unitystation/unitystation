using UnityEngine;


	[ExecuteInEditMode]
	public class RegisterCloset : RegisterObject
	{
		private bool isClosed = true;
		public bool IsClosed {
			set {
				isClosed = value;
				Passable = !isClosed;
			}
			get { return isClosed; }
		}
	}
