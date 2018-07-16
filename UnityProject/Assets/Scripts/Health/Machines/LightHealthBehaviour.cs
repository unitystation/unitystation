using Lighting;

public class LightHealthBehaviour : HealthBehaviour
{
	protected override void OnDeathActions()
	{
		//        TADB_Debug.Log("Light ded!");
		GetComponentInParent<LightSource>().Trigger(false); //insert better solution here
	}
}