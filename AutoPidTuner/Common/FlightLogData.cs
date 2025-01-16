using System.IO;

namespace AutoPidTuner.Common
{
    public class FlightLogData
    {
        // Flight controller configuration
        public string FirmwareVersion { get; private set; } = string.Empty;
        public string CraftName { get; private set; } = string.Empty;
        public Dictionary<string, PidCoefficients> PidSettings { get; private set; } = [];

        // Time series data
        public List<double> TimeStamps { get; private set; } = [];
        public List<Vector3> PidP { get; private set; } = [];
        public List<Vector3> PidI { get; private set; } = [];
        public List<Vector3> PidD { get; private set; } = [];
        public List<Vector3> PidF { get; private set; } = [];
        public List<Vector3> RcCommands { get; private set; } = [];
        public List<Vector3> GyroData { get; private set; } = [];

        public static FlightLogData FromCsv(string filePath)
        {
            var logData = new FlightLogData();
            var lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
            {
                throw new ArgumentException("Log file is empty");
            }

            // Find the header line
            var headerIndex = -1;
            var configSection = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("\"loopIteration\""))
                {
                    headerIndex = i;
                    break;
                }
                configSection.Add(lines[i]);
            }

            if (headerIndex == -1)
            {
                throw new FormatException("Could not find header line containing 'loopIteration'");
            }

            // Parse configuration data
            foreach (var line in configSection)
            {
                var parts = line.Split([','], 2); // Split only on first comma
                if (parts.Length != 2) continue;

                var key = parts[0].Trim('"');
                var value = parts[1].Trim('"');

                switch (key)
                {
                    case "Firmware revision":
                        logData.FirmwareVersion = value;
                        break;
                    case "Craft name":
                        logData.CraftName = value;
                        break;
                    case "rollPID":
                        logData.PidSettings["Roll"] = ParsePidString(value);
                        break;
                    case "pitchPID":
                        logData.PidSettings["Pitch"] = ParsePidString(value);
                        break;
                    case "yawPID":
                        logData.PidSettings["Yaw"] = ParsePidString(value);
                        break;
                }
            }

            // Get column indices from header, handling quoted values
            var headers = lines[headerIndex].Split(',')
                                          .Select(h => h.Trim('"'))
                                          .ToArray();

            var columnIndices = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                columnIndices[headers[i]] = i;
            }

            // Parse time series data
            for (int i = headerIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Split(',');
                if (values.Length < columnIndices.Count) continue;

                try
                {
                    // Time
                    logData.TimeStamps.Add(ParseDouble(values[columnIndices["time"]]));

                    // PID values
                    logData.PidP.Add(new Vector3(
                        ParseDouble(values[columnIndices["axisP[0]"]]),
                        ParseDouble(values[columnIndices["axisP[1]"]]),
                        ParseDouble(values[columnIndices["axisP[2]"]])
                    ));

                    logData.PidI.Add(new Vector3(
                        ParseDouble(values[columnIndices["axisI[0]"]]),
                        ParseDouble(values[columnIndices["axisI[1]"]]),
                        ParseDouble(values[columnIndices["axisI[2]"]])
                    ));

                    logData.PidD.Add(new Vector3(
                        ParseDouble(values[columnIndices["axisD[0]"]]),
                        ParseDouble(values[columnIndices["axisD[1]"]]),
                        0 // Yaw typically doesn't use D term
                    ));

                    logData.PidF.Add(new Vector3(
                        ParseDouble(values[columnIndices["axisF[0]"]]),
                        ParseDouble(values[columnIndices["axisF[1]"]]),
                        ParseDouble(values[columnIndices["axisF[2]"]])
                    ));

                    // RC Commands
                    logData.RcCommands.Add(new Vector3(
                        ParseDouble(values[columnIndices["rcCommand[0]"]]),
                        ParseDouble(values[columnIndices["rcCommand[1]"]]),
                        ParseDouble(values[columnIndices["rcCommand[2]"]])
                    ));

                    // Gyro data
                    logData.GyroData.Add(new Vector3(
                        ParseDouble(values[columnIndices["gyroADC[0]"]]),
                        ParseDouble(values[columnIndices["gyroADC[1]"]]),
                        ParseDouble(values[columnIndices["gyroADC[2]"]])
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                    continue;
                }
            }

            return logData;
        }

        private static PidCoefficients ParsePidString(string pidString)
        {
            var values = pidString.Split(',')
                                .Select(ParseDouble)
                                .ToArray();

            return new PidCoefficients
            {
                P = values[0],
                I = values[1],
                D = values[3],
                FF = values.Length > 4 ? values[4] : 0
            };
        }

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, out double result) ? result : 0;
        }
    }

    public struct Vector3(double x, double y, double z)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Z { get; set; } = z;

        public override readonly string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
