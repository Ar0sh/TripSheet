using HelperLib.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TripSheet_SQLite
{
    /// <summary>
    /// Interaction logic for Insert.xaml
    /// </summary>
    public partial class Insert : Window
    {
        List<PipeData> pipeList;
        List<CsgData> csgList;
        TripSheetData RowData;

        // Public fields, accessible for TripSheet window when dialog ends.
        public long Time { get { return SetTime; } }
        public decimal? BitDepth { get { return SetBitDepth; } }
        public decimal? TripVolume { get { return SetTripVolume; } }
        public decimal? EmptyFill { get { return SetEmptyFill; } }
        public decimal? Displacement_OE { get { return SetDisplacement_OE; } }
        public decimal? Displacement_CE { get { return SetDisplacement_CE; } }
        public string PipeId { get { return SetPipeId; } }
        public bool Saved { get { return SetSaved; } }

        // Private variables
        long SetTime { get; set; }
        decimal? SetBitDepth { get; set; }
        decimal? SetTripVolume { get; set; }
        decimal? SetEmptyFill { get; set; }
        decimal? SetDisplacement_OE { get; set; }
        decimal? SetDisplacement_CE { get; set; }
        string SetPipeId { get; set; }
        bool SetSaved { get; set; }

        public Insert(TripSheetData rowData, PipeData pipeData, CsgData csgData)
        {
            InitializeComponent();
            SetBitDepth = 0;
            SetTripVolume = 0;
            SetEmptyFill = 0;
            SetDisplacement_OE = 0;
            SetDisplacement_CE = 0;
            SetTime = 0;
            SetPipeId = "";
            SetSaved = false;
            RowData = rowData;
            Load_PipeData();
            PopulateBoxes(pipeData, csgData);
            if(cbCsg.Items.Count == 0)
            {
                cbCsg.IsEnabled = false;
                RbCsg.IsEnabled = false;
            }
        }

        // Load pipe and casing displacements
        private void Load_PipeData()
        {
            pipeList = Startup.sqlSlave.tripSheetModel.PipeData.OrderBy(a => a.CEDisplacement).ToList();
            cbPipe.ItemsSource = pipeList;
            csgList = Startup.sqlSlave.tripSheetModel.CsgData.OrderBy(a => a.CEDisplacement).ToList();
            cbCsg.ItemsSource = csgList;
        }

        // Populate all data, if no RowData exists it will become default values.
        private void PopulateBoxes(PipeData pipeData, CsgData csgData)
        {
            dtDateTime.Value = RowData.Time != 0 ? DateTimeOffset.FromUnixTimeSeconds(RowData.Time).UtcDateTime.ToLocalTime() : DateTime.Now;
            tbBitDep.Text = RowData.BDepth != null ? RowData.BDepth.ToString() : "0";
            tbTripVol.Text = RowData.TripVolume != null ? RowData.TripVolume.ToString() : "0";
            tbEmpFill.Text = RowData.EmptyFill != null ? RowData.EmptyFill.ToString() : "0";

            var pipeDet = Startup.sqlSlave.tripSheetModel.PipeData.FirstOrDefault(a => a.Id == RowData.PipeId);
            var pindex = pipeDet == null ? cbPipe.Items.IndexOf(pipeData) : cbPipe.Items.IndexOf(pipeDet);
            cbPipe.SelectedIndex = pindex != -1 ? pindex : 0;

            var csgDet = Startup.sqlSlave.tripSheetModel.CsgData.FirstOrDefault(a => a.Id == RowData.PipeId);
            var cindex = csgDet == null ? cbCsg.Items.IndexOf(csgData) : cbCsg.Items.IndexOf(csgDet);
            cbCsg.SelectedIndex = cindex != -1 ? cindex : 0;

            RbCsg.IsChecked = csgDet != null;
        }

        // Save data, with validation checks for textboxes. If validation fails, notify user with visuals.
        private void Save()
        {
            BindingExpression bitdep = tbBitDep.GetBindingExpression(TextBox.TextProperty);
            bitdep.UpdateSource();
            BindingExpression tripvol = tbTripVol.GetBindingExpression(TextBox.TextProperty);
            tripvol.UpdateSource();
            BindingExpression emptyfill = tbEmpFill.GetBindingExpression(TextBox.TextProperty);
            emptyfill.UpdateSource();
            if (!bitdep.HasError && !tripvol.HasError && !emptyfill.HasError)
            {
                SetTime = (long)dtDateTime.Value.Subtract(new DateTime(1970, 1, 1)).TotalSeconds -
                    (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? 7200 : 3600);
                SetBitDepth = decimal.TryParse(tbBitDep.Text, out decimal tmpBit) ?
                       tmpBit : (decimal?)null;
                SetTripVolume = decimal.TryParse(tbTripVol.Text, out decimal tmpTrip) ?
                       tmpTrip : (decimal?)null;
                SetEmptyFill = decimal.TryParse(tbEmpFill.Text, out decimal tmpEmp) ?
                       tmpEmp : (decimal?)null;
                // Check if row selected had pipe or casing ID, and use this to send correct displacement.
                object pd;
                if (RbPipe.IsChecked == true)
                    pd = (PipeData)cbPipe.SelectedItem;
                else
                    pd = (CsgData)cbCsg.SelectedItem;
                SetDisplacement_OE = pd.GetType() == typeof(PipeData) ? (pd as PipeData).OEDisplacement : (pd as CsgData).OEDisplacement;
                SetDisplacement_CE = pd.GetType() == typeof(PipeData) ? (pd as PipeData).CEDisplacement : (pd as CsgData).CEDisplacement;
                SetPipeId = pd.GetType() == typeof(PipeData) ? (pd as PipeData).Id : (pd as CsgData).Id;
                SetSaved = true;
                Close();
            }
            lbError.Foreground = Brushes.Red;
            lbError.FontWeight = FontWeights.Bold;
            lbError.Content = "Input Error, data not saved!";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        // Disable "comma" character for textboxes
        private void CommaCheck_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemComma)
                e.Handled = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class DecimalRule : ValidationRule
    {
        public DecimalRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (!decimal.TryParse(value.ToString(), out decimal check))
            {
                //ValidationResult(false,*) => in error
                return new ValidationResult(false, "Please enter a valid number");
            }
            //ValidationResult(true,*) => is ok
            return new ValidationResult(true, null);
        }
    }

    public class InputBinds
    {
        public InputBinds()
        {
            BitDepth = 0;
            TripVolume = 0;
            EmptyFill = 0;
        }

        public decimal BitDepth { get; set; }
        public decimal TripVolume { get; set; }
        public decimal EmptyFill { get; set; }
    }
}
