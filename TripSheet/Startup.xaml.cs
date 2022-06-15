using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TripSheet_SQLite.Model;
using System.Net;
//using HelperLib.Model;
using HelperLib;
using System.IO;
using Microsoft.Win32;

namespace TripSheet_SQLite
{
    public partial class Startup : Window
    {
        public static DevEnum DevStatus = DevEnum.RELEASE;

        // Public variables.
        public static string Version = "v0.79";
        public static CDAconn GetCDA;
        public static dynamic dllInstance;
        //public static TripSheetModel tripSheetModel;
        public static SQLSlave sqlSlave;

        // Private variables.
        private List<string> TestHosts = new List<string> { "BHI61G25S2", "BHICZHX3G2" };

        ObservableCollection<HelperLib.Model.TripSheetDetail> _New_TripSheetDetail = new ObservableCollection<HelperLib.Model.TripSheetDetail>();
        /// <summary>
        /// TripSheet dataset, ObservableCollection so it notifies if added, modified or deleted.
        /// </summary>
        public ObservableCollection<HelperLib.Model.TripSheetDetail> New_TripSheetDetail
        {
            get { return _New_TripSheetDetail; }
            set
            {
                _New_TripSheetDetail = value;
                OnPropertyChanged("New_TripSheetDetail");
            }
        }

        // INotifyPropertyChanged region start
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)

        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        public Startup()
        {
            InitializeComponent();
            Title += " " + Version;
            if (DevStatus == DevEnum.DEVELOPMENT)
            {
                Title += " | DEVELOPMENT";
            }
            else if (DevStatus == DevEnum.TESTING)
            {
                if (TestHosts.Contains(Dns.GetHostName()))
                {
                    Title += " | TESTING";
                }
                else
                {
                    MessageBox.Show("Host not approved for testing.", "Invalid Host", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Close();
                }
            }
        }

        /// <summary>
        /// Check if SQLite DB exists.
        /// </summary>
        private void InitializeDB()
        {
            sqlSlave.tripSheetModel = new HelperLib.Model.TripSheetModel();
            if (!sqlSlave.tripSheetModel.Database.Exists())
            {
                MessageBox.Show("Database does not exist, please contact Technical Support?", "Missing DB", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
            TripSheet.saveStartup = true;
        }

        // Run startup methods after window is rendered.
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Create CDA object to use.
            GetCDA = new CDAconn();
            sqlSlave = new SQLSlave();
            EnableUI();
            InitializeDB();
            New_TripSheetDetail = new ObservableCollection<HelperLib.Model.TripSheetDetail>();
            LoadSheets();
        }

        /// <summary>
        /// Enable or disable UI elements based on CDA connection.
        /// </summary>
        private void EnableUI()
        {
            string connTest = GetCDA.dllInstance.GetValueAsString("WELL_ID");
            if (connTest == "")
            {
                Dispatcher.Invoke(() =>
                {
                    btnLoad.IsEnabled = false;
                    btnNew.IsEnabled = false;
                    btnEdit.IsEnabled = false;
                    btnBlankDB.IsEnabled = true;
                    btnRestore.IsEnabled = true;
                    btnConCDA.Visibility = Visibility.Visible;
                    btnConCDA.IsEnabled = true;
                    lbStatus.Content = "Status: CDA not found, message server not running?";
                    lbStatus.Foreground = Brushes.Red;
                });
                return;
            }
            Dispatcher.Invoke(() =>
            {
                btnLoad.IsEnabled = true;
                btnNew.IsEnabled = true;
                btnDelete.IsEnabled = true;
                btnEditPipe.IsEnabled = true;
                btnEditCsg.IsEnabled = true;
                cbSheets.IsEnabled = true;
                btnEdit.IsEnabled = true;
                btnBlankDB.IsEnabled = true;
                btnRestore.IsEnabled = true;
                btnConCDA.Visibility = Visibility.Hidden;
                btnConCDA.IsEnabled = false;
                lbStatus.Content = "Status: No issues...";
                lbStatus.Foreground = Brushes.Green;
            });
        }

        /// <summary>
        /// Load existing TripSheets from SQLite DB.
        /// </summary>
        private void LoadSheets(bool edit = false, string id = "")
        {
            int selected = -1;
            if (edit)
            {
                selected = cbSheets.SelectedIndex;
                cbSheets.ItemsSource = null;
                cbSheets.Items.Refresh();
            }
            sqlSlave.sheetList = sqlSlave.tripSheetModel.TripSheetDetail.OrderBy(a => a.Name).ToList();

            cbSheets.ItemsSource = sqlSlave.sheetList;
            if (id != "")
            {
                var selectedItem = sqlSlave.tripSheetModel.TripSheetDetail.First(a => a.Id == id);
                cbSheets.SelectedItem = selectedItem;
                return;
            }
            cbSheets.SelectedIndex = selected == -1 ? 0 : selected;
        }

        /// <summary>
        /// Create a new trip sheet.
        /// </summary>
        private void NewClick()
        {
            if (sqlSlave.tripSheetModel.PipeData.Count() == 0)
            {
                MessageBox.Show("Please add pipe details before making a tripsheet.", "Add pipe data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (txtNewName.Text == "" || txtNewName.Text == "Enter Sheet Name" || sqlSlave.sheetList.Where(s => s.Name == txtNewName.Text).ToList().Count != 0)
            {
                lbNameErr.Content = "Name exists or not allowed";
                lbNameErr.Foreground = Brushes.Red;
                return;
            }
            lbNameErr.Content = "";
            NewSheet();
        }

        /// <summary>
        /// Create and save new sheet to DB.
        /// <para />
        /// Load new sheet window.
        /// </summary>
        private void NewSheet()
        {
            sqlSlave.NewSheet(txtNewName.Text,
                txtNewDetails.Text != "Enter Helpful Details" ? txtNewDetails.Text : "",
                GetCDA.GetValueAsString("WELL_ID"),
                GetCDA.GetValueAsString("HOLE_ID"));
            Hide();
            string activeID = sqlSlave.GetSheetID(txtNewName.Text);
            TripSheet tripSheet = new TripSheet(activeID, txtNewName.Text);
            tripSheet.ShowDialog();
            txtNewDetails.Text = "Enter Helpful Details";
            txtNewName.Text = "Enter Sheet Name";
            Show();
            LoadSheets(false, activeID);
        }

        /// <summary>
        /// Load existing trip sheet from database.
        /// </summary>
        private void LoadClick()
        {
            if (sqlSlave.tripSheetModel.PipeData.Count() == 0)
            {
                MessageBox.Show("Please add pipe details before loading tripsheet.", "Add pipe data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cbSheets.SelectedValue != null)
            {
                Hide();
                TripSheet tripSheet = new TripSheet(((HelperLib.Model.TripSheetDetail)cbSheets.SelectedItem).Id, ((HelperLib.Model.TripSheetDetail)cbSheets.SelectedItem).Name);
                tripSheet.ShowDialog();
                Show();
            }
        }

        /// <summary>
        /// Delete sheet from database.
        /// </summary>
        private void DeleteClick()
        {
            try
            {
                MessageBoxResult result = MessageBox.Show("Delete " + ((HelperLib.Model.TripSheetDetail)cbSheets.SelectedItem).Name, "Confirm", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    if (cbSheets.SelectedValue != null)
                    {
                        sqlSlave.DeleteSheet(((HelperLib.Model.TripSheetDetail)cbSheets.SelectedItem).Id);
                        LoadSheets();
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Open pipe dialog.
        /// </summary>
        private static void EditPipeClick()
        {
            AddPipeInfo addPipeInfo = new AddPipeInfo();
            addPipeInfo.ShowDialog();
        }

        /// <summary>
        /// Open casing dialog.
        /// </summary>
        private static void EditCsgClick()
        {
            AddCsgInfo addCsgInfo = new AddCsgInfo();
            addCsgInfo.ShowDialog();
        }

        private void CbSheets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbSheets.Items.Count > 0 && (HelperLib.Model.TripSheetDetail)(sender as ComboBox).SelectedItem != null)
            {
                HelperLib.Model.TripSheetDetail selectedSheet = (HelperLib.Model.TripSheetDetail)(sender as ComboBox).SelectedItem;
                txtName.Text = selectedSheet.Name;
                txtId.Text = selectedSheet.Id;
                txtDetails.Text = selectedSheet.Details;
                txtWell.Text = selectedSheet.Well;
                txtWellbore.Text = selectedSheet.Wellbore;
                return;
            }
            txtName.Text = "";
            txtId.Text = "";
            txtDetails.Text = "";
            txtWell.Text = "";
            txtWellbore.Text = "";
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            NewClick();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadClick();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteClick();
        }
        private void BtnEditPipe_Click(object sender, RoutedEventArgs e)
        {
            EditPipeClick();
        }

        private void BtnEditCsg_Click(object sender, RoutedEventArgs e)
        {
            EditCsgClick();
        }

        private void TxtNewName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtNewName.Text == "Enter Sheet Name")
            {
                txtNewName.Text = "";
            }
        }

        private void TxtNewName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtNewName.Text == "")
            {
                txtNewName.Text = "Enter Sheet Name";
            }
        }

        private void TxtNewDetails_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtNewDetails.Text == "Enter Helpful Details")
            {
                txtNewDetails.Text = "";
            }
        }

        private void TxtNewDetails_LostFocus(object sender, RoutedEventArgs e)
        {
            if (txtNewDetails.Text == "")
            {
                txtNewDetails.Text = "Enter Helpful Details";
            }
        }

        private void BtnConCDA_Click(object sender, RoutedEventArgs e)
        {
            EnableUI();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            EditSheet();
        }

        private void EditSheet()
        {
            if (cbSheets.Items.Count > 0)
            {
                Edit edit = new Edit(txtId.Text);
                edit.ShowDialog();
                LoadSheets(true);
            }
        }

        private void BtnBlankDB_Click(object sender, RoutedEventArgs e)
        {
            DBManager dBManager = new DBManager();
            try
            {
                MessageBoxResult result = MessageBox.Show("Restore blank database?\nPipe and Casing data will be copied over to blank DB.", "Confirm", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    dBManager.RestoreBlankDB();
                    LoadSheets();
                }
            }
            catch
            {
                MessageBox.Show("Error restoring blank DB", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            DBManager dBManager = new DBManager();
            try
            {
                OpenFileDialog fileDialog = new OpenFileDialog
                {
                    InitialDirectory = Directory.GetCurrentDirectory() + "\\Database",
                    DefaultExt = ".sqlite",
                    Filter = "sqlite files | *.sqlite",
                    Title = "Select sqlite backup to restore"
                };
                MessageBoxResult result = MessageBox.Show("Backup current database before restoring database?", "Backup", MessageBoxButton.YesNo, MessageBoxImage.Question);
                bool backup = result == MessageBoxResult.Yes ? true : false;
                if (fileDialog.ShowDialog() == true)
                {
                    dBManager.RestoreBlankDB(fileDialog.FileName, backup);
                    LoadSheets();
                }
            }
            catch
            {
                MessageBox.Show("Cannot restore DB", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
