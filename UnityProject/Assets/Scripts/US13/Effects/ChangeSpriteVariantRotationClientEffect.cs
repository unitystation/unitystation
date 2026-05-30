using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Core.Transform;
using Util;

public class ChangeSpriteVariantRotationClientEffect : MonoBehaviour
{
	public SpriteHandler SpriteHandler;

    public void Start()
    {
	    //SpriteHandler.SetSpriteVariant((int)transform.localRotation.eulerAngles.z.Angle360ToOrientationEnum().AddDirectionsTogether(OrientationEnum.Down_By180 ));
    }


}
