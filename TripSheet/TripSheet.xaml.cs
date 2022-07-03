using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Windows.Controls;
using ScottPlot.Plottable;
using Microsoft.Win32;
using HelperLib.Model;
using Calculations;
using System.Windows.Media;
using ScottPlot;
using System.Globalization;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;

namespace TripSheet_SQLite
{
    public partial class TripSheet : Window
    {
        Volumes volumes = new Volumes();
        bool Edit = true;
        public static bool saveStartup = false;
        List<PipeData> pipeList;
        List<CsgData> csgList;
        string SheetGuid = "";
        string PipeGuid = "";
        Brush primaryColor = Brushes.Green;
        Brush secondaryColor = Brushes.Red;
        System.Drawing.Color primaryColorPlot = System.Drawing.Color.Green;
        System.Drawing.Color secondaryColorPlot = System.Drawing.Color.Red;
        DataGridCellInfo cellInfo;
        // X, Y, XT(Time X axis) data sets for plot

        List<double[]> dataLX;
        List<double[]> dataLY;
        List<DateTime> dataLXT = new List<DateTime>();
        // Variables used to make points on plot
        double[] dataX1;
        double[] dataX2;
        double[] dataY1;
        double[] dataY2;
        double[] dataXT;
        // ScottPlot variables and Highlights
        private MarkerPlot[] HighlightedPoint = new MarkerPlot[2];
        System.Drawing.Color[] colors = new System.Drawing.Color[] { System.Drawing.Color.Black, System.Drawing.Color.Orange };
        private int[] LastHighlightedIndex = { -1, -1 };
        private ScatterPlot[] MyScatterPlot;
        ObservableCollection<TripSheetData> _New_TripSheetInput = new ObservableCollection<TripSheetData>();

