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
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Handles the window loaded event.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Use dispatcher to ensure UI is fully ready before loading
            this.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                new Action(() => LoadSavedPath()));
        }

        /// <summary>
        /// Loads the last used data path from config file.
        /// </summary>
        private void LoadSavedPath()
        {
            try
            {
                _config = AppConfig.Load();
                
                if (!string.IsNullOrEmpty(_config.LastDataPath) && Directory.Exists(_config.LastDataPath))
                {
                    DataPathTextBox.Text = _config.LastDataPath;
                    ValidatePath(_config.LastDataPath);
                }
            }
            catch (Exception ex)
            {
                // Show config loading errors
                StatusText.Text = "Error loading saved path: " + ex.Message;
                StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                _config = new AppConfig();
            }
        }

        /// <summary>
        /// Saves the current data path to config file.
        /// </summary>
        private void SavePath(string path)
        {
            if (_config == null)
                _config = new AppConfig();
                
            _config.LastDataPath = path;
            _config.Save();
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
        /// Handles the TextBox KeyDown event (Enter key triggers validation).
        /// </summary>
        private void DataPathTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string path = DataPathTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(path))
                {
                    ValidatePath(path);
                }
            }
        }

        /// <summary>
        /// Validates that the selected path contains required files and loads them.
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

            // Files exist, try to load them
            try
            {
                StatusText.Text = "Loading files...";
                StatusText.Foreground = System.Windows.Media.Brushes.Blue;
                LoadButton.IsEnabled = false;
                BrowseButton.IsEnabled = false;
                this.Cursor = System.Windows.Input.Cursors.Wait;

                // Force UI update
                System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(delegate { }));

                // Clear any previously loaded data
                FileManager.Reset();

                // Initialize FileManager with the data path
                FileManager.Initialize(path);

                StatusText.Text = "Files loaded successfully! Click 'Open Drop Table Editor' to continue.";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                LoadButton.IsEnabled = true;
                BrowseButton.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                SavePath(path);
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading files: " + ex.Message;
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                LoadButton.IsEnabled = false;
                BrowseButton.IsEnabled = true;
                this.Cursor = System.Windows.Input.Cursors.Arrow;
                
                // Log full exception for debugging
                System.Diagnostics.Debug.WriteLine("ValidatePath Exception: " + ex.ToString());
            }
        }

        /// <summary>
        /// Handles the Load button click - opens the Drop Table Editor.
        /// </summary>
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open the Drop Table Viewer (files are already loaded by ValidatePath)
                var viewer = new DropTableViewer();
                viewer.Owner = this;
                viewer.Show();

                // Hide the main window while viewer is open
                this.Hide();
                
                // When viewer closes, show main window again
                viewer.Closed += (s, args) =>
                {
                    this.Show();
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error opening Drop Table Editor:\n\n{0}", ex.Message), 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
