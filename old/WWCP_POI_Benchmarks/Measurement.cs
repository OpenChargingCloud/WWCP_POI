/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics;

namespace WWCP_POI_Benchmarks;

internal sealed record OperationResult(Int64 OutputBytes, Int64 WrittenBytes, String Identity);

internal sealed record Sample(Double ElapsedMilliseconds, Double CpuMilliseconds, Int64 AllocatedBytes,
    Int64 ManagedBeforeBytes, Int64 SampledManagedPeakBytes, Int64 WorkingSetBeforeBytes,
    Int64 SampledWorkingSetPeakBytes, Int64 ProcessLifetimePeakWorkingSetBytes, Int32 Gen0, Int32 Gen1, Int32 Gen2,
    OperationResult Result);

internal static class Measurement
{
    internal static Sample Run(Func<OperationResult> action)
    {
        // Setup and warmup precede this forced collection. Allocation is measured on the operation thread.
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var managedBefore = GC.GetTotalMemory(false);
        var workingBefore = process.WorkingSet64;
        Int64 managedPeak = managedBefore, workingPeak = workingBefore;
        using var stop = new ManualResetEventSlim();
        using var started = new ManualResetEventSlim();
        var sampler = new Thread(() => {
            using var observed = Process.GetCurrentProcess();
            started.Set();
            while (!stop.Wait(10))
            {
                managedPeak = Math.Max(managedPeak, GC.GetTotalMemory(false));
                observed.Refresh();
                workingPeak = Math.Max(workingPeak, observed.WorkingSet64);
            }
        }) { IsBackground = true };
        sampler.Start(); started.Wait();
        var cpuBefore = process.TotalProcessorTime;
        var collections = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        OperationResult result;
        Int64 allocated;
        Double cpuMilliseconds;
        try
        {
            result = action();
            timer.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            cpuMilliseconds = (process.TotalProcessorTime - cpuBefore).TotalMilliseconds;
        }
        finally { timer.Stop(); stop.Set(); sampler.Join(); }
        process.Refresh();
        managedPeak = Math.Max(managedPeak, GC.GetTotalMemory(false));
        workingPeak = Math.Max(workingPeak, process.WorkingSet64);
        return new(timer.Elapsed.TotalMilliseconds, cpuMilliseconds,
            allocated, managedBefore, managedPeak, workingBefore, workingPeak, process.PeakWorkingSet64,
            GC.CollectionCount(0) - collections[0], GC.CollectionCount(1) - collections[1],
            GC.CollectionCount(2) - collections[2], result);
    }
}
