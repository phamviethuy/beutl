﻿using BeUtl.Compute.OpenCL;
using BeUtl.Compute.PlatformLayer;

namespace BeUtl.Compute.Runtime;

public unsafe class CommandQueue : ComputeObject
{
    public CommandQueue(Context context, Device device)
    {
        var status = (int)CLStatusCode.CL_SUCCESS;
        Context = context;
        Device = device;
        Pointer = CL.CreateCommandQueue(context.Pointer, device.Pointer, CLCommandQueueProperties.CL_QUEUE_PROFILING_ENABLE, &status);
        status.CheckError();
    }

    public Context Context { get; }

    public Device Device { get; }

    public void* Pointer { get; }

    public Event NDRangeKernel(Kernel kernel, params Event[] eventWaitList)
    {
        return kernel.NDRange(this, eventWaitList);
    }

    public void WaitFinish()
    {
        CL.Finish(Pointer).CheckError();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        CL.ReleaseCommandQueue(Pointer).CheckError();
    }
}