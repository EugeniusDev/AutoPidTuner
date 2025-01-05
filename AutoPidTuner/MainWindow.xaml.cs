using AutoPidTuner.Common;
using Microsoft.Win32;
using System.IO;
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

        private string blackboxFileContent = string.Empty;
        private string recommendations = string.Empty;
        private readonly List<TextBlock> pidValuesTextboxes = [];
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

                string filePath = openFileDialog.FileName;
                FilenameTextblock.Text = filePath;
                blackboxFileContent = File.ReadAllText(filePath);                

                // TODO perform logic here
            }
            catch
            {
                SetPidTunerStatus(AutoTunerStatuses.Error);
                FilenameTextblock.Text = Strings.noFileOpened;
                MessageBox.Show($"Please select a valid CSV file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyCliCommandsButton_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("CLI commands copied to clipboard", 
                "Solution copied", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}