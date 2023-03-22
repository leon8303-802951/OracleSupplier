using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSupplier.Models.Supplier
{
    public class OREMPOracleFileSchemaDefineItemMd
    {

        public OREMPOracleFileSchemaDefineItemMd()
        //public OREMPOracleFileSchemaDefineItemMd(List<OREMPExportColumnItemMd> ExportColumnData)
        {
            //初始化
            DefLst = new List<OREMPExportColumnItemMd>();
            DefLst.Add(new OREMPExportColumnItemMd("Batch ID", "", -1, false, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Import Action *", "IMPORT_ACTION", 0, true, "CREATE"));
            DefLst.Add(new OREMPExportColumnItemMd("Supplier Name*", "SUPPLIER_NAME", 1, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Supplier Name New", "SUPPLIER_NAME_NEW", 2, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Supplier Number", "SUPPLIER_NUMBER", 3, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Alternate Name", "ALTERNATE_NAME", 4, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Tax Organization Type", "TAX_ORGANIZATION_TYPE", 5, true, "CORPORATION"));
            DefLst.Add(new OREMPExportColumnItemMd("Supplier Type", "SUPPLIER_TYPE", 6, true, "Supplier"));
            DefLst.Add(new OREMPExportColumnItemMd("Inactive Date", "INACTIVE_DATE", 7, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Business Relationship*", "BUSINESS_RELATIONSHIP", 8, true, "SPEND_AUTHORIZED"));
            DefLst.Add(new OREMPExportColumnItemMd("Parent Supplier", "PARENT_SUPPLIER", 9, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Alias", "ALIAS", 10, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("D-U-N-S Number", "D_U_N_S_NUMBER", 11, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("One-time supplier", "ONE_TIME_SUPPLIER", 12, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Customer Number", "CUSTOMER_NUMBER", 13, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("SIC", "SIC", 14, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("National Insurance Number", "NATIONAL_INSURANCE_NUMBER", 15, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Corporate Web Site", "CORPORATE_WEB_SITE", 16, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Chief Executive Title", "CHIEF_EXECUTIVE_TITLE", 17, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Chief Executive Name", "CHIEF_EXECUTIVE_NAME", 18, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Business Classifications Not Applicable", "BUSINESS_CLASSIFICATIONS_NOT_APPLICABLE", 19, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Taxpayer Country", "TAXPAYER_COUNTRY", 20, true, "TW"));
            DefLst.Add(new OREMPExportColumnItemMd("Taxpayer ID", "TAXPAYER_ID", 21, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Federal reportable", "FEDERAL_REPORTABLE", 22, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Federal Income Tax Type", "FEDERAL_INCOME_TAX_TYPE", 23, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("State reportable", "STATE_REPORTABLE", 24, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Tax Reporting Name", "TAX_REPORTING_NAME", 25, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Name Control", "NAME_CONTROL", 26, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Tax Verification Date", "TAX_VERIFICATION_DATE", 27, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Use withholding tax", "USE_WITHHOLDING_TAX", 28, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Withholding Tax Group", "WITHHOLDING_TAX_GROUP", 29, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Vat Code", "VAT_CODE", 30, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Tax Registration Number", "TAX_REGISTRATION_NUMBER", 31, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Auto Tax Calc Override", "AUTO_TAX_CALC_OVERRIDE", 32, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Method", "PAYMENT_METHOD", 33, true, "EFT"));
            DefLst.Add(new OREMPExportColumnItemMd("Delivery Channel", "DELIVERY_CHANNEL", 34, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Bank Instruction 1", "BANK_INSTRUCTION_1", 35, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Bank Instruction 2", "BANK_INSTRUCTION_2", 36, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Bank Instruction", "BANK_INSTRUCTION", 37, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Settlement Priority", "SETTLEMENT_PRIORITY", 38, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Text Message 1", "PAYMENT_TEXT_MESSAGE_1", 39, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Text Message 2", "PAYMENT_TEXT_MESSAGE_2", 40, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Text Message 3", "PAYMENT_TEXT_MESSAGE_3", 41, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Bank Charge Bearer", "BANK_CHARGE_BEARER", 42, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Reason", "PAYMENT_REASON", 43, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Reason Comments", "PAYMENT_REASON_COMMENTS", 44, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Payment Format", "PAYMENT_FORMAT", 45, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_CATEGORY", "ATTRIBUTE_CATEGORY", 46, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE1", "ATTRIBUTE1", 47, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE2", "ATTRIBUTE2", 48, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE3", "ATTRIBUTE3", 49, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE4", "ATTRIBUTE4", 50, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE5", "ATTRIBUTE5", 51, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE6", "ATTRIBUTE6", 52, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE7", "ATTRIBUTE7", 53, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE8", "ATTRIBUTE8", 54, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE9", "ATTRIBUTE9", 55, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE10", "ATTRIBUTE10", 56, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE11", "ATTRIBUTE11", 57, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE12", "ATTRIBUTE12", 58, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE13", "ATTRIBUTE13", 59, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE14", "ATTRIBUTE14", 60, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE15", "ATTRIBUTE15", 61, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE16", "ATTRIBUTE16", 62, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE17", "ATTRIBUTE17", 63, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE18", "ATTRIBUTE18", 64, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE19", "ATTRIBUTE19", 65, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE20", "ATTRIBUTE20", 66, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE1", "ATTRIBUTE_DATE1", 67, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE2", "ATTRIBUTE_DATE2", 68, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE3", "ATTRIBUTE_DATE3", 69, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE4", "ATTRIBUTE_DATE4", 70, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE5", "ATTRIBUTE_DATE5", 71, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE6", "ATTRIBUTE_DATE6", 72, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE7", "ATTRIBUTE_DATE7", 73, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE8", "ATTRIBUTE_DATE8", 74, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE9", "ATTRIBUTE_DATE9", 75, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_DATE10", "ATTRIBUTE_DATE10", 76, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP1", "ATTRIBUTE_TIMESTAMP1", 77, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP2", "ATTRIBUTE_TIMESTAMP2", 78, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP3", "ATTRIBUTE_TIMESTAMP3", 79, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP4", "ATTRIBUTE_TIMESTAMP4", 80, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP5", "ATTRIBUTE_TIMESTAMP5", 81, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP6", "ATTRIBUTE_TIMESTAMP6", 82, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP7", "ATTRIBUTE_TIMESTAMP7", 83, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP8", "ATTRIBUTE_TIMESTAMP8", 84, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP9", "ATTRIBUTE_TIMESTAMP9", 85, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_TIMESTAMP10", "ATTRIBUTE_TIMESTAMP10", 86, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER1", "ATTRIBUTE_NUMBER1", 87, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER2", "ATTRIBUTE_NUMBER2", 88, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER3", "ATTRIBUTE_NUMBER3", 89, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER4", "ATTRIBUTE_NUMBER4", 90, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER5", "ATTRIBUTE_NUMBER5", 91, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER6", "ATTRIBUTE_NUMBER6", 92, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER7", "ATTRIBUTE_NUMBER7", 93, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER8", "ATTRIBUTE_NUMBER8", 94, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER9", "ATTRIBUTE_NUMBER9", 95, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ATTRIBUTE_NUMBER10", "ATTRIBUTE_NUMBER10", 96, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_CATEGORY", "GLOBAL_ATTRIBUTE_CATEGORY", 97, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE1", "GLOBAL_ATTRIBUTE1", 98, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE2", "GLOBAL_ATTRIBUTE2", 99, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE3", "GLOBAL_ATTRIBUTE3", 100, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE4", "GLOBAL_ATTRIBUTE4", 101, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE5", "GLOBAL_ATTRIBUTE5", 102, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE6", "GLOBAL_ATTRIBUTE6", 103, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE7", "GLOBAL_ATTRIBUTE7", 104, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE8", "GLOBAL_ATTRIBUTE8", 105, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE9", "GLOBAL_ATTRIBUTE9", 106, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE10", "GLOBAL_ATTRIBUTE10", 107, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE11", "GLOBAL_ATTRIBUTE11", 108, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE12", "GLOBAL_ATTRIBUTE12", 109, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE13", "GLOBAL_ATTRIBUTE13", 110, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE14", "GLOBAL_ATTRIBUTE14", 111, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE15", "GLOBAL_ATTRIBUTE15", 112, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE16", "GLOBAL_ATTRIBUTE16", 113, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE17", "GLOBAL_ATTRIBUTE17", 114, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE18", "GLOBAL_ATTRIBUTE18", 115, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE19", "GLOBAL_ATTRIBUTE19", 116, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE20", "GLOBAL_ATTRIBUTE20", 117, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE1", "GLOBAL_ATTRIBUTE_DATE1", 118, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE2", "GLOBAL_ATTRIBUTE_DATE2", 119, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE3", "GLOBAL_ATTRIBUTE_DATE3", 120, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE4", "GLOBAL_ATTRIBUTE_DATE4", 121, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE5", "GLOBAL_ATTRIBUTE_DATE5", 122, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE6", "GLOBAL_ATTRIBUTE_DATE6", 123, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE7", "GLOBAL_ATTRIBUTE_DATE7", 124, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE8", "GLOBAL_ATTRIBUTE_DATE8", 125, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE9", "GLOBAL_ATTRIBUTE_DATE9", 126, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_DATE10", "GLOBAL_ATTRIBUTE_DATE10", 127, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP1", "GLOBAL_ATTRIBUTE_TIMESTAMP1", 128, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP2", "GLOBAL_ATTRIBUTE_TIMESTAMP2", 129, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP3", "GLOBAL_ATTRIBUTE_TIMESTAMP3", 130, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP4", "GLOBAL_ATTRIBUTE_TIMESTAMP4", 131, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP5", "GLOBAL_ATTRIBUTE_TIMESTAMP5", 132, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP6", "GLOBAL_ATTRIBUTE_TIMESTAMP6", 133, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP7", "GLOBAL_ATTRIBUTE_TIMESTAMP7", 134, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP8", "GLOBAL_ATTRIBUTE_TIMESTAMP8", 135, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP9", "GLOBAL_ATTRIBUTE_TIMESTAMP9", 136, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_TIMESTAMP10", "GLOBAL_ATTRIBUTE_TIMESTAMP10", 137, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER1", "GLOBAL_ATTRIBUTE_NUMBER1", 138, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER2", "GLOBAL_ATTRIBUTE_NUMBER2", 139, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER3", "GLOBAL_ATTRIBUTE_NUMBER3", 140, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER4", "GLOBAL_ATTRIBUTE_NUMBER4", 141, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER5", "GLOBAL_ATTRIBUTE_NUMBER5", 142, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER6", "GLOBAL_ATTRIBUTE_NUMBER6", 143, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER7", "GLOBAL_ATTRIBUTE_NUMBER7", 144, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER8", "GLOBAL_ATTRIBUTE_NUMBER8", 145, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER9", "GLOBAL_ATTRIBUTE_NUMBER9", 146, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("GLOBAL_ATTRIBUTE_NUMBER10", "GLOBAL_ATTRIBUTE_NUMBER10", 147, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("Registry ID", "REGISTRY_ID", 148, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("EMPTY", "EMPTY", 149, true, ""));
            DefLst.Add(new OREMPExportColumnItemMd("ENDCOLUMN", "ENDCOLUMN", 150, true, "END"));


            
        }

       
        public string this[string key]
        {
            get
            {
                return FindValue(key);
            }
            set
            {
                FindItem(key).Value = value;
            }
        }


        public OREMPExportColumnItemMd FindItem(string Key)
        {
            return DefLst.Where(p => p.Mapping == Key).FirstOrDefault();
        }
        public string FindValue(string Key)
        {
            return (DefLst.Where(p => p.Mapping == Key).Select(o => o.Value)).FirstOrDefault();
        }

        public List<OREMPExportColumnItemMd> DefLst { get; set; }



        public void init(List<OREMPExportColumnItemMd> ExportColumnData)
        {
            DefLst = ExportColumnData;
        }

        public string RowValue()
        {
            return string.Join(",", DefLst.Where(p => p.SortIndex >= 0 && p.IsRequired).OrderBy(p => p.SortIndex).Select(p => p.Value));
        }

        public string DecodeRowValue()
        {
            return System.Web.HttpUtility.HtmlDecode(RowValue());
        }
    }

}
