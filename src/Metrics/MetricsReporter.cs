using System;

namespace NPitaya.Metrics
{
    class MetricsReporter
    {
        readonly PrometheusReporter _prometheusServer;
        readonly PitayaReporter _pitayaMetrics ;

        internal MetricsReporter(MetricsConfiguration config)
        {
            _prometheusServer = new PrometheusReporter(config);
            _pitayaMetrics = new PitayaReporter(_prometheusServer);
        }

        internal void Start()
        {
            _prometheusServer.Start();
        }

        internal IntPtr GetPitayaPtr()
        {
            return _pitayaMetrics.Ptr;
        }

        internal void IncCounter(string name, string[]? labels)
        {
            _prometheusServer.IncCounter(name, labels);
        }

        internal void SetGauge(string name, float value, string[]? labels)
        {
            _prometheusServer.SetGauge(name, value, labels);
        }

        internal void ObserveHistogram(string name, float value, string[]? labels)
        {
            _prometheusServer.ObserveHistogram(name, value, labels);
        }
    }
}