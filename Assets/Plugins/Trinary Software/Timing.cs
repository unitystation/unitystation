using UnityEngine;
using System.Collections.Generic;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

// /////////////////////////////////////////////////////////////////////////////////////////
//                              More Effective Coroutines
//                                        v1.10.1
// 
// This is an improved implementation of coroutines that boasts zero per-frame memory allocations,
// runs about twice as fast as Unity's built in coroutines and has a range of extra features.
// 
// This is the free version. MEC also has a pro version, which can be found here:
// https://www.assetstore.unity3d.com/en/#!/content/68480
// The pro version contains exactly the same core that the free version uses, but also
// contains additional features.
// 
// For manual, support, or upgrade guide visit http://trinary.tech/
//
// Created by Teal Rogers
// Trinary Software
// All rights preserved
// /////////////////////////////////////////////////////////////////////////////////////////

namespace MovementEffects
{
    public class Timing : MonoBehaviour
    {
        private class WaitingProcess
        {
            public class ProcessData
            {
                public IEnumerator<float> Task;
                public string Tag;
                public Segment Segment;
            }

            public IEnumerator<float> Trigger;
            public string TriggerTag;
            public bool Killed;
            public readonly List<ProcessData> Tasks = new List<ProcessData>();
        }

        private struct ProcessIndex : System.IEquatable<ProcessIndex>
        {
            public Segment seg;
            public int i;

            public bool Equals(ProcessIndex other)
            {
                return seg == other.seg && i == other.i;
            }

            public override bool Equals(object other)
            {
                if (other is ProcessIndex)
                    return Equals((ProcessIndex)other);
                return false;
            }

            public static bool operator ==(ProcessIndex a, ProcessIndex b)
            {
                return a.seg == b.seg && a.i == b.i;
            }

            public static bool operator !=(ProcessIndex a, ProcessIndex b)
            {
                return a.seg != b.seg || a.i != b.i;
            }

            public override int GetHashCode()
            {
                return (((int)seg - 2) * (int.MaxValue / 3)) + i;
            }
        }

        public float TimeBetweenSlowUpdateCalls = 1f / 7f;
        public int NumberOfUpdateCoroutines;
        public int NumberOfFixedUpdateCoroutines;
        public int NumberOfLateUpdateCoroutines;
        public int NumberOfSlowUpdateCoroutines;

        [HideInInspector]
        public double localTime;
        public static double LocalTime { get { return Instance.localTime; } }
        [HideInInspector]
        public float deltaTime;
        public static float DeltaTime { get { return Instance.deltaTime; } }

        private bool _runningUpdate;
        private bool _runningFixedUpdate;
        private bool _runningLateUpdate;
        private bool _runningSlowUpdate;
        private int _nextUpdateProcessSlot;
        private int _nextLateUpdateProcessSlot;
        private int _nextFixedUpdateProcessSlot;
        private int _nextSlowUpdateProcessSlot;
        private double _lastUpdateTime;
        private double _lastLateUpdateTime;
        private double _lastFixedUpdateTime;
        private double _lastSlowUpdateTime;
        private ushort _framesSinceUpdate;
        private ushort _expansions = 1;

        private const ushort FramesUntilMaintenance = 64;
        private const int ProcessArrayChunkSize = 64;
        private const int InitialBufferSizeLarge = 256;
        private const int InitialBufferSizeMedium = 64;
        private const int InitialBufferSizeSmall = 8;

        public System.Action<System.Exception> OnError;
        public static System.Func<IEnumerator<float>, Segment, string, IEnumerator<float>> ReplacementFunction;
        private readonly List<WaitingProcess> _waitingProcesses = new List<WaitingProcess>();
        private readonly Queue<System.Exception> _exceptions = new Queue<System.Exception>();
        private readonly Dictionary<ProcessIndex, string> _processTags = new Dictionary<ProcessIndex, string>();
        private readonly Dictionary<string, HashSet<ProcessIndex>> _taggedProcesses = new Dictionary<string, HashSet<ProcessIndex>>();

        private IEnumerator<float>[] UpdateProcesses = new IEnumerator<float>[InitialBufferSizeLarge];
        private IEnumerator<float>[] LateUpdateProcesses = new IEnumerator<float>[InitialBufferSizeSmall];
        private IEnumerator<float>[] FixedUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];
        private IEnumerator<float>[] SlowUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];

        private static Timing _instance;
        public static Timing Instance
        {
            get
            {
                if (_instance == null || !_instance.gameObject)
                {
                    GameObject instanceHome = GameObject.Find("Movement Effects");
                    System.Type movementType =
                        System.Type.GetType("MovementEffects.Movement, MovementOverTime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

                    if(instanceHome == null)
                    {
                        instanceHome = new GameObject { name = "Movement Effects" };
                        DontDestroyOnLoad(instanceHome);

                        if (movementType != null)
                            instanceHome.AddComponent(movementType);

                        _instance = instanceHome.AddComponent<Timing>();
                    }
                    else
                    {
                         if (movementType != null && instanceHome.GetComponent(movementType) == null) 
                            instanceHome.AddComponent(movementType);

                        _instance = instanceHome.GetComponent<Timing>() ?? instanceHome.AddComponent<Timing>();
                    }
                }

                return _instance;
            }

            set { _instance = value; }
        }

        void Awake()
        {
            if(_instance == null)
                _instance = this;
            else
                deltaTime = _instance.deltaTime;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            if(_lastSlowUpdateTime + TimeBetweenSlowUpdateCalls < Time.realtimeSinceStartup && _nextSlowUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.SlowUpdate };
                _runningSlowUpdate = true;
                UpdateTimeValues(coindex.seg);

                for (coindex.i = 0; coindex.i < _nextSlowUpdateProcessSlot; coindex.i++)
                {
                    if (SlowUpdateProcesses[coindex.i] != null && !(Time.realtimeSinceStartup < SlowUpdateProcesses[coindex.i].Current))
                    {
                        Profiler.BeginSample("Processing Coroutine (Slow Update)");

                        try
                        {
                            if (!SlowUpdateProcesses[coindex.i].MoveNext())
                            {
                                SlowUpdateProcesses[coindex.i] = null;
                            }
                            else if (SlowUpdateProcesses[coindex.i] != null && float.IsNaN(SlowUpdateProcesses[coindex.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    SlowUpdateProcesses[coindex.i] = null;
                                }
                                else
                                {
                                    SlowUpdateProcesses[coindex.i] = ReplacementFunction(SlowUpdateProcesses[coindex.i],
                                        coindex.seg, _processTags.ContainsKey(coindex) ? _processTags[coindex] : null);

                                    ReplacementFunction = null;
                                    coindex.i--;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            SlowUpdateProcesses[coindex.i] = null;
                        }

                        Profiler.EndSample();
                    }
                }

                _runningSlowUpdate = false;
            }

            if (_nextUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.Update };
                _runningUpdate = true;
                UpdateTimeValues(coindex.seg);

                for (coindex.i = 0; coindex.i < _nextUpdateProcessSlot; coindex.i++)
                {
                    if (UpdateProcesses[coindex.i] != null && !(localTime < UpdateProcesses[coindex.i].Current))
                    {
                        Profiler.BeginSample("Processing Coroutine");

                        try
                        {
                            if (!UpdateProcesses[coindex.i].MoveNext())
                            {
                                UpdateProcesses[coindex.i] = null;
                            }
                            else if (UpdateProcesses[coindex.i] != null && float.IsNaN(UpdateProcesses[coindex.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    UpdateProcesses[coindex.i] = null;
                                }
                                else
                                {
                                    UpdateProcesses[coindex.i] = ReplacementFunction(UpdateProcesses[coindex.i],
                                        coindex.seg, _processTags.ContainsKey(coindex) ? _processTags[coindex] : null);

                                    ReplacementFunction = null;
                                    coindex.i--;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            UpdateProcesses[coindex.i] = null;
                        }

                        Profiler.EndSample();
                    }
                }

                _runningUpdate = false;
            }

            if(++_framesSinceUpdate > FramesUntilMaintenance)
            {
                _framesSinceUpdate = 0;

                Profiler.BeginSample("Maintenance Task");

                RemoveUnused();

                Profiler.EndSample();
            }

            if (_exceptions.Count > 0)
                 throw _exceptions.Dequeue();
        }

        private void FixedUpdate()
        {
            if(_nextFixedUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.FixedUpdate };
                _runningFixedUpdate = true;
                UpdateTimeValues(coindex.seg);

                for (coindex.i = 0; coindex.i < _nextFixedUpdateProcessSlot; coindex.i++)
                {
                    if (FixedUpdateProcesses[coindex.i] != null && !(localTime < FixedUpdateProcesses[coindex.i].Current))
                    {
                        Profiler.BeginSample("Processing Coroutine");

                        try
                        {
                            if (!FixedUpdateProcesses[coindex.i].MoveNext())
                            {
                                FixedUpdateProcesses[coindex.i] = null;
                            }
                            else if (FixedUpdateProcesses[coindex.i] != null && float.IsNaN(FixedUpdateProcesses[coindex.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    FixedUpdateProcesses[coindex.i] = null;
                                }
                                else
                                {
                                    FixedUpdateProcesses[coindex.i] = ReplacementFunction(FixedUpdateProcesses[coindex.i],
                                        coindex.seg, _processTags.ContainsKey(coindex) ? _processTags[coindex] : null);

                                    ReplacementFunction = null;
                                    coindex.i--;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            FixedUpdateProcesses[coindex.i] = null;
                        }

                        Profiler.EndSample();
                    }
                }

                _runningFixedUpdate = false;
            }

            if (_exceptions.Count > 0)
                throw _exceptions.Dequeue();
        }

        private void LateUpdate()
        {
            if(_nextLateUpdateProcessSlot > 0)
            {
                ProcessIndex coindex = new ProcessIndex { seg = Segment.LateUpdate };
                _runningLateUpdate = true;
                UpdateTimeValues(coindex.seg);

                for (coindex.i = 0; coindex.i < _nextLateUpdateProcessSlot; coindex.i++)
                {
                    if (LateUpdateProcesses[coindex.i] != null && !(localTime < LateUpdateProcesses[coindex.i].Current))
                    {
                        Profiler.BeginSample("Processing Coroutine");

                        try
                        {
                            if (!LateUpdateProcesses[coindex.i].MoveNext())
                            {
                                LateUpdateProcesses[coindex.i] = null;
                            }
                            else if (LateUpdateProcesses[coindex.i] != null && float.IsNaN(LateUpdateProcesses[coindex.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    LateUpdateProcesses[coindex.i] = null;
                                }
                                else
                                {
                                    LateUpdateProcesses[coindex.i] = ReplacementFunction(LateUpdateProcesses[coindex.i],
                                        coindex.seg, _processTags.ContainsKey(coindex) ? _processTags[coindex] : null);

                                    ReplacementFunction = null;
                                    coindex.i--;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            LateUpdateProcesses[coindex.i] = null;
                        }

                        Profiler.EndSample();
                    }
                }

                _runningLateUpdate = false;
            }

            if (_exceptions.Count > 0)
                throw _exceptions.Dequeue();
        }

        private void UpdateTimeValues(Segment segment)
        {
            switch(segment)
            {
                case Segment.Update:
                    deltaTime = Time.deltaTime;
                    _lastUpdateTime += deltaTime;
                    localTime = _lastUpdateTime;
                    break;
                case Segment.LateUpdate:
                    deltaTime = Time.deltaTime;
                    _lastLateUpdateTime += deltaTime;
                    localTime = _lastLateUpdateTime;
                    break;
                case Segment.FixedUpdate:
                    deltaTime = Time.deltaTime;
                    _lastFixedUpdateTime += deltaTime;
                    localTime = _lastFixedUpdateTime;
                    break;
                case Segment.SlowUpdate:
                    if(_lastSlowUpdateTime == 0d)
                        deltaTime = TimeBetweenSlowUpdateCalls;
                    else
                        deltaTime = Time.realtimeSinceStartup - (float)_lastSlowUpdateTime;

                    localTime = _lastSlowUpdateTime = Time.realtimeSinceStartup;
                    break;
            }
        }

        private void SetTimeValues(Segment segment)
        {
            switch (segment)
            {
                case Segment.Update:
                    deltaTime = Time.deltaTime;
                    localTime = _lastUpdateTime;
                    break;
                case Segment.LateUpdate:
                    deltaTime = Time.deltaTime;
                    localTime = _lastLateUpdateTime;
                    break;
                case Segment.FixedUpdate:
                    deltaTime = Time.deltaTime;
                    localTime = _lastFixedUpdateTime;
                    break;
                case Segment.SlowUpdate:
                    deltaTime = Time.realtimeSinceStartup - (float)_lastSlowUpdateTime;
                    localTime = _lastSlowUpdateTime = Time.realtimeSinceStartup;
                    break;
            }
        }

        /// <summary>
        /// Resets the value of LocalTime to zero (only for the Update, LateUpdate, and FixedUpdate segments).
        /// </summary>
        public void ResetTimeCountOnInstance()
        {
            localTime = 0d;

            _lastUpdateTime = 0d;
            _lastLateUpdateTime = 0d;
            _lastFixedUpdateTime = 0d;
        }

        /// <summary>
        /// This will pause all coroutines running on the current MEC instance until ResumeAllCoroutines is called.
        /// </summary>
        public static void PauseCoroutines()
        {
            if(_instance != null)
                _instance.PauseCoroutinesOnInstance();
        }

        /// <summary>
        /// This will pause all coroutines running on this MEC instance until ResumeAllCoroutinesOnInstance is called.
        /// </summary>
        public void PauseCoroutinesOnInstance()
        {
            enabled = false;
        }

        /// <summary>
        /// This resumes all coroutines on the current MEC instance if they are currently paused, otherwise it has
        /// no effect.
        /// </summary>
        public static void ResumeCoroutines()
        {
            if(_instance != null)
                _instance.ResumeCoroutinesOnInstance();
        }

        /// <summary>
        /// This resumes all coroutines on this MEC instance if they are currently paused, otherwise it has no effect.
        /// </summary>
        public void ResumeCoroutinesOnInstance()
        {
            enabled = true;
        }

        private void RemoveUnused()
        {
            ProcessIndex outer, inner;
            outer.seg = inner.seg = Segment.Update;
            for (outer.i = inner.i = 0; outer.i < _nextUpdateProcessSlot; outer.i++)
            {
                if (UpdateProcesses[outer.i] != null)
                {
                    if(outer.i != inner.i)
                    {
                        UpdateProcesses[inner.i] = UpdateProcesses[outer.i];
                        MoveTag(outer, inner);
                    }
                    inner.i++;
                }
            }
            for(outer.i = inner.i;outer.i < _nextUpdateProcessSlot;outer.i++)
            {
                UpdateProcesses[outer.i] = null;
                RemoveTag(outer);
            }

            NumberOfUpdateCoroutines = _nextUpdateProcessSlot = inner.i;

            outer.seg = inner.seg = Segment.FixedUpdate;
            for (outer.i = inner.i = 0; outer.i < _nextFixedUpdateProcessSlot; outer.i++)
            {
                if(FixedUpdateProcesses[outer.i] != null)
                {
                    if(outer.i != inner.i)
                    {
                        FixedUpdateProcesses[inner.i] = FixedUpdateProcesses[outer.i];
                        MoveTag(outer, inner);
                    }
                    inner.i++;
                }
            }
            for(outer.i = inner.i;outer.i < _nextFixedUpdateProcessSlot;outer.i++)
            {
                FixedUpdateProcesses[outer.i] = null;
                RemoveTag(outer);
            }

            NumberOfFixedUpdateCoroutines = _nextFixedUpdateProcessSlot = inner.i;

            outer.seg = inner.seg = Segment.LateUpdate;
            for (outer.i = inner.i = 0; outer.i < _nextLateUpdateProcessSlot; outer.i++)
            {
                if(LateUpdateProcesses[outer.i] != null)
                {
                    if(outer.i != inner.i)
                    {
                        LateUpdateProcesses[inner.i] = LateUpdateProcesses[outer.i];
                        MoveTag(outer, inner);
                    }
                    inner.i++;
                }
            }
            for(outer.i = inner.i;outer.i < _nextLateUpdateProcessSlot;outer.i++)
            {
                LateUpdateProcesses[outer.i] = null;
                RemoveTag(outer);
            }

            NumberOfLateUpdateCoroutines = _nextLateUpdateProcessSlot = inner.i;

            outer.seg = inner.seg = Segment.SlowUpdate;
            for (outer.i = inner.i = 0; outer.i < _nextSlowUpdateProcessSlot; outer.i++)
            {
                if (SlowUpdateProcesses[outer.i] != null)
                {
                    if (outer.i != inner.i)
                    {
                        SlowUpdateProcesses[inner.i] = SlowUpdateProcesses[outer.i];
                        MoveTag(outer, inner);
                    }
                    inner.i++;
                }
            }
            for (outer.i = inner.i; outer.i < _nextSlowUpdateProcessSlot; outer.i++)
            { 
                SlowUpdateProcesses[outer.i] = null;
                RemoveTag(outer);
            }

            NumberOfSlowUpdateCoroutines = _nextSlowUpdateProcessSlot = inner.i;
        }

        private void AddTag(string tag, ProcessIndex coindex)
        {
            _processTags.Add(coindex, tag);

            if (_taggedProcesses.ContainsKey(tag))
                _taggedProcesses[tag].Add(coindex);
            else
                _taggedProcesses.Add(tag, new HashSet<ProcessIndex> { coindex });
        }

        private string RemoveTag(ProcessIndex coindex)
        {
            if (_processTags.ContainsKey(coindex))
            {
                string tag = _processTags[coindex];

                if (_taggedProcesses[tag].Count > 1)
                    _taggedProcesses[tag].Remove(coindex);
                else
                    _taggedProcesses.Remove(tag);

                _processTags.Remove(coindex);

                return tag;
            }

            return null;
        }

        private void MoveTag(ProcessIndex coindexFrom, ProcessIndex coindexTo)
        {
            RemoveTag(coindexTo);

            if (_processTags.ContainsKey(coindexFrom))
            {
                _taggedProcesses[_processTags[coindexFrom]].Remove(coindexFrom);
                _taggedProcesses[_processTags[coindexFrom]].Add(coindexTo);

                _processTags.Add(coindexTo, _processTags[coindexFrom]);
                _processTags.Remove(coindexFrom);
            }
        }

        /// <summary>
        /// Run a new coroutine in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static IEnumerator<float> RunCoroutine(IEnumerator<float> coroutine)
        {
            return coroutine == null ? null : Instance.RunCoroutineOnInstance(coroutine, Segment.Update, null);
        }

        /// <summary>
        /// Run a new coroutine in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static IEnumerator<float> RunCoroutine(IEnumerator<float> coroutine, string tag)
        {
            return coroutine == null ? null : Instance.RunCoroutineOnInstance(coroutine, Segment.Update, tag);
        }

        /// <summary>
        /// Run a new coroutine.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="timing">The segment that the coroutine should run in.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static IEnumerator<float> RunCoroutine(IEnumerator<float> coroutine, Segment timing)
        {
            return coroutine == null ? null : Instance.RunCoroutineOnInstance(coroutine, timing);
        }

        /// <summary>
        /// Run a new coroutine.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="timing">The segment that the coroutine should run in.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public static IEnumerator<float> RunCoroutine(IEnumerator<float> coroutine, Segment timing, string tag)
        {
            return coroutine == null ? null : Instance.RunCoroutineOnInstance(coroutine, timing, tag);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public IEnumerator<float> RunCoroutineOnInstance(IEnumerator<float> coroutine) 
        {
            return coroutine == null ? null : RunCoroutineOnInstance(coroutine, Segment.Update, null);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance in the Update segment.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public IEnumerator<float> RunCoroutineOnInstance(IEnumerator<float> coroutine, string tag)
        {
            return coroutine == null ? null : RunCoroutineOnInstance(coroutine, Segment.Update, tag);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="timing">The segment that the coroutine should run in.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public IEnumerator<float> RunCoroutineOnInstance(IEnumerator<float> coroutine, Segment timing)
        {
            return coroutine == null ? null : RunCoroutineOnInstance(coroutine, timing, null);
        }

        /// <summary>
        /// Run a new coroutine on this Timing instance.
        /// </summary>
        /// <param name="coroutine">The new coroutine's handle.</param>
        /// <param name="timing">The segment that the coroutine should run in.</param>
        /// <param name="tag">An optional tag to attach to the coroutine which can later be used for Kill operations.</param>
        /// <returns>The coroutine's handle, which can be used for Wait and Kill operations.</returns>
        public IEnumerator<float> RunCoroutineOnInstance(IEnumerator<float> coroutine, Segment timing, string tag)
        {
            if(coroutine == null) 
                return null;

            ProcessIndex slot = new ProcessIndex {seg = timing};
            switch(timing)
            {
                case Segment.Update:

                    if(_nextUpdateProcessSlot >= UpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldArray = UpdateProcesses;
                        UpdateProcesses = new IEnumerator<float>[UpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        for(int i = 0;i < oldArray.Length;i++)
                            UpdateProcesses[i] = oldArray[i];
                    }

                    slot.i = _nextUpdateProcessSlot++;
                    UpdateProcesses[slot.i] = coroutine;

                    if(tag != null)
                        AddTag(tag, slot);

                    if(!_runningUpdate)
                    {
                        try
                        {
                            _runningUpdate = true;
                            SetTimeValues(slot.seg);

                            if(!UpdateProcesses[slot.i].MoveNext())
                            {
                                UpdateProcesses[slot.i] = null;
                            }
                            else if (UpdateProcesses[slot.i] != null && float.IsNaN(UpdateProcesses[slot.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    UpdateProcesses[slot.i] = null;
                                }
                                else
                                {
                                    UpdateProcesses[slot.i] = ReplacementFunction(UpdateProcesses[slot.i], timing,
                                        _processTags.ContainsKey(slot) ? _processTags[slot] : null);

                                    ReplacementFunction = null;

                                    if (UpdateProcesses[slot.i] != null)
                                        UpdateProcesses[slot.i].MoveNext();
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            UpdateProcesses[slot.i] = null;
                        }
                        finally
                        {
                            _runningUpdate = false;
                        }
                    }

                    return coroutine;

                case Segment.FixedUpdate:

                    if(_nextFixedUpdateProcessSlot >= FixedUpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldArray = FixedUpdateProcesses;
                        FixedUpdateProcesses = new IEnumerator<float>[FixedUpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        for(int i = 0;i < oldArray.Length;i++)
                            FixedUpdateProcesses[i] = oldArray[i];
                    }

                    slot.i = _nextFixedUpdateProcessSlot++;
                    FixedUpdateProcesses[slot.i] = coroutine;

                    if (tag != null)
                        AddTag(tag, slot);

                    if(!_runningFixedUpdate)
                    {
                        try
                        {
                            _runningFixedUpdate = true;
                            SetTimeValues(slot.seg);

                            if(!FixedUpdateProcesses[slot.i].MoveNext())
                            {
                                FixedUpdateProcesses[slot.i] = null;
                            }
                            else if (FixedUpdateProcesses[slot.i] != null && float.IsNaN(FixedUpdateProcesses[slot.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    FixedUpdateProcesses[slot.i] = null;
                                }
                                else
                                {
                                    FixedUpdateProcesses[slot.i] = ReplacementFunction(FixedUpdateProcesses[slot.i], timing,
                                        _processTags.ContainsKey(slot) ? _processTags[slot] : null);

                                    ReplacementFunction = null;

                                    if (FixedUpdateProcesses[slot.i] != null)
                                        FixedUpdateProcesses[slot.i].MoveNext();
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            FixedUpdateProcesses[slot.i] = null;
                        }
                        finally
                        {
                            _runningFixedUpdate = false;
                        }
                    }

                    return coroutine;

                case Segment.LateUpdate:

                    if(_nextLateUpdateProcessSlot >= LateUpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldArray = LateUpdateProcesses;
                        LateUpdateProcesses = new IEnumerator<float>[LateUpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        for(int i = 0;i < oldArray.Length;i++)
                            LateUpdateProcesses[i] = oldArray[i];
                    }

                    slot.i = _nextLateUpdateProcessSlot++;
                    LateUpdateProcesses[slot.i] = coroutine;

                    if(tag != null)
                        AddTag(tag, slot);

                    if(!_runningLateUpdate)
                    {
                        try
                        {
                            _runningLateUpdate = true;
                            SetTimeValues(slot.seg);

                            if(!LateUpdateProcesses[slot.i].MoveNext())
                            {
                                LateUpdateProcesses[slot.i] = null;
                            }
                            else if (LateUpdateProcesses[slot.i] != null && float.IsNaN(LateUpdateProcesses[slot.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    LateUpdateProcesses[slot.i] = null;
                                }
                                else
                                {
                                    LateUpdateProcesses[slot.i] = ReplacementFunction(LateUpdateProcesses[slot.i], timing,
                                        _processTags.ContainsKey(slot) ? _processTags[slot] : null);

                                    ReplacementFunction = null;

                                    if (LateUpdateProcesses[slot.i] != null)
                                        LateUpdateProcesses[slot.i].MoveNext();
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            LateUpdateProcesses[slot.i] = null;
                        }
                        finally
                        {
                            _runningLateUpdate = false;
                        }
                    }

                    return coroutine;

                case Segment.SlowUpdate:

                    if(_nextSlowUpdateProcessSlot >= SlowUpdateProcesses.Length)
                    {
                        IEnumerator<float>[] oldArray = SlowUpdateProcesses;
                        SlowUpdateProcesses = new IEnumerator<float>[SlowUpdateProcesses.Length + (ProcessArrayChunkSize * _expansions++)];
                        for(int i = 0;i < oldArray.Length;i++)
                            SlowUpdateProcesses[i] = oldArray[i];
                    }

                    slot.i = _nextSlowUpdateProcessSlot++;
                    SlowUpdateProcesses[slot.i] = coroutine;

                    if(tag != null)
                        AddTag(tag, slot);

                    if(!_runningSlowUpdate)
                    {
                        try
                        {
                            _runningSlowUpdate = true;
                            SetTimeValues(slot.seg);

                            if(!SlowUpdateProcesses[slot.i].MoveNext())
                            {
                                SlowUpdateProcesses[slot.i] = null;
                            }
                            else if (SlowUpdateProcesses[slot.i] != null && float.IsNaN(SlowUpdateProcesses[slot.i].Current))
                            {
                                if(ReplacementFunction == null)
                                {
                                    SlowUpdateProcesses[slot.i] = null;
                                }
                                else
                                {
                                    SlowUpdateProcesses[slot.i] = ReplacementFunction(SlowUpdateProcesses[slot.i], timing,
                                        _processTags.ContainsKey(slot) ? _processTags[slot] : null);

                                    ReplacementFunction = null;

                                    if (SlowUpdateProcesses[slot.i] != null)
                                        SlowUpdateProcesses[slot.i].MoveNext();
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            if (OnError == null)
                                _exceptions.Enqueue(ex);
                            else
                                OnError(ex);

                            SlowUpdateProcesses[slot.i] = null;
                        }
                        finally
                        {
                            _runningSlowUpdate = false;
                        }
                    }

                    return coroutine;

                default:
                    return null;
            }
        }

        private void CoindexKill(ProcessIndex coindex)
        {
            switch(coindex.seg)
            {
                case Segment.Update:
                    UpdateProcesses[coindex.i] = null;
                    return;
                case Segment.FixedUpdate:
                    FixedUpdateProcesses[coindex.i] = null;
                    return;
                case Segment.LateUpdate:
                    LateUpdateProcesses[coindex.i] = null;
                    return;
                case Segment.SlowUpdate:
                    SlowUpdateProcesses[coindex.i] = null;
                    return;
            }
        }

        private bool CoindexMatches(ProcessIndex coindex, IEnumerator<float> handle)
        {
            switch (coindex.seg)
            {
                case Segment.Update:
                    return UpdateProcesses[coindex.i] == handle;
                case Segment.FixedUpdate:
                    return FixedUpdateProcesses[coindex.i] == handle;
                case Segment.LateUpdate:
                    return LateUpdateProcesses[coindex.i] == handle;
                case Segment.SlowUpdate:
                    return SlowUpdateProcesses[coindex.i] == handle;
                default:
                    return false;
            }
        }

        /// <summary>
        /// This will kill all coroutines running on the main MEC instance.
        /// </summary>
        /// <returns>The number of coroutines that were killed.</returns>
        public static void KillAllCoroutines()
        {
            if(_instance != null)
                _instance.KillAllCoroutinesOnInstance();
        }

        /// <summary>
        /// This will kill all coroutines running on the current MEC instance.
        /// </summary>
        /// <returns>The number of coroutines that were killed.</returns>
        public void KillAllCoroutinesOnInstance()
        {
            UpdateProcesses = new IEnumerator<float>[InitialBufferSizeLarge];
            NumberOfUpdateCoroutines = 0;
            _nextUpdateProcessSlot = 0;

            LateUpdateProcesses = new IEnumerator<float>[InitialBufferSizeSmall];
            NumberOfLateUpdateCoroutines = 0;
            _nextLateUpdateProcessSlot = 0;

            FixedUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];
            NumberOfFixedUpdateCoroutines = 0;
            _nextFixedUpdateProcessSlot = 0;

            SlowUpdateProcesses = new IEnumerator<float>[InitialBufferSizeMedium];
            NumberOfSlowUpdateCoroutines = 0;
            _nextSlowUpdateProcessSlot = 0;

            _processTags.Clear();
            _taggedProcesses.Clear();
            _waitingProcesses.Clear();
            _exceptions.Clear();
            _expansions = 1;

            ResetTimeCountOnInstance();
        }

        /// <summary>
        /// Kills all instances of the coroutine handle on the main Timing instance.
        /// </summary>
        /// <param name="coroutine">The handle of the coroutine to kill.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public static int KillCoroutines(IEnumerator<float> coroutine)
        {
            return _instance == null ? 0 : _instance.KillCoroutinesOnInstance(coroutine);
        }

        /// <summary>
        /// Kills all instances of the coroutine handle on this Timing instance.
        /// </summary>
        /// <param name="coroutine">The handle of the coroutine to kill.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public int KillCoroutinesOnInstance(IEnumerator<float> coroutine)
        {
            int numberFound = 0;

            for (int i = 0; i < _nextUpdateProcessSlot; i++)
            {
                if (UpdateProcesses[i] == coroutine)
                {
                    UpdateProcesses[i] = null;
                    numberFound++;
                }
            }

            for (int i = 0; i < _nextFixedUpdateProcessSlot; i++)
            {
                if (FixedUpdateProcesses[i] == coroutine)
                {
                    FixedUpdateProcesses[i] = null;
                    numberFound++;
                }
            }

            for (int i = 0; i < _nextLateUpdateProcessSlot; i++)
            {
                if (LateUpdateProcesses[i] == coroutine)
                {
                    LateUpdateProcesses[i] = null;
                    numberFound++;
                }
            }

            for (int i = 0; i < _nextSlowUpdateProcessSlot; i++)
            {
                if (SlowUpdateProcesses[i] == coroutine)
                {
                    SlowUpdateProcesses[i] = null;
                    numberFound++;
                }
            }

            for(int i = 0;i < _waitingProcesses.Count;i++)
            {
                if(_waitingProcesses[i].Trigger == coroutine && !_waitingProcesses[i].Killed && !_waitingProcesses[i].Killed)
                {
                    _waitingProcesses[i].Killed = true;
                    numberFound++;
                }

                for (int j = 0; j < _waitingProcesses[i].Tasks.Count; j++)
                {
                    if(_waitingProcesses[i].Tasks[j].Task == coroutine && _waitingProcesses[i].Tasks[j].Task != null)
                    {
                        _waitingProcesses[i].Tasks[j].Task = null;
                        numberFound++;
                    }
                }
            }

            return numberFound;
        }

        /// <summary>
        /// Kills all coroutines that have the given tag.
        /// </summary>
        /// <param name="tag">All coroutines with this tag will be killed.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public static int KillCoroutines(string tag)
        {
            return _instance == null ? 0 : _instance.KillCoroutinesOnInstance(tag);
        }

        /// <summary>
        /// Kills all coroutines that have the given tag.
        /// </summary>
        /// <param name="tag">All coroutines with this tag will be killed.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public int KillCoroutinesOnInstance(string tag)
        {
            int numberFound = 0;

            if (_taggedProcesses.ContainsKey(tag))
            {
                foreach(ProcessIndex coindex in _taggedProcesses[tag])
                {
                    CoindexKill(coindex);
                    _processTags.Remove(coindex);
                    numberFound++;
                }
                _taggedProcesses.Remove(tag);
            }

            for (int i = 0; i < _waitingProcesses.Count; i++)
            {
                if(_waitingProcesses[i].TriggerTag == tag && !_waitingProcesses[i].Killed && !_waitingProcesses[i].Killed)
                {
                    _waitingProcesses[i].Killed = true;
                    numberFound++;
                }

                for (int j = 0; j < _waitingProcesses[i].Tasks.Count; j++)
                {
                    if(_waitingProcesses[i].Tasks[j].Tag == tag && _waitingProcesses[i].Tasks[j].Task != null)
                    {
                        _waitingProcesses[i].Tasks[j].Task = null;
                        numberFound++;
                    }
                }
            }

            return numberFound;
        }

        /// <summary>
        /// Kills all instances that match both the coroutine handle and the tag on the main Timing instance.
        /// </summary>
        /// <param name="coroutine">The handle of the coroutine to kill.</param>
        /// <param name="tag">The tag to also match for.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public static int KillAllCoroutines(IEnumerator<float> coroutine, string tag)
        {
            return _instance == null ? 0 : _instance.KillAllCoroutinesOnInstance(coroutine, tag);
        }

        /// <summary>
        /// Kills all instances that match both the coroutine handle and the tag on this Timing instance.
        /// </summary>
        /// <param name="coroutine">The handle of the coroutine to kill.</param>
        /// <param name="tag">The tag to also match for.</param>
        /// <returns>The number of coroutines that were found and killed.</returns>
        public int KillAllCoroutinesOnInstance(IEnumerator<float> coroutine, string tag)
        {
            int numberFound = 0;

            if (_taggedProcesses.ContainsKey(tag))
            {
                foreach (ProcessIndex coindex in _taggedProcesses[tag])
                {
                    if (CoindexMatches(coindex, coroutine))
                    {
                        CoindexKill(coindex);
                        _processTags.Remove(coindex);
                        numberFound++;
                    }
                }
                if (numberFound == _taggedProcesses[tag].Count)
                    _taggedProcesses.Remove(tag);
            }


            for (int i = 0; i < _waitingProcesses.Count; i++)
            {
                if(_waitingProcesses[i].Trigger == coroutine && _waitingProcesses[i].TriggerTag == tag && !_waitingProcesses[i].Killed &&
                   !_waitingProcesses[i].Killed)
                {
                    _waitingProcesses[i].Killed = true;
                    numberFound++;
                }

                for (int j = 0; j < _waitingProcesses[i].Tasks.Count; j++)
                {
                    if(_waitingProcesses[i].Tasks[j].Task == coroutine && _waitingProcesses[i].Tasks[j].Tag == tag &&
                       _waitingProcesses[i].Tasks[j].Task != null)
                    {
                        _waitingProcesses[i].Tasks[j].Task = null;
                        numberFound++;
                    }
                }
            }

            return numberFound;
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(otherCoroutine);" to pause the current 
        /// coroutine until otherCoroutine is done.
        /// </summary>
        /// <param name="otherCoroutine">The coroutine to pause for.</param>
        public static float WaitUntilDone(IEnumerator<float> otherCoroutine)
        {
            return WaitUntilDone(otherCoroutine, true, Instance);
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(otherCoroutine);" to pause the current 
        /// coroutine until otherCoroutine is done.
        /// </summary>
        /// <param name="otherCoroutine">The coroutine to pause for.</param>
        /// <param name="warnOnIssue">Post a warning to the console if no hold action was actually performed.</param>
        public static float WaitUntilDone(IEnumerator<float> otherCoroutine, bool warnOnIssue)
        {
            return WaitUntilDone(otherCoroutine, warnOnIssue, Instance);
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(otherCoroutine);" to pause the current 
        /// coroutine until the otherCoroutine is done.
        /// </summary>
        /// <param name="otherCoroutine">The coroutine to pause for.</param>
        /// <param name="warnOnIssue">Post a warning to the console if no hold action was actually performed.</param>
        /// <param name="instance">The instance that the otherCoroutine is attached to. Only use this if you are using 
        /// multiple instances of the Timing object.</param>
        public static float WaitUntilDone(IEnumerator<float> otherCoroutine, bool warnOnIssue, Timing instance)
        {
            if(instance == null || !instance.gameObject)
                throw new System.ArgumentNullException();

            if(otherCoroutine == null)
            {
                if (warnOnIssue)
                    throw new System.ArgumentNullException();

                return -1f;
            }

            for(int i = 0;i < instance._waitingProcesses.Count;i++)
            {
                if(instance._waitingProcesses[i].Trigger == otherCoroutine)
                {
                    WaitingProcess proc = instance._waitingProcesses[i];
                    ReplacementFunction = (input, segment, tag) =>
                    {
                        proc.Tasks.Add(new WaitingProcess.ProcessData
                        { 
                            Task = input,
                            Tag = tag,
                            Segment = segment
                        });

                        return null;
                    };

                    return float.NaN;
                }

                for(int j = 0;j < instance._waitingProcesses[i].Tasks.Count;j++)
                {
                    if(instance._waitingProcesses[i].Tasks[j].Task == otherCoroutine)
                    {
                        WaitingProcess proc = new WaitingProcess { Trigger = otherCoroutine };

                        instance._waitingProcesses[i].Tasks[j].Task = instance._StartWhenDone(proc);

                        ReplacementFunction = (input, segment, tag) =>
                        {
                            proc.Tasks.Add(new WaitingProcess.ProcessData
                            {
                                Task = input,
                                Tag = tag,
                                Segment = segment
                            });

                            instance._waitingProcesses.Add(proc);

                            return null;
                        };

                        return float.NaN;
                    }
                }
            }

            WaitingProcess newProcess = new WaitingProcess { Trigger = otherCoroutine };

            if(instance.ReplaceCoroutine(otherCoroutine, instance._StartWhenDone(newProcess), out newProcess.TriggerTag))
            {
                ReplacementFunction = (input, segment, tag) =>
                {
                    newProcess.Tasks.Add(new WaitingProcess.ProcessData
                    {
                        Task = input,
                        Tag = tag,
                        Segment = segment
                    });

                    instance._waitingProcesses.Add(newProcess);

                    return null;
                };

                return float.NaN;
            }

            if (warnOnIssue)
                Debug.LogWarning("WaitUntilDone cannot hold: The coroutine instance that was passed in was not found.\n" + otherCoroutine);

            return -1f;
        }

        private IEnumerator<float> _StartWhenDone(WaitingProcess processData)
        {
            try
            {
                if (processData.Killed)
                {
                    CloseWaitingProcess(processData);
                    yield break;
                }

                if (processData.Trigger.Current > localTime)
                {
                    yield return processData.Trigger.Current;

                    if (processData.Killed)
                    {
                        CloseWaitingProcess(processData);
                        yield break;
                    }
                }

                while (processData.Trigger.MoveNext())
                {
                    yield return processData.Trigger.Current;

                    if (processData.Killed)
                    {
                        CloseWaitingProcess(processData);
                        yield break;
                    }
                }
            }
            finally
            {
                CloseWaitingProcess(processData);
            }
        }

        private void CloseWaitingProcess(WaitingProcess processData)
        {
            if (_waitingProcesses.Contains(processData))
            {
                _waitingProcesses.Remove(processData);

                foreach (WaitingProcess.ProcessData taskData in processData.Tasks)
                    RunCoroutineOnInstance(taskData.Task, taskData.Segment, taskData.Tag);
            }
        }

        private bool ReplaceCoroutine(IEnumerator<float> coroutine, IEnumerator<float> replacement, out string tagFound)
        {
            ProcessIndex coindex;
            for (coindex.i = 0; coindex.i < _nextUpdateProcessSlot; coindex.i++)
            {
                if (UpdateProcesses[coindex.i] == coroutine)
                {
                    coindex.seg = Segment.Update;
                    UpdateProcesses[coindex.i] = replacement;
                    tagFound = RemoveTag(coindex);

                    return true;
                }
            }

            for (coindex.i = 0; coindex.i < _nextFixedUpdateProcessSlot; coindex.i++)
            {
                if (FixedUpdateProcesses[coindex.i] == coroutine)
                {
                    coindex.seg = Segment.FixedUpdate;
                    FixedUpdateProcesses[coindex.i] = replacement;
                    tagFound = RemoveTag(coindex);

                    return true;
                }
            }

            for (coindex.i = 0; coindex.i < _nextLateUpdateProcessSlot; coindex.i++)
            {
                if (LateUpdateProcesses[coindex.i] == coroutine)
                {
                    coindex.seg = Segment.LateUpdate;
                    LateUpdateProcesses[coindex.i] = replacement;
                    tagFound = RemoveTag(coindex);

                    return true;
                }
            }

            for (coindex.i = 0; coindex.i < _nextSlowUpdateProcessSlot; coindex.i++)
            {
                if (SlowUpdateProcesses[coindex.i] == coroutine)
                {
                    coindex.seg = Segment.SlowUpdate;
                    SlowUpdateProcesses[coindex.i] = replacement;
                    tagFound = RemoveTag(coindex);

                    return true;
                }
            }

            tagFound = null;
            return false;
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(wwwObject);" to pause the current 
        /// coroutine until the wwwObject is done.
        /// </summary>
        /// <param name="wwwObject">The www object to pause for.</param>
        public static float WaitUntilDone(WWW wwwObject)
        {
            ReplacementFunction = (input, timing, tag) => _StartWhenDone(wwwObject, input);
            return float.NaN;
        }

        private static IEnumerator<float> _StartWhenDone(WWW www, IEnumerator<float> pausedProc)
        {
            while (!www.isDone)
                yield return 0f;

            ReplacementFunction = delegate { return pausedProc; };
            yield return float.NaN;
        }

        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(operation);" to pause the current 
        /// coroutine until the operation is done.
        /// </summary>
        /// <param name="operation">The operation variable returned.</param>
        public static float WaitUntilDone(AsyncOperation operation)
        {
            if (operation == null || operation.isDone) return 0f;
            ReplacementFunction = (input, timing, tag) => _StartWhenDone(operation, input);
            return float.NaN;
        }

        private static IEnumerator<float> _StartWhenDone(AsyncOperation operation, IEnumerator<float> pausedProc)
        {
            while (!operation.isDone)
                yield return 0f;

            ReplacementFunction = delegate { return pausedProc; };
            yield return float.NaN;
        }

#if !UNITY_4_6 && !UNITY_4_7 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
        /// <summary>
        /// Use the command "yield return Timing.WaitUntilDone(operation);" to pause the current 
        /// coroutine until the operation is done.
        /// </summary>
        /// <param name="operation">The operation variable returned.</param>
        public static float WaitUntilDone(CustomYieldInstruction operation)
        {
            if (operation == null || !operation.keepWaiting) return 0f;
            ReplacementFunction = (input, timing, tag) => _StartWhenDone(operation, input);
            return float.NaN;
        }

        private static IEnumerator<float> _StartWhenDone(CustomYieldInstruction operation, IEnumerator<float> pausedProc)
        {
            while (operation.keepWaiting)
                yield return 0f;

            ReplacementFunction = delegate { return pausedProc; };
            yield return float.NaN;
        }
#endif

        /// <summary>
        /// Use in a yield return statement to wait for the specified number of seconds.
        /// </summary>
        /// <param name="waitTime">Number of seconds to wait.</param>
        public static float WaitForSeconds(float waitTime)
        {
            if (float.IsNaN(waitTime)) waitTime = 0f;
            return (float)LocalTime + waitTime;
        }

        /// <summary>
        /// Use in a yield return statement to wait for the specified number of seconds.
        /// </summary>
        /// <param name="waitTime">Number of seconds to wait.</param>
        public float WaitForSecondsOnInstance(float waitTime)
        {
            if (float.IsNaN(waitTime)) waitTime = 0f;
            return (float)localTime + waitTime;
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action.</param>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        public static void CallDelayed<TRef>(TRef reference, float delay, System.Action<TRef> action)
        {
            if (action == null) return;

            if (delay >= -0.001f)
                RunCoroutine(Instance._CallDelayBack(reference, delay, action));
            else
                action(reference);
        }
        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action.</param>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        public void CallDelayedOnInstance<TRef>(TRef reference, float delay, System.Action<TRef> action)
        {
            if(action == null) return;

            if (delay >= -0.001f)
                RunCoroutineOnInstance(_CallDelayBack(reference, delay, action));
            else
                action(reference);
        }

        private IEnumerator<float> _CallDelayBack<TRef>(TRef reference, float delay, System.Action<TRef> action)
        {
            yield return (float)localTime + delay;

            CallDelayed(reference, -1f, action);
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        public static void CallDelayed(float delay, System.Action action)
        {
            if (action == null) return;

            if (delay >= -0.0001f)
                RunCoroutine(Instance._CallDelayBack(delay, action));
            else
                action();
        }

        /// <summary>
        /// Calls the specified action after a specified number of seconds.
        /// </summary>
        /// <param name="delay">The number of seconds to wait before calling the action.</param>
        /// <param name="action">The action to call.</param>
        public void CallDelayedOnInstance(float delay, System.Action action)
        {
            if (action == null) return;

            if (delay >= -0.0001f)
                RunCoroutineOnInstance(_CallDelayBack(delay, action));
            else
                action();
        }

        private IEnumerator<float> _CallDelayBack(float delay, System.Action action)
        {
            yield return (float)localTime + delay;

            CallDelayed(-1f, action);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallPeriodically(float timeframe, float period, System.Action action, System.Action onDone = null)
        {
            if (action != null)
                RunCoroutine(Instance._CallContinuously(timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallPeriodicallyOnInstance(float timeframe, float period, System.Action action, System.Action onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallPeriodically(float timeframe, float period, System.Action action, Segment timing, System.Action onDone = null)
        {
            if(action != null)
                RunCoroutine(Instance._CallContinuously(timeframe, period, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallPeriodicallyOnInstance(float timeframe, float period, System.Action action, Segment timing, System.Action onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(timeframe, period, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallContinuously(float timeframe, System.Action action, System.Action onDone = null)
        {
            if (action != null)
                RunCoroutine(Instance._CallContinuously(timeframe, 0f, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallContinuouslyOnInstance(float timeframe, System.Action action, System.Action onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(timeframe, 0f, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallContinuously(float timeframe, System.Action action, Segment timing, System.Action onDone = null)
        {
            if(action != null)
                RunCoroutine(Instance._CallContinuously(timeframe, 0f, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallContinuouslyOnInstance(float timeframe, System.Action action, Segment timing, System.Action onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(timeframe, 0f, action, onDone), timing);
        }

        private IEnumerator<float> _CallContinuously(float timeframe, float period, System.Action action, System.Action onDone)
        {
            double startTime = localTime;
            while (localTime <= startTime + timeframe)
            {
                yield return WaitForSeconds(period);

                action();
            }

            if (onDone != null)
                onDone();
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallPeriodically<T>(T reference, float timeframe, float period, System.Action<T> action, System.Action<T> onDone = null)
        {
            if (action != null)
                RunCoroutine(Instance._CallContinuously(reference, timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallPeriodicallyOnInstance<T>(T reference, float timeframe, float period, System.Action<T> action, System.Action<T> onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, period, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallPeriodically<T>(T reference, float timeframe, float period, System.Action<T> action, 
            Segment timing, System.Action<T> onDone = null)
        {
            if(action != null)
                RunCoroutine(Instance._CallContinuously(reference, timeframe, period, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action at the given rate for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each period.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="period">The amount of time between calls.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallPeriodicallyOnInstance<T>(T reference, float timeframe, float period, System.Action<T> action,
            Segment timing, System.Action<T> onDone = null)
        {
            if(action != null)
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, period, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallContinuously<T>(T reference, float timeframe, System.Action<T> action, System.Action<T> onDone = null)
        {
            if(action != null)
                RunCoroutine(Instance._CallContinuously(reference, 0f, timeframe, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallContinuouslyOnInstance<T>(T reference, float timeframe, System.Action<T> action, System.Action<T> onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(reference, 0f, timeframe, action, onDone), Segment.Update);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public static void CallContinuously<T>(T reference, float timeframe, System.Action<T> action, 
            Segment timing, System.Action<T> onDone = null)
        {
            if(action != null)
                RunCoroutine(Instance._CallContinuously(reference, timeframe, 0f, action, onDone), timing);
        }

        /// <summary>
        /// Calls the supplied action every frame for a given number of seconds.
        /// </summary>
        /// <param name="reference">A value that will be passed in to the supplied action each frame.</param>
        /// <param name="timeframe">The number of seconds that this function should run.</param>
        /// <param name="action">The action to call every frame.</param>
        /// <param name="timing">The timing segment to run in.</param>
        /// <param name="onDone">An optional action to call when this function finishes.</param>
        public void CallContinuouslyOnInstance<T>(T reference, float timeframe, System.Action<T> action,
            Segment timing, System.Action<T> onDone = null)
        {
            if (action != null)
                RunCoroutineOnInstance(_CallContinuously(reference, timeframe, 0f, action, onDone), timing);
        }

        private IEnumerator<float> _CallContinuously<T>(T reference, float timeframe, float period,
            System.Action<T> action, System.Action<T> onDone = null)
        {
            double startTime = localTime;
            while (localTime <= startTime + timeframe)
            {
                yield return WaitForSeconds(period);

                action(reference);
            }

            if (onDone != null)
                onDone(reference);
        }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine(System.Collections.IEnumerator routine) { return null; }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine(string methodName, object value) { return null; }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine(string methodName) { return null; }

        [System.Obsolete("Unity coroutine function, use RunCoroutine instead.", true)]
        public new Coroutine StartCoroutine_Auto(System.Collections.IEnumerator routine) { return null; }

        [System.Obsolete("Unity coroutine function, use KillCoroutine instead.", true)]
        public new void StopCoroutine(string methodName) { }

        [System.Obsolete("Unity coroutine function, use KillCoroutine instead.", true)]
        public new void StopCoroutine(System.Collections.IEnumerator routine) { }

        [System.Obsolete("Unity coroutine function, use KillCoroutine instead.", true)]
        public new void StopCoroutine(Coroutine routine) { }

        [System.Obsolete("Unity coroutine function, use KillAllCoroutines instead.", true)]
        public new void StopAllCoroutines() { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Destroy(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void Destroy(Object obj, float f) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyObject(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyObject(Object obj, float f) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyImmediate(Object obj) { }

        [System.Obsolete("Use your own GameObject for this.", true)]
        public new static void DestroyImmediate(Object obj, bool b) { }

        [System.Obsolete("Just.. no.", true)]
        public new static T FindObjectOfType<T>() where T : Object { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static Object FindObjectOfType(System.Type t) { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static T[] FindObjectsOfType<T>() where T : Object { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static Object[] FindObjectsOfType(System.Type t) { return null; }

        [System.Obsolete("Just.. no.", true)]
        public new static void print(object message) { }
    }

    public enum Segment
    {
        Update,
        FixedUpdate,
        LateUpdate,
        SlowUpdate,
    }

    public static class MECExtensionMethods 
    {
        /// <summary>
        /// Cancels this coroutine when the supplied game object is destroyed or made inactive.
        /// </summary>
        /// <param name="coroutine">The coroutine handle to act upon.</param>
        /// <param name="gameObject">The GameObject to test.</param>
        /// <returns>The modified coroutine handle.</returns>
        public static IEnumerator<float> CancelWith(this IEnumerator<float> coroutine, GameObject gameObject)
        {
            while (gameObject && gameObject.activeInHierarchy && coroutine.MoveNext())
                yield return coroutine.Current;
        }

        /// <summary>
        /// Cancels this coroutine when the supplied game objects are destroyed or made inactive.
        /// </summary>
        /// <param name="coroutine">The coroutine handle to act upon.</param>
        /// <param name="gameObject1">The first GameObject to test.</param>
        /// <param name="gameObject2">The second GameObject to test</param>
        /// <returns>The modified coroutine handle.</returns>
        public static IEnumerator<float> CancelWith(this IEnumerator<float> coroutine, GameObject gameObject1, GameObject gameObject2)
        {
            while (gameObject1 && gameObject1.activeInHierarchy && gameObject2 && gameObject2.activeInHierarchy && coroutine.MoveNext())
                yield return coroutine.Current;
        }

        /// <summary>
        /// Cancels this coroutine when the supplied game objects are destroyed or made inactive.
        /// </summary>
        /// <param name="coroutine">The coroutine handle to act upon.</param>
        /// <param name="gameObject1">The first GameObject to test.</param>
        /// <param name="gameObject2">The second GameObject to test</param>
        /// <param name="gameObject3">The third GameObject to test.</param>
        /// <returns>The modified coroutine handle.</returns>
        public static IEnumerator<float> CancelWith(this IEnumerator<float> coroutine,
            GameObject gameObject1, GameObject gameObject2, GameObject gameObject3)
        {
            while (gameObject1 && gameObject1.activeInHierarchy && gameObject2 && gameObject2.activeInHierarchy &&
                    gameObject3 && gameObject3.activeInHierarchy && coroutine.MoveNext())
                yield return coroutine.Current;
        }
    }
}
