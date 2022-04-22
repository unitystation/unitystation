// The following dependency was taken from https://github.com/dave-hillier/disruptor-unity3d
// The Apache License 2.0 this dependency follows is located at https://github.com/dave-hillier/disruptor-unity3d/blob/master/LICENSE.
// Modifications were made by SoftwareGuy (Coburn). 

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace IgnoranceThirdparty
{
    /// <summary>
    /// Implementation of the Disruptor pattern
    /// </summary>
    /// <typeparam name="T">the type of item to be stored</typeparam>
    public class RingBuffer<T>
    {
        private readonly T[] _entries;
        private readonly int _modMask;
        private Volatile.PaddedLong _consumerCursor = new Volatile.PaddedLong();
        private Volatile.PaddedLong _producerCursor = new Volatile.PaddedLong();

        /// <summary>
        /// Creates a new RingBuffer with the given capacity
        /// </summary>
        /// <param name="capacity">The capacity of the buffer</param>
        /// <remarks>Only a single thread may attempt to consume at any one time</remarks>
        public RingBuffer(int capacity)
        {
            capacity = NextPowerOfTwo(capacity);
            _modMask = capacity - 1;
            _entries = new T[capacity];
        }

        /// <summary>
        /// The maximum number of items that can be stored
        /// </summary>
        public int Capacity
        {
            get { return _entries.Length; }
        }

        public T this[long index]
        {
            get { unchecked { return _entries[index & _modMask]; } }
            set { unchecked { _entries[index & _modMask] = value; } }
        }

        /// <summary>
        /// Removes an item from the buffer.
        /// </summary>
        /// <returns>The next available item</returns>
        public T Dequeue()
        {
            var next = _consumerCursor.ReadAcquireFence() + 1;
            while (_producerCursor.ReadAcquireFence() < next) // makes sure we read the data from _entries after we have read the producer cursor
            {
                Thread.SpinWait(1);
            }
            var result = this[next];
            _consumerCursor.WriteReleaseFence(next); // makes sure we read the data from _entries before we update the consumer cursor
            return result;
        }

        /// <summary>
        /// Attempts to remove an items from the queue
        /// </summary>
        /// <param name="obj">the items</param>
        /// <returns>True if successful</returns>
        public bool TryDequeue(out T obj)
        {
            var next = _consumerCursor.ReadAcquireFence() + 1;

            if (_producerCursor.ReadAcquireFence() < next)
            {
                obj = default(T);
                return false;
            }
            obj = Dequeue();
            return true;
        }

        /// <summary>
        /// Add an item to the buffer
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            var next = _producerCursor.ReadAcquireFence() + 1;

            long wrapPoint = next - _entries.Length;
            long min = _consumerCursor.ReadAcquireFence();

            while (wrapPoint > min)
            {
                min = _consumerCursor.ReadAcquireFence();
                Thread.SpinWait(1);
            }

            this[next] = item;
            _producerCursor.WriteReleaseFence(next); // makes sure we write the data in _entries before we update the producer cursor
        }

        /// <summary>
        /// The number of items in the buffer
        /// </summary>
        /// <remarks>for indicative purposes only, may contain stale data</remarks>
        public int Count { get { return (int)(_producerCursor.ReadFullFence() - _consumerCursor.ReadFullFence()); } }

        private static int NextPowerOfTwo(int x)
        {
            var result = 2;
            while (result < x)
            {
                result <<= 1;
            }
            return result;
        }


    }
    public static class Volatile
    {
        private const int CacheLineSize = 64;

        [StructLayout(LayoutKind.Explicit, Size = CacheLineSize * 2)]
        public struct PaddedLong
        {
            [FieldOffset(CacheLineSize)]
            private long _value;

            /// <summary>
            /// Create a new <see cref="PaddedLong"/> with the given initial value.
            /// </summary>
            /// <param name="value">Initial value</param>
            public PaddedLong(long value)
            {
                _value = value;
            }

            /// <summary>
            /// Read the value without applying any fence
            /// </summary>
            /// <returns>The current value</returns>
            public long ReadUnfenced()
            {
                return _value;
            }

            /// <summary>
            /// Read the value applying acquire fence semantic
            /// </summary>
            /// <returns>The current value</returns>
            public long ReadAcquireFence()
            {
                var value = _value;
                Thread.MemoryBarrier();
                return value;
            }

            /// <summary>
            /// Read the value applying full fence semantic
            /// </summary>
            /// <returns>The current value</returns>
            public long ReadFullFence()
            {
                Thread.MemoryBarrier();
                return _value;
            }

            /// <summary>
            /// Read the value applying a compiler only fence, no CPU fence is applied
            /// </summary>
            /// <returns>The current value</returns>
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public long ReadCompilerOnlyFence()
            {
                return _value;
            }

            /// <summary>
            /// Write the value applying release fence semantic
            /// </summary>
            /// <param name="newValue">The new value</param>
            public void WriteReleaseFence(long newValue)
            {
                Thread.MemoryBarrier();
                _value = newValue;
            }

            /// <summary>
            /// Write the value applying full fence semantic
            /// </summary>
            /// <param name="newValue">The new value</param>
            public void WriteFullFence(long newValue)
            {
                Thread.MemoryBarrier();
                _value = newValue;
            }

            /// <summary>
            /// Write the value applying a compiler fence only, no CPU fence is applied
            /// </summary>
            /// <param name="newValue">The new value</param>
            [MethodImpl(MethodImplOptions.NoOptimization)]
            public void WriteCompilerOnlyFence(long newValue)
            {
                _value = newValue;
            }

            /// <summary>
            /// Write without applying any fence
            /// </summary>
            /// <param name="newValue">The new value</param>
            public void WriteUnfenced(long newValue)
            {
                _value = newValue;
            }

            /// <summary>
            /// Atomically set the value to the given updated value if the current value equals the comparand
            /// </summary>
            /// <param name="newValue">The new value</param>
            /// <param name="comparand">The comparand (expected value)</param>
            /// <returns></returns>
            public bool AtomicCompareExchange(long newValue, long comparand)
            {
                return Interlocked.CompareExchange(ref _value, newValue, comparand) == comparand;
            }

            /// <summary>
            /// Atomically set the value to the given updated value
            /// </summary>
            /// <param name="newValue">The new value</param>
            /// <returns>The original value</returns>
            public long AtomicExchange(long newValue)
            {
                return Interlocked.Exchange(ref _value, newValue);
            }

            /// <summary>
            /// Atomically add the given value to the current value and return the sum
            /// </summary>
            /// <param name="delta">The value to be added</param>
            /// <returns>The sum of the current value and the given value</returns>
            public long AtomicAddAndGet(long delta)
            {
                return Interlocked.Add(ref _value, delta);
            }

            /// <summary>
            /// Atomically increment the current value and return the new value
            /// </summary>
            /// <returns>The incremented value.</returns>
            public long AtomicIncrementAndGet()
            {
                return Interlocked.Increment(ref _value);
            }

            /// <summary>
            /// Atomically increment the current value and return the new value
            /// </summary>
            /// <returns>The decremented value.</returns>
            public long AtomicDecrementAndGet()
            {
                return Interlocked.Decrement(ref _value);
            }

            /// <summary>
            /// Returns the string representation of the current value.
            /// </summary>
            /// <returns>the string representation of the current value.</returns>
            public override string ToString()
            {
                var value = ReadFullFence();
                return value.ToString();
            }
        }
    }
}
