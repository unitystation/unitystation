using System.Collections.Generic;

namespace Core.Pathfinding
{
    public class PriorityQueue<T>
    {
        private List<Tuple<T>> list = new List<Tuple<T>>();

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public void Enqueue(T item, float priority)
        {
            list.Add(new Tuple<T>(item, priority));
        }

        public T Dequeue()
        {
            T result = default(T);

            if (list.Count > 0)
            {
                int index = 0;

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].priority < list[index].priority)
                    {
                        index = i;
                    }
                }

                result = list[index].item;
                list.RemoveAt(index);
            }

            return result;
        }
    }

    struct Tuple<T>
    {
        public T item;
        public float priority;

        public Tuple(T item, float priority)
        {
            this.item = item;
            this.priority = priority;
        }
    }
}