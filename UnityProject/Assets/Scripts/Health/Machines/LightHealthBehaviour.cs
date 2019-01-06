public class LightHealthBehaviour : LivingHealthBehaviour
{
	protected override void OnDeathActions()
	{
		//        Logger.Log("Light ded!");
		GetComponentInParent<LightSource>().Trigger(false); //insert better solution here
	}
}