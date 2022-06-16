using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HelperLib.Model;

namespace TripSheet_SQLite
{
    public partial class AddCsgInfo : Window
    {
        ObservableCollection<CsgData> _New_CsgData = new ObservableCollection<CsgData>();
        public ObservableCollection<CsgData> New_CsgData
        {
            get { return _New_CsgData; }
            set
            {
                _New_CsgData = value;
                OnPropertyChanged("New_CsgData");
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

        public AddCsgInfo()
        {
            InitializeComponent();
            New_CsgData = new ObservableCollection<CsgData>();
            LoadCsg();
        }

        private void LoadCsg()
        {
            New_CsgData.Clear();
            List<CsgData> csgData = new List<CsgData>();
            csgData = Startup.sqlSlave.tripSheetModel.CsgData.OrderBy(a => a.CEDisplacement).ToList();
            foreach (CsgData dis in csgData)
            {
                New_CsgData.Add(dis);
            }
            dgCsgData.DataContext = this;
        }
        private void SavePipeToSql()
        {
            Startup.sqlSlave.tripSheetModel.CsgData.Add(new CsgData()
            {
                Id = Guid.NewGuid().ToString(),
                Name = txtName.Text,
                OEDisplacement = Convert.ToDecimal(txtOE.Text),
                CEDisplacement = Convert.ToDecimal(txtCE.Text),
                Details = txtDetails.Text
            });
            Startup.sqlSlave.tripSheetModel.SaveChanges();
        }
        private void BtnSavePipe_Click(object sender, RoutedEventArgs e)
        {
            CheckContent();
        }

        private void Add()
        {
            SavePipeToSql();
            LoadCsg();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            IList<DataGridCellInfo> selected = dgCsgData.SelectedCells;
            List<CsgData> input = new List<CsgData>();
            if (selected.Count > 0)
            {
                string ExistsError = "These items are in use and locked: ";
                bool exists = false;
                foreach (DataGridCellInfo info in selected)
                {
                    if (!input.Contains((CsgData)info.Item))
                        input.Add((CsgData)info.Item);
                }
                foreach (CsgData output in input)
                {
                    exists = Startup.sqlSlave.tripSheetModel.TripSheetData.FirstOrDefault(a => a.PipeId == output.Id) != null;
                    if (!exists)
                        Startup.sqlSlave.tripSheetModel.CsgData.Remove(Startup.sqlSlave.tripSheetModel.CsgData.First(a => a.Id == output.Id));
                    else
                        ExistsError += "\nName: " + output.Name + " - " + "Id: " + output.Id;
                }
                if (exists)
                {
                    MessageBox.Show(ExistsError, "Object locked", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Startup.sqlSlave.tripSheetModel.SaveChanges();
                LoadCsg();
            }
        }

        private void TxtDetails_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                CheckContent();
        }

        private void CheckContent()
        {
            if (txtName.Text == "")
            {
                ChangeErrors(txtName, Brushes.Red, 2, "", lbNameErr, "Missing input");
            }
            if (txtCE.Text == "")
            {
                ChangeErrors(txtCE, Brushes.Red, 2, "", lbCEErr, "Missing input");
            }
            if (txtOE.Text == "")
            {
                ChangeErrors(txtOE, Brushes.Red, 2, "", lbOEErr, "Missing input");
            }
            if (txtCE.Text != "" && txtOE.Text != "" && txtName.Text != "")
            {
                Add();
                var brush = (Brush)new BrushConverter().ConvertFrom("#FFABADB3");
                ChangeErrors(txtName, brush, 1, "", lbNameErr, "");
                ChangeErrors(txtCE, brush, 1, "", lbCEErr, "");
                ChangeErrors(txtOE, brush, 1, "", lbOEErr, "");
                txtDetails.Text = "";
                txtName.Focus();
            }
        }

        private void ChangeErrors(TextBox textBox, Brush color, int thickness, string txt, Label label, string lbtext)
        {
            textBox.Text = txt;
            textBox.BorderBrush = color;
            textBox.BorderThickness = new Thickness(thickness);
            label.Content = lbtext;
        }

        private void TxtOE_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //e.Handled = !IsTextAllowed(e.Text, @"[^[0-9]([.,][0-9]{1,3})?$]");
            e.Handled = !IsTextAllowed(e.Text, @"[^0-9.]");
        }

        private static bool IsTextAllowed(string Text, string AllowedRegex)
        {
            try
            {
                var regex = new Regex(AllowedRegex);
                return !regex.IsMatch(Text);
            }
            catch
            {
                return true;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
