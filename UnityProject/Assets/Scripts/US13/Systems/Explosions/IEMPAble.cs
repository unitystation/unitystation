//Interface is triggered when object is affected by EMP

namespace US13.Systems.Explosions
{
	public interface IEmpAble
	{
		void OnEmp(int EmpStrength);
	}
}
