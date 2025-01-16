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
        private readonly List<TextBlock> pidValuesTextboxes;
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
                    throw new ArgumentException("No recommendations were formed");
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
            var analyses = analyzer.AnalyzeFlightData();
            recommendedPids = analyzer.PidRecommendations;
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
            foreach (string axis in Strings.axesToOptimize)
            {
                var pid = recommendedPids.PidValues[axis];
                AppendPidCliCommands(stringBuilder, axis, pid);
            }
            cliCommands = stringBuilder.ToString();
        }

        private static void AppendPidCliCommands(StringBuilder sb, string axis, PidCoefficients pid)
        {
            if (!pid.P.Equals(0d)) sb.AppendLine($"{CliStrings.GetCommand(axis, "P")}{pid.P}");
            if (!pid.I.Equals(0d)) sb.AppendLine($"{CliStrings.GetCommand(axis, "I")}{pid.I}");
            if (!pid.D.Equals(0d)) sb.AppendLine($"{CliStrings.GetCommand(axis, "D")}{pid.D}");
            if (!pid.FF.Equals(0d)) sb.AppendLine($"{CliStrings.GetCommand(axis, "FF")}{pid.FF}");
        }

        private void DisplaySolution()
        {
            SetPidTunerStatus(AutoTunerStatuses.Success);
            SolutionOverviewTextblock.Text = solutionOverview;
            PopulateRecommendationsTableWithFlightLogData();
        }

        private void PopulateRecommendationsTableWithFlightLogData()
        {
            foreach (string axis in Strings.axesToOptimize)
            {
                var pid = recommendedPids.PidValues[axis];
                PopulatePidValues(axis, pid);
            }
        }

        private void PopulatePidValues(string axis, PidCoefficients pid)
        {
            switch (axis)
            {
                case "Roll":
                    RollP.Text = FormatPidValue(pid.P);
                    RollI.Text = FormatPidValue(pid.I);
                    RollD.Text = FormatPidValue(pid.D);
                    RollFF.Text = FormatPidValue(pid.FF);
                    break;
                case "Pitch":
                    PitchP.Text = FormatPidValue(pid.P);
                    PitchI.Text = FormatPidValue(pid.I);
                    PitchD.Text = FormatPidValue(pid.D);
                    PitchFF.Text = FormatPidValue(pid.FF);
                    break;
                case "Yaw":
                    YawP.Text = FormatPidValue(pid.P);
                    YawI.Text = FormatPidValue(pid.I);
                    YawFF.Text = FormatPidValue(pid.FF);
                    break;
            }
        }

        private static string FormatPidValue(double value)
        {
            return value.Equals(0d) ? Strings.minus : value.ToString();
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