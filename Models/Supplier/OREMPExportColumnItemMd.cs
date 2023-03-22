using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSupplier.Models.Supplier
{
    public class OREMPExportColumnItemMd
    {
        public OREMPExportColumnItemMd(string Key, string Mapping, int SortIndex, bool IsRequired = false, string DefaultValue = "")
        {
            this.Key = Key;
            this.Mapping = Mapping;
            this.SortIndex = SortIndex;
            this.IsRequired = IsRequired;
            this.DefaultValue = DefaultValue;
            if (string.IsNullOrWhiteSpace(DefaultValue) == false)
                this.Value = DefaultValue;
        }
        /// <summary>
        /// Excel Column
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// C# Mapping
        /// </summary>
        public string Mapping { get; set; }

        /// <summary>
        /// 順序 > CSV 欄位順序
        /// </summary>
        public int SortIndex { get; set; }


        /// <summary>
        /// 是否輸出
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 預設值
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }
    }

}
