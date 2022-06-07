using HelperLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLib
{
    public class SQLSlave
    {
        public TripSheetModel tripSheetModel;

        public SQLSlave()
        {
            tripSheetModel = new TripSheetModel();
            List<TripSheetData> tripSheetDatas = tripSheetModel.TripSheetData.OrderBy(a => a.Time).ToList();
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
    }
}
