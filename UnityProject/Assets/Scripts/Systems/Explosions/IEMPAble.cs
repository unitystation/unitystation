//Interface is triggered when object is affected by EMP

namespace Systems.Explosions
{
	public interface IEMPAble
	{
		void OnEMP(int EMPStrength);
	}
}
