namespace AutoPidTuner.Common
{
    public static class FlightLogDataExtensions
    {
        public static PidAnalyzer CreateAnalyzer(this FlightLogData logData)
        {
            return new PidAnalyzer(logData);
        }
    }
}
