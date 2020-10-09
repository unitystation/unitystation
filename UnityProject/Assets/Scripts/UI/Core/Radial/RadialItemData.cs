using System;

namespace UI.Core.Radial
{
    [Serializable]
    public struct RadialItemData<T> where T : RadialItem<T>
    {
        public T radialItemPrefab;
        public int maxShownItems;
    }
}
