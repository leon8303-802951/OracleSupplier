using LogMgr;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSupplier.Models
{
    public class Secrets
    {
        private LogOput log;
        public Secrets(LogOput log)
        {
            this.log = log;
            OracleSupplierIniFile = "OracleSupplier.txt";

            IniFolder = $"{AppDomain.CurrentDomain.BaseDirectory}ini/";
            IniFile = $"{IniFolder}{OracleSupplierIniFile}";

            MyKeys = new Dictionary<string, string>();

            LoadKey();
        }
        public string OracleSupplierIniFile { get; set; }
        public string IniFile { get; set; }
        public string IniFolder { get; set; }

        private Dictionary<string, string> MyKeys;

        public string GetValueOfKey(string key)
        {
            var rst = "";
            try
            {
                if (MyKeys.ContainsKey(key))
                {
                    rst = MyKeys[key];
                }
                else
                {
                    throw new Exception($"無此key:{key}");
                }
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"取得 MyKeys 裡的 Key 失敗:key={key}, Error:{ex.Message}");
                throw;
            }
            return rst;
        }


        public void LoadKey()
        {
            if (Directory.Exists(IniFolder))
            {
                if (File.Exists(IniFile))
                {
                    foreach (var line in File.ReadAllLines(IniFile))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var tmp1 = line.Split('=');
                            if (tmp1.Length > 1)
                            {
                                var key = tmp1[0];
                                var val = "";
                                for (int idx = 1; idx < tmp1.Length; idx++)
                                {
                                    val = $"{val}{tmp1[idx]}";
                                }
                                try
                                {
                                    MyKeys.Add(key, val);
                                }
                                catch (Exception ex)
                                {
                                    log.WriteErrorLog($"{IniFile} Load key 失敗:Key={key}, Error={ex.Message}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception($"找不到 {IniFile}");
                }
            }
            else
            {
                Directory.CreateDirectory(IniFolder);
                throw new Exception("找不到 OracleSupplier.txt");
            }
        }

    }
}
