
namespace AutoPidTuner.Common
{
    public class PidAnalyzer
    {
        private readonly FlightLogData _logData;
        private const double MIN_SUSTAINED_INPUT_DURATION = 0.2;
        private const double OSCILLATION_THRESHOLD = 50.0;
        private const double OVERSHOOT_THRESHOLD = 1.2;
        private const int MIN_OSCILLATION_COUNT = 3;
        private const double ANALYSIS_WINDOW = 0.1;
        private const double INPUT_CHANGE_THRESHOLD = 20.0;

        public Pids pidRecommendations { get; set; } = new();

        public bool Debug { get; set; } = false;

        public class AxisAnalysis
        {
            public string Axis { get; set; }
            public bool HasOscillations { get; set; }
            public bool HasOvershoot { get; set; }
            public bool HasUndershoot { get; set; }
            public double OscillationFrequency { get; set; }
            public double OscillationAmplitude { get; set; }
            public double OvershootAmount { get; set; }
            public string Recommendation { get; set; }
            public List<(double startTime, double endTime)> AnalyzedSegments { get; set; } = new List<(double, double)>();
        }

        public PidAnalyzer(FlightLogData logData)
        {
            _logData = logData;
        }

        public List<AxisAnalysis> AnalyzeFlightData()
        {
            var analyses = new List<AxisAnalysis>();
            string[] axes = { "Roll", "Pitch", "Yaw" };

            if (Debug)
            {
                Console.WriteLine($"Total data points: {_logData.TimeStamps.Count}");
                Console.WriteLine($"Time range: {_logData.TimeStamps.First():F3}s to {_logData.TimeStamps.Last():F3}s");
            }

            for (int axis = 0; axis < 3; axis++)
            {
                if (Debug)
                {
                    Console.WriteLine($"\nAnalyzing {axes[axis]} axis...");
                }

                var analysis = new AxisAnalysis { Axis = axes[axis] };
                var sustainedInputs = FindSustainedInputs(axis);

                if (Debug)
                {
                    Console.WriteLine($"Found {sustainedInputs.Count} sustained inputs for {axes[axis]}");
                    foreach (var (startIdx, endIdx) in sustainedInputs)
                    {
                        Console.WriteLine($"Segment: {_logData.TimeStamps[startIdx]:F3}s to {_logData.TimeStamps[endIdx]:F3}s");
                    }
                }

                if (sustainedInputs.Count > 0)
                {
                    foreach (var (startIdx, endIdx) in sustainedInputs)
                    {
                        analysis.AnalyzedSegments.Add((_logData.TimeStamps[startIdx], _logData.TimeStamps[endIdx]));
                        AnalyzeResponse(axis, startIdx, endIdx, analysis);
                    }

                    analysis.Recommendation = GenerateRecommendation(analysis, axes[axis]);
                    analyses.Add(analysis);
                }
            }

            return analyses;
        }

        private List<(int startIdx, int endIdx)> FindSustainedInputs(int axis)
        {
            var sustainedInputs = new List<(int startIdx, int endIdx)>();
            int? startIdx = null;
            double baselineCommand = 0;
            int steadyCount = 0;
            const int REQUIRED_STEADY_SAMPLES = 5; // Number of samples required to confirm steady state

            for (int i = 1; i < _logData.TimeStamps.Count; i++)
            {
                double currentCommand = GetAxisValue(_logData.RcCommands[i], axis);
                double previousCommand = GetAxisValue(_logData.RcCommands[i - 1], axis);
                double commandDelta = Math.Abs(currentCommand - previousCommand);

                if (commandDelta > INPUT_CHANGE_THRESHOLD)
                {
                    if (startIdx == null)
                    {
                        startIdx = i;
                        baselineCommand = currentCommand;
                        steadyCount = 0;
                    }
                }
                else if (startIdx != null)
                {
                    // Check if we're still around the same command value
                    if (Math.Abs(currentCommand - baselineCommand) < INPUT_CHANGE_THRESHOLD)
                    {
                        steadyCount++;
                    }
                    else
                    {
                        steadyCount = 0;
                        baselineCommand = currentCommand;
                    }

                    // If we've been steady for enough samples and have minimum duration
                    if (steadyCount >= REQUIRED_STEADY_SAMPLES)
                    {
                        double duration = _logData.TimeStamps[i] - _logData.TimeStamps[startIdx.Value];
                        if (duration >= MIN_SUSTAINED_INPUT_DURATION)
                        {
                            sustainedInputs.Add((startIdx.Value, i));
                            if (Debug)
                            {
                                Console.WriteLine($"Found sustained input: {duration:F3}s at {_logData.TimeStamps[startIdx.Value]:F3}s");
                            }
                        }
                        startIdx = null;
                        steadyCount = 0;
                    }
                }
            }

            // Handle case where we're still in a sustained input at the end of the log
            if (startIdx != null)
            {
                double duration = _logData.TimeStamps.Last() - _logData.TimeStamps[startIdx.Value];
                if (duration >= MIN_SUSTAINED_INPUT_DURATION)
                {
                    sustainedInputs.Add((startIdx.Value, _logData.TimeStamps.Count - 1));
                }
            }

            return sustainedInputs;
        }

        private void AnalyzeResponse(int axis, int startIdx, int endIdx, AxisAnalysis analysis)
        {
            var gyroValues = new List<double>();
            var rcValues = new List<double>();
            var times = new List<double>();

            // Include some data before the input change to better detect overshoots
            int analysisStartIdx = Math.Max(0, startIdx - 10);
            int analysisEndIdx = Math.Min(_logData.TimeStamps.Count - 1, endIdx + 10);

            for (int i = analysisStartIdx; i <= analysisEndIdx; i++)
            {
                gyroValues.Add(GetAxisValue(_logData.GyroData[i], axis));
                rcValues.Add(GetAxisValue(_logData.RcCommands[i], axis));
                times.Add(_logData.TimeStamps[i]);
            }

            double targetResponse = rcValues.Skip(10).Take(rcValues.Count - 20).Average();
            double responseRange = gyroValues.Max() - gyroValues.Min();

            // Detect oscillations with improved sensitivity
            var crossings = FindZeroCrossings(gyroValues, rcValues);
            if (crossings.Count >= MIN_OSCILLATION_COUNT)
            {
                analysis.HasOscillations = true;
                analysis.OscillationFrequency = crossings.Count / (times.Last() - times.First());
                analysis.OscillationAmplitude = CalculateAverageAmplitude(gyroValues, rcValues);

                if (Debug)
                {
                    Console.WriteLine($"Detected oscillations: {crossings.Count} crossings, {analysis.OscillationFrequency:F1} Hz");
                }
            }

            // Detect overshoots with improved sensitivity
            double maxResponse = gyroValues.Max();
            double minResponse = gyroValues.Min();

            if (Math.Abs(maxResponse) > Math.Abs(targetResponse) * OVERSHOOT_THRESHOLD)
            {
                analysis.HasOvershoot = true;
                analysis.OvershootAmount = (Math.Abs(maxResponse) - Math.Abs(targetResponse)) / Math.Abs(targetResponse);

                if (Debug)
                {
                    Console.WriteLine($"Detected overshoot: {analysis.OvershootAmount:P1}");
                }
            }

            // Detect undershoots with improved sensitivity
            if (responseRange > 0 && Math.Abs(minResponse - targetResponse) > responseRange * 0.3)
            {
                analysis.HasUndershoot = true;

                if (Debug)
                {
                    Console.WriteLine($"Detected undershoot");
                }
            }
        }

        private List<int> FindZeroCrossings(List<double> gyroValues, List<double> rcValues)
        {
            var crossings = new List<int>();
            double target = rcValues.Average();
            bool wasAbove = gyroValues[0] > target;

            for (int i = 1; i < gyroValues.Count; i++)
            {
                bool isAbove = gyroValues[i] > target;
                if (wasAbove != isAbove)
                {
                    crossings.Add(i);
                    wasAbove = isAbove;
                }
            }

            return crossings;
        }

        private double CalculateAverageAmplitude(List<double> gyroValues, List<double> rcValues)
        {
            double target = rcValues.Average();
            return gyroValues.Select(v => Math.Abs(v - target)).Average();
        }

        private string GenerateRecommendation(AxisAnalysis analysis, string axis)
        {
            var recommendations = new List<string>();
            var pidSettings = _logData.PidSettings[axis];
            if (analysis.HasOscillations)
            {
                if (analysis.OscillationFrequency > 30) // High frequency oscillations
                {
                    pidRecommendations.PidValues[axis].D = pidSettings.D - pidSettings.D * .15d;
                    recommendations.Add($"Reduce {axis} D-term by 15-20% (currently {pidSettings.D:F1})");
                    if (pidSettings.P > 1.0)
                    {
                        pidRecommendations.PidValues[axis].P = pidSettings.P - pidSettings.P * .1d;
                        recommendations.Add($"Consider reducing {axis} P-term by 10% (currently {pidSettings.P:F1})");
                    }
                }
                else // Low frequency oscillations
                {
                    pidRecommendations.PidValues[axis].P = pidSettings.P - pidSettings.P * .15d;
                    recommendations.Add($"Reduce {axis} P-term by 15-20% (currently {pidSettings.P:F1})");
                }
            }

            if (analysis.HasOvershoot)
            {
                if (analysis.OvershootAmount > 0.5) // Significant overshoot
                {
                    pidRecommendations.PidValues[axis].P = pidSettings.P - pidSettings.P * .2d;
                    recommendations.Add($"Reduce {axis} P-term by 20% (currently {pidSettings.P:F1})");
                    pidRecommendations.PidValues[axis].D = pidSettings.D + pidSettings.D * .1d;
                    recommendations.Add($"Consider increasing {axis} D-term by 10% (currently {pidSettings.D:F1})");
                }
            }

            if (analysis.HasUndershoot)
            {
                pidRecommendations.PidValues[axis].I = pidSettings.I + pidSettings.I * .15d;
                recommendations.Add($"Increase {axis} I-term by 15% (currently {pidSettings.I:F1})");
                if (pidSettings.FF > 0)
                {
                    pidRecommendations.PidValues[axis].FF = pidSettings.FF + pidSettings.FF * .1d;
                    recommendations.Add($"Consider increasing {axis} FF-term by 10% (currently {pidSettings.FF:F1})");
                }
            }

            if (!recommendations.Any())
            {
                return $"{axis} axis response looks good, no changes recommended.";
            }

            return string.Join("\n", recommendations);
        }

        private static double GetAxisValue(Vector3 vector, int axis)
        {
            return axis switch
            {
                0 => vector.X,
                1 => vector.Y,
                2 => vector.Z,
                _ => throw new ArgumentOutOfRangeException(nameof(axis))
            };
        }
    }

    // Extension method for FlightLogData to easily create analyzer
    public static class FlightLogDataExtensions
    {
        public static PidAnalyzer CreateAnalyzer(this FlightLogData logData)
        {
            return new PidAnalyzer(logData);
        }
    }
}
