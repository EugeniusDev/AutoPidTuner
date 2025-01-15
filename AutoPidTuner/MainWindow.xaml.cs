using AutoPidTuner.Common;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AutoPidTuner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Dictionary<AutoTunerStatuses, (string, SolidColorBrush)> statusesData = new()
        {
            { AutoTunerStatuses.Waiting, (Strings.waitingForFileStatus, new(Colors.MediumAquamarine)) },
            { AutoTunerStatuses.Processing, (Strings.processingStatus, new(Colors.Gold)) },
            { AutoTunerStatuses.Error, (Strings.errorStatus, new(Colors.DarkRed)) },
            { AutoTunerStatuses.Success, (Strings.successStatus, new(Colors.DarkGreen)) },
        };

        private FlightLogData? flightLogData;
        private string openedFilepath = string.Empty;
        private string solutionOverview = string.Empty;
        private string cliCommands = Strings.minus;
        private readonly List<TextBlock> pidValuesTextboxes = [];
        private Pids recommendedPids = new();
        public MainWindow()
        {
            InitializeComponent();
            pidValuesTextboxes =
            [
                RollP, RollI, RollD, RollFF,
                PitchP, PitchI, PitchD, PitchFF,
                YawP, YawI, YawFF
            ];
            ResetUi();
        }

        private void ResetUi()
        {
            FilenameTextblock.Text = Strings.noFileOpened;
            SetPidTunerStatus(AutoTunerStatuses.Waiting);
            FirmwareTextBlock.Text = Strings.noInfo;
            CraftTextBlock.Text = Strings.noInfo;
            DataPointsTextBlock.Text = Strings.noInfo;

            pidValuesTextboxes.ForEach(textblock => textblock.Text = Strings.minus);

            SolutionOverviewTextblock.Text = Strings.noInfo;
        }

        private void SetPidTunerStatus(AutoTunerStatuses wantedStatus)
        {
            StatusTextblock.Text = statusesData[wantedStatus].Item1;
            StatusTextblock.Foreground = statusesData[wantedStatus].Item2;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            ResetValues();
            ResetUi();
            OpenFileDialog openFileDialog = new()
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Open converted to CSV Blackbox File"
            };

            if (openFileDialog.ShowDialog() is null)
            {
                return;
            }

            try
            {
                SetPidTunerStatus(AutoTunerStatuses.Processing);

                openedFilepath = openFileDialog.FileName;
                flightLogData = FlightLogData.FromCsv(openedFilepath);
                if (flightLogData is null)
                {
                    throw new ArgumentException("Nothing was found in log data");
                }
                PopulateUiWithGeneralData();

                FormRecommendations();
                if (string.IsNullOrWhiteSpace(solutionOverview))
                {
                    throw new Exception("No recommendations were formed");
                }

                DisplaySolution();
            }
            catch
            {
                SetPidTunerStatus(AutoTunerStatuses.Error);
                FilenameTextblock.Text = Strings.noFileOpened;
                MessageBox.Show($"Could not provide recommendations. " +
                    $"Check for file validity or try flying more aggressively",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetValues()
        {
            openedFilepath = Strings.noFileOpened;
            solutionOverview = string.Empty;
            cliCommands = Strings.minus;
        }

        private void PopulateUiWithGeneralData()
        {
            FilenameTextblock.Text = openedFilepath;

            FirmwareTextBlock.Text = flightLogData!.FirmwareVersion;
            CraftTextBlock.Text = flightLogData.CraftName;
            DataPointsTextBlock.Text = flightLogData.TimeStamps.Count.ToString();
        }

        private void FormRecommendations()
        {
            var analyzer = flightLogData!.CreateAnalyzer();
            // analyzer.Debug = true;
            var analyses = analyzer.AnalyzeFlightData();
            recommendedPids = analyzer.pidRecommendations;
            StringBuilder sb = new();
            foreach (var analysis in analyses)
            {
                sb.AppendLine($"Analysis for {analysis.Axis} axis:");
                sb.AppendLine($"Analyzed {analysis.AnalyzedSegments.Count} segments");
                sb.AppendLine(analysis.Recommendation);
                sb.AppendLine();
            }
            solutionOverview = sb.ToString();

            FormCliCommands();
        }

        private void FormCliCommands()
        {
            StringBuilder stringBuilder = new();

            FlightLogData.PidCoefficients roll = recommendedPids.PidValues["Roll"];
            if (!roll.P.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.p_roll, roll.P.ToString()));
            }
            if (!roll.I.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.i_roll, roll.I.ToString()));
            }
            if (!roll.D.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.d_roll, roll.D.ToString()));
            }
            if (!roll.FF.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.f_roll, roll.FF.ToString()));
            }

            FlightLogData.PidCoefficients pitch = recommendedPids.PidValues["Pitch"];
            if (!pitch.P.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.p_pitch, pitch.P.ToString()));
            }
            if (!pitch.I.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.i_pitch, pitch.I.ToString()));
            }
            if (!pitch.D.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.d_pitch, pitch.D.ToString()));
            }
            if (!pitch.FF.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.f_pitch, pitch.FF.ToString()));
            }

            FlightLogData.PidCoefficients yaw = recommendedPids.PidValues["Yaw"];
            if (!yaw.P.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.p_yaw, yaw.P.ToString()));
            }
            if (!yaw.I.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.i_yaw, yaw.I.ToString()));
            }
            if (!yaw.FF.Equals(0d))
            {
                stringBuilder.AppendLine(string.Concat(CliStrings.f_yaw, yaw.FF.ToString()));
            }

            cliCommands = stringBuilder.ToString();
        }

        private void DisplaySolution()
        {
            SetPidTunerStatus(AutoTunerStatuses.Success);
            SolutionOverviewTextblock.Text = solutionOverview;
            PopulateRecommendationsTableWithFlightLogData();
        }

        private void PopulateRecommendationsTableWithFlightLogData()
        {
            FlightLogData.PidCoefficients roll = recommendedPids.PidValues["Roll"];
            RollP.Text = roll.P.Equals(0d) ? Strings.minus : roll.P.ToString();
            RollI.Text = roll.I.Equals(0d) ? Strings.minus : roll.I.ToString();
            RollD.Text = roll.D.Equals(0d) ? Strings.minus : roll.D.ToString();
            RollFF.Text = roll.FF.Equals(0d) ? Strings.minus : roll.FF.ToString();

            FlightLogData.PidCoefficients pitch = recommendedPids.PidValues["Pitch"];
            PitchP.Text = pitch.P.Equals(0d) ? Strings.minus : pitch.P.ToString();
            PitchI.Text = pitch.I.Equals(0d) ? Strings.minus : pitch.I.ToString();
            PitchD.Text = pitch.D.Equals(0d) ? Strings.minus : pitch.D.ToString();
            PitchFF.Text = pitch.FF.Equals(0d) ? Strings.minus : pitch.FF.ToString();

            FlightLogData.PidCoefficients yaw = recommendedPids.PidValues["Yaw"];
            YawP.Text = yaw.P.Equals(0d) ? Strings.minus : yaw.P.ToString();
            YawI.Text = yaw.I.Equals(0d) ? Strings.minus : yaw.I.ToString();
            YawFF.Text = yaw.FF.Equals(0d) ? Strings.minus : yaw.FF.ToString();
        }

        private void CopyCliCommandsButton_Click(object sender, RoutedEventArgs e)
        {
            if (cliCommands.Equals(Strings.minus))
            {
                return;
            }

            Clipboard.SetText(cliCommands);
            MessageBox.Show("CLI commands copied to clipboard", 
                "Solution copied", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}