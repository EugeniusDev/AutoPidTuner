namespace AutoPidTuner.Common
{
    public static class Strings
    {
        public static readonly string noFileOpened = "No";
        public static readonly string noInfo = "None";
        public static readonly string minus = "-";
        public static readonly string[] axesToOptimize = ["Roll", "Pitch", "Yaw"];

        public static readonly string waitingForFileStatus = "Waiting for blackbox file to be selected";
        public static readonly string processingStatus = "Processing provided data";
        public static readonly string errorStatus = "Error occured, try again";
        public static readonly string successStatus = "Processing successful, view solution proposition below";
    }

    public static class CliStrings
    {
        public static readonly string p_pitch = "set p_pitch = ";
        public static readonly string i_pitch = "set i_pitch = ";
        public static readonly string d_pitch = "set d_pitch = ";

        public static readonly string p_roll = "set p_roll = ";
        public static readonly string i_roll = "set i_roll = ";
        public static readonly string d_roll = "set d_roll = ";

        public static readonly string p_yaw = "set p_yaw = ";
        public static readonly string i_yaw = "set i_yaw = ";

        public static readonly string f_roll = "set f_roll = ";
        public static readonly string f_pitch = "set f_pitch = ";
        public static readonly string f_yaw = "set f_yaw = ";

        public static string GetCommand(string axis, string parameterType)
        {
            return (axis.ToLower(), parameterType.ToUpper()) switch
            {
                ("roll", "P") => p_roll,
                ("roll", "I") => i_roll,
                ("roll", "D") => d_roll,
                ("roll", "FF") => f_roll,
                ("pitch", "P") => p_pitch,
                ("pitch", "I") => i_pitch,
                ("pitch", "D") => d_pitch,
                ("pitch", "FF") => f_pitch,
                ("yaw", "P") => p_yaw,
                ("yaw", "I") => i_yaw,
                ("yaw", "FF") => f_yaw,
                _ => throw new ArgumentException($"Invalid axis or parameterType: {axis}, {parameterType}")
            };
        }
    }
}
