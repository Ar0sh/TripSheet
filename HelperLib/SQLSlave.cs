using HelperLib.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLib
{
    public class SQLSlave
    {
        public TripSheetModel tripSheetModel;
        public List<TripSheetDetail> sheetList;

        public SQLSlave()
        {
            tripSheetModel = new TripSheetModel();
        }

        public void NewSheet(string name, string details, string wellID, string wellBoreID)
        {
            tripSheetModel.TripSheetDetail.Add(new TripSheetDetail()
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Details = details,
                Well = wellID,
                Wellbore = wellBoreID
            });
            tripSheetModel.SaveChanges();
        }

        private string Name;
        private string Sheet_ID
        {
            get
            {
                return tripSheetModel.TripSheetDetail.First(a => a.Name == Name).Id;
            }
        }

        public string GetSheetID(string name)
        {
            Name = name;
            return Sheet_ID;
        }

        public void DeleteSheet(string Id)
        {
            var dataList = tripSheetModel.TripSheetData.Where(a => a.SheetId == Id).ToList();
            if (dataList.Count > 0)
                tripSheetModel.TripSheetData.RemoveRange(dataList);
            tripSheetModel.TripSheetDetail.Remove(tripSheetModel.TripSheetDetail.First(a => a.Id == Id));
            tripSheetModel.SaveChanges();
        }

        public List<TripSheetDetail> LoadSheets()
        {
            return tripSheetModel.TripSheetDetail.OrderBy(a => a.Name).ToList();
        }


        private List<PipeData> exportPipe;
        private List<CsgData> exportCsg;

        public delegate void teetet(int abd);

        public void RestoreBlankDB(string fullPath = "", bool backup = true)
        {
            if (fullPath == "")
                ExportPipe();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            var directory = Directory.GetCurrentDirectory();
            if (backup)
                File.Move(directory + "\\Database\\TripSheet.sqlite", directory + "\\Database\\TripSheet_" + DateTime.Now.ToString("ddMMMyy_HHmmss") + ".sqlite");
            if (fullPath == "")
            {
                File.Copy(directory + "\\Database\\TripSheet_BlankDB.sqlite", directory + "\\Database\\TripSheet.sqlite", true);
                ImportPipe();
            }
            else
            {
                File.Copy(fullPath, directory + "\\Database\\TripSheet.sqlite", true);
            }
        }

        private void ImportPipe()
        {
            foreach (var pipedata in exportPipe)
            {
                tripSheetModel.PipeData.Add(pipedata);
            }
            foreach (var csgdata in exportCsg)
            {
                tripSheetModel.CsgData.Add(csgdata);
            }
            exportCsg = null;
            exportCsg = null;
            tripSheetModel.SaveChanges();
        }

        private void ExportPipe()
        {
            exportPipe = tripSheetModel.PipeData.OrderBy(a => a.Name).ToList();
            exportCsg = tripSheetModel.CsgData.OrderBy(a => a.Name).ToList();
        }
    }
}
