using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Used to identify Stock parts, With their level, type  and Other identifying information
/// </summary>
public class ConstructionComponent : NetworkBehaviour
{
	[SyncVar(hook = "UpdateSprite")]
	public ConstructionElementType CType;

	public int level = 0;

	public SpriteRenderer SR;

	public Sprite Manipulator;
	public Sprite Capacitor;
	public Sprite ScanningModule;
	public Sprite MatterBin;
	public Sprite Laser;
	public Sprite Battery;

	public void setTypeLevel(ConstructionElementType Type, int _level) {
		level = _level;
		UpdateSprite(Type);
	}

	public void UpdateSprite(ConstructionElementType Type) { 
		CType = Type;
		switch (Type)
		{
			case ConstructionElementType.Manipulator:
				SR.sprite = Manipulator; gameObject.name = Type.ToString(); break;
			case ConstructionElementType.Capacitor:
				SR.sprite = Capacitor; gameObject.name = Type.ToString();  break;
			case ConstructionElementType.ScanningModule:
				SR.sprite = ScanningModule; gameObject.name = Type.ToString();  break;
			case ConstructionElementType.MatterBin:
				SR.sprite = MatterBin; gameObject.name = Type.ToString();  break;
			case ConstructionElementType.Laser:
				SR.sprite = Laser; gameObject.name = Type.ToString();  break;
			case ConstructionElementType.Battery:
				SR.sprite = Battery; gameObject.name = Type.ToString();  break;

		}
	}


	// Start is called before the first frame update
	void Start()
	{
		setTypeLevel(CType, level);
	}


	public override void OnStartClient()
	{
		UpdateSprite(this.CType);
		base.OnStartClient();
	}
	private void OnStartServer()
	{
		UpdateSprite(this.CType);

		//if extending another component
		base.OnStartServer();
	}
}

public enum ConstructionElementType
{ 
	Null,
	Manipulator,
	Capacitor, 
	ScanningModule,
	MatterBin,
	Laser,
	Battery,
}