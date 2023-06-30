using HealthV2;

namespace Items.Implants.Organs.Vomit
{
	public interface IVomitExtension
	{
		public void OnVomit(float amount, LivingHealthMasterBase health, Stomach stomach);
	}
}