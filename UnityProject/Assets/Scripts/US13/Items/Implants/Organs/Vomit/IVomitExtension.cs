using US13.HealthV2.Living;

namespace US13.Items.Implants.Organs.Vomit
{
	public interface IVomitExtension
	{
		public void OnVomit(float amount, LivingHealthMasterBase health, Stomach stomach);
	}
}