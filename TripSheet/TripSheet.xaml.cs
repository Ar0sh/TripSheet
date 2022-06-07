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
using TripSheet_SQLite.Model;
//using HelperLib.Model;
using Calculations;
using System.Windows.Media;
using ScottPlot;

namespace TripSheet_SQLite
{
    public partial class TripSheet : Window
    {
        public static TripSheetModel tripSheetModel = new TripSheetModel();
        // Data context
        //public static string connectionString;
        //public static TripSheetModel tripSheetModel;
        public static bool saveStartup = false;
        List<PipeData> pipeList;
        List<CsgData> csgList;
        string SheetGuid = "";
        string PipeGuid = "";
        Brush primaryColor = Brushes.Green;
        Brush secondaryColor = Brushes.Red;
        System.Drawing.Color primaryColorPlot = System.Drawing.Color.Green;
        System.Drawing.Color secondaryColorPlot = System.Drawing.Color.Red;
        // X, Y, XT(Time X axis) data sets
        List<double> dataLX1 = new List<double>();
        List<double> dataLX2 = new List<double>();
        List<double> dataLY1 = new List<double>();
        List<double> dataLY2 = new List<double>();
        List<DateTime> dataLXT = new List<DateTime>();
        // Variables used to make points
        double[] dataX1;
        double[] dataX2;
        double[] dataY1;
        double[] dataY2;
        double[] dataXT;
        // ScottPlot variables and Highligths
        private MarkerPlot[] HighlightedPoint = new MarkerPlot[2];
        System.Drawing.Color[] colors = new System.Drawing.Color[] { System.Drawing.Color.Black, System.Drawing.Color.Orange };
        private int[] LastHighlightedIndex = { -1, -1 };
        private ScatterPlot MyScatterPlot1;
        private ScatterPlot MyScatterPlot2;
        ObservableCollection<TripSheetData> _New_TripSheetInput = new ObservableCollection<TripSheetData>();
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
            SheetGuid = guid;
            InitializeComponent();
            Title = "TripSheet " + "| " + titleName;
            TripPlot.Configuration.DoubleClickBenchmark = false;
            TripPlot.RightClicked -= TripPlot.DefaultRightClickEvent;
            TripPlot.RightClicked += CustomRightClickMenu;
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

        private void OpenPlotWindow(object sender, RoutedEventArgs e)
        {
            WpfPlotViewer wpfPlotViewer = new WpfPlotViewer(TripPlot.Plot)
            {
                Title = "Depth Based Plot"
            };
            wpfPlotViewer.Show();
        }

        private void ZoomToFit(object sender, RoutedEventArgs e)
        {
            TripPlot.Plot.AxisAuto();
            TripPlot.Refresh();
        }

        private void Load_PipeData()
        {
            pipeList = Startup.tripSheetModel.PipeData.OrderBy(a => a.Name).ToList();
            cbPipe.ItemsSource = pipeList;
        }

        private void Load_CsgData()
        {
            csgList = Startup.tripSheetModel.CsgData.OrderBy(a => a.Name).ToList();
            cbCsg.ItemsSource = csgList;
        }

        private void CbPipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PipeData selectedPipe = (PipeData)(sender as ComboBox).SelectedItem;
            if(RbPipe.IsChecked == true)
            {
                CheckPipe(selectedPipe);
            }
        }

