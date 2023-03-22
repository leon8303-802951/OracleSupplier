using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSupplier.Models.Supplier
{
    public class SupplierSame
    {
        public SupplierSame()
        {
            ADDRESS_LINE_1 = "台北市大安區敦化南路段105號12樓"; //1個supplier地址必須要都一樣才行
        }
        //銀行代碼
        public string VD_BK_CODE { get; set; }    

        //分行代碼
        public string VD_BKB_CODE { get; set; }    

        //批號
        public string BATCHID { get; set; }    
        //供應商地址，都要寫公司地址
        public string ADDRESS_LINE_1 { get; set; }    
        //員工編號
        public string ADDRESS_NAME { get; set; }    


    }
}
