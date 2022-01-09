//Interface is triggered when object is affected by EMP

namespace Systems.Explosions
{
	public interface IEmpAble
	{
		void OnEmp(int EmpStrength);
	}
}
