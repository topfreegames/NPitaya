using System;
using System.Runtime.InteropServices;

namespace NPitaya
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MetricsOpts
    {
        public IntPtr MetricNamespace;
        public IntPtr Subsystem;
        public IntPtr Name;
        public IntPtr Help;
        public IntPtr VariableLabels;
        public UInt32 VariableLabelsCount;
        public BucketOpts Buckets;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BucketOpts
    {
        public IntPtr Kind;
        public double Start;
        public double Increment;
        public UInt32 Count;
    }

    public struct PitayaError
    {
        public string Code;
        public string Message;

        public PitayaError(IntPtr codePtr, IntPtr msgPtr)
        {
            this.Code = Marshal.PtrToStringAnsi(codePtr);
            this.Message = Marshal.PtrToStringAnsi(msgPtr);
        }
    }

    public enum NotificationType
    {
        ServerAdded = 0,
        ServerRemoved = 1,
    }

    public class Server
    {
        IntPtr serverHandle;

        public Server(string id, string kind, string metadata, string hostname, bool frontend)
        {
            serverHandle = PitayaCluster.pitaya_server_new(id, kind, metadata, hostname, frontend ? 1 : 0);
        }

        public Server(IntPtr serverHandle)
        {
            this.serverHandle = serverHandle;
        }

        public string Id => Marshal.PtrToStringAnsi(PitayaCluster.pitaya_server_id(serverHandle));
        public string Kind => Marshal.PtrToStringAnsi(PitayaCluster.pitaya_server_kind(serverHandle));
        public string Metadata => Marshal.PtrToStringAnsi(PitayaCluster.pitaya_server_metadata(serverHandle));
        public string Hostname => Marshal.PtrToStringAnsi(PitayaCluster.pitaya_server_hostname(serverHandle));
        public bool Frontend => PitayaCluster.pitaya_server_frontend(serverHandle) != 0;
        public IntPtr Handle => serverHandle;

        ~Server()
        {
            if (serverHandle != IntPtr.Zero)
            {
                PitayaCluster.pitaya_server_drop(serverHandle);
                serverHandle = IntPtr.Zero;
            }
        }
    }

    public enum NativeLogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Critical = 5,
    }

    public enum NativeLogKind
    {
        Console = 0,
        Json = 1,
        Function = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Route
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string svType;
        [MarshalAs(UnmanagedType.LPStr)]
        public string service;
        [MarshalAs(UnmanagedType.LPStr)]
        public string method;

        public Route(string svType, string service, string method):this(service, method)
        {
            this.svType = svType;
        }

        public Route(string service, string method)
        {
            this.service = service;
            this.method = method;
            svType = "";
        }

        public static Route FromString(string r)
        {
            string[] res = r.Split(new[] { "." }, StringSplitOptions.None);
            if (res.Length == 3)
            {
                return new Route(res[0], res[1], res[2]);
            }
            if (res.Length == 2)
            {
                return new Route(res[0], res[1]);
            }
            Console.WriteLine($"LEN IS {r.Length}");
            throw new Exception($"invalid route: {r}");
        }

        public override string ToString()
        {
            if (svType.Length > 0)
            {
                return $"{svType}.{service}.{method}";
            }
            return $"{service}.{method}";
        }
    }
}

class StructWrapper : IDisposable
{
    public IntPtr Ptr { get; private set; }

    public StructWrapper(object obj)
    {
        Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
        Marshal.StructureToPtr(obj, Ptr, false);
    }

    ~StructWrapper()
    {
        if (Ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(Ptr);
            Ptr = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(Ptr);
        Ptr = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    public static implicit operator IntPtr(StructWrapper w)
    {
        return w.Ptr;
    }
}