        private void CbCsg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CsgData selectedCsg = (CsgData)(sender as ComboBox).SelectedItem;
            if(RbCsg.IsChecked == true)
            {
                CheckCasing(selectedCsg);
            }
        }

        private void Load_TripData()
        {
            New_TripSheetInput.Clear();
            List<TripSheetData> tripSheetDatas = Startup.tripSheetModel.TripSheetData.Where(b => b.SheetId == SheetGuid).OrderBy(a => a.Time).ToList();
            if (tripSheetDatas.Count == 0 && tbCE.Text != "")
            {
                TripSheetData zeroItem = new TripSheetData();
                EnterTripData(zeroItem, null, true);
                Startup.tripSheetModel.TripSheetData.Add(zeroItem);
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
                pipeList = Startup.tripSheetModel.PipeData.OrderBy(a => a.Name).ToList();
                var testing = tripSheetDatas.Last().PipeId;
                var pipeDet = Startup.tripSheetModel.PipeData.FirstOrDefault(a => a.Id == testing);
                var index = cbPipe.Items.IndexOf(pipeDet);
                cbPipe.SelectedIndex = index != -1 ? index : 0;
            }

            dgTripData.DataContext = this;
            if (Startup.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }

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

        private void UpdateSheet()
        {
            dgTripData.CommitEdit();
            dgTripData.CommitEdit();
            int k = dgTripData.Items.Count;
            //var test = dgTripData.Items[k - 1];
            TripSheetData tnew = (TripSheetData)dgTripData.Items[k - 1];
            TripSheetData tbefore = k != 1 ? (TripSheetData)dgTripData.Items[k - 2] : null;
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
                    Startup.tripSheetModel.TripSheetData.Add(tnew);
                }
                if (tnew.Id == null && tnew.TripVolume != null && tnew.BDepth != 0 && tbefore != null)
                {
                    tnew.Id = Guid.NewGuid().ToString();
                    tnew.SheetId = SheetGuid;
                    tnew.PipeId = PipeGuid;
                    tnew.Displacement_OE = Convert.ToDecimal(tbOE.Text);
                    tnew.Displacement_CE = Convert.ToDecimal(tbCE.Text);
                    tnew.ActualVolume = Volumes.ActualVolume(tnew.TripVolume, tbefore.TripVolume, tnew.EmptyFill);
                    tnew.TheoreticalVol_OE = Volumes.TheoreticalVol(tnew.BDepth, tbefore.BDepth, Convert.ToDecimal(tbOE.Text));
                    tnew.TheoreticalVol_CE = Volumes.TheoreticalVol(tnew.BDepth, tbefore.BDepth, Convert.ToDecimal(tbCE.Text));
                    tnew.Diff_OE = tnew.ActualVolume - tnew.TheoreticalVol_OE;
                    tnew.Diff_CE = tnew.ActualVolume - tnew.TheoreticalVol_CE;
                    tnew.TotDiff_OE = tbefore.TotDiff_OE + tnew.Diff_OE;
                    tnew.TotDiff_CE = tbefore.TotDiff_CE + tnew.Diff_CE;
                    tnew.TimeDiffMin = (int)(tnew.Time - tbefore.Time);
                    tnew.LossGainRate_OE = (tnew.Diff_OE / tnew.TimeDiffMin) * 60 * 60;
                    tnew.LossGainRate_CE = (tnew.Diff_CE / tnew.TimeDiffMin) * 60 * 60;
                    Startup.tripSheetModel.TripSheetData.Add(tnew);
                }
                else if (tnew.BDepth == 0)
                {
                    MessageBox.Show("Bit depth");
                }
                SaveToSql();
            }
            if (Startup.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
            if(dgTripData.Items.Count > 0)
                dgTripData.ScrollIntoView(dgTripData.Items.GetItemAt(dgTripData.Items.Count - 1));
        }

        private void SaveToSql()
        {
            bool firstLineEdit = false;
            List<TripSheetData> list_tripSheetData = New_TripSheetInput.OrderBy(des => des.Time).ToList();
            for (int i = 0; i < list_tripSheetData.Count; i++)
            {
                var id = list_tripSheetData[i].Id;
                var editItem = Startup.tripSheetModel.TripSheetData.FirstOrDefault(item => item.Id == id);
                if (list_tripSheetData[i].Id != "" && list_tripSheetData[i].Id != null && editItem != null)
                {
                    if (list_tripSheetData[i].TripVolume != null &&
                        (list_tripSheetData[i].TripVolume != editItem.TripVolume ||
                        list_tripSheetData[i].EmptyFill != editItem.EmptyFill ||
                        list_tripSheetData[i].BDepth != editItem.BDepth ||
                        list_tripSheetData[i].Time != editItem.Time ||
                        list_tripSheetData[i].Displacement_OE != editItem.Displacement_OE ||
                        list_tripSheetData[i].Displacement_CE != editItem.Displacement_CE ||
                        firstLineEdit))
                    {
                        editItem.Time = list_tripSheetData[i].Time;
                        editItem.BDepth = list_tripSheetData[i].BDepth;
                        editItem.TripVolume = list_tripSheetData[i].TripVolume;
                        editItem.EmptyFill = list_tripSheetData[i].EmptyFill;
                        editItem.Displacement_OE = list_tripSheetData[i].Displacement_OE;
                        editItem.Displacement_CE = list_tripSheetData[i].Displacement_CE;
                        if (i == 0)
                        {
                            // Set line 0 to be all zeroes due to start point.
                            CalculateTripData(editItem, null, false);
                            firstLineEdit = true;
                            // Skip rest of for loop and go to next iteration
                            continue;
                        }

                        TripSheetData tbefore = list_tripSheetData[i - 1];
                        CalculateTripData(editItem, tbefore);
                        CalculateTripData(list_tripSheetData[i], editItem, false);
                        if (i != list_tripSheetData.Count - 1)
                        {
                            for (int j = i + 1; j < list_tripSheetData.Count; j++)
                            {
                                var idj = list_tripSheetData[j].Id;
                                var idb = list_tripSheetData[j - 1].Id;
                                var editItemj = Startup.tripSheetModel.TripSheetData.First(item => item.Id == idj);
                                var beforeItem = Startup.tripSheetModel.TripSheetData.First(item => item.Id == idb);
                                CalculateTripData(editItemj, beforeItem);
                            }
                        }
                    }
                }
            }
            try
            {
                Startup.tripSheetModel.SaveChanges();
            }
            catch (Exception ex)
            {
                string exem = ex.Message;
            }
            Load_TripData();
        }

        private static void CalculateTripData(TripSheetData editItem, TripSheetData tbefore, bool docalc = true)
        {
            editItem.ActualVolume = docalc == true ? Volumes.ActualVolume(editItem.TripVolume, tbefore.TripVolume, editItem.EmptyFill) : tbefore == null ? 0.00M : tbefore.ActualVolume;
            editItem.TheoreticalVol_OE = docalc == true ? Volumes.TheoreticalVol(editItem.BDepth, tbefore.BDepth, (decimal)editItem.Displacement_OE) : tbefore == null ? 0.00M : tbefore.TheoreticalVol_OE;
            editItem.TheoreticalVol_CE = docalc == true ? Volumes.TheoreticalVol(editItem.BDepth, tbefore.BDepth, (decimal)editItem.Displacement_CE) : tbefore == null ? 0.00M : tbefore.TheoreticalVol_CE;
            editItem.Diff_OE = docalc == true ? editItem.ActualVolume - editItem.TheoreticalVol_OE : tbefore == null ? 0.00M : tbefore.Diff_OE;
            editItem.Diff_CE = docalc == true ? editItem.ActualVolume - editItem.TheoreticalVol_CE : tbefore == null ? 0.00M : tbefore.Diff_CE;
            editItem.TotDiff_OE = docalc == true ? tbefore.TotDiff_OE + editItem.Diff_OE : tbefore == null ? 0.00M : tbefore.TotDiff_OE;
            editItem.TotDiff_CE = docalc == true ? tbefore.TotDiff_CE + editItem.Diff_CE : tbefore == null ? 0.00M : tbefore.TotDiff_CE;
            editItem.LossGainRate_OE = (editItem.Diff_OE / editItem.TimeDiffMin) * 60 * 60;
            editItem.LossGainRate_CE = (editItem.Diff_CE / editItem.TimeDiffMin) * 60 * 60;
        }

        // Call PlotTheData to create graphs in ScottPlot
        private void PlotTheData()
        {
            TripPlot.Plot.Clear();
            TripPlot.Plot.Title(tbTimeBased.IsChecked == false ? (RbCE.IsChecked == false ? "Depth OE" : "Depth CE") : (RbCE.IsChecked == false ? "Time OE" : "Time CE"));
            TripPlot.Plot.XAxis.Label(tbTimeBased.IsChecked == false ? "m³" : "");
            TripPlot.Plot.YLabel(tbTimeBased.IsChecked == false ? "Depth" : "m³");
            TripPlot.Plot.XAxis.DateTimeFormat(tbTimeBased.IsChecked == false ? false : true);

            // Intermediate datastorage
            dataLX1 = new List<double>();
            dataLX2 = new List<double>();
            dataLY1 = new List<double>();
            dataLY2 = new List<double>();
            dataLXT = new List<DateTime>();

            // Add data from database model
            foreach (var data in Startup.tripSheetModel.TripSheetData.Where(b => b.SheetId == SheetGuid).OrderBy(a => a.Time).ToList())
            {
                if (RbCE.IsChecked == true)
                {
                    dataLX1.Add(data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0);
                    dataLX2.Add(data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0);
                    dataLY1.Add(tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) :
                        (data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0));
                    dataLY2.Add(tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) :
                        (data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0));
                    dataLXT.Add(DateTimeOffset.FromUnixTimeSeconds(data.Time).UtcDateTime.ToLocalTime());
                    continue;
                }
                dataLX1.Add(data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0);
                dataLX2.Add(data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0);
                dataLY1.Add(tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) :
                    (data.TotDiff_OE != null ? (double)data.TotDiff_OE : 0));
                dataLY2.Add(tbTimeBased.IsChecked == false ? (data.BDepth != null ? -(double)data.BDepth : 0) :
                    (data.TotDiff_CE != null ? (double)data.TotDiff_CE : 0));
                dataLXT.Add(DateTimeOffset.FromUnixTimeSeconds(data.Time).UtcDateTime.ToLocalTime());
            }

            // Convert to double array, required by ScottPlot
            dataX1 = dataLX1.ToArray();
            dataX2 = dataLX2.ToArray();
            dataY1 = dataLY1.ToArray();
            dataY2 = dataLY2.ToArray();
            // Convert to OADate for ScottPlot
            dataXT = dataLXT.Select(x => x.ToOADate()).ToArray();
            // Set axis limits based on min/max of data
            double minX = dataX1.Min() < dataX2.Min() ? dataX1.Min() : dataX2.Min();
            double[] minmaxX = new double[] { dataX1.Min() < dataX2.Min() ? dataX1.Min() : dataX2.Min(), dataX1.Max() > dataX2.Max() ? dataX1.Max() : dataX2.Max() };
            double[] minmaxY = new double[] { dataY1.Min() < dataY2.Min() ? dataY1.Min() : dataY2.Min(), dataY1.Max() > dataY2.Max() ? dataY1.Max() : dataY2.Max() };

            // Plot as timebased or depthbased, and apply limits
            if (tbTimeBased.IsChecked == false)
            {
                TripPlot.Plot.AddVerticalLine(0, System.Drawing.Color.Black, 2, LineStyle.Solid);
                TripPlot.Plot.SetAxisLimits(new AxisLimits(minmaxX[0] - 5, minmaxX[1] + 5, minmaxY[0] - 20, minmaxY[1] + 20), 0, 0);
            }
            else if (Startup.tripSheetModel.TripSheetData.Count() != 0)
            {
                TripPlot.Plot.AddHorizontalLine(0, System.Drawing.Color.Black, 1, LineStyle.Solid);
            }

            // MyScatterPlot is the plot used in the XAML component
            var x2 = TripPlot.Plot.AddScatter(tbTimeBased.IsChecked == false ? dataX2 : dataXT, dataY2);
            x2.XAxisIndex = 0;
            x2.Color = secondaryColorPlot;
            x2.MarkerSize = 1;
            x2.LineWidth = 1;
            x2.LineStyle = LineStyle.DashDotDot;
            var x1 = TripPlot.Plot.AddScatter(tbTimeBased.IsChecked == false ? dataX1 : dataXT, dataY1);
            x1.XAxisIndex = 0;
            x1.Color = primaryColorPlot;
            x1.MarkerSize = 1;
            x1.LineWidth = 2;
            MyScatterPlot1 = x1;
            MyScatterPlot2 = x2;

            // HighlightedPoint is for mouseover
            for (int i = 0; i < HighlightedPoint.Count(); i++)
            {
                HighlightedPoint[i] = AddMarkerPoint(new int[] { 0, 0 }, colors[i], 6, MarkerShape.openCircle, false);
            }

            // Draw plot
            TripPlot.Refresh();
        }// Mouseover method

        private MarkerPlot AddMarkerPoint(int[] xy, System.Drawing.Color color, int markerSize, MarkerShape shape, bool visible)
        {
            MarkerPlot marker = new MarkerPlot();
            marker = TripPlot.Plot.AddPoint(xy[0], xy[1]);
            marker.Color = color;
            marker.MarkerSize = markerSize;
            marker.MarkerShape = shape;
            marker.IsVisible = visible;
            return marker;
        }

        private void TripPlot_MouseMove(object sender, MouseEventArgs e)
        {
            string PipeMode1 = RbCE.IsChecked == true ? "Closed Ended" : "Open Ended";
            string PipeMode2 = RbOE.IsChecked == false ? "Open Ended" : "Closed Ended";
            try
            {

               (double mouseCoordX, double mouseCoordY) = TripPlot.GetMouseCoordinates();
                double xyRatio = TripPlot.Plot.XAxis.Dims.PxPerUnit / TripPlot.Plot.YAxis.Dims.PxPerUnit;
                (double pointX1, double pointY1, int pointIndex1) = MyScatterPlot1.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);
                (double pointX2, double pointY2, int pointIndex2) = MyScatterPlot2.GetPointNearest(mouseCoordX, mouseCoordY, xyRatio);

                int[] pointIndex = { pointIndex1, pointIndex2 };
                double[] pointX = { pointX1, pointX2 };
                double[] pointY = { pointY1, pointY2 };
                for(int i = 0; i < HighlightedPoint.Count(); i++)
                {
                    HighlightedPoint[i].X = pointX[i];
                    HighlightedPoint[i].Y = pointY[i];
                    HighlightedPoint[i].IsVisible = true;
                }

                // Render if the highlighted point changed
                for(int i = 0; i < LastHighlightedIndex.Count(); i++)
                {
                    if (LastHighlightedIndex[i] != pointIndex[i])
                    {
                        LastHighlightedIndex[i] = pointIndex[i];
                    }
                }
                TripPlot.Refresh();

                // Render if the highlighted point changed
                //if (LastHighlightedIndex2 != pointIndex2)
                //{
                //    LastHighlightedIndex2 = pointIndex2;
                //    TripPlot.Refresh();
                //}

                if (tbTimeBased.IsChecked == false)
                {
                    lbMouseover1.Content = $"{PipeMode1}: \t({Math.Round(pointX1, 2)}m³, {Math.Round(pointY1, 2)}m MD)";
                    lbMouseover2.Content = $"{PipeMode2}: \t({Math.Round(pointX2, 2)}m³, {Math.Round(pointY2, 2)}m MD)";
                }
                else
                {
                    lbMouseover1.Content = $"{PipeMode1}: \t({DateTime.FromOADate(pointX1)}, {Math.Round(pointY1, 2)}m³)";
                    lbMouseover2.Content = $"{PipeMode2}: \t({DateTime.FromOADate(pointX2)}, {Math.Round(pointY2, 2)}m³)";
                }
            }
            catch
            {

            }
        }

        private void DgTripData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key.Equals(Key.Enter)) || (e.Key.Equals(Key.Return)))
            {
                UpdateSheet();
            }
        }

        private void DgTripData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void BtnNewLine_Click(object sender, RoutedEventArgs e)
        {
            New_TripSheetInput.Add(new TripSheetData()
            {
                Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                BDepth = Math.Round(Convert.ToDecimal(Startup.GetCDA.GetValue("BITDEP")), 2),
                TripVolume = Math.Round(Convert.ToDecimal(Startup.GetCDA.GetValue("TRIPPVT")), 2),
                EmptyFill = 0.00M
            });
            UpdateSheet();
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


        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            IList<DataGridCellInfo> selected = dgTripData.SelectedCells;
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
                    Startup.tripSheetModel.TripSheetData.Remove(Startup.tripSheetModel.TripSheetData.First(a => a.Id == output.Id));
                }
                Startup.tripSheetModel.SaveChanges();
                Load_TripData();
            }
        }

        private void TbTimeBased_Checked(object sender, RoutedEventArgs e)
        {
            if (Startup.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }

        private void RbOpenEnded_Checked(object sender, RoutedEventArgs e)
        {
            ChangeColumns(Visibility.Visible, Visibility.Hidden, primaryColor, secondaryColor, "Open Ended", "Closed Ended");
            if (Startup.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }
        private void RbCloseEnded_Checked(object sender, RoutedEventArgs e)
        {
            ChangeColumns(Visibility.Hidden, Visibility.Visible, secondaryColor, primaryColor, "Closed Ended", "Open Ended");
            if (Startup.tripSheetModel.TripSheetData.Count() != 0)
            {
                PlotTheData();
            }
        }
        private void ChangeColumns(Visibility VisOpenEnded, Visibility VisClosedEnded, Brush ColOpenEnded, Brush ColClosedEnded, string primary, string secondary)
        {
            int[] arr = { 4, 7, 9, 11, 13 };
            for (int i = 4; i <= 14; i++)
            {
                if (arr.Contains(i))
                    dgTripData.Columns[i].Visibility = VisOpenEnded;
                else if (i != 6)
                    dgTripData.Columns[i].Visibility = VisClosedEnded;
            }
            tbOE.BorderBrush = ColOpenEnded;
            tbCE.BorderBrush = ColClosedEnded;
            RbOE.Foreground = ColOpenEnded;
            RbCE.Foreground = ColClosedEnded;
            lbMouseover1.Foreground = primaryColor;
            lbMouseover2.Foreground = secondaryColor;
            lbMouseover1.Content = $"{primary}:";
            lbMouseover2.Content = $"{secondary}:";
        }

        private void RbPipe_Checked(object sender, RoutedEventArgs e)
        {
            PipeData selectedPipe = (PipeData)cbPipe.SelectedItem;
            CheckPipe(selectedPipe);
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
            CsgData selectedCsg = (CsgData)cbCsg.SelectedItem;
            CheckCasing(selectedCsg);
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
    }
}
