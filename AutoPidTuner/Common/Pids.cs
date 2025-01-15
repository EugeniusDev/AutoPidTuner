using static AutoPidTuner.Common.FlightLogData;

namespace AutoPidTuner.Common
{
    public class Pids
    {
        public Dictionary<string, PidCoefficients> PidValues = new()
        {
            { "Roll", new() },
            { "Pitch", new() },
            { "Yaw", new() }
        };
    }
}
