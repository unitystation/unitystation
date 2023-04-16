using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChargeable
{

	public bool FullyCharged();

	public void ChargeBy(float Watts);

}
