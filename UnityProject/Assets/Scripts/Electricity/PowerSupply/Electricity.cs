namespace Electricity
{
	public struct Electricity
	{
		public float voltage;
		public float current;
		//The supply found at index 0 is the actual source that has combined the resources of the other sources and
		//sent the actual electrcity struct. So reference that one for supplySource
		public PowerSupply[] suppliers; //Where did this electricity come from
	}
}
