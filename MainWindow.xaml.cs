using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using DropTableEditor.Engine;
using DropTableEditor.Windows;
using MessageBox = System.Windows.MessageBox;

namespace DropTableEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ConfigFileName = "DropTableEditor.config";

        public MainWindow()
        {
            InitializeComponent();
            LoadSavedPath();
        }

        /// <summary>
        /// Loads the last used data path from config file.
        /// </summary>
        private void LoadSavedPath()
        {
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    string savedPath = File.ReadAllText(ConfigFileName).Trim();
                    if (Directory.Exists(savedPath))
                    {
                        DataPathTextBox.Text = savedPath;
                        ValidatePath(savedPath);
                    }
                }
            }
            catch
            {
                // Ignore config loading errors
            }
        }

        /// <summary>
        /// Saves the current data path to config file.
        /// </summary>
        private void SavePath(string path)
        {
            try
            {
                File.WriteAllText(ConfigFileName, path);
            }
            catch
            {
                // Ignore config saving errors
            }
        }

        /// <summary>
        /// Handles the Browse button click.
        /// </summary>
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select your ROSE Online 3DDATA folder";
                dialog.ShowNewFolderButton = false;

                if (!string.IsNullOrEmpty(DataPathTextBox.Text))
                {
                    dialog.SelectedPath = DataPathTextBox.Text;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DataPathTextBox.Text = dialog.SelectedPath;
                    ValidatePath(dialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Validates that the selected path contains required files.
        /// </summary>
        private void ValidatePath(string path)
        {
            string stbPath = Path.Combine(path, "STB");
            
            if (!Directory.Exists(stbPath))
            {
                StatusText.Text = "Error: STB folder not found in the selected directory.";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                LoadButton.IsEnabled = false;
                return;
            }

            // Check for required files
            string itemDropPath = Path.Combine(stbPath, "ITEM_DROP.STB");
            if (!File.Exists(itemDropPath))
            {
                StatusText.Text = "Error: ITEM_DROP.STB not found. Please select a valid 3DDATA folder.";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                LoadButton.IsEnabled = false;
                return;
            }

            StatusText.Text = "Valid 3DDATA folder detected. Ready to load drop tables.";
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
            LoadButton.IsEnabled = true;
            SavePath(path);
        }

        /// <summary>
        /// Handles the Load button click.
        /// </summary>
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            string dataPath = DataPathTextBox.Text;

            if (string.IsNullOrEmpty(dataPath) || !Directory.Exists(dataPath))
            {
                MessageBox.Show("Please select a valid 3DDATA folder.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Show loading message
                StatusText.Text = "Loading files...";
                StatusText.Foreground = System.Windows.Media.Brushes.Blue;
                LoadButton.IsEnabled = false;
                BrowseButton.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                // Force UI update
                System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(delegate { }));

                // Initialize FileManager with the data path
                FileManager.Initialize(dataPath);

                StatusText.Text = "Files loaded successfully!";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;

                // Open the Drop Table Viewer
                var viewer = new DropTableViewer();
                viewer.Owner = this;
                viewer.Show();

                // Hide the main window while viewer is open
                this.Hide();
                
                // When viewer closes, show main window again
                viewer.Closed += (s, args) =>
                {
                    this.Show();
                    LoadButton.IsEnabled = true;
                    BrowseButton.IsEnabled = true;
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                };
            }
            catch (Exception ex)
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                LoadButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
                StatusText.Text = "Error loading files. See message below.";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                MessageBox.Show(string.Format("Error loading files:\n\n{0}\n\nPlease make sure you selected the correct 3DDATA folder.", 
                    ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the Exit button click.
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