        int cellRow;
        /// <summary>
        /// TripSheetInput dataset, ObservableCollection so it notifies if added, modified or deleted.
        /// </summary>
        public ObservableCollection<TripSheetData> New_TripSheetInput
        {
            get { return _New_TripSheetInput; }
            set
            {
                _New_TripSheetInput = value;
                OnPropertyChanged("New_TripSheetInput");
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

        public TripSheet(string guid, string titleName)
        {
            titleName = Startup.DevStatus == DevEnum.TESTING ? titleName + " TESTING" : titleName;
            SheetGuid = guid;
            InitializeComponent();
            Title = "TripSheet " + Startup.Version + " | " + titleName;
            TripPlot.Configuration.DoubleClickBenchmark = false;
            TripPlot.RightClicked -= TripPlot.DefaultRightClickEvent;
            TripPlot.RightClicked += CustomRightClickMenu;
            TripPlot.Plot.YAxis.TickLabelNotation(invertSign: true);
            dgTripData.Items.Clear();
            DataContext = this;
            New_TripSheetInput = new ObservableCollection<TripSheetData>();
            dgTripData.Focus();
            Load_PipeData();
            Load_CsgData();
            cbPipe.SelectedIndex = 0;
            cbCsg.SelectedIndex = 0;
            RbPipe.IsChecked = true;
            Load_TripData();
            RbOE.IsChecked = true;

        }

        /// <summary>
        /// Custom right click menu for plot, including reset zoom, plot window and save image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CustomRightClickMenu(object sender, EventArgs e)
        {
            MenuItem zoomToFit = new MenuItem() { Header = "Zoom To Fit" };
            zoomToFit.Click += ZoomToFit;
            MenuItem openPlot = new MenuItem() { Header = "Open in new Window" };
            openPlot.Click += OpenPlotWindow;
            MenuItem saveImage = new MenuItem() { Header = "Save Image" };
            saveImage.Click += SavePlotImage;

            ContextMenu rightClickMenu = new ContextMenu();
            rightClickMenu.Items.Add(zoomToFit);
            rightClickMenu.Items.Add(openPlot);
            rightClickMenu.Items.Add(saveImage);

            rightClickMenu.IsOpen = true;
        }
        // Save plot image method.
        private void SavePlotImage(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                InitialDirectory = Directory.GetCurrentDirectory(),
                Filter = "Png (*.png) | *.png",
                FileName = "TripPlot_" + DateTime.Now.ToString("ddMMyyyyHHmmss")
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                TripPlot.Plot.SaveFig(saveFileDialog.FileName);
            }
        }
        // Open separate plot window.
        private void OpenPlotWindow(object sender, RoutedEventArgs e)
        {
            WpfPlotViewer wpfPlotViewer = new WpfPlotViewer(TripPlot.Plot)
            {
                Title = "Tripping Plot"
            };
            wpfPlotViewer.Show();
        }
        // Zoom to fit/reset zoom
        private void ZoomToFit(object sender, RoutedEventArgs e)
        {
            TripPlot.Plot.AxisAuto();
            TripPlot.Refresh();
        }
        // Load drill pipe table data
        private void Load_PipeData()
        {
            pipeList = Startup.sqlSlave.tripSheetModel.PipeData.OrderBy(a => a.CEDisplacement).ToList();
            cbPipe.ItemsSource = pipeList;
        }
        // Load casing table data
        private void Load_CsgData()
        {
            csgList = Startup.sqlSlave.tripSheetModel.CsgData.OrderBy(a => a.CEDisplacement).ToList();
            cbCsg.ItemsSource = csgList;
        }
        // Changed selection of pipe in combobox trigger
        private void CbPipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RbPipe.IsChecked == true)
            {
                CheckPipe((PipeData)(sender as ComboBox).SelectedItem);
            }
        }
        // Changed selection of casing in combobox trigger
        private void CbCsg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RbCsg.IsChecked == true)
            {
                CheckCasing((CsgData)(sender as ComboBox).SelectedItem);
            }
        }
        // Load trip table data
        private void Load_TripData()
        {
            New_TripSheetInput.Clear();
            List<TripSheetData> tripSheetDatas = Startup.sqlSlave.tripSheetModel.TripSheetData.Where(b => b.SheetId == SheetGuid).OrderBy(a => a.Time).ToList();
            if (tripSheetDatas.Count == 0 && tbCE.Text != "")
            {
                TripSheetData zeroItem = new TripSheetData();
                EnterTripData(zeroItem, null, true);
                Startup.sqlSlave.tripSheetModel.TripSheetData.Add(zeroItem);
                New_TripSheetInput.Add(zeroItem);
                SaveToSql();
            }
            if (tripSheetDatas.Count > 0)
            {
                for (int i = 0; i < tripSheetDatas.Count; i++)
                {
                    TripSheetData newItem = new TripSheetData();
                    EnterTripData(newItem, tripSheetDatas[i]);
                    New_TripSheetInput.Add(newItem);
                }
                pipeList = Startup.sqlSlave.tripSheetModel.PipeData.OrderBy(a => a.Name).ToList();
                var testing = tripSheetDatas.Last().PipeId;
                var pipeDet = Startup.sqlSlave.tripSheetModel.PipeData.FirstOrDefault(a => a.Id == testing);
                var index = cbPipe.Items.IndexOf(pipeDet);
                cbPipe.SelectedIndex = index != -1 ? index : 0;
            }

            dgTripData.DataContext = this;
            if (Startup.sqlSlave.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }

        // Set new tripsheetdata items values, if first entry or not.
        private void EnterTripData(TripSheetData newItem, TripSheetData tripSheetData, bool zero = false)
        {
            newItem.Id = zero == false ? tripSheetData.Id : Guid.NewGuid().ToString();
            newItem.Time = zero == false ? tripSheetData.Time : DateTimeOffset.Now.ToUnixTimeSeconds();
            newItem.ActualVolume = zero == false ? tripSheetData.ActualVolume : 0.00M;
            newItem.TripVolume = zero == false ? tripSheetData.TripVolume : Math.Round(Convert.ToDecimal(Startup.GetCDA.GetValue("TRIPPVT")), 2);
            newItem.BDepth = zero == false ? tripSheetData.BDepth : Math.Round(Convert.ToDecimal(Startup.GetCDA.GetValue("BITDEP")), 2);
            newItem.Diff_OE = zero == false ? tripSheetData.Diff_OE : 0.00M;
            newItem.Diff_CE = zero == false ? tripSheetData.Diff_CE : 0.00M;
            newItem.TotDiff_OE = zero == false ? tripSheetData.TotDiff_OE : 0.00M;
            newItem.TotDiff_CE = zero == false ? tripSheetData.TotDiff_CE : 0.00M;
            newItem.TheoreticalVol_OE = zero == false ? tripSheetData.TheoreticalVol_OE : 0.00M;
            newItem.TheoreticalVol_CE = zero == false ? tripSheetData.TheoreticalVol_CE : 0.00M;
            newItem.EmptyFill = zero == false ? tripSheetData.EmptyFill : 0;
            newItem.Displacement_OE = zero == false ? tripSheetData.Displacement_OE : null;
            newItem.Displacement_CE = zero == false ? tripSheetData.Displacement_CE : null;
            newItem.SheetId = zero == false ? tripSheetData.SheetId : SheetGuid;
            newItem.PipeId = zero == false ? tripSheetData.PipeId : PipeGuid;
            newItem.TimeDiffMin = zero == false ? tripSheetData.TimeDiffMin : null;
            newItem.LossGainRate_OE = zero == false ? tripSheetData.LossGainRate_OE : null;
            newItem.LossGainRate_CE = zero == false ? tripSheetData.LossGainRate_CE : null;
        }

        // Update trip data table
        private void UpdateSheet(int row = -1, bool insert = false)
        {
            int k = dgTripData.Items.Count;
            TripSheetData tnew = (TripSheetData)dgTripData.Items[k - 1];
            TripSheetData tbefore = k != 1 ? (TripSheetData)dgTripData.Items[k - 2] : null;
            // Check if new or edited row. If edited, redo calculations.
            if (tnew.Id != null || (tnew.Id == null && tnew.EmptyFill != null))
            {
                if (tbefore == null && tnew.Id == null)
                {
                    tnew.SheetId = SheetGuid;
                    tnew.PipeId = PipeGuid;
                    tnew.EmptyFill = 0;
                    tnew.Displacement_OE = Convert.ToDecimal(tbOE.Text);
                    tnew.Displacement_CE = Convert.ToDecimal(tbCE.Text);
                    tnew.ActualVolume = tnew.TripVolume;
                    tnew.TheoreticalVol_OE = tnew.TripVolume;
                    tnew.TheoreticalVol_CE = tnew.TripVolume;
                    tnew.Diff_OE = 0.00M;
                    tnew.Diff_CE = 0.00M;
                    tnew.TotDiff_OE = 0.00M;
                    tnew.TotDiff_CE = 0.00M;
                    Startup.sqlSlave.tripSheetModel.TripSheetData.Add(tnew);
                }
                else if (insert == true)
                {
                    tnew.Id = Guid.NewGuid().ToString();
                    tnew.SheetId = SheetGuid;
                    tnew.ActualVolume = volumes.ActualVolume(tnew.TripVolume, tbefore.TripVolume, tnew.EmptyFill);
                    tnew.TheoreticalVol_OE = volumes.TheoreticalVol(tnew.BDepth, tbefore.BDepth, Convert.ToDecimal(tbOE.Text));
                    tnew.TheoreticalVol_CE = volumes.TheoreticalVol(tnew.BDepth, tbefore.BDepth, Convert.ToDecimal(tbCE.Text));
                    tnew.Diff_OE = volumes.Subtract(tnew.ActualVolume, tnew.TheoreticalVol_OE);
                    tnew.Diff_CE = volumes.Subtract(tnew.ActualVolume, tnew.TheoreticalVol_CE);
                    tnew.TotDiff_OE = volumes.Addition(tbefore.TotDiff_OE, tnew.Diff_OE);
                    tnew.TotDiff_CE = volumes.Addition(tbefore.TotDiff_CE, tnew.Diff_CE);
                    tnew.TimeDiffMin = (int)(tnew.Time - tbefore.Time);
                    tnew.LossGainRate_OE = volumes.GainLossTime(tnew.Diff_OE, tnew.TimeDiffMin);
                    tnew.LossGainRate_CE = volumes.GainLossTime(tnew.Diff_CE, tnew.TimeDiffMin);
                    Startup.sqlSlave.tripSheetModel.TripSheetData.Add(tnew);
                }
                else if (tnew.Id == null && tnew.TripVolume != null && tnew.BDepth != 0 && tbefore != null)
                {
                    tnew.Id = Guid.NewGuid().ToString();
                    tnew.SheetId = SheetGuid;
                    tnew.PipeId = PipeGuid;
                    tnew.Displacement_OE = Convert.ToDecimal(tbOE.Text);
                    tnew.Displacement_CE = Convert.ToDecimal(tbCE.Text);
                    tnew.ActualVolume = volumes.ActualVolume(tnew.TripVolume, tbefore.TripVolume, tnew.EmptyFill);
                    tnew.TheoreticalVol_OE = volumes.TheoreticalVol(tnew.BDepth, tbefore.BDepth, Convert.ToDecimal(tbOE.Text));
                    tnew.TheoreticalVol_CE = volumes.TheoreticalVol(tnew.BDepth, tbefore.BDepth, Convert.ToDecimal(tbCE.Text));
                    tnew.Diff_OE = volumes.Subtract(tnew.ActualVolume, tnew.TheoreticalVol_OE);
                    tnew.Diff_CE = volumes.Subtract(tnew.ActualVolume, tnew.TheoreticalVol_CE);
                    tnew.TotDiff_OE = volumes.Addition(tbefore.TotDiff_OE, tnew.Diff_OE);
                    tnew.TotDiff_CE = volumes.Addition(tbefore.TotDiff_CE, tnew.Diff_CE);
                    tnew.TimeDiffMin = (int)(tnew.Time - tbefore.Time);
                    tnew.LossGainRate_OE = volumes.GainLossTime(tnew.Diff_OE, tnew.TimeDiffMin);
                    tnew.LossGainRate_CE = volumes.GainLossTime(tnew.Diff_CE, tnew.TimeDiffMin);
                    Startup.sqlSlave.tripSheetModel.TripSheetData.Add(tnew);
                }
                else if (tnew.BDepth == 0)
                {
                    MessageBox.Show("Bit depth");
                }
                SaveToSql(row);
            }
            if (Startup.sqlSlave.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
            if (dgTripData.Items.Count > 0)
                dgTripData.ScrollIntoView(dgTripData.Items.GetItemAt(dgTripData.Items.Count - 1));
        }

        /// <summary>
        /// Save new row to database, or update edited item.
        /// </summary>
        private void SaveToSql(int row = -1, bool firstLineEdit = false)
        {
            List<TripSheetData> list_tripSheetData = New_TripSheetInput.OrderBy(des => des.Time).ToList();
            Startup.sqlSlave.tripSheetModel.SaveChanges();
            while (row != -1)
            {
                var id = list_tripSheetData[row].Id;
                var editItem = Startup.sqlSlave.tripSheetModel.TripSheetData.FirstOrDefault(item => item.Id == id);
                editItem.Time = list_tripSheetData[row].Time;
                editItem.BDepth = list_tripSheetData[row].BDepth;
                editItem.TripVolume = list_tripSheetData[row].TripVolume;
                editItem.EmptyFill = list_tripSheetData[row].EmptyFill;
                editItem.Displacement_OE = list_tripSheetData[row].Displacement_OE;
                editItem.Displacement_CE = list_tripSheetData[row].Displacement_CE;
                if (row == 0)
                {
                    // Set line 0 to be all zeroed due to start point.
                    CalculateTripData(editItem, null, false);
                    if (list_tripSheetData.Count > 1)
                    {
                        row++;
                        continue;
                    }
                }
                else
                {
                    TripSheetData tbefore = list_tripSheetData[row - 1];
                    CalculateTripData(editItem, tbefore);
                    CalculateTripData(list_tripSheetData[row], editItem, false);
                    if (row != list_tripSheetData.Count - 1)
                    {
                        for (int j = row; j < list_tripSheetData.Count; j++)
                        {
                            var idj = list_tripSheetData[j].Id;
                            var editItemj = Startup.sqlSlave.tripSheetModel.TripSheetData.First(item => item.Id == idj);
                            var beforeItem = list_tripSheetData[j - 1];
                            CalculateTripData(editItemj, beforeItem);
                            CalculateTripData(list_tripSheetData[j], editItemj, false);
                        }
                    }
                }
                row = -1;
            }
            try
            {
                // Commit data to SQLite database
                Startup.sqlSlave.tripSheetModel.SaveChanges();
            }
            catch (Exception ex)
            {
                string exem = ex.Message;
            }
            dgTripData.Items.Refresh();
        }

        // Volume calculations
        private void CalculateTripData(TripSheetData editItem, TripSheetData tbefore, bool docalc = true)
        {
            editItem.ActualVolume = docalc == true ? volumes.ActualVolume(editItem.TripVolume, tbefore.TripVolume, editItem.EmptyFill) : tbefore == null ? 0.00M : tbefore.ActualVolume;
            editItem.TheoreticalVol_OE = docalc == true ? volumes.TheoreticalVol(editItem.BDepth, tbefore.BDepth, (decimal)editItem.Displacement_OE) : tbefore == null ? 0.00M : tbefore.TheoreticalVol_OE;
            editItem.TheoreticalVol_CE = docalc == true ? volumes.TheoreticalVol(editItem.BDepth, tbefore.BDepth, (decimal)editItem.Displacement_CE) : tbefore == null ? 0.00M : tbefore.TheoreticalVol_CE;
            editItem.Diff_OE = docalc == true ? volumes.Subtract(editItem.ActualVolume, editItem.TheoreticalVol_OE) : tbefore == null ? 0.00M : tbefore.Diff_OE;
            editItem.Diff_CE = docalc == true ? volumes.Subtract(editItem.ActualVolume, editItem.TheoreticalVol_CE) : tbefore == null ? 0.00M : tbefore.Diff_CE;
            editItem.TotDiff_OE = docalc == true ? volumes.Addition(tbefore.TotDiff_OE, editItem.Diff_OE) : tbefore == null ? 0.00M : tbefore.TotDiff_OE;
            editItem.TotDiff_CE = docalc == true ? volumes.Addition(tbefore.TotDiff_CE, editItem.Diff_CE) : tbefore == null ? 0.00M : tbefore.TotDiff_CE;
            editItem.LossGainRate_OE = volumes.GainLossTime(editItem.Diff_OE, editItem.TimeDiffMin);
            editItem.LossGainRate_CE = volumes.GainLossTime(editItem.Diff_CE, editItem.TimeDiffMin);
        }

        // Call PlotTheData to create graphs in ScottPlot
        private void PlotTheData()
        {
            TripPlot.Plot.Clear();
            TripPlot.Plot.Title(tbTimeBased.IsChecked == false ? (RbCE.IsChecked == false ? "Depth OE" : "Depth CE") : (RbCE.IsChecked == false ? "Time OE" : "Time CE"));
            TripPlot.Plot.XAxis.Label(tbTimeBased.IsChecked == false ? "m³" : "");
            TripPlot.Plot.YLabel(tbTimeBased.IsChecked == false ? "Depth" : "m³");
            TripPlot.Plot.XAxis.DateTimeFormat(tbTimeBased.IsChecked == false ? false : true);
            // Intermediate data storage
            dataLX = new List<double[]>();
            dataLY = new List<double[]>();
            dataLXT = new List<DateTime>();

            // Add data from database model
            foreach (var data in Startup.sqlSlave.tripSheetModel.TripSheetData.Where(b => b.SheetId == SheetGuid).OrderBy(a => a.Time).ToList())
            {
                if (RbCE.IsChecked == true)
                {
                    dataLX.Add(new double[2] { data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0, data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0 });
                    dataLY.Add(new double[2] {
                        tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) : (data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0),
                        tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) : (data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0)
                    });
                    dataLXT.Add(DateTimeOffset.FromUnixTimeSeconds(data.Time).UtcDateTime.ToLocalTime());
                    continue;
                }
                dataLX.Add(new double[2] { data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0, data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0 });
                dataLY.Add(new double[2] {
                    tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) : (data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0),
                    tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) : (data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0)});
                dataLXT.Add(DateTimeOffset.FromUnixTimeSeconds(data.Time).UtcDateTime.ToLocalTime());
            }

            // Convert to double array, required by ScottPlot
            dataX1 = dataLX.Select(x => x[0]).ToArray();
            dataX2 = dataLX.Select(x => x[1]).ToArray();
            dataY1 = dataLY.Select(x => x[0]).ToArray();
            dataY2 = dataLY.Select(x => x[1]).ToArray();
            // Convert to OADate for ScottPlot
            dataXT = dataLXT.Select(x => x.ToOADate()).ToArray();
            // Set axis limits based on min/max of data
            double minX = dataX1.Min() < dataX2.Min() ? dataX1.Min() : dataX2.Min();
            double[] minmaxX = new double[] { dataX1.Min() < dataX2.Min() ? dataX1.Min() : dataX2.Min(), dataX1.Max() > dataX2.Max() ? dataX1.Max() : dataX2.Max() };
            double[] minmaxY = new double[] { dataY1.Min() < dataY2.Min() ? dataY1.Min() : dataY2.Min(), dataY1.Max() > dataY2.Max() ? dataY1.Max() : dataY2.Max() };

            // Plot as timebase or depthbase, and apply limits
            if (tbTimeBased.IsChecked == false)
            {
                TripPlot.Plot.AddVerticalLine(0, System.Drawing.Color.Black, 2, LineStyle.Solid);
                TripPlot.Plot.SetAxisLimits(new AxisLimits(minmaxX[0] - 5, minmaxX[1] + 5, minmaxY[0] - 20, minmaxY[1] + 20), 0, 0);
            }
            else if (Startup.sqlSlave.tripSheetModel.TripSheetData.Count() != 0)
            {
                TripPlot.Plot.AddHorizontalLine(0, System.Drawing.Color.Black, 1, LineStyle.Solid);
            }

            // MyScatterPlot is the plot used in the XAML component
            var Scatter1 = TripPlot.Plot.AddScatter(tbTimeBased.IsChecked == false ? dataX1 : dataXT, dataY1);
            Scatter1.XAxisIndex = 0;
            Scatter1.Color = primaryColorPlot;
            Scatter1.MarkerSize = 1;
            Scatter1.LineWidth = 2;
            var Scatter2 = TripPlot.Plot.AddScatter(tbTimeBased.IsChecked == false ? dataX2 : dataXT, dataY2);
            Scatter2.XAxisIndex = 0;
            Scatter2.Color = secondaryColorPlot;
            Scatter2.MarkerSize = 1;
            Scatter2.LineWidth = 1;
            Scatter2.LineStyle = LineStyle.DashDotDot;
            MyScatterPlot = new ScatterPlot[2] { Scatter1, Scatter2 };

            // HighlightedPoint is for mouse over
            for (int i = 0; i < HighlightedPoint.Count(); i++)
            {
                HighlightedPoint[i] = AddMarkerPoint(new int[] { 0, 0 }, colors[i], 6, MarkerShape.openCircle, false);
            }

            // Draw plot
            TripPlot.Refresh();
        }

        // For ScottPlot highlight.
        private MarkerPlot AddMarkerPoint(int[] xy, System.Drawing.Color color, int markerSize, MarkerShape shape, bool visible)
        {
            MarkerPlot marker = TripPlot.Plot.AddPoint(xy[0], xy[1]);
            marker.Color = color;
            marker.MarkerSize = markerSize;
            marker.MarkerShape = shape;
            marker.IsVisible = visible;
            return marker;
        }

        // ScottPlot mouse over
        private void TripPlot_MouseMove(object sender, MouseEventArgs e)
        {
            string PipeMode1 = RbCE.IsChecked == true ? "Closed Ended" : "Open Ended";
            string PipeMode2 = RbOE.IsChecked == false ? "Open Ended" : "Closed Ended";
            try
            {

                (double mouseCoordX, double mouseCoordY) = TripPlot.GetMouseCoordinates();
                double xyRatio = TripPlot.Plot.XAxis.Dims.PxPerUnit / TripPlot.Plot.YAxis.Dims.PxPerUnit;
                (double pointX1, double pointY1, int pointIndex1) = tbTimeBased.IsChecked == true ?
                    MyScatterPlot[0].GetPointNearestX(mouseCoordX) :
                    MyScatterPlot[0].GetPointNearestY(mouseCoordY);
                (double pointX2, double pointY2, int pointIndex2) = tbTimeBased.IsChecked == true ?
                    MyScatterPlot[1].GetPointNearestX(mouseCoordX) :
                    MyScatterPlot[1].GetPointNearestY(mouseCoordY);

                int[] pointIndex = { pointIndex1, pointIndex2 };
                double[] pointX = { pointX1, pointX2 };
                double[] pointY = { pointY1, pointY2 };
                for (int i = 0; i < HighlightedPoint.Count(); i++)
                {
                    HighlightedPoint[i].MarkerLineWidth = 2;
                    HighlightedPoint[i].X = pointX[i];
                    HighlightedPoint[i].Y = pointY[i];
                    HighlightedPoint[i].IsVisible = true;
                }

                // Render if the highlighted point changed
                for (int i = 0; i < LastHighlightedIndex.Count(); i++)
                {
                    if (LastHighlightedIndex[i] != pointIndex[i])
                    {
                        LastHighlightedIndex[i] = pointIndex[i];
                    }
                }
                TripPlot.Refresh();

                if (tbTimeBased.IsChecked == false)
                {
                    lbMouseover1.Text = $"{PipeMode1}: \t({Math.Round(pointX1, 2)}m³, {Math.Abs(Math.Round(pointY1, 2))}m MD)";
                    lbMouseover2.Text = $"{PipeMode2}: \t({Math.Round(pointX2, 2)}m³, {Math.Abs(Math.Round(pointY2, 2))}m MD)";
                }
                else
                {
                    lbMouseover1.Text = $"{PipeMode1}: \t({DateTime.FromOADate(pointX1)}, {Math.Round(pointY1, 2)}m³)";
                    lbMouseover2.Text = $"{PipeMode2}: \t({DateTime.FromOADate(pointX2)}, {Math.Round(pointY2, 2)}m³)";
                }
            }
            catch
            {

            }
        }

        public void NewLine(long time = 0,
            decimal? bdepth = 0,
            decimal? tripvol = 0,
            decimal? emptyfill = 0,
            decimal? dispOE = 0,
            decimal? dispCE = 0,
            string pipeid = "",
            bool insert = false)
        {
            if (!insert)
            {
                New_TripSheetInput.Add(new TripSheetData()
                {
                    Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    BDepth = Math.Round(Convert.ToDecimal(Startup.GetCDA.GetValue("BITDEP")), 2),
                    TripVolume = Math.Round(Convert.ToDecimal(Startup.GetCDA.GetValue("TRIPPVT")), 2),
                    EmptyFill = 0.00M
                });
                UpdateSheet();
                return;
            }
            New_TripSheetInput.Add(new TripSheetData()
            {
                Time = time,
                BDepth = bdepth,
                TripVolume = tripvol,
                EmptyFill = emptyfill,
                Displacement_CE = dispCE,
                Displacement_OE = dispOE,
                PipeId = pipeid
            });
            UpdateSheet(0, true);

        }

        // Add a new row to table, and run calculations on it.
        private void BtnNewLine_Click(object sender, RoutedEventArgs e)
        {
            NewLine();
        }

        private void BtnAddPipe_Click(object sender, RoutedEventArgs e)
        {
            AddPipeInfo addPipeInfo = new AddPipeInfo();
            addPipeInfo.ShowDialog();
            Load_PipeData();
        }

        private void BtnAddCsg_Click(object sender, RoutedEventArgs e)
        {
            AddCsgInfo addCsgInfo = new AddCsgInfo();
            addCsgInfo.ShowDialog();
            Load_CsgData();
        }

        // Context menu in table to delete row, which will delete it from SQL as well.
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            IList<DataGridCellInfo> selected = dgTripData.SelectedCells;
            var firstCell = dgTripData.Items.IndexOf(selected[0].Item) == dgTripData.Items.Count - 1 ? 0 : dgTripData.Items.IndexOf(selected[0].Item);
            List<TripSheetData> input = new List<TripSheetData>();
            if (selected.Count > 0)
            {
                foreach (DataGridCellInfo info in selected)
                {
                    if (!input.Contains((TripSheetData)info.Item))
                        input.Add((TripSheetData)info.Item);
                }
                foreach (TripSheetData output in input)
                {
                    Startup.sqlSlave.tripSheetModel.TripSheetData.Remove(Startup.sqlSlave.tripSheetModel.TripSheetData.First(a => a.Id == output.Id));
                }
                Startup.sqlSlave.tripSheetModel.SaveChanges();
                Load_TripData();
                UpdateSheet(firstCell == 0 ? 0 : firstCell - 1);
            }
        }

        // Context menu in table to insert a new line, a new Insert window will pop up.
        private void MenuInsert_Click(object sender, RoutedEventArgs e)
        {
            TripSheetData item = new TripSheetData();
            try
            {
                item = (TripSheetData)dgTripData.SelectedCells[0].Item;
            }
            catch
            {

            }

            InsertLine(item, (PipeData)cbPipe.SelectedItem, (CsgData)cbCsg.SelectedItem);
        }
        private void TbTimeBased_Checked(object sender, RoutedEventArgs e)
        {
            if (Startup.sqlSlave.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }

        // Can change what data to show as primary and in table.
        private void RbOpenEnded_Checked(object sender, RoutedEventArgs e)
        {
            ChangeColumns(Visibility.Visible, Visibility.Hidden, primaryColor, secondaryColor, "Open Ended", "Closed Ended");
            if (Startup.sqlSlave.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }
        private void RbCloseEnded_Checked(object sender, RoutedEventArgs e)
        {
            ChangeColumns(Visibility.Hidden, Visibility.Visible, secondaryColor, primaryColor, "Closed Ended", "Open Ended");
            if (Startup.sqlSlave.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }
        private void ChangeColumns(Visibility VisOpenEnded, Visibility VisClosedEnded, Brush ColOpenEnded, Brush ColClosedEnded, string primary, string secondary)
        {
            // Change dgTripData table to show Open Ended or Close Ended data by show/hide columns
            int[] arr = { 4, 7, 9, 11, 13 };
            for (int i = 4; i <= 14; i++)
            {
                if (arr.Contains(i))
                    dgTripData.Columns[i].Visibility = VisOpenEnded;
                else if (i != 6)
                    dgTripData.Columns[i].Visibility = VisClosedEnded;
            }
            // Change colors to match selected choice
            tbOE.BorderBrush = ColOpenEnded;
            tbCE.BorderBrush = ColClosedEnded;
            RbOE.Foreground = ColOpenEnded;
            RbCE.Foreground = ColClosedEnded;
            lbMouseover1.Foreground = primaryColor;
            lbMouseover2.Foreground = secondaryColor;
            lbMouseover1.Text = $"{primary}:";
            lbMouseover2.Text = $"{secondary}:";
        }

        private void RbPipe_Checked(object sender, RoutedEventArgs e)
        {
            CheckPipe((PipeData)cbPipe.SelectedItem);
        }

        private void CheckPipe(PipeData selectedPipe)
        {
            if (cbPipe.Items.Count > 0)
            {
                PipeGuid = selectedPipe.Id;
                tbOE.Text = selectedPipe.OEDisplacement.ToString();
                tbCE.Text = selectedPipe.CEDisplacement.ToString();
                btnNewLine.IsEnabled = true;
                return;
            }

            PipeGuid = "";
            tbOE.Text = "";
            tbCE.Text = "";
            btnNewLine.IsEnabled = false;
        }

        private void RbCsg_Checked(object sender, RoutedEventArgs e)
        {
            CheckCasing((CsgData)cbCsg.SelectedItem);
        }

        private void CheckCasing(CsgData selectedCsg)
        {
            if (cbCsg.Items.Count > 0)
            {
                PipeGuid = selectedCsg.Id;
                tbOE.Text = selectedCsg.OEDisplacement.ToString();
                tbCE.Text = selectedCsg.CEDisplacement.ToString();
                btnNewLine.IsEnabled = true;
                return;
            }

            PipeGuid = "";
            tbOE.Text = "";
            tbCE.Text = "";
            btnNewLine.IsEnabled = false;
        }

        private void DgTripData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            bool isAllowed = false;
            if (e.EditingElement is TextBox editingEle)
            {
                // Check if user entered a bit too many dots or commas and correct it, and replace commas with dots
                (e.EditingElement as TextBox).Text = Regex.Replace((e.EditingElement as TextBox).Text, @"\,+", ".");
                (e.EditingElement as TextBox).Text = Regex.Replace((e.EditingElement as TextBox).Text, @"\.+", ".");
                // Check input, only allow 0-9.,
                isAllowed = editingEle.Text.Trim(' ') == "" ? false :
                    editingEle.Text.Count(c => c == '.') > 1 ? false :
                    IsTextAllowed(editingEle.Text.Trim(' '), @"[^0-9.,]");
                // If input check fails, disable the possibility to add new line. input textbox will also be red, indicating wrong input.
                // Button will be enabled if input is OK again.
                btnNewLine.IsEnabled = isAllowed ? true : false;
            }
            if (Edit == true && isAllowed)
            {
                // Commit edit and udate sheet with new calculations
                // Try catch and edit flag due to CommitEdit triggers DataGridCellEditEndingEventArgs
                try
                {
                    cellInfo = dgTripData.SelectedCells[0];
                }
                catch { }
                cellRow = dgTripData.Items.IndexOf(cellInfo.Item);
                Edit = false;
                dgTripData.CommitEdit();
                dgTripData.CommitEdit();
                UpdateSheet(cellRow);
            }
            Edit = true;
        }

        // Create Insert window, send data needed to window and receive data when saved.
        private void InsertLine(TripSheetData data, PipeData pipeData, CsgData csgData)
        {
            Insert insert = new Insert(data, pipeData, csgData);
            insert.ShowDialog();
            if (insert.Saved == true)
            {
                NewLine(insert.Time,
                    insert.BitDepth,
                    insert.TripVolume,
                    insert.EmptyFill,
                    insert.Displacement_OE,
                    insert.Displacement_CE,
                    insert.PipeId,
                    true);
                Load_TripData();
            }
        }

        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MnuShutDown_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MnuAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void MnuHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Directory.GetCurrentDirectory() + "\\Help\\TripSheet.pdf");
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

    }
}
