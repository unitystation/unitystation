using System;

[Serializable]
public class MetaDataNode
{
	private int room = 0;

	private int damage = 0;

	public string WindowDmgType { get; set; } = "";

	public int Room
	{
		get { return room; }
		set { room = value; }
	}

	public void Reset()
	{
		Room = 0;
	}

	public void ResetDamage()
	{
		damage = 0;
	}

	public bool IsSpace()
	{
		return Room < 0;
	}

	public int GetDamage { get { return damage; } }

	public void AddDamage(int amt)
	{
		damage += amt;
	}
}