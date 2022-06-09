using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TripSheet_SQLite.Model;
//using HelperLib.Model;
using HelperLib;

namespace TripSheet_SQLite
{
    public partial class Startup : Window
    {
        // Public variables.
        public static CDAconn GetCDA;
        public static dynamic dllInstance;
        public static TripSheetModel tripSheetModel;
        public static SQLSlave sqlSlave;

        // Private variables.
        private List<TripSheetDetail> sheetList;

        ObservableCollection<TripSheetDetail> _New_TripSheetDetail = new ObservableCollection<TripSheetDetail>();
        /// <summary>
        /// TripSheet dataset, ObservableCollection so it notifies if added, modified or deleted.
        /// </summary>
        public ObservableCollection<TripSheetDetail> New_TripSheetDetail
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
        }

        /// <summary>
        /// Check if SQLite DB exists.
        /// </summary>
        private void InitializeDB()
        {
            tripSheetModel = new TripSheetModel();
            //tripSheetModel = sqlSlave.tripSheetModel;
            if (!tripSheetModel.Database.Exists())
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
            New_TripSheetDetail = new ObservableCollection<TripSheetDetail>();
            LoadSheets();
        }

        /// <summary>
        /// Enable or disable UI elements based on CDA connection.
        /// </summary>
        private void EnableUI()
        {
            string connTest = "";
            try
            {
                connTest = GetCDA.dllInstance.GetValueAsString("WELL_ID");
            }
            catch
            {

            }
            if (connTest == "")
            {
                Dispatcher.Invoke(() =>
                {
                    btnLoad.IsEnabled = false;
                    btnNew.IsEnabled = false;
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
                btnConCDA.Visibility = Visibility.Hidden;
                btnConCDA.IsEnabled = false;
                lbStatus.Content = "Status: No issues...";
                lbStatus.Foreground = Brushes.Green;
            });
        }

        /// <summary>
        /// Load existing TripSheets from SQLite DB.
        /// </summary>
        private void LoadSheets()
        {
            sheetList = new List<TripSheetDetail>();
            sheetList = tripSheetModel.TripSheetDetail.OrderBy(a => a.Name).ToList();
            //sheetList = sqlSlave.LoadSheets();
            cbSheets.ItemsSource = sheetList;
        }

        /// <summary>
        /// Create a new trip sheet.
        /// </summary>
        private void NewClick()
        {
            if (tripSheetModel.PipeData.Count() == 0)
            {
                MessageBox.Show("Please add pipe details before making a tripsheet.", "Add pipe data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (txtNewName.Text == "" || txtNewName.Text == "Enter Sheet Name" || sheetList.Where(s => s.Name == txtNewName.Text).ToList().Count != 0)
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
            TripSheet tripSheet = new TripSheet(sqlSlave.GetSheetID(txtNewName.Text), txtNewName.Text);
            tripSheet.ShowDialog();
            Show();
            LoadSheets();
        }

        /// <summary>
        /// Load existing trip sheet from database.
        /// </summary>
        private void LoadClick()
        {
            if (tripSheetModel.PipeData.Count() == 0)
            {
                MessageBox.Show("Please add pipe details before loading tripsheet.", "Add pipe data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (cbSheets.SelectedValue != null)
            {
                Hide();
                TripSheet tripSheet = new TripSheet(((TripSheetDetail)cbSheets.SelectedItem).Id, ((TripSheetDetail)cbSheets.SelectedItem).Name);
                tripSheet.ShowDialog();
                Show();
            }
        }

        /// <summary>
        /// Delete sheet from database.
        /// </summary>
        private void DeleteClick()
        {
            MessageBoxResult result = MessageBox.Show("Delete " + ((TripSheetDetail)cbSheets.SelectedItem).Name, "Confirm", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                if (cbSheets.SelectedValue != null)
                {
                    sqlSlave.DeleteSheet(((TripSheetDetail)cbSheets.SelectedItem).Id);
                    LoadSheets();
                }
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
            if (cbSheets.Items.Count > 0 && (TripSheetDetail)(sender as ComboBox).SelectedItem != null)
            {
                TripSheetDetail selectedSheet = (TripSheetDetail)(sender as ComboBox).SelectedItem;
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

        }
    }
}
