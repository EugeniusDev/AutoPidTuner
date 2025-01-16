namespace AutoPidTuner.Common
{
    public class Pids
    {
        private readonly Dictionary<string, PidCoefficients> _pidValues = new()
        {
            { "Roll", new PidCoefficients() },
            { "Pitch", new PidCoefficients() },
            { "Yaw", new PidCoefficients() }
        };

        public Dictionary<string, PidCoefficients> PidValues
        {
            get => _pidValues;
        }
    }
}
