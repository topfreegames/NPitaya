using System.Collections.Generic;

namespace NPitaya.Metrics
{
    internal class MetricSpec
    {
        internal readonly string Name;
        internal readonly string Help;
        internal readonly string[] Labels;

        internal MetricSpec(string name, string help, string[] labels)
        {
            Name = name;
            Help = help;
            Labels = labels;
        }
    }

    public enum HistogramBucketKind
    {
        Exponential,
        Linear
    }

    public struct HistogramBuckets
    {
        public readonly HistogramBucketKind Kind;
        public readonly double Start;
        public readonly double Inc;
        public readonly uint Count;

        public HistogramBuckets(HistogramBucketKind kind, double start, double inc, uint count)
        {
            Kind = kind;
            Start = start;
            Inc = inc;
            Count = count;
        }
    }

    internal class HistogramSpec : MetricSpec
    {
        public readonly HistogramBuckets Buckets;

        public HistogramSpec(
            string name,
            string help,
            string[] labels,
            HistogramBuckets buckets): base(name, help, labels)
        {
            Buckets = buckets;
        }
    }

    public class CustomMetrics
    {
        internal readonly List<MetricSpec> Counters;
        internal readonly List<MetricSpec> Gauges;
        internal readonly List<HistogramSpec> Histograms;

        public CustomMetrics()
        {
            Counters = new List<MetricSpec>();
            Gauges = new List<MetricSpec>();
            Histograms = new List<HistogramSpec>();
        }

        public void AddCounter(string name, string help = null, string[] labels = null)
        {
            Counters.Add(new MetricSpec(name, help, labels));
        }

        public void AddGauge(string name, string help = null, string[] labels = null)
        {
            Gauges.Add(new MetricSpec(name, help, labels));
        }

        public void AddHistogram(string name, HistogramBuckets buckets, string help = null, string[] labels = null)
        {
            Histograms.Add(new HistogramSpec(name, help, labels, buckets));
        }
    }
}