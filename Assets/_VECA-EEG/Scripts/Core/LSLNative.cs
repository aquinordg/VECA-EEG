using System;
using System.Runtime.InteropServices;

namespace LSL
{
    internal enum ChannelFormat { String = 3 }

    public sealed class StreamInfo : IDisposable
    {
        internal IntPtr handle;

        public StreamInfo(string name, string type, int channelCount = 1, double nominalSrate = 0.0, string sourceId = "")
        {
            handle = NativeMethods.lsl_create_streaminfo(name, type, channelCount, nominalSrate, ChannelFormat.String, sourceId);
            if (handle == IntPtr.Zero) throw new InvalidOperationException("lsl_create_streaminfo returned null.");
        }

        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            NativeMethods.lsl_destroy_streaminfo(handle);
            handle = IntPtr.Zero;
        }
    }

    public sealed class StreamOutlet : IDisposable
    {
        private IntPtr handle;

        public StreamOutlet(StreamInfo info, int chunkSize = 0, int maxBuffered = 360)
        {
            handle = NativeMethods.lsl_create_outlet(info.handle, chunkSize, maxBuffered);
            if (handle == IntPtr.Zero) throw new InvalidOperationException("lsl_create_outlet returned null.");
        }

        public void PushSample(string[] sample)
        {
            if (handle != IntPtr.Zero)
                NativeMethods.lsl_push_sample_str(handle, sample);
        }

        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            NativeMethods.lsl_destroy_outlet(handle);
            handle = IntPtr.Zero;
        }
    }

    internal static class NativeMethods
    {
        private const string Dll = "lsl";

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern IntPtr lsl_create_streaminfo(string name, string type, int channelCount,
            double nominalSrate, ChannelFormat channelFormat, string sourceId);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void lsl_destroy_streaminfo(IntPtr info);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern IntPtr lsl_create_outlet(IntPtr info, int chunkSize, int maxBuffered);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void lsl_destroy_outlet(IntPtr outlet);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true)]
        internal static extern int lsl_push_sample_str(IntPtr outlet, string[] data);
    }
}
