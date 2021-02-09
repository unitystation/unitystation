using System.Text;

public abstract class NetUIStringElement : NetUIElement<string>
{
	public override byte[] BinaryValue
	{
		get => Encoding.UTF8.GetBytes(Value ?? string.Empty);
		set => Value = Encoding.UTF8.GetString(value);
	}
}