using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TripSheet_SQLite
{
    class DBManager
    {
        private List<HelperLib.Model.PipeData> exportPipe;
        private List<HelperLib.Model.CsgData> exportCsg;

        public DBManager()
        {

        }

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
                Startup.sqlSlave.tripSheetModel.PipeData.Add(pipedata);
            }
            foreach (var csgdata in exportCsg)
            {
                Startup.sqlSlave.tripSheetModel.CsgData.Add(csgdata);
            }
            exportCsg = null;
            exportCsg = null;
            Startup.sqlSlave.tripSheetModel.SaveChanges();
        }

        private void ExportPipe()
        {
            exportPipe = Startup.sqlSlave.tripSheetModel.PipeData.OrderBy(a => a.Name).ToList();
            exportCsg = Startup.sqlSlave.tripSheetModel.CsgData.OrderBy(a => a.Name).ToList();
        }
    }
}
