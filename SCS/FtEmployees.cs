using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace OracleNewQuitEmployee.SCS
{
    public class FtEmployees
    {
        [JsonProperty("ReportID")]
        public string ReportId { get; set; }

        [JsonProperty("DataSet")]
        public DataSet DataSet { get; set; }
    }

    public class DataSet
    {
        [JsonProperty("ReportHeader")]
        public ReportHeader[] ReportHeader { get; set; }

        [JsonProperty("ReportBody")]
        public ReportBody[] ReportBody { get; set; }
    }

    public class ReportBody
    {
        /// <summary>
        /// 部門編號  "r_dept"
        /// </summary>
        [JsonProperty("DEPARTID")]
        public string Departid { get; set; }

        /// <summary>
        /// 部門名稱
        /// </summary>
        [JsonProperty("DEPARTNAME")]
        public string Departname { get; set; }

        /// <summary>
        /// 員工工號  r_code
        /// </summary>
        [JsonProperty("EMPLOYEEID")]
        public string Employeeid { get; set; }

        /// <summary>
        ///  中文姓名  r_cname
        /// </summary>
        [JsonProperty("EMPLOYEENAME")]
        public string Employeename { get; set; }

        /// <summary>
        /// 英文姓名 r_ename
        /// </summary>
        [JsonProperty("EMPLOYEEENGNAME")]
        public string Employeeengname { get; set; }

        /// <summary>
        /// 在職狀態 r_online
        /// 0=未就職(在職)
        /// 1=試用(在職)
        /// 2=正式(在職)
        /// 3=約聘(在職)
        /// 4=留職停薪(在職)
        /// 5=離職(離職)
        /// </summary>
        [JsonProperty("JOBSTATUS")]
        public string Jobstatus { get; set; }

        /// <summary>
        /// 生日 r_birthday
        /// </summary>
        [JsonProperty("BIRTHDATE")]
        public string Birthdate { get; set; }

        /// <summary>
        /// 性別 r_sex
        /// </summary>
        [JsonProperty("SEX")]
        public string Sex { get; set; }

        /// <summary>
        /// 公司EMAIL r_email
        /// </summary>
        [JsonProperty("PSNEMAIL")]
        public string Psnemail { get; set; }

        /// <summary>
        /// 行動電話 r_cell_phone
        /// </summary>
        [JsonProperty("MOIBLE")]
        public string Moible { get; set; }

        /// <summary>
        ///  分機 r_phone_ext
        /// </summary>
        [JsonProperty("OFFICETEL1")]
        public string Officetel1 { get; set; }

        /// <summary>
        ///  Skype ID  r_skype_id
        /// </summary>
        [JsonProperty("OFFICETEL2")]
        public string Officetel2 { get; set; }

        /// <summary>
        /// 到職日期  r_online_date
        /// </summary>
        [JsonProperty("STARTDATE")]
        public string Startdate { get; set; }

        /// <summary>
        /// 職務編號
        /// </summary>
        [JsonProperty("DUTYID", NullValueHandling = NullValueHandling.Ignore)]
        public string Dutyid { get; set; }

        /// <summary>
        /// 職務名稱  r_degress
        /// </summary>
        [JsonProperty("DUTYNAME", NullValueHandling = NullValueHandling.Ignore)]
        public string Dutyname { get; set; }

        /// <summary>
        /// 工作地點編號
        /// </summary>
        [JsonProperty("WORKPLACEID", NullValueHandling = NullValueHandling.Ignore)]
        public string Workplaceid { get; set; }

        /// <summary>
        /// 工作地點名稱
        /// </summary>
        [JsonProperty("WORKPLACENAME", NullValueHandling = NullValueHandling.Ignore)]
        public string Workplacename { get; set; }

        /// <summary>
        /// 帳戶身分證號
        /// </summary>
        [JsonProperty("IDNO", NullValueHandling = NullValueHandling.Ignore)]
        public string Idno { get; set; }

        /// <summary>
        /// 銀行帳戶戶名
        /// </summary>
        [JsonProperty("SALACCOUNTNAME", NullValueHandling = NullValueHandling.Ignore)]
        public string Salaccountname { get; set; }

        /// <summary>
        /// 銀行代碼
        /// </summary>
        [JsonProperty("BANKCODE", NullValueHandling = NullValueHandling.Ignore)]
        public string Bankcode { get; set; }

        /// <summary>
        /// 銀行分行代碼
        /// </summary>
        [JsonProperty("BANKBRANCH", NullValueHandling = NullValueHandling.Ignore)]
        public string Bankbranch { get; set; }

        /// <summary>
        /// 銀行收款帳號
        /// </summary>
        [JsonProperty("SALACCOUNTID", NullValueHandling = NullValueHandling.Ignore)]
        public string Salaccountid { get; set; }


        /// <summary>
        /// 離職日期  r_offline_date
        /// </summary>
        [JsonProperty("SEPARATIONDATE", NullValueHandling = NullValueHandling.Ignore)]
        public string Separationdate { get; set; }
    }

    public class ReportHeader
    {
        [JsonProperty("COMPANYNAME")]
        public string Companyname { get; set; }

        [JsonProperty("COMPANYCURRENCYID")]
        public string Companycurrencyid { get; set; }

        [JsonProperty("COMPANYCURRENCYNAME")]
        public string Companycurrencyname { get; set; }

        [JsonProperty("COMPOINTPRICE")]
        public string Compointprice { get; set; }

        [JsonProperty("COMPOINTMONEY")]
        public string Compointmoney { get; set; }

        [JsonProperty("COMPOINTCURRRATE")]
        public string Compointcurrrate { get; set; }

        [JsonProperty("REPORTTITLE")]
        public string Reporttitle { get; set; }

        [JsonProperty("REPORTTAIL")]
        public string Reporttail { get; set; }

        [JsonProperty("REPORTUSERID")]
        public string Reportuserid { get; set; }

        [JsonProperty("REPORTUSERNAME")]
        public string Reportusername { get; set; }

        [JsonProperty("REPORTFILTER")]
        public string Reportfilter { get; set; }
    }




}
