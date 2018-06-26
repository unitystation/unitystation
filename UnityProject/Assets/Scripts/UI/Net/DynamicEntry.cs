using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// Dynamic list entry
/// </summary>
public class DynamicEntry : NetUIElement {
	public List<NetUIElement> Elements => GetComponentsInChildren<NetUIElement>(false).ToList();
	
	public override string Value {
		get {
			Vector3 localPos = transform.localPosition;
			return $"{( int ) localPos.x}x{( int ) localPos.y}";
		}
		set {
			externalChange = true;
			var posData = value.Split( 'x' );
			int x = int.Parse(posData[0]); //or TryParse?
			int y = int.Parse(posData[1]);
			Vector2 pos = new Vector2(x, y);
			transform.localPosition = pos;
			externalChange = false;
		}
	}

	public Vector2 Position {
		get { return transform.localPosition; }
		set { transform.localPosition = value; }
	}

	public override void ExecuteServer() {}
}