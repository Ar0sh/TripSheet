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
using TripSheet_SQLite.Model;
//using HelperLib.Model;

namespace TripSheet_SQLite
{
    public partial class AddPipeInfo : Window
    {
        ObservableCollection<HelperLib.Model.PipeData> _New_DisplacementData = new ObservableCollection<HelperLib.Model.PipeData>();
        public ObservableCollection<HelperLib.Model.PipeData> New_DisplacementData
        {
            get { return _New_DisplacementData; }
            set
            {
                _New_DisplacementData = value;
                OnPropertyChanged("New_DisplacementData");
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

        public AddPipeInfo()
        {
            InitializeComponent();
            New_DisplacementData = new ObservableCollection<HelperLib.Model.PipeData>();
            LoadPipes();
        }

        private void LoadPipes()
        {
            New_DisplacementData.Clear();
            List<HelperLib.Model.PipeData> displacementDatas = new List<HelperLib.Model.PipeData>();
            displacementDatas = Startup.sqlSlave.tripSheetModel.PipeData.OrderBy(a => a.CEDisplacement).ToList();
            foreach (HelperLib.Model.PipeData dis in displacementDatas)
            {
                New_DisplacementData.Add(dis);
            }
            dgPipeData.DataContext = this;
        }
        private void SavePipeToSql()
        {
            Startup.sqlSlave.tripSheetModel.PipeData.Add(new HelperLib.Model.PipeData()
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
            LoadPipes();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            IList<DataGridCellInfo> selected = dgPipeData.SelectedCells;
            List<PipeData> input = new List<PipeData>();
            if (selected.Count > 0)
            {
                string ExistsError = "These items are in use and locked: ";
                bool exists = false;
                foreach (DataGridCellInfo info in selected)
                {
                    if (!input.Contains((PipeData)info.Item))
                        input.Add((PipeData)info.Item);
                }
                foreach (PipeData output in input)
                {
                    exists = Startup.sqlSlave.tripSheetModel.TripSheetData.FirstOrDefault(a => a.PipeId == output.Id) != null ? true : false;
                    if (!exists)
                        Startup.sqlSlave.tripSheetModel.PipeData.Remove(Startup.sqlSlave.tripSheetModel.PipeData.First(a => a.Id == output.Id));
                    else
                        ExistsError += "\nName: " + output.Name + " - " + "Id: " + output.Id;
                }
                if (exists)
                {
                    MessageBox.Show(ExistsError, "Object locked", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                Startup.sqlSlave.tripSheetModel.SaveChanges();
                LoadPipes();
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
            e.Handled = !IsTextAllowed(e.Text, @"[^[0-9]([.,][0-9]{1,3})?$]");
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
