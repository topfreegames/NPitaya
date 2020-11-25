using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using NPitaya.Models;

namespace NPitaya.Metrics
{
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    public class PitayaReporter
    {
        static readonly string[] NoLabels = new string[0];
        const string LabelSeparator = "_";
        const string PitayaSubsystem = "pitaya";

        readonly PitayaCluster.RegisterCounterFn _registerCounterDelegate;
        readonly PitayaCluster.RegisterGaugeFn _registerGaugeDelegate;
        readonly PitayaCluster.RegisterHistogramFn _registerHistogramDelegate;
        readonly PitayaCluster.IncCounterFn _incCounterDelegate;
        readonly PitayaCluster.SetGaugeFn _setGaugeDelegate;
        readonly PitayaCluster.AddGaugeFn _addGaugeFn;
        readonly PitayaCluster.ObserveHistFn _observeHistFn;

        public IntPtr Ptr { get; }

        public PitayaReporter(PrometheusReporter prometheusReporter)
        {
            var handle = GCHandle.Alloc(prometheusReporter, GCHandleType.Normal);
            var reporterPtr = GCHandle.ToIntPtr(handle);

            _registerCounterDelegate = RegisterCounterFn;
            _registerGaugeDelegate = RegisterGaugeFn;
            _registerHistogramDelegate = RegisterHistogramFn;
            _incCounterDelegate = IncCounterFn;
            _setGaugeDelegate = SetGaugeFn;
            _addGaugeFn = AddGaugeFn;
            _observeHistFn = ObserveHistFn;

            Ptr = PitayaCluster.pitaya_metrics_reporter_new(
                _registerCounterDelegate,
                _registerHistogramDelegate,
                _registerGaugeDelegate,
                _incCounterDelegate,
                _observeHistFn,
                _setGaugeDelegate,
                _addGaugeFn,
                reporterPtr);
        }

        ~PitayaReporter()
        {
            PitayaCluster.pitaya_metrics_reporter_drop(Ptr);
        }

        static void RegisterCounterFn(IntPtr prometheusPtr, MetricsOpts opts)
        {
            // TODO (felipe.rodopoulos): prometheus-net does not support subsystem label yet. We'll use a hardcoded one.
            var name = Marshal.PtrToStringAnsi(opts.Name);
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn("Tried to register a counter with an empty name");
                return;
            }
            var help = Marshal.PtrToStringAnsi(opts.Help) ?? string.Empty;
            var labels = ReadLabels(opts.VariableLabels, opts.VariableLabelsCount);
            var prometheus = RetrievePrometheus(prometheusPtr);
            var key = BuildKey(name);
            prometheus?.RegisterCounter(key, help, labels);
        }

