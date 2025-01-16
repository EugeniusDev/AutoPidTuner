namespace AutoPidTuner.Common
{
    public class AxisAnalysis
    {
        public string Axis { get; set; } = string.Empty;
        public bool HasOscillations { get; set; }
        public bool HasOvershoot { get; set; }
        public bool HasUndershoot { get; set; }
        public double OscillationFrequency { get; set; }
        public double OscillationAmplitude { get; set; }
        public double OvershootAmount { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public List<(double startTime, double endTime)> AnalyzedSegments { get; set; } = [];
    }
}
