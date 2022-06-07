using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelperLib
{
    public class CDAconn
    {
        private readonly string rootfolder;
        private readonly string filename = "CDAwrapper.dll";
        private Assembly dllAssembly;
        private readonly Type dllType;
        public dynamic dllInstance;

        private string GetDllLocation()
        {
            if (Directory.Exists(@"C:\BHI\Advantage\bin"))
            {
                return @"C:\BHI\Advantage\bin";
            }
            else if(Directory.Exists(@"D:\inteq\Advantage\bin"))
            {
                return @"D:\inteq\Advantage\bin";
            }
            return "";
        }

        public CDAconn()
        {
            rootfolder = GetDllLocation();
            if(rootfolder != "")
            {
                dllAssembly = Assembly.LoadFrom(Path.Combine(rootfolder, filename));
                dllType = dllAssembly.GetType("CDAwrapper.CDA", true);
                dllInstance = Activator.CreateInstance(dllType);
                dllInstance.SetAppName("Trip Sheet");
            }
        }

        public dynamic GetDllInstance()
        {
            return dllInstance;
        }

        public double GetValue(string mnemonic)
        {
            double value = 0;
            try
            {
                value = dllInstance.GetValue(mnemonic);
            }
            catch
            {
                value = -9999;
            }
            return value;
        }

        public string GetValueAsString(string mnemonic)
        {
            string value = "";
            try
            {
                value = dllInstance.GetValueAsString(mnemonic);
            }
            catch
            {
                value = "";
            }
            return value;
        }
    }
}
