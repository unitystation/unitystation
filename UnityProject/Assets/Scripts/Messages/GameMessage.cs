using System.Collections;

public abstract class GameMessage<T> : GameMessageBase
{
	public abstract IEnumerator Process();
}