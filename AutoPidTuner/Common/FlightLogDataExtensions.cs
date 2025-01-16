namespace AutoPidTuner.Common
{
    // Extension method for FlightLogData to easily create analyzer
    public static class FlightLogDataExtensions
    {
        public static PidAnalyzer CreateAnalyzer(this FlightLogData logData)
        {
            return new PidAnalyzer(logData);
        }
    }
}
