using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using UI.Core.Radial;

namespace UI.Core.RightClick
{
    public class ActionRadial : Radial<RightClickRadialButton>
    {
	    [SerializeField]
        private RectTransform borderPrefab = default;

        [SerializeField]
        private Image radialMask = default;

        [SerializeField]
        private Transform background = default;

        [Tooltip("Set the angle for each item in the action radial.")]
        [Range(1, 180)]
        [SerializeField]
        private int itemAngle = default;

        private RectTransform startBorder;
        private RectTransform endBorder;

        private T InitOrGet<T>(ref T obj, T prefab) where T : Component
        {
            if (obj == null)
            {
                obj = Instantiate(prefab, transform);
                obj.SetActive(true);
            }

            return obj;
        }

        public RectTransform StartBorder => InitOrGet(ref startBorder, borderPrefab);

        public RectTransform EndBorder => InitOrGet(ref endBorder, borderPrefab);

        public ParentConstraint ParentConstraint { get; private set; }

        public void Awake()
        {
	        ParentConstraint = GetComponent<ParentConstraint>();
        }

        public void LateUpdate()
        {
	        background.rotation = Quaternion.identity;
        }

        public override void Setup(int itemCount)
        {
	        FillAngle = itemCount * itemAngle;
	        base.Setup(itemCount);
	        radialMask.fillAmount = (1f / 360f) * FillAngle;
	        StartBorder.localPosition = new Vector3(0, -.5f, 0);
	        EndBorder.localEulerAngles = new Vector3(0f, 0f, ItemArcAngle * itemCount);
	        EndBorder.localPosition = new Vector3(-0.5f, 0, 0);
	        if (Items.Count > 0)
	        {
		        Items[0].SetDividerActive(false);
	        }
        }

        public void UpdateRotation(int index, IRadial radial)
        {
	        var buttonAngle = index * radial.ItemArcAngle;
	        var zOffset = buttonAngle + (radial.ItemArcAngle * 0.5f - ShownItemsCount * 0.5f * ItemArcAngle);
	        ParentConstraint.SetRotationOffset(0, new Vector3(0, 0, zOffset));
        }

        public void SetConstraintSource(Transform rotationSource)
        {
	        ParentConstraint.AddSource(new ConstraintSource {sourceTransform = rotationSource, weight = 1});
	        ParentConstraint.constraintActive = true;
        }
    }
}
