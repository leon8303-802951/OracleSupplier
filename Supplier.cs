using AQuartzJobUTL;
using LogMgr;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinnieJob;
using OracleNewQuitEmployee.ORSyncOracleData;
using System.Data;
using OracleNewQuitEmployee.ORSyncOracleData.Model;
using System.Data.SQLite;
using System.Threading;
using BoxmanBase;
using OracleNewQuitEmployee.Models;
using OracleSupplier.Models;
using OracleNewQuitEmployee.SCS;
using OracleSupplier.Models.Supplier;

namespace OracleSupplier
{
    public class Supplier : Autojob
    {
        keroroConnectBase.keroroConn conn;

        /// <summary>
        /// 同步所有到職日在這天以後的同仁
        /// </summary>
        //public string SyncOnDutyDay { get; set; }
        public string only { get; set; }
        public string JLOG_LOGMODE { get; set; }//LOG使用 須宣告的參數
        private LogOput log;
        public string 執行用逗號分隔的指定員編 { get; set; }

        /// <summary>
        /// 建構式
        /// </summary>
        public Supplier()
        {
            only = "one";
            JLOG_LOGMODE = "5";//5 = 所有log都寫

            log = new LogOput();
            conn = new keroroConnectBase.keroroConn();


            db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
            if (!Directory.Exists(db_path))
            {
                Directory.CreateDirectory(db_path);
            }
            db_file = $"{db_path}OracleData.sqlite";

        }
        SyncOracleDataBack sync_oracle;
        SQLiteUtl sqlite;

        string db_path;
        string db_file;
        string db_Batch_file;



        /// <summary>
        /// 類似 boxman 的 secrets load 進 MyKeys
        /// </summary>




        //因為執行很久，想要改良，還沒上線
        public void ExecJob_New(Dictionary<string, string> datamap)
        {
            try
            {
                log.setLog(this.GetType(), datamap);
                log.WriteLog("5", "開始");

                //sqlite = new SQLiteUtl(db_file);

                log.WriteLog("5", "準備取得 secrets");
                //類似 boxman 的 secrets load 進 MyKeys
                Secrets secrets = new Secrets(log);

                sync_oracle = new SyncOracleDataBack(conn, log);
                sync_oracle.UserName = secrets.GetValueOfKey("oracle_UserName");
                sync_oracle.Password = secrets.GetValueOfKey("oracle_Password");

                //先取得 飛騰的資料
                SCSUtils scs = new SCSUtils();
                var guid = scs.GetLoginToken();
                log.WriteLog("5", $"guid={guid}");

                //飛騰部門資料
                List<Hum0010300> depts = scs.GetDeptsDetails(guid);
                //網家部門
                var deptNos80 = (from dept in depts
                                 where dept.TMP_DECCOMPANYID == "80"
                                 select dept.SysViewid).ToList();
                log.WriteLog("5", $"網家部門數:{deptNos80.Count}");
                //飛騰人員資料
                ReportBody[] emps = scs.GetEmployees(guid);
                //網家的人
                var empsIn80 = (from emp in emps
                                where deptNos80.Contains(emp.Departid)
                                select emp).ToList();
                log.WriteLog("5", $"網家人員數:{empsIn80.Count}");

                log.WriteLog("5", "準備 get key:OracleApBaseURL");
                var OracleApBaseUrl = secrets.GetValueOfKey("OracleApBaseURL");


                //oracle 供應商 Supplier
                var Supplier_url = $"{OracleApBaseUrl}/Supplier";
                var SupplierAddress_url = $"{OracleApBaseUrl}/SupplierAddress";
                var SupplierSite_url = $"{OracleApBaseUrl}/SupplierSite";
                var SupplierSiteAssignment_url = $"{OracleApBaseUrl}/SupplierSiteAssignment";
                var SupplierBank_url = $"{OracleApBaseUrl}/SupplierBank";

                //要先查詢銀行資料
                var tmp_bankinfo = sync_oracle.GetOracleBankData();
                if (tmp_bankinfo == null)
                {
                    throw new Exception("銀行資料取得失敗");
                }
                List<OracleApiChkBankBranchesMd> oracle_bank_info = tmp_bankinfo.ToList();


                foreach (var ft_emp in empsIn80)
                {

                    var EmpNo = ft_emp.Employeeid; //員工編號
                    var EmpName = ft_emp.Employeename; //員工姓名
                    var DeptNo = ft_emp.Departid; //部門編號
                    var DeptName = ft_emp.Departname; //部門名稱
                    var TW_ID = ft_emp.Idno; //身份證號
                    var ACCOUNT_NUMBER = ft_emp.Salaccountid; //銀行帳號
                    var BANK_NUMBER = ft_emp.Bankcode; //銀行代碼
                    var BRANCH_NUMBER = ft_emp.Bankbranch; //分行代碼
                    var ACCOUNT_NAME = ft_emp.Salaccountname; //銀行戶名
                    log.WriteLog("5", $"準備同步 {EmpNo} {EmpName} TW_ID={TW_ID}, ACCOUNT_NUMBER={ACCOUNT_NUMBER}, BANK_NUMBER={BANK_NUMBER}, BRANCH_NUMBER={BRANCH_NUMBER}, ACCOUNT_NAME={ACCOUNT_NAME}");
                    try
                    {

                        if (string.IsNullOrWhiteSpace(TW_ID))
                        {
                            log.WriteLog("5", $"{EmpNo} {EmpName} 身份證號空白，略過!");
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(ACCOUNT_NUMBER))
                        {
                            log.WriteLog("5", $"{EmpNo} {EmpName} 銀行帳號空白，略過!");
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(BANK_NUMBER))
                        {
                            log.WriteLog("5", $"{EmpNo} {EmpName} 銀行代號空白，略過!");
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(BRANCH_NUMBER))
                        {
                            log.WriteLog("5", $"{EmpNo} {EmpName} 分行代號空白，略過!");
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(ACCOUNT_NAME))
                        {
                            log.WriteLog("5", $"{EmpNo} {EmpName} 銀行戶名空白，略過!");
                            continue;
                        }




                        //開始為每個員工做 員工供應商同步
                        var BatchID = DateTime.Now.ToString("yyyyMMddHHmmss");




                        //boxman 中的 WorkData
                        //SupplierSame supplier_same = new SupplierSame();
                        OREMPOracleFileSchemaDefineItemMd File_1_md = new OREMPOracleFileSchemaDefineItemMd();
                        //FOR CREATE
                        File_1_md["REGISTRY_ID"] = BatchID;
                        File_1_md["IMPORT_ACTION"] = "CREATE";
                        File_1_md["TAX_ORGANIZATION_TYPE"] = "INDIVIDUAL";
                        File_1_md["SUPPLIER_TYPE"] = "EMPLOYEE";
                        File_1_md["SUPPLIER_NAME"] = $"{EmpNo}.{EmpName}";  //員工姓名;
                        File_1_md["SUPPLIER_NUMBER"] = $"{EmpNo}"; //員工編號
                        File_1_md["ALTERNATE_NAME"] = $"{EmpNo}.{EmpName}";  //員工姓名;
                                                                             //df["TAXPAYER_ID"] = item.TAXPAYER_ID;

                        // 魏曉琴 <vickywei@pchome.tw>
                        // Re: 關於員工供應商
                        // Hi Alvin,
                        // 已與Athena討論，請將供應商(員工)的身份證字號放在供應商主檔1st檔案中的
                        //＂Tax Registration Number＂ (AG欄)。Thank you.
                        File_1_md["TAX_REGISTRATION_NUMBER"] = TW_ID;

                        //var STR = string.Join(Environment.NewLine, CSVFile.Select(p => p.DecodeRowValue()).ToList());
                        var str = File_1_md.DecodeRowValue();


                        var FindBnkData = oracle_bank_info.Where(
                            p => p.BANK_NUMBER == BANK_NUMBER
                                && p.BRANCH_NUMBER == BRANCH_NUMBER).FirstOrDefault();

                        if (FindBnkData == null)
                        {
                            throw new Exception("銀行資料取得失敗");
                        }
                        else
                        {
                            //var file_1_url = Supplier_url;
                            //var neParam = new
                            //{
                            //    batch = BatchID,
                            //    ecFile = workItem.FileBase64Str,
                            //    user = EmpNoData.ApplyMan,
                            //    dept = "ec"
                            //};

                        }
                    }
                    catch (Exception exft)
                    {
                        log.WriteErrorLog($"{EmpNo} {EmpName} 同步員工供應商失敗! {exft.Message}");
                    }
                }

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"ExecJob失敗:{ex.Message}{ex.InnerException}");

            }
        }