        static void RegisterHistogramFn(IntPtr prometheusPtr, MetricsOpts opts)
        {
            // TODO (felipe.rodopoulos): prometheus-net does not support subsystem label yet. We'll use a hardcoded one.
            var name = Marshal.PtrToStringAnsi(opts.Name);
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn("Tried to register an histogram with an empty name");
                return;
            }
            var prometheus = RetrievePrometheus(prometheusPtr);
            var key = BuildKey(name);
            var help = Marshal.PtrToStringAnsi(opts.Help) ?? string.Empty;
            var labels = ReadLabels(opts.VariableLabels, opts.VariableLabelsCount);
            var bucketsConfig = ReadBuckets(ref opts.Buckets);
            prometheus?.RegisterHistogram(key, help, labels, bucketsConfig);
        }

        static void RegisterGaugeFn(IntPtr prometheusPtr, MetricsOpts opts)
        {
            // TODO (felipe.rodopoulos): prometheus-net does not support subsystem label yet. We'll use a hardcoded one.
            var name = Marshal.PtrToStringAnsi(opts.Name);
            if (string.IsNullOrEmpty(name))
            {
                Logger.Warn("Tried to register a gaugee with an empty name");
                return;
            }
            var help = Marshal.PtrToStringAnsi(opts.Help) ?? string.Empty;
            var labels = ReadLabels(opts.VariableLabels, opts.VariableLabelsCount);
            var prometheus = RetrievePrometheus(prometheusPtr);
            var key = BuildKey(name);
            prometheus?.RegisterGauge(key, help, labels);
        }

        static void IncCounterFn(IntPtr prometheusPtr, IntPtr name, IntPtr labels, UInt32 labelsCount)
        {
            string nameStr = Marshal.PtrToStringAnsi(name) ?? string.Empty;
            if (string.IsNullOrEmpty(nameStr))
            {
                Logger.Warn("Tried to increment a counter with an empty name");
                return;
            }
            var key = BuildKey(nameStr);
            var labelsArr = ReadLabels(labels, labelsCount);
            var prometheus = RetrievePrometheus(prometheusPtr);
            prometheus?.IncCounter(key, labelsArr);
        }

        static void ObserveHistFn(IntPtr prometheusPtr, IntPtr name, double value, IntPtr labels, UInt32 labelsCount)
        {
            string nameStr = Marshal.PtrToStringAnsi(name) ?? string.Empty;
            if (string.IsNullOrEmpty(nameStr))
            {
                Logger.Warn("Tried to observe an histogram with an empty name");
                return;
            }
            var key = BuildKey(nameStr);
            var labelsArr = ReadLabels(labels, labelsCount);
            var prometheus = RetrievePrometheus(prometheusPtr);
            prometheus?.ObserveHistogram(key, value, labelsArr);
        }

        static void SetGaugeFn(IntPtr prometheusPtr, IntPtr name, double value, IntPtr labels, UInt32 labelsCount)
        {
            string nameStr = Marshal.PtrToStringAnsi(name) ?? string.Empty;
            if (string.IsNullOrEmpty(nameStr))
            {
                Logger.Warn("Tried to set a gauge with an empty name");
                return;
            }
            var key = BuildKey(nameStr);
            var labelsArr = ReadLabels(labels, labelsCount);
            var prometheus = RetrievePrometheus(prometheusPtr);
            prometheus?.SetGauge(key, value, labelsArr);
        }

        static void AddGaugeFn(IntPtr prometheusPtr, IntPtr name, double value, IntPtr labels, UInt32 labelsCount)
        {
            string nameStr = Marshal.PtrToStringAnsi(name) ?? string.Empty;
            if (string.IsNullOrEmpty(nameStr))
            {
                Logger.Warn("Tried to add to a gauge with an empty name");
                return;
            }
            var key = BuildKey(nameStr);
            var labelsArr = ReadLabels(labels, labelsCount);
            var prometheus = RetrievePrometheus(prometheusPtr);
            prometheus?.AddGauge(key, value, labelsArr);
        }

        private static PrometheusReporter? RetrievePrometheus(IntPtr ptr)
        {
            var handle = GCHandle.FromIntPtr(ptr);
            return handle.Target as PrometheusReporter;
        }

        static unsafe string[] ReadLabels(IntPtr labelsPtr, UInt32 size)
        {
            if (size == 0)
            {
                return NoLabels;
            }

            var ptr = (IntPtr*) labelsPtr.ToPointer();
            var labels = new string[size];
            for (var i = 0; i < size; i++)
            {
                var label = Marshal.PtrToStringAnsi(ptr[i]);
                if (label != null)
                {
                    labels[i] = label;
                }
            }

            return labels;
        }

        static unsafe HistogramBuckets ReadBuckets(ref BucketOpts buckets)
        {
            if (buckets.Count == 0)
            {
                throw new Exception("Tried to register histogram with no buckets");
            }

            var typeStr = Marshal.PtrToStringAnsi(buckets.Kind);
            HistogramBucketKind kind = typeStr switch
            {
                "linear" => HistogramBucketKind.Linear,
                "exponential" => HistogramBucketKind.Exponential,
                _ => throw new Exception($"Invalid metric buckets type {typeStr}")
            };

            return new HistogramBuckets(kind, buckets.Start, buckets.Increment, buckets.Count);
        }

        private static string BuildKey(string suffix)
        {
            return $"{PitayaSubsystem}{LabelSeparator}{suffix}";
        }
    }
}