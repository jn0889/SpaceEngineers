﻿#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using VRage.Library.Utils;

#endregion

namespace VRage.Profiler
{
    public struct MyProfilerBlockKey
    {
        public readonly string File;
        public readonly string Member;
        public readonly string Name;
        public readonly int Line;
        public readonly int ParentId;
        public readonly int HashCode;

        public MyProfilerBlockKey(string file, string member, string name, int line, int parentId)
        {
            File = file;
            Member = member;
            Name = name;
            Line = line;
            ParentId = parentId;
            unchecked
            {
                HashCode = file.GetHashCode();
                HashCode = (397 * HashCode) ^ member.GetHashCode();
                HashCode = (397 * HashCode) ^ (name ?? String.Empty).GetHashCode();
                HashCode = (397 * HashCode) ^ parentId.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            throw new InvalidBranchException("Equals is not supposed to be called, use comparer!");
        }

        public override int GetHashCode()
        {
            throw new InvalidBranchException("Get hash code is not supposed to be called, use comparer!");
        }
    }

    public class MyProfilerBlockKeyComparer : IEqualityComparer<MyProfilerBlockKey>
    {
        public bool Equals(MyProfilerBlockKey x, MyProfilerBlockKey y)
        {
            return x.ParentId == y.ParentId && x.Name == y.Name && x.Member == y.Member && x.File == y.File && x.Line == y.Line;
        }

        public int GetHashCode(MyProfilerBlockKey obj)
        {
            return obj.HashCode;
        }
    }

    public class MyProfilerBlock
    {
        public int Id { get; private set; }
        public MyProfilerBlockKey Key { get; private set; }
        public int ForceOrder { get; private set; }

        public string Name { get { return Key.Name; } }

        // Immediate data not accessed by Draw
        public long MeasureStartTimestamp = 0;
        public long ElapsedTimestamp = 0;

        public MyTimeSpan Elapsed;
        public long StartManagedMB = 0;
        public long EndManagedMB = 0;
        public long DeltaManagedB = 0; //?
        public float TotalManagedMB = 0; //?
        public long StartProcessMB = 0;
        public long EndProcessMB = 0;
        public float DeltaProcessB = 0;       //?
        public float TotalProcessMB = 0;      //?

        public bool Invalid = false;

        public int NumCalls = 0;
        public float CustomValue = 0;

        public string TimeFormat;
        public string ValueFormat;
        public string CallFormat;

        public long ManagedDeltaMB
        {
            //conversion to MB
            get { return (DeltaManagedB / 1024 / 1024); }
        }

        public float ProcessDeltaMB
        {
            //conversion to MB
            get { return (DeltaProcessB) * 0.000000953674f; }
        }

        // History data not accessed by Start/End
        public float[] ProcessMemory = new float[MyProfiler.MAX_FRAMES];
        public long[] ManagedMemoryBytes = new long[MyProfiler.MAX_FRAMES];
        public float[] Miliseconds = new float[MyProfiler.MAX_FRAMES];
        public float[] CustomValues = new float[MyProfiler.MAX_FRAMES];
        public int[] NumCallsArray = new int[MyProfiler.MAX_FRAMES];

        // Unused
        public float averageMiliseconds;

        public List<MyProfilerBlock> Children = new List<MyProfilerBlock>();
        public MyProfilerBlock Parent = null;

        public MyProfilerBlock()
        {
        }

        public MyProfilerBlock(ref MyProfilerBlockKey key, string memberName, int blockId, int forceOrder = int.MaxValue)
        {
            Id = blockId;
            Key = key;
            ForceOrder = forceOrder;
        }

        public void SetBlockData(ref MyProfilerBlockKey key, int blockId, int forceOrder = int.MaxValue)
        {
            Id = blockId;
            Key = key;
            ForceOrder = forceOrder;
        }

        public void Reset()
        {
            MeasureStartTimestamp = Stopwatch.GetTimestamp();
            Elapsed = MyTimeSpan.Zero;
        }

        /// <summary>
        /// Clears immediate data
        /// </summary>
        public void Clear()
        {
            Reset();
            NumCalls = 0;

            StartManagedMB = 0;
            EndManagedMB = 0;
            DeltaManagedB = 0;

            StartProcessMB = 0;
            EndProcessMB = 0;
            DeltaProcessB = 0;

            CustomValue = 0;
        }

        public void Start(bool memoryProfiling)
        {
            NumCalls++;

            // Timestamp at the start, include own overhead
            MeasureStartTimestamp = Stopwatch.GetTimestamp();

            StartManagedMB = GC.GetTotalMemory(false);   // About 1ms per 2000 calls

            if (memoryProfiling)
            {
                // TODO: OP! Use better non-alloc call
                StartProcessMB = System.Environment.WorkingSet;   // WorkingSet is allocating memory in each call, also its expensive (about 7ms per 2000 calls).
            }
        }

        public void End(bool memoryProfiling, MyTimeSpan? customTime = null)
        {
            EndManagedMB = System.GC.GetTotalMemory(false);
            DeltaManagedB += EndManagedMB - StartManagedMB;

            if (memoryProfiling)
            {
                // TODO: OP! Use better non-alloc call
                EndProcessMB = System.Environment.WorkingSet;   // WorkingSet is allocating memory in each call, also its expensive (about 7ms per 2000 calls).
                DeltaProcessB += EndProcessMB - StartProcessMB;
            }

            // Time stamp at the end, include own overhead
            long endTimestamp = Stopwatch.GetTimestamp();
            ElapsedTimestamp = endTimestamp - MeasureStartTimestamp;
            Elapsed += customTime ?? new MyTimeSpan(ElapsedTimestamp);
        }

        public override string ToString()
        {
            return Key.Name + " (" + NumCalls.ToString() + " calls)";
        }

        internal void Dump(StringBuilder sb, int frame)
        {
            if (NumCallsArray[frame] < 0.01)
                return;
            sb.Append(string.Format("<Block Name=\"{0}\">\n", Name));
            sb.Append(string.Format("<Time>{0}</Time>\n<Calls>{1}</Calls>\n", Miliseconds[frame], NumCallsArray[frame]));
            foreach (var child in Children)
                child.Dump(sb, frame);
            sb.Append("</Block>\n");
        }
    }
}