        public void ExecJob(Dictionary<string, string> datamap)
        {
            try
            {

                log.setLog(this.GetType(), datamap);
                log.WriteLog("5", "Start");
                //        "內湖科技園區分行",
                //   "信義分行"
                // 有成功更新
                //var rtn = sync_oracle.UpdateBankInfo("801524",
                //                      "18203000197633",
                //                       "上海商業儲蓄銀行",
                //                       "信義分行",
                //                      "陳松柏",
                //                        "18203000197633",
                //                 "上海商業儲蓄銀行",
                //                      "內湖科技園區分行");


                string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                Secrets secrets = new Secrets(log);


                //要先取得信義上俊中的 oracle ap 位置, 不然打不出去
                sync_oracle = new SyncOracleDataBack(conn, log);
                sync_oracle.UserName = secrets.GetValueOfKey("oracle_UserName");
                sync_oracle.Password = secrets.GetValueOfKey("oracle_Password");

                sqlite = new SQLiteUtl(db_file);
                log.WriteLog("5", "準備取得 GetOracleApDomainName");
                sync_oracle.Oracle_AP = sync_oracle.GetOracleApDomainName();
                log.WriteLog("5", $"sync_oracle.Oracle_AP={sync_oracle.Oracle_AP}");


                // 從 HR 同步回來 
                SyncFromHRFT(BatchNo);

                // 2021-10-27 因為現在有 employee api, 所以這一段可以跳過不用了 
                // 從 oracle 同步回來
                //SyncFromOracle1(BatchNo);


                // 這個不需要了，因為下面的推送會自已判斷需不需要同步
                // 需要的會自行同步
                // 從 oracle 同步 supplier 回來 給下面推送使用
                // 下面在推送之前會先檢查是否 oracle 有此 supplier
                // 若沒有就會先同步
                //SyncSupplierFromOracle(BatchNo);

                // 這個不需要了
                //FOR TEST 先MARK
                // 這個其實可以不用，因為在 push supplier to oracle 時，
                // 會看若沒有這個 user 就會做 create 
                //CreateWorkerIfNotExists(BatchNo);




                // 推送 supplier to oracle
                PushSupplierToOracle(BatchNo);

                // 停用 oracle 上的帳號
                //SuspendAccount(BatchNo);

                // 最後寫入 BatchNo
                string sql = $@"
INSERT INTO [HR_Batch]
		([BatchNo])
	VALUES
		(:0)
";

                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                sqlite.ExecuteByCmd(sql, BatchNo);

                // 這裡要再加上 刪除3天以前的資料
                string B4BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                sql = $@"DELETE FROM [HR_Batch]    WHERE [BatchNo] <= :0";
                sqlite.ExecuteByCmd(sql, B4BatchNo);
                //sql = $@"DELETE FROM [HR_Employee]    WHERE [BatchNo] <= :0";
                //sqlite.ExecuteByCmd(sql, B4BatchNo);

                log.WriteLog("5", "結束");

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"ExecJob失敗:{ex.Message}{ex.InnerException}");

            }


        }

        /// <summary>
        /// 推送 supplier 到 oracle
        /// </summary>
        /// <param name="BatchNo"></param>
        void PushSupplierToOracle(string BatchNo)
        {


            List<OracleApiChkBankBranchesMd> oracle_bank_info = new List<OracleApiChkBankBranchesMd>();
            try
            {
                log.WriteLog("5", "進入 PushSupplierToOracle 準備取得 oracle 銀行資料");
                var tmp_bankinfo = sync_oracle.GetOracleBankData();
                if (tmp_bankinfo != null)
                {
                    oracle_bank_info = tmp_bankinfo.ToList();
                }
                log.WriteLog("5", "oracle 銀行資料已取得");

            }
            catch (Exception exbank)
            {
                log.WriteErrorLog($"取得 oracle 銀行資料失敗!");
            }


            try
            {
                log.WriteLog("5", "準備推送員工供應商到 oracel");

                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //if (!Directory.Exists(db_path))
                //{
                //    Directory.CreateDirectory(db_path);
                //}
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);




                try
                {
                    string sql = "";
                    //                    sql = $@"
                    //select *
                    //from [Supplier]
                    //		where [BatchNo] =  :0
                    //	AND R_CODE LIKE '8%'

                    //-- for test
                    //--and r_code  LIKE '801527%'
                    //order by r_code desc
                    //";


                    // for test
                    //                        sql = $@"
                    //select *
                    //from [Supplier]
                    //where batchno = :0
                    //and r_code in (
                    //'803465', 
                    //'805303', 
                    //'805422', 
                    //'802790', 
                    //'803582', 
                    //'805622', 
                    //'802425', 
                    //'803845', 
                    //'802408', 
                    //'803808', 
                    //'804035', 
                    //'805738', 
                    //'805149', 
                    //'804881', 
                    //'804882', 
                    //'802165'
                    //);
                    //";




                    DataTable tb = null;
                    List<OREmpNoData> suppliers = new List<OREmpNoData>();

                    //-.取飛騰資料
                    log.WriteLog("5", "準備取飛騰資料");
                    SCSUtils scs = new SCSUtils();
                    log.WriteLog("5", "準備取飛騰Token");
                    var guid = scs.GetLoginToken();

                    log.WriteLog("5", "準備取飛騰 GetDeptsDetails");
                    List<Hum0010300> depts = scs.GetDeptsDetails(guid)
                        .Where(dept => string.IsNullOrWhiteSpace(dept.STOPDATE)
                        && dept.TMP_DECCOMPANYID == "80").ToList();
                    var deptNos = (from dp in depts
                                   select dp.SysViewid).ToList();
                    ReportBody[] emps = scs.GetEmployees(guid);

                    var 飛騰_emps = (from emp in emps
                                   where emp.Jobstatus != "5"
                                   && deptNos.Contains(emp.Departid)
                                   select emp).ToList();


                    bool ForceSyncOracleSupplier = false;
                    if (!string.IsNullOrWhiteSpace(執行用逗號分隔的指定員編))
                    {
                        ForceSyncOracleSupplier = true;
                        string[] 指定的員編們 = 執行用逗號分隔的指定員編.Split(',');
                        飛騰_emps = 飛騰_emps.Where(item => 指定的員編們.Contains(item.Employeeid)).ToList();
                    }

                    log.WriteLog("5", $"取得飛騰資料:{飛騰_emps.Count()}");

                    //排序一下 我想讓號碼大的先做(倒著排)
                    飛騰_emps.Sort(delegate (ReportBody x, ReportBody y)
                    {
                        int ix = 0;
                        int iy = 0;
                        int rst = -1;
                        try
                        {
                            ix = int.Parse(x.Employeeid);
                            iy = int.Parse(y.Employeeid);
                            if (ix > iy) return -1;
                            if (ix == iy) return 0;
                            if (ix < iy) return 1;
                        }
                        catch (Exception)
                        { }
                        return rst;
                    });
                    //建立帳號/相關資料
                    foreach (var _emp in 飛騰_emps)
                    //foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo))
                    {
                        string new_empno = _emp.Employeeid;
                        string empName = _emp.Employeename;
                        //string new_empno = $"{row["r_code"]}";
                        //string empName = $"{row["r_cname"]}";
                        //string old_empno = GetOldEmpNo(new_empno);
                        string old_empno = new_empno;
                        //string empno = old_empno;
                        string empno = new_empno;

                        // 判斷是否需要同步員工供應商回來，需要就立即同步
                        // 為何要同步回來?
                        // 同步是為了下次不用再做
                        sync_oracle.SyncOneSupplierByEmpNo(empno, ForceSyncOracleSupplier);

                        //bool oracle_has_this_lastname = false;
                        //oracle_has_this_lastname = get_oracle_has_this_lastname(empno, BatchNo);
                        //if (!oracle_has_this_lastname)
                        //{
                        //    // 到時上正式要用   
                        //    CreateWorkerNewEmpNo(new_empno, BatchNo);

                        //    // FOR TEST STAGE
                        //    //CreateWorker(new_empno, BatchNo); //這支自已會轉成舊員編
                        //}


                        var redo = "";
                        // 檢查是否要做第一個檔案
                        sql = $@"
select count(1) cnt
from oracleSupplier
where suppliernumber=:0
";

                        foreach (DataRow suprow in sqlite.QueryOkWithDataRows(out tb, sql, empno))
                        {
                            string cnt = $"{suprow["cnt"]}";
                            if (cnt == "0")
                            {
                                redo += ",1";
                            }
                        }


                        // 檢查是否要做第二個檔案
                        sql = $@"
select count(1) cnt
from OracleSupplierAddr
where suppliernumber=:0
";

                        foreach (DataRow suprow in sqlite.QueryOkWithDataRows(out tb, sql, empno))
                        {
                            string cnt = $"{suprow["cnt"]}";
                            if (cnt == "0")
                            {
                                redo += ",2";
                            }
                        }
                        // 檢查是否要做第三個檔案
                        sql = $@"
select count(1) cnt
from OracleSupplierSite
where suppliernumber=:0
";

                        foreach (DataRow suprow in sqlite.QueryOkWithDataRows(out tb, sql, empno))
                        {
                            string cnt = $"{suprow["cnt"]}";
                            if (cnt == "0")
                            {
                                redo += ",3";
                            }
                        }

                        // 檢查是否要做第四個檔案
                        sql = $@"
select count(1) cnt
from OracleSupplierAssignment
where suppliernumber=:0
";

                        foreach (DataRow suprow in sqlite.QueryOkWithDataRows(out tb, sql, empno))
                        {
                            string cnt = $"{suprow["cnt"]}";
                            if (cnt == "0")
                            {
                                redo += ",4";
                            }
                        }

                        // 檢查是否要做第五個檔案
                        sql = $@"
select count(1) cnt
from OracleSupplierBankAccount
where suppliernumber=:0
";

                        foreach (DataRow suprow in sqlite.QueryOkWithDataRows(out tb, sql, empno))
                        {
                            string cnt = $"{suprow["cnt"]}";
                            if (cnt == "0")
                            {
                                redo += ",5";
                            }
                        }



                        if (string.IsNullOrWhiteSpace(redo))
                        {
                            log.WriteLog("5", $"{empno} {empName} 已經有 oracle supplier 資料了");
                        }
                        else
                        {

                            redo = redo.Substring(1);

                            suppliers.Clear();

                            var idno = _emp.Idno;
                            var _BankNumber = _emp.Bankcode;
                            var _ACCOUNT_NUMBER = _emp.Salaccountid;
                            var _ACCOUNT_NAME = _emp.Salaccountname;
                            var _BranchNumber = _emp.Bankbranch;

                            //var idno = $"{row["Idno"]}";
                            //var _BankNumber = $"{row["Bankcode"]}";
                            //var _ACCOUNT_NUMBER = $"{row["Salaccountid"]}";
                            //var _ACCOUNT_NAME = $"{row["Salaccountname"]}";
                            //var _BranchNumber = $"{row["Bankbranch"]}";

                            suppliers.Add(new OREmpNoData()
                            {
                                EmpNo = empno, // 員編
                                EmpName = $"{empName}", // 中文姓名
                                TW_ID = idno, // 帳戶身分證號
                                BankNumber = _BankNumber, // 銀行代碼
                                ACCOUNT_NUMBER = _ACCOUNT_NUMBER, // 銀行收款帳號
                                ACCOUNT_NAME = _ACCOUNT_NAME, // 銀行帳戶戶名
                                BranchNumber = _BranchNumber, // 銀行分行代碼
                                ApplyMan = "Job",
                                ReDoStep = redo
                            });
                            var par = new
                            {
                                Data = suppliers
                            };
                            var ppar = Newtonsoft.Json.JsonConvert.SerializeObject(par);
                            log.WriteLog("5", $"準備推送員工供應商 {empno} {empName} {redo}, 先暫停 3 秒鐘{Environment.NewLine}Payload={ppar}");
                            Thread.Sleep(1000 * 3);
                            var ret = ApiOperation.CallApi<string>(new ApiRequestSetting()
                            {
                                MethodRoute = "api/OREMP/EMPACC/",
                                Data = par,
                                MethodType = "POST",
                                TimeOut = 1000 * 60 * 30  // 5分鐘竟然不夠
                            });
                            if (ret.Success)
                            {
                                ApiResultModel model = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResultModel>(ret.Data);
                                if (model.Result)
                                {
                                    log.WriteLog("5", $"{new_empno},{old_empno} ,{old_empno}, {empName} Supplier push 成功! {model.ErrorCode} {model.ErrorMessages}");

                                }
                                else
                                {
                                    log.WriteErrorLog($"{new_empno},{old_empno} ,{old_empno}, {empName}  Supplier push 失敗! {ret.Data} {ret.ErrorMessage} {ret.ErrorException}");
                                }
                                /*
{"Result":false,"ErrorCode":null,"ErrorMessages":["Supplier:ERROR","SupplierAddress:ERROR","SupplierSite:ERROR","SupplierSiteAssignment:ERROR","SupplierBank:ERROR"]}

                                 * 
                                 * 
                                 * */

                            }
                            else
                            {
                                log.WriteErrorLog($"{new_empno},{old_empno} ,{old_empno}, {empName}  Supplier push 失敗!{Environment.NewLine}{ret.Data}{Environment.NewLine}{ret.ErrorMessage}{Environment.NewLine}{ret.ErrorException}");
                            }

                        }


                        log.WriteLog("5", $"檢查帳戶資料是否需更新");
                        // 看看是否需要 update bank account ifno
                        sql = $@"
select 
 [BatchNo]
		,[SupplierNumber]
		,[BankAccountId]
		,[AccountNumber]
		,[AccountName]
		,[BankName]
		,[BranchName]
from OracleSupplierBankAccount
where suppliernumber=:0
";
                        bool sqlite無此supplier資料 = true;
                        foreach (DataRow suprow in sqlite.QueryOkWithDataRows(out tb, sql, empno))
                        {
                            sqlite無此supplier資料 = false;
                            // BankAccountId, // 這個沒有用
                            // AccountNumber, // 這個就是帳號
                            // AccountName, //戶名
                            // BankName, //銀行名稱
                            // BranchName, //分行名稱
                            string AccountNumber = $"{suprow["AccountNumber"]}"; //帳號
                            string AccountName = $"{suprow["AccountName"]}"; //戶名
                            string BankName = $"{suprow["BankName"]}"; //銀行名稱
                            string BranchName = $"{suprow["BranchName"]}"; //分行名稱

                            var 收款帳號 = $"{_emp.Salaccountid}";
                            var 銀行帳戶戶名 = $"{_emp.Salaccountname}";
                            var 銀行代碼 = $"{_emp.Bankcode}";
                            var 分行代碼 = $"{_emp.Bankbranch}";
                            //var 身份證字號 = $"{_emp.Idno}";
                            var bk1 = (from _bk in oracle_bank_info
                                       where _bk.BANK_NUMBER == 銀行代碼
                                       && _bk.BRANCH_NUMBER == 分行代碼
                                       select _bk).FirstOrDefault();
                            var 銀行名稱 = bk1.BANK_NAME;
                            var 分行名稱 = bk1.BANK_BRANCH_NAME;

                            int cnt = 0;
                            if (AccountNumber != 收款帳號) cnt++;
                            if (AccountName != 銀行帳戶戶名) cnt++;
                            if (BankName != 銀行名稱) cnt++;
                            if (BranchName != 分行名稱) cnt++;


                            log.WriteLog("5", $"-- 準備更新員工供應商的帳號資料: {empno}");
                            log.WriteLog("5", $"AccountNumber: {AccountNumber}");
                            log.WriteLog("5", $"BankName: {BankName}");
                            log.WriteLog("5", $"BranchName: {BranchName}");
                            log.WriteLog("5", $"銀行帳戶戶名: {銀行帳戶戶名}");
                            log.WriteLog("5", $"收款帳號: {收款帳號}");
                            log.WriteLog("5", $"銀行代碼: {銀行代碼}");
                            log.WriteLog("5", $"銀行名稱: {銀行名稱}");
                            log.WriteLog("5", $"分行代碼: {分行代碼}");
                            log.WriteLog("5", $"分行名稱: {分行名稱}");

                            //大於零代表有資料不同
                            if (cnt > 0)
                            {
                                try
                                {
                                    // 準備更新帳號資料
                                    log.WriteLog("5", $"-- 準備更新員工供應商的帳號資料: {empno}");
                                    log.WriteLog("5", $"AccountNumber: {AccountNumber}");
                                    log.WriteLog("5", $"BankName: {BankName}");
                                    log.WriteLog("5", $"BranchName: {BranchName}");
                                    log.WriteLog("5", $"銀行帳戶戶名: {銀行帳戶戶名}");
                                    log.WriteLog("5", $"收款帳號: {收款帳號}");
                                    log.WriteLog("5", $"銀行代碼: {銀行代碼}");
                                    log.WriteLog("5", $"銀行名稱: {銀行名稱}");
                                    log.WriteLog("5", $"分行代碼: {分行代碼}");
                                    log.WriteLog("5", $"分行名稱: {分行名稱}");

                                    var rtn = sync_oracle.UpdateBankInfo(empno,
                                            AccountNumber,
                                             BankName,
                                             BranchName,
                                             銀行帳戶戶名,
                                             收款帳號,
                                             銀行名稱,
                                             分行名稱);
                                    log.WriteLog("5", $"更新員工供應商結果 : {rtn}");

                                }
                                catch (Exception exUpdateAcc)
                                {
                                    log.WriteErrorLog($"更新員工帳號失敗! {empno} {exUpdateAcc.Message}");
                                }
                            }
                            else
                            {
                                log.WriteLog("5", "資料相同，不更新。");
                            }

                        }
                        if (sqlite無此supplier資料)
                        {
                            log.WriteErrorLog($"{empno} sqlite 查無此帳戶資料!");
                        }
                    }


                    //List<OREmpNoData> sups = new List<OREmpNoData>();
                    //foreach (OREmpNoData emp in suppliers)
                    //{
                    //    sups.Clear();
                    //    sups.Add(emp);
                    //    try
                    //    {
                    //        var par = new
                    //        {
                    //            Data = sups
                    //        };
                    //        var ret = ApiOperation.CallApi<string>(new ApiRequestSetting()
                    //        {
                    //            MethodRoute = "api/OREMP/EMPACC/",
                    //            Data = par,
                    //            MethodType = "POST",
                    //            TimeOut = 1000 * 60 * 10  // 5分鐘竟然不夠
                    //        });
                    //        if (ret.Success)
                    //        {
                    //            foreach (var _sup in sups)
                    //            {
                    //                log.WriteLog("5", $"{_sup.EmpNo},{_sup.EmpName} Supplier push 成功!");
                    //            }
                    //        }
                    //        else
                    //        {
                    //            foreach (var _sup in sups)
                    //            {
                    //                log.WriteLog("5", $"{_sup.EmpNo},{_sup.EmpName} Supplier push 失敗! {ret.ErrorMessage} {ret.ErrorException}");
                    //            }
                    //        }
                    //    }
                    //    catch (Exception exPush)
                    //    {
                    //        foreach (var _sup in sups)
                    //        {
                    //            log.WriteLog("5", $"{_sup.EmpNo},{_sup.EmpName} Supplier push 失敗! {exPush.Message} {exPush.InnerException}");
                    //        }
                    //    }
                    //}





                }
                catch (Exception exhr)
                {
                    log.WriteErrorLog($"推送員工供應商失敗:{exhr.Message}{exhr.InnerException}");
                }
                //using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}", db_file)))
                //{
                //    conn.Open();
                //    SQLiteCommand cmd = new SQLiteCommand(conn);

                //    DateTime time_start = DateTime.Now;
                //    //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                //  conn.Close();
                //}

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"PushSupplierToOracle Error:{ex.Message}{ex.InnerException}");
            }
        }

        string GetOldEmpNo(string EmpNo)
        {
            string empNo = EmpNo.ToUpper();
            string rst = EmpNo;
            try
            {
                if (empNo.Substring(0, 2) != "PC")
                {

                    //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                    //if (!Directory.Exists(db_path))
                    //{
                    //    Directory.CreateDirectory(db_path);
                    //}
                    //db_file = $"{db_path}OracleData.sqlite";
                    //SQLiteUtl sqlite = new SQLiteUtl(db_file);


                    using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}", db_file)))
                    {
                        conn.Open();
                        SQLiteCommand cmd = new SQLiteCommand(conn);

                        DateTime time_start = DateTime.Now;
                        //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        try
                        {
                            string sql = "";
                            sql = $@"
SELECT [OEmpNo]
		,[NEmpNo]
	FROM [HR_Oracle_Mapping]
where [NEmpNo] = :0
";

                            DataTable tb = null;
                            foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, empNo))
                            {
                                rst = $"{row["OEmpNo"]}";
                            }
                            conn.Close();
                        }
                        catch (Exception exhr)
                        {
                            log.WriteErrorLog($"GetOldEmpNo 失敗:{exhr.Message}{exhr.InnerException}");
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                rst = empNo;
            }

            return rst;
        }

        public void SuspendAccount(string BatchNo)
        {
            try
            {

                string success = "";
                string errmsg = "";
                sync_oracle.SuspendAccsIfNeeds(BatchNo);

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"SuspendAccount 停用 Oracle 帳號 執行失敗!{ex.Message}{ex.InnerException}");
            }
        }


        bool get_oracle_has_this_lastname(string old_empno, string BatchNo)
        {
            bool rst = false;
            try
            {

                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //if (!Directory.Exists(db_path))
                //{
                //    Directory.CreateDirectory(db_path);
                //}
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);

                try
                {
                    string sql = "";
                    sql = $@"
SELECT *
FROM
WORKERNAMES
WHERE LASTNAME = :0
AND 	  [BatchNo] =  :1
";



                    DataTable tb = null;
                    List<OREmpNoData> suppliers = new List<OREmpNoData>();
                    foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, old_empno, BatchNo))
                    {
                        rst = true;
                    }
                }
                catch (Exception exhr)
                {
                    log.WriteErrorLog($"get_oracle_has_this_lastname 失敗:{exhr.Message}{exhr.InnerException}");
                }

                //using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}", db_file)))
                //{
                //    conn.Open();
                //    SQLiteCommand cmd = new SQLiteCommand(conn);

                //    DateTime time_start = DateTime.Now;
                //    //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                //    conn.Close();
                //}

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"get_oracle_has_this_lastname Error:{ex.Message}{ex.InnerException}");
            }

            return rst;
        }


        /// <summary>
        /// 上正式用，員編用801234建立帳號
        /// </summary>
        /// <param name="EEmpNo"></param>
        /// <param name="BatchNo"></param>
        public void CreateWorkerNewEmpNo(string EEmpNo, string BatchNo)
        {
            try
            {
                log.WriteLog("5", $"準備 Create Worker {EEmpNo} 到 oracle");

                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //if (!Directory.Exists(db_path))
                //{
                //    Directory.CreateDirectory(db_path);
                //}
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);


                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}", db_file)))
                {
                    conn.Open();
                    SQLiteCommand cmd = new SQLiteCommand(conn);

                    DateTime time_start = DateTime.Now;
                    //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                    try
                    {
                        string sql = "";
                        sql = $@"
select *
from [Supplier]
		where [BatchNo] =  :0
and R_CODE = :1
";



                        DataTable tb = null;
                        List<OREmpNoData> suppliers = new List<OREmpNoData>();
                        foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EEmpNo))
                        {
                            string new_empno = $"{row["r_code"]}";
                            //string old_empno = GetOldEmpNo(new_empno);
                            // 測試區用舊員編
                            // for test
                            string empno = new_empno;

                            string PersonNumber = empno;
                            //string PersonNumber = $"{row["idno"]}";
                            string EmpNo = empno;
                            string EmpName = $"{row["r_cname"]}";
                            string EmpEngName = $"{row["r_ename"]}";
                            string email = $"{row["r_email"]}";

                            try
                            {

                                // string r_online_date = $"{row["r_online_date"]}";  //到職日
                                if (!string.IsNullOrWhiteSpace(PersonNumber))
                                {
                                    string success = "false";
                                    string errmsg = "";
                                    //ORSyncOracleData.SyncOracleDataBack sync_oracle = new ORSyncOracleData.SyncOracleDataBack(null, log);
                                    sync_oracle.AddWorkerByMiddle(PersonNumber, EmpNo, EmpName, EmpEngName, email, out success, out errmsg);

                                    //// 在 ORACLE 中找不到 準備新增
                                    //// add user to oracle
                                    //AddWorkerCommand worder = new AddWorkerCommand();
                                    //worder.EmpNo = EmpNo;
                                    //worder.EmpName = EmpName;
                                    //worder.EngName = EmpEngName;
                                    //worder.Email = string.IsNullOrWhiteSpace(email) ? "" : $"{email}@staff.pchome.com.tw";
                                    ////var ret = ApiOperation.CallApi<string>("api/BoxmanOracleEmployee/BoxmanGetUsersByPagesAsync", WebRequestMethods.Http.Post, par);
                                    //DateTime dt_start = DateTime.Now;
                                    //ResponseData<string> ret = ApiOperation.CallApi<string>(new ApiRequestSetting()
                                    //{
                                    //    MethodRoute = "api/BoxmanOracleEmployee/BoxmanAddOracleWorker",
                                    //    Data = worder,
                                    //    MethodType = "POST",
                                    //    TimeOut = 1000 * 60 * 5
                                    //}
                                    //);

                                    //// 檢查 回傳值

                                    //if (ret.StatusCode == 200)
                                    //{
                                    //    string rtndata = ret.Data;
                                    //    string _str = Encoding.UTF8.GetString(Convert.FromBase64String(ret.Data));
                                    //    // 收到 ORACLE 的回傳值
                                    //    WebRequestResult ret2 = Newtonsoft.Json.JsonConvert.DeserializeObject<WebRequestResult>(_str);
                                    //    if (ret2.StatusCode == HttpStatusCode.Created)
                                    //    {
                                    //        log.WriteLog($"{EmpNo} {EmpName} Oracle帳號建立成功:{ret2.ReturnData}");


                                    //    }
                                    //    else
                                    //    {
                                    //        throw new Exception($"{ret2.ErrorMessage}");
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    throw new Exception($"{ret.ErrorMessage}");
                                    //}
                                }
                            }
                            catch (Exception exCreate)
                            {
                                log.WriteErrorLog($"{EmpNo} {EmpName} Oracle帳號建立失敗:{exCreate}");
                            }



                        }

                        //List<OREmpNoData> sups = new List<OREmpNoData>();
                        //foreach (OREmpNoData emp in suppliers)
                        //{
                        //    sups.Clear();
                        //    sups.Add(emp);
                        //    try
                        //    {
                        //        var par = new
                        //        {
                        //            Data = sups
                        //        };
                        //        var ret = ApiOperation.CallApi<string>(new ApiRequestSetting()
                        //        {
                        //            MethodRoute = "api/OREMP/EMPACC/",
                        //            Data = par,
                        //            MethodType = "POST",
                        //            TimeOut = 1000 * 60 * 10  // 5分鐘竟然不夠
                        //        });
                        //        if (ret.Success)
                        //        {
                        //            foreach (var _sup in sups)
                        //            {
                        //                log.WriteLog("5", $"{_sup.EmpNo},{_sup.EmpName} Supplier push 成功!");
                        //            }
                        //        }
                        //        else
                        //        {
                        //            foreach (var _sup in sups)
                        //            {
                        //                log.WriteLog("5", $"{_sup.EmpNo},{_sup.EmpName} Supplier push 失敗! {ret.ErrorMessage} {ret.ErrorException}");
                        //            }
                        //        }
                        //    }
                        //    catch (Exception exPush)
                        //    {
                        //        foreach (var _sup in sups)
                        //        {
                        //            log.WriteLog("5", $"{_sup.EmpNo},{_sup.EmpName} Supplier push 失敗! {exPush.Message} {exPush.InnerException}");
                        //        }
                        //    }




                        //}


                        conn.Close();



                    }
                    catch (Exception exhr)
                    {
                        log.WriteErrorLog($"create worker 失敗:{exhr.Message}{exhr.InnerException}");
                    }
                }

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"create worker Error:{ex.Message}{ex.InnerException}");
            }

        }



        /// <summary>
        /// 從 oracle 同步回來
        /// </summary>
        void SyncFromOracle1(string BatchNo)
        {
            //ORSyncOracleData.SyncOracleDataBack sync_oracle = new ORSyncOracleData.SyncOracleDataBack(conn, log);
            sync_oracle.SyncWorkers(BatchNo);
            sync_oracle.SyncUsers(BatchNo);
        }

        void SyncFromHRFT(string BatchNo)
        {
            try
            {
                //MiddleModel model = new MiddleModel();                
                //model.SendingData = "";
                //model.Method = "GET";
                //ResponseData<string> ret = ApiOperation.CallApi<string>("api/ORHR/GetHRData", WebRequestMethods.Http.Get, null);
                log.WriteLog("5", $"準備取得飛騰資料 api/ORHR/ADAPI/");
                ParseFnModel model = new ParseFnModel();
                model.FnName = "GetEmployeeForOracle";
                model.SendBase64Data = "";
                var ret = ApiOperation.CallApi<string>(new ApiRequestSetting()
                {
                    MethodRoute = "api/ORHR/ADAPI/",
                    Data = model,
                    MethodType = "POST",
                    TimeOut = 1000 * 60 * 5
                });
                if (ret.Success)
                {

                    //System.IO.File.WriteAllText("i:/alvin/ft.json", ret.Data);
                    List<PChomeEmpModel> rtnobj = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PChomeEmpModel>>(ret.Data);
                    //SaveHREmployeesToSqlite(rtnobj, BatchNo);
                    SaveSupplierToSqlite(rtnobj, BatchNo);
                }
                else
                {
                    throw new Exception($"{ret.StatusCode} {ret.StatusDescription} {ret.ErrorMessage} {ret.ErrorException}");
                }


            }
            catch (Exception exhr)
            {
                log.WriteErrorLog($"從 HR 同步回來 失敗:{exhr.Message}{exhr.InnerException}");
            }

        }


        void SaveSupplierToSqlite(List<PChomeEmpModel> emps, string BatchNo)
        {
            try
            {
                log.WriteLog("5", $"準備建立 sqlite tabls 及寫入資料");
                CreateSQLiteFileIfNotExists_HR_Batch();
                CreateMappingTable();
                CreateSQLiteFileIfNotExists_Supplier();
                InsertIntoSupplierTable(emps, BatchNo);
                log.WriteLog("5", $"寫入完畢 BatchNo={BatchNo}");

            }
            catch (Exception exhr)
            {
                log.WriteErrorLog($"SaveHREmployeesToSqlite 失敗:{exhr.Message}{exhr.InnerException}");
            }
        }

        void CreateSQLiteFileIfNotExists_HR_Batch()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_HR_Batch";
            try
            {
                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //if (!Directory.Exists(db_path))
                //{
                //    Directory.CreateDirectory(db_path);
                //}
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);

                string table_name = "HR_Batch";
                db_Batch_file = table_name;
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table users
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) 
)";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM {table_name} ORDER BY BatchNo DESC";
                    string BatchNo = "";
                    int cnt = 0;
                    foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, null))
                    {
                        if (cnt > 2)
                        {
                            BatchNo = $"{row[0]}";
                            break;
                        }
                        cnt++;
                    }
                    if (!string.IsNullOrWhiteSpace(BatchNo))
                    {
                        sql = $"DELETE FROM {table_name} WHERE BATCHNO <= :0";
                        sqlite.ExecuteByCmd(sql, BatchNo);
                    }
                }


            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"{fn_name} 失敗:{ex.Message}{ex.InnerException}");
            }
        }

        void CreateMappingTable()
        {

            string fn_name = "CreateMappingTable";
            try
            {
                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //if (!Directory.Exists(db_path))
                //{
                //    Directory.CreateDirectory(db_path);
                //}
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);

                string table_name = "OracleHRMap";
                db_Batch_file = table_name;
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table users
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"
CREATE TABLE [{table_name}] (
	[PersonNumber] nvarchar(50), 
	[OEmpNo] nvarchar(20), 
	[NEmpNo] nvarchar(20)
)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"{fn_name} 失敗:{ex.Message}{ex.InnerException}");
            }

        }

        void CreateSQLiteFileIfNotExists_Supplier()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Supplier";
            try
            {
                //db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                //if (!Directory.Exists(db_path))
                //{
                //    Directory.CreateDirectory(db_path);
                //}
                //db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "Supplier";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table users

                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
        BatchNo nvarchar(30) ,
        r_code nvarchar(100),
        r_cname nvarchar(100),
        r_ename nvarchar(100),
        r_dept nvarchar(300),
        r_degress nvarchar(100),
        r_cell_phone nvarchar(300),
        r_birthday nvarchar(20),
        r_sex nvarchar(20),
        r_email nvarchar(100),
        r_phone_ext nvarchar(100),
        r_skype_id nvarchar(300),
        r_online_date nvarchar(20),
        r_online nvarchar(20),
        r_offline_date nvarchar(20),
        departname nvarchar(100),
        dutyid nvarchar(100),
        workplaceid nvarchar(100),
        Workplacename nvarchar(100),
        idno nvarchar(20),
        salaccountname nvarchar(100),
        bankcode nvarchar(100),
        bankbranch nvarchar(100),
        salaccountid nvarchar(100),
        jobstatus nvarchar(10)
)";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (r_code);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM {table_name} ORDER BY BatchNo DESC";
                    string BatchNo = "";
                    int cnt = 0;
                    foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, null))
                    {
                        if (cnt > 2)
                        {
                            BatchNo = $"{row[0]}";
                            break;
                        }
                        cnt++;
                    }
                    if (!string.IsNullOrWhiteSpace(BatchNo))
                    {
                        sql = $"DELETE FROM {table_name} WHERE BATCHNO <= :0";
                        sqlite.ExecuteByCmd(sql, BatchNo);
                    }
                }


            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"{fn_name} 失敗:{ex.Message}{ex.InnerException}");
            }
        }


        /// <summary>
        /// 寫入 supplier Table
        /// </summary>
        /// <param name="emps"></param>
        /// <param name="BatchNo"></param>
        void InsertIntoSupplierTable(List<PChomeEmpModel> emps, string BatchNo)
        {
            db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
            if (!Directory.Exists(db_path))
            {
                Directory.CreateDirectory(db_path);
            }
            db_file = $"{db_path}OracleData.sqlite";
            using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}", db_file)))
            {
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn);

                DateTime time_start = DateTime.Now;
                //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                try
                {
                    string sql = "";
                    sql = $@"
INSERT INTO [Supplier]
		([BatchNo]
		,[r_code]
		,[r_cname]
		,[r_ename]
		,[r_dept]
		,[r_degress]
		,[r_cell_phone]
		,[r_birthday]
		,[r_sex]
		,[r_email]
		,[r_phone_ext]
		,[r_skype_id]
		,[r_online_date]
		,[r_online]
		,[r_offline_date]
		,[departname]
		,[dutyid]
		,[workplaceid]
		,[workplacename]
		,[idno]
		,[salaccountname]
		,[bankcode]
		,[bankbranch]
		,[salaccountid]
		,[jobstatus]

)
	VALUES (
	:0,
	:1,
	:2,
	:3,
	:4,
	:5,
	:6,
	:7,
	:8,
	:9,
	:10,
	:11,
	:12,
	:13,
	:14,
	:15,
	:16,
	:17,
	:18,
	:19,
	:20,
	:21,
	:22,
	:23,
	:24
)
";
                    cmd.CommandText = sql;
                    int total = 0;
                    foreach (PChomeEmpModel emp in emps)
                    {
                        // 網家都是 8 開頭
                        if (!emp.RCode.StartsWith("8"))
                        {
                            continue;
                        }

                        cmd.Parameters.Clear();
                        cmd.Parameters.Add(new SQLiteParameter("0", BatchNo));
                        cmd.Parameters.Add(new SQLiteParameter("1", emp.RCode));
                        cmd.Parameters.Add(new SQLiteParameter("2", emp.RCname));
                        cmd.Parameters.Add(new SQLiteParameter("3", emp.REname));
                        cmd.Parameters.Add(new SQLiteParameter("4", emp.RDept));
                        cmd.Parameters.Add(new SQLiteParameter("5", emp.RDegress));
                        cmd.Parameters.Add(new SQLiteParameter("6", emp.RCellPhone));
                        cmd.Parameters.Add(new SQLiteParameter("7", emp.RBirthday));
                        cmd.Parameters.Add(new SQLiteParameter("8", emp.RSex));
                        cmd.Parameters.Add(new SQLiteParameter("9", emp.REmail));
                        cmd.Parameters.Add(new SQLiteParameter("10", emp.RPhoneExt));
                        cmd.Parameters.Add(new SQLiteParameter("11", emp.RSkypeId));
                        cmd.Parameters.Add(new SQLiteParameter("12", emp.ROnlineDate));
                        cmd.Parameters.Add(new SQLiteParameter("13", emp.ROnline));
                        cmd.Parameters.Add(new SQLiteParameter("14", emp.ROfflineDate));
                        cmd.Parameters.Add(new SQLiteParameter("15", emp.Departname));
                        cmd.Parameters.Add(new SQLiteParameter("16", emp.Dutyid));
                        cmd.Parameters.Add(new SQLiteParameter("17", emp.Workplaceid));
                        cmd.Parameters.Add(new SQLiteParameter("18", emp.Workplacename));
                        cmd.Parameters.Add(new SQLiteParameter("19", emp.Idno));
                        cmd.Parameters.Add(new SQLiteParameter("20", emp.Salaccountname));
                        cmd.Parameters.Add(new SQLiteParameter("21", emp.Bankcode));
                        cmd.Parameters.Add(new SQLiteParameter("22", emp.Bankbranch));
                        cmd.Parameters.Add(new SQLiteParameter("23", emp.Salaccountid));
                        cmd.Parameters.Add(new SQLiteParameter("24", emp.Jobstatus));

                        int cnt = cmd.ExecuteNonQuery();
                        total += cnt;
                    }
                    log.WriteLog("5", $"從飛騰同步了 {total} 筆回來");


                    //                    sql = $@"
                    //INSERT INTO [HR_Batch]
                    //		([BatchNo])
                    //	VALUES
                    //		(:BatchNo)
                    //";
                    //                    cmd.CommandText = sql;
                    //                    cmd.Parameters.Clear();
                    //                    cmd.Parameters.Add(new SQLiteParameter("BatchNo", BatchNo));
                    //                    int cnt2 = cmd.ExecuteNonQuery();

                    //                    Console.WriteLine($"insert need {DateTime.Now.Subtract(time_start).TotalSeconds} sec(s).");

                    //                    // 這裡要再加上 刪除3天以前的資料
                    //                    string B4BatchNo = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    //                    sql = $@"DELETE FROM [HR_Employee] WHERE [BatchNo] <= :BatchNo";
                    //                    cmd.Parameters.Clear();
                    //                    cmd.CommandText = sql;
                    //                    cmd.Parameters.Clear();
                    //                    cmd.Parameters.Add(new SQLiteParameter("BatchNo", B4BatchNo));
                    //                    cnt2 = cmd.ExecuteNonQuery();

                    //                    sql = $@"DELETE FROM [HR_Batch]  WHERE [BatchNo] <= :BatchNo";
                    //                    cmd.Parameters.Clear();
                    //                    cmd.CommandText = sql;
                    //                    cmd.Parameters.Clear();
                    //                    cmd.Parameters.Add(new SQLiteParameter("BatchNo", B4BatchNo));
                    //                    cnt2 = cmd.ExecuteNonQuery();

                    conn.Close();



                }
                catch (Exception exhr)
                {
                    log.WriteErrorLog($"InsertIntoHRTable 失敗:{exhr.Message}{exhr.InnerException}");
                }

            }
        }




    }
}
