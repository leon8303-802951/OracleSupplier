using AQuartzJobUTL;
using BoxmanBase;
using LogMgr;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OracleNewQuitEmployee.ORSyncOracleData.Model;
using OracleNewQuitEmployee.ORSyncOracleData.Model.EmpAsm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OracleSupplier.Models.UpdateSupplierBankData;
using Winnie.DataAccess.Client;

namespace OracleNewQuitEmployee.ORSyncOracleData
{

    public class SyncOracleDataBack
    {

        keroroConnectBase.keroroConn conn;

        private LogOput log;


        public string Oracle_AP { get; set; }
        public string Oracle_Domain { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// 建構式
        /// </summary>
        public SyncOracleDataBack(keroroConnectBase.keroroConn Conn, LogOput Log)
        {
            conn = Conn;
            log = Log;
            #region 機敏處理
            try
            {
                keroroConnectBase.OracleCommand _sql = new keroroConnectBase.OracleCommand();
                string sql = @"
SELECT OCLCHKOPN,OCLVAL1 ,OCLVAL2 ,OCLCHKNAME
  FROM ECOPER.OR_CHKOPNLST
 WHERE OCLCHKNAME in (:pOCLCHKNAME1,:pOCLCHKNAME2)";
                var Data = new List<OracleParameter>();
                /*因為用到的資料都放在同一個DB，依Value去抓想要的資料*/
                Data.Add(new OracleParameter { ParameterName = "pOCLCHKNAME1", Direction = ParameterDirection.Input, Value = "OracleSupplier_JOB_TEST", OracleDbType = OracleDbType.Varchar2 });
                Data.Add(new OracleParameter { ParameterName = "pOCLCHKNAME2", Direction = ParameterDirection.Input, Value = "OracleSupplier_JOB", OracleDbType = OracleDbType.Varchar2 });
                Data.ForEach(x => _sql.Parameters.Add(x));
                _sql.CommandText = sql;
                var temp = _sql.ExecuteQuery().Tables[0];

                if (temp.Rows.Count > 0)
                {
                    foreach (DataRow Row in temp.Rows)
                    {
                        switch (Row["OCLCHKNAME"].ToString())
                        {
                            //測試
                            case "OracleSupplier_JOB_TEST":
                                //Oracle_AP = Row["OCLCHKOPN"].ToString();
                                //Oracle_Domain = Row["OCLVAL1"].ToString();
                                //ap53URLBase = Row["OCLVAL2"].ToString();
                                break;
                            //正式
                            case "OracleSupplier_JOB":
                                Oracle_AP = Row["OCLCHKOPN"].ToString();
                                Oracle_Domain = Row["OCLVAL1"].ToString();
                                ap53URLBase = Row["OCLVAL2"].ToString();
                                break;
                        }
                    }
                }
                if (string.IsNullOrWhiteSpace(Oracle_AP) || string.IsNullOrWhiteSpace(Oracle_Domain) || string.IsNullOrWhiteSpace(ap53URLBase))
                {
                    throw new Exception("參數有誤");
                }
            }
            catch (Exception e)
            {
                log.WriteLog($"查詢參數發生錯誤， 失敗:{e.Message}{e.InnerException}");
                return;
            }           
            #endregion

            db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
            if (!Directory.Exists(db_path))
            {
                Directory.CreateDirectory(db_path);
            }
            db_file = $"{db_path}OracleData.sqlite";
            sqlite = new SQLiteUtl(db_file);

        }
        SQLiteUtl sqlite;
        DataTable tb;

        string db_path;
        string db_file;

        public string ap53URLBase { get; set; }

        public void SuspendAccsIfNeeds(string BatchNo)
        {
            var empno = "";
            var success = "";
            var errmsg = "";
            //            var sql = $@"
            //select r_code
            //from supplier
            //where batchno = :0
            //and jobstatus in ('0','5')
            //";

            var sql = $@"

SELECT  
s.r_code
, w.lastname
, s.r_cname
, jobstatus 
		,[SuspendedFlag] 
		,s.*
	FROM supplier s
	left JOIN  [WorkerNames] W on s.r_code = w.lastname and s.batchno = w.batchno
	left join [OracleUsers] U ON U.PERSONID = W.PERSONID AND U.BATCHNO = W.BATCHNO	
	WHERE s.BATCHNO = :0 
	and jobstatus in ('0','5')
";


            DataTable tb;
            foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo))
            {
                empno = $"{row["r_code"]}";
                SuspendAccount(BatchNo, empno, "false", out success, out errmsg);
            }
        }


        public void SuspendAccount(string BatchNo, string EmpNo, string Enable, out string success, out string errmsg)
        {
            success = "false";
            errmsg = "";
            try
            {
                var empname = "";
                var id = "";
                var sql = $@"

SELECT 
U.GUID, N.FIRSTNAME
	FROM [OracleUsers] U
	JOIN ORACLEWORKERS W ON U.PERSONID = W.PERSONID AND U.BATCHNO = W.BATCHNO
	JOIN  WORKERNAMES N ON U.PERSONID = N.PERSONID AND U.BATCHNO = N.BATCHNO
	where  N.LASTNAME = :0
	AND U.BATCHNO = :1

";

                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, EmpNo, BatchNo))
                {
                    id = $"{row["GUID"]}";
                    empname = $"{row["FIRSTNAME"]}";
                }

                if (!string.IsNullOrWhiteSpace(id))
                {
                    var tmp = "";
                    if (Enable.ToUpper() == "TRUE")
                    {
                        log.WriteLog("5", $"開始嘗試 啟用 {EmpNo}");
                    }
                    else if (Enable.ToUpper() == "FALSE")
                    {
                        log.WriteLog("5", $"開始嘗試 啟用 {EmpNo}");
                    }
                    else
                    {
                        log.WriteLog("5", $"開始嘗試 Enable = {Enable} {EmpNo}");
                    }
                    SuspendUserModel model = new SuspendUserModel();
                    model.Active = Enable;
                    model.Schemas = new string[] { "urn:scim:schemas:core:2.0:User" };


                    MiddleModel2 send_model2 = new MiddleModel2();
                    string url = $"{Oracle_Domain}/hcmRestApi/scim/Users/{id}";
                    log.WriteLog("5", $"SuspendAccount  url={url}");
                    string payload = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                    MiddleReturn mr = HttpPatchFromOracleAP(url, model);
                    if (mr.StatusCode == "200")
                    {
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        if (string.IsNullOrWhiteSpace(desc_str))
                        {
                            throw new Exception("取得  Worker names 失敗, 回傳空白!");
                        }
                        SuspendUserReturnModel usrrtn = Newtonsoft.Json.JsonConvert.DeserializeObject<SuspendUserReturnModel>(desc_str);
                        //log.WriteLog("5", $"SuspendAccount Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName}){Environment.NewLine}{desc_str}");
                        var rtn_enable = usrrtn.Active.ToUpper();
                        if (rtn_enable == Enable.ToUpper())
                        {
                            success = "true";
                            var word = "啟用";
                            if (rtn_enable == "TRUE")
                            {
                                word = "啟用";
                            }
                            if (rtn_enable == "FALSE")
                            {
                                word = "停用";
                            }

                            log.WriteLog("5", $"EmpNO:{EmpNo} {empname} 已 {word}");
                        }
                        else
                        {
                            log.WriteErrorLog($"SuspendAccount 操作失敗: Enable 狀態沒有改變!");

                        }
                    }
                    else
                    {
                        throw new Exception($"{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}");
                    }









                    //send_model2.URL = url;
                    //log.WriteLog("5", $"SuspendAccount payload={payload}");
                    //send_model2.SendingData = payload;
                    //send_model2.Method = "PATCH";
                    ////string username = this.UserName;
                    ////string password = this.Password;
                    //send_model2.UserName = this.UserName;
                    //send_model2.Password = this.Password;
                    //string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                    //send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                    ////CredentialCache myCred = new CredentialCache();
                    ////myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                    ////send_model2.Cred = myCred;
                    //send_model2.Timeout = 1000 * 60 * 30;


                    //// for BOXMAN API
                    //MiddleModel send_model = new MiddleModel();
                    //var _url = $"{Oracle_AP}/api/Middle/Call/";
                    //send_model.URL = _url;
                    //send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                    //send_model.Method = "POST";
                    //send_model.Timeout = 1000 * 60 * 30;
                    //var ret = ApiOperation.CallApi(
                    //        new ApiRequestSetting()
                    //        {
                    //            Data = send_model,
                    //            MethodRoute = "api/Middle/Call",
                    //            MethodType = "POST",
                    //            TimeOut = 1000 * 60 * 30
                    //        }
                    //        );
                    //if (ret.Success)
                    //{
                    //    string receive_str = ret.ResponseContent;
                    //    try
                    //    {
                    //        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                    //        if (!string.IsNullOrWhiteSpace(mr2.ErrorMessage))
                    //        {
                    //            throw new Exception(mr2.ErrorMessage);
                    //        }
                    //        if (string.IsNullOrWhiteSpace(mr2.ReturnData))
                    //        {
                    //            throw new Exception("mr2.ReturnData is null, 伺服器回傳空白!");
                    //        }
                    //        MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                    //        if (mr.StatusCode == "200")
                    //        {
                    //            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                    //            string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                    //            if (string.IsNullOrWhiteSpace(desc_str))
                    //            {
                    //                throw new Exception("取得  Worker names 失敗, 回傳空白!");
                    //            }
                    //            SuspendUserReturnModel usrrtn = Newtonsoft.Json.JsonConvert.DeserializeObject<SuspendUserReturnModel>(desc_str);
                    //            //log.WriteLog("5", $"SuspendAccount Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName}){Environment.NewLine}{desc_str}");
                    //            var rtn_enable = usrrtn.Active.ToUpper();
                    //            if (rtn_enable == Enable.ToUpper())
                    //            {
                    //                success = "true";
                    //                var word = "啟用";
                    //                if (rtn_enable == "TRUE")
                    //                {
                    //                    word = "啟用";
                    //                }
                    //                if (rtn_enable == "FALSE")
                    //                {
                    //                    word = "停用";
                    //                }

                    //                log.WriteLog("5", $"EmpNO:{EmpNo} {empname} 已 {word}");
                    //            }
                    //            else
                    //            {
                    //                log.WriteErrorLog($"SuspendAccount 操作失敗: Enable 狀態沒有改變!");

                    //            }
                    //        }
                    //        else
                    //        {
                    //            throw new Exception($"{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}");
                    //        }
                    //    }
                    //    catch (Exception exbs64)
                    //    {
                    //        log.WriteErrorLog($"SuspendAccount 失敗:{exbs64.Message}{exbs64.InnerException}");
                    //    }
                    //}
                    //else
                    //{
                    //    log.WriteErrorLog($"SuspendAccount Call Boxman api {_url} 失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                    //}


                }
                else
                {
                    log.WriteErrorLog($"SuspendAccount check id is null!");

                }


            }
            catch (Exception ex)
            {

                errmsg = $"SuspendAccount 失敗:EmpNo={EmpNo}, {ex.Message}{ex.InnerException}";
                log.WriteErrorLog(errmsg);
            }
        }

        /// <summary>
        /// 取得 boxman ap 中的  _boxmanUtilitiesService.OracleApiUrl(); 值
        /// </summary>
        /// <returns></returns>
        public string TestGet()
        {
            string rst = "";
            ORGetData pars = new ORGetData();
            var ret = ApiOperation.CallApi(
                    new ApiRequestSetting()
                    {
                        MethodRoute = "api/OREMP/TestGet",
                        MethodType = "GET",
                        TimeOut = 1000 * 60 * 30
                    }
                    );
            if (ret.Success)
            {
                rst = ret.ResponseContent;
            }


            return rst;
        }
        /// <summary>
        /// 取得 boxman ap 中的  _boxmanUtilitiesService.OracleApiUrl(); 值
        /// </summary>
        /// <returns></returns>
        public string TestPost(string fn)
        {
            string rst = "";
            ORGetData pars = new ORGetData();
            pars.FunctionName = fn;
            var ret = ApiOperation.CallApi(
                    new ApiRequestSetting()
                    {
                        Data = pars,
                        MethodRoute = "api/OREMP/TestPost",
                        MethodType = "POST",
                        TimeOut = 1000 * 60 * 30
                    }
                    );
            if (ret.Success)
            {
                rst = ret.ResponseContent;
            }


            return rst;
        }

        /// <summary>
        /// 取得 boxman ap 中的  _boxmanUtilitiesService.OracleApiUrl(); 值
        /// </summary>
        /// <returns></returns>
        public string GetOracleApDomainName()
        {
            string rst = "";
            ORGetData pars = new ORGetData();
            pars.FunctionName = "GetOracleApDomainName";
            var ret = ApiOperation.CallApi(
                    new ApiRequestSetting()
                    {
                        Data = pars,
                        MethodRoute = "api/OREMP/ORGetData",
                        MethodType = "POST",
                        TimeOut = 1000 * 60 * 30
                    }
                    );
            if (ret.Success)
            {
                rst = ret.ResponseContent;
                ResultObject<string> _rst2 = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultObject<string>>(rst);
                if (_rst2.Result)
                {
                    var url = _rst2.Data;
                    log.WriteLog("5", $"取得阿中的 oracle ap 位置:{url}");                    
                    var urls = url.Split(new string[] { "uri=" }, StringSplitOptions.None);
                    foreach (var _u in urls)
                    {
                        int pos1 = _u.ToLower().IndexOf("/api/");
                        if (pos1 >= 0)
                        {
                            rst = _u.Substring(0, pos1);
                            Oracle_AP = rst;
                        }
                    }
                }
            }


            return rst;
        }

        /// <summary>
        /// Create oracle 帳號，同時 update username/部門/主管員編/job level
        /// </summary>
        /// <param name="PersonNumber"></param>
        /// <param name="EmpNo"></param>
        /// <param name="EmpName"></param>
        /// <param name="EngName"></param>
        /// <param name="Email"></param>
        /// <param name="DeptNo">部門代碼</param>
        /// <param name="ManagerEmpNo">主管員編</param>
        /// <param name="JobLevel">0,1,2,3,4  空白=0</param>
        /// <param name="success"></param>
        /// <param name="errmsg"></param>
        public void CreateWorkerByMiddle(string PersonNumber, string EmpNo, string EmpName,
            string EngName, string Email, string DeptNo, string ManagerEmpNo, string JobLevel,
            out string success, out string errmsg)
        {
            success = "false";
            errmsg = "";
            //log.WriteLog("5", $"CreateWorkerByMiddle, EmpNo={EmpNo}, EmpName={EmpName} 準備新增 oracle worker");
            try
            {
                // ORACLE STAGE
                // for  oracle ap
                string email = Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new Exception($"EmpNo={EmpNo}, EmpName={EmpName}, 沒有 email, 無法建立 Oracle Worker!");
                }
                else
                {
                    if (email.IndexOf('@') < 0)
                    {
                        email = $"{email}@staff.pchome.com.tw";
                    }
                }
                //CreateWorkerModel4 model = new CreateWorkerModel4();
                //model.Names[0].FirstName = EmpName;
                //model.Names[0].MiddleNames = EngName;
                //model.Names[0].LastName = EmpNo;
                //model.Emails[0].EmailType = "W1";
                //model.Emails[0].EmailAddress = email;

                CreateWorkerModel3 model = new CreateWorkerModel3();
                model.PersonNumber = PersonNumber;
                model.Names[0].FirstName = EmpName;
                model.Names[0].MiddleNames = EngName;
                model.Names[0].LastName = EmpNo;
                model.Emails[0].EmailType = "W1";
                model.Emails[0].EmailAddress = email;


                MiddleModel2 send_model2 = new MiddleModel2();
                string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/workers";
                //log.WriteLog("5", $"CreateWorkerByMiddle  url={url}");
                send_model2.URL = url;
                string payload = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                //log.WriteLog("5", $"CreateWorkerByMiddle payload={payload}");
                send_model2.SendingData = payload;
                send_model2.Method = "POST";
                //string username = this.UserName;
                //string password = this.Password;
                send_model2.UserName = this.UserName;
                send_model2.Password = this.Password;
                string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                //CredentialCache myCred = new CredentialCache();
                //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                //send_model2.Cred = myCred;
                send_model2.Timeout = 1000 * 60 * 30;


                // for BOXMAN API
                MiddleModel send_model = new MiddleModel();
                var _url = $"{Oracle_AP}/api/Middle/Call/";
                send_model.URL = _url;
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model,
                            MethodRoute = "api/Middle/Call",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                    if (!string.IsNullOrWhiteSpace(mr2.ErrorMessage))
                    {
                        throw new Exception($"建立Worker失敗:{mr2.ErrorMessage}");
                    }
                    if (string.IsNullOrWhiteSpace(mr2.ReturnData))
                    {
                        throw new Exception("mr2.ReturnData is null, 伺服器回傳空白!");
                    }
                    MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                    if (mr.StatusCode == "201")
                    {
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        Worker worker = Newtonsoft.Json.JsonConvert.DeserializeObject<Worker>(desc_str);
                        log.WriteLog("5", $"CreateWorkerByMiddle Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName})");
                        //log.WriteLog("5", $"CreateWorkerByMiddle Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName}){Environment.NewLine}{desc_str}");

                        //update username
                        先檢查Oracle的UserName不一樣才更新(EmpNo, EmpNo);
                        //部門/主管員編/job level
                        更新DefaultExpenseAccount_JobLevel_LineManager(EmpNo, DeptNo, ManagerEmpNo, JobLevel);
                        success = "true";
                    }
                    else
                    {
                        throw new Exception($"{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}");
                    }
                }
                else
                {
                    var _msg = $"CreateWorkerByMiddle Call Boxman Api {_url} 失敗:{ret.ErrorMessage}. {ret.ErrorException}";
                    log.WriteErrorLog(_msg);
                    throw new Exception(_msg);
                }
            }
            catch (Exception ex)
            {
                errmsg = $"CreateWorkerByMiddle 失敗:{ex.Message}{ex.InnerException}";
                log.WriteErrorLog(errmsg);
                throw;
            }

        }

        /// <summary>
        ///  新增 oracle 帳號, 但是沒有update部門/主管員編/job level
        /// </summary>
        /// <param name="PersonNumber"></param>
        /// <param name="EmpNo"></param>
        /// <param name="EmpName"></param>
        /// <param name="EngName"></param>
        /// <param name="Email"></param>
        /// <param name="success"></param>
        /// <param name="errmsg"></param>
        public void AddWorkerByMiddle(string PersonNumber, string EmpNo, string EmpName,
            string EngName, string Email, out string success, out string errmsg)
        {
            success = "false";
            errmsg = "";
            log.WriteLog("5", $"EmpNo={EmpNo}, EmpName={EmpName} 準備建立 oracle worker. (AddWorkerByMiddle)");
            try
            {
                // ORACLE STAGE
                // for  oracle ap
                string email = Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new Exception($"EmpNo={EmpNo}, EmpName={EmpName}, 沒有 email, 無法建立 Oracle Worker!");
                }
                else
                {
                    if (email.IndexOf('@') < 0)
                    {
                        email = $"{email}@staff.pchome.com.tw";
                    }
                }
                //CreateWorkerModel4 model = new CreateWorkerModel4();
                //model.Names[0].FirstName = EmpName;
                //model.Names[0].MiddleNames = EngName;
                //model.Names[0].LastName = EmpNo;
                //model.Emails[0].EmailType = "W1";
                //model.Emails[0].EmailAddress = email;

                CreateWorkerModel3 model = new CreateWorkerModel3();
                model.PersonNumber = PersonNumber;
                model.Names[0].FirstName = EmpName;
                model.Names[0].MiddleNames = EngName;
                model.Names[0].LastName = EmpNo;
                model.Emails[0].EmailType = "W1";
                model.Emails[0].EmailAddress = email;


                MiddleModel2 send_model2 = new MiddleModel2();
                string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/workers";
                log.WriteLog("5", $"url={url}  (AddWorkerByMiddle)");
                send_model2.URL = url;
                string payload = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                log.WriteLog("5", $"payload={payload}  (AddWorkerByMiddle)");
                send_model2.SendingData = payload;
                send_model2.Method = "POST";
                //string username = this.UserName;
                //string password = this.Password;
                send_model2.UserName = this.UserName;
                send_model2.Password = this.Password;
                string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                //CredentialCache myCred = new CredentialCache();
                //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                //send_model2.Cred = myCred;
                send_model2.Timeout = 1000 * 60 * 30;


                // for BOXMAN API
                MiddleModel send_model = new MiddleModel();
                var _url = $"{Oracle_AP}/api/Middle/Call/";
                send_model.URL = _url;
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model,
                            MethodRoute = "api/Middle/Call",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    try
                    {
                        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                        if (!string.IsNullOrWhiteSpace(mr2.ErrorMessage))
                        {
                            throw new Exception(mr2.ErrorMessage);
                        }
                        if (string.IsNullOrWhiteSpace(mr2.ReturnData))
                        {
                            throw new Exception("mr2.ReturnData is null, 伺服器回傳空白!");
                        }
                        MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                        if (mr.StatusCode == "201")
                        {
                            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                            string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                            Worker worker = Newtonsoft.Json.JsonConvert.DeserializeObject<Worker>(desc_str);
                            log.WriteLog("5", $"Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName})  (AddWorkerByMiddle )");
                            //log.WriteLog("5", $"AddWorkerByMiddle Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName}){Environment.NewLine}{desc_str}");
                            success = "true";

                        }
                        else
                        {
                            throw new Exception($"{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}");
                        }
                    }
                    catch (Exception exbs64)
                    {
                        log.WriteErrorLog($"AddWorkerByMiddle Create Worker 失敗:{exbs64.Message}{exbs64.InnerException}");
                    }
                }
                else
                {
                    log.WriteErrorLog($"AddWorkerByMiddle Call Boxman Api {_url} 失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                errmsg = $"AddWorkerByMiddle 失敗:{ex.Message}{ex.InnerException}";
                log.WriteErrorLog(errmsg);
            }

        }

        /// <summary>
        ///  新增 oracle 帳號
        /// </summary>
        /// <param name="PersonNumber"></param>
        /// <param name="EmpNo"></param>
        /// <param name="EmpName"></param>
        /// <param name="EngName"></param>
        /// <param name="Email"></param>
        /// <param name="success"></param>
        /// <param name="errmsg"></param>
        public void AddWorkerByMiddle2(string PersonNumber, string EmpNo, string EmpName,
            string EngName, string Email, string DeptNo, string JobLevel, string ManagerEmpNo,
            out string success, out string errmsg)
        {
            success = "false";
            errmsg = "";
            log.WriteLog("5", $"AddWorkerByMiddle2, EmpNo={EmpNo}, EmpName={EmpName} 準備新增 oracle worker");
            try
            {
                // ORACLE STAGE
                // for  oracle ap
                string email = Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new Exception($"EmpNo={EmpNo}, EmpName={EmpName}, 沒有 email, 無法建立 Oracle Worker!");
                }
                else
                {
                    if (email.IndexOf('@') < 0)
                    {
                        email = $"{email}@staff.pchome.com.tw";
                    }
                }
                //CreateWorkerModel4 model = new CreateWorkerModel4();
                //model.Names[0].FirstName = EmpName;
                //model.Names[0].MiddleNames = EngName;
                //model.Names[0].LastName = EmpNo;
                //model.Emails[0].EmailType = "W1";
                //model.Emails[0].EmailAddress = email;

                CreateWorkerModel3 model = new CreateWorkerModel3();
                model.PersonNumber = PersonNumber;
                model.Names[0].FirstName = EmpName;
                model.Names[0].MiddleNames = EngName;
                model.Names[0].LastName = EmpNo;
                model.Emails[0].EmailType = "W1";
                model.Emails[0].EmailAddress = email;


                MiddleModel2 send_model2 = new MiddleModel2();
                string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/workers";
                log.WriteLog("5", $"AddWorkerByMiddle  url={url}");
                send_model2.URL = url;
                string payload = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                log.WriteLog("5", $"AddWorkerByMiddle payload={payload}");
                send_model2.SendingData = payload;
                send_model2.Method = "POST";
                //string username = this.UserName;
                //string password = this.Password;
                send_model2.UserName = this.UserName;
                send_model2.Password = this.Password;
                string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                //CredentialCache myCred = new CredentialCache();
                //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                //send_model2.Cred = myCred;
                send_model2.Timeout = 1000 * 60 * 30;


                // for BOXMAN API
                MiddleModel send_model = new MiddleModel();
                var _url = $"{Oracle_AP}/api/Middle/Call/";
                send_model.URL = _url;
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model,
                            MethodRoute = "api/Middle/Call",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    try
                    {
                        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                        if (!string.IsNullOrWhiteSpace(mr2.ErrorMessage))
                        {
                            throw new Exception(mr2.ErrorMessage);
                        }
                        if (string.IsNullOrWhiteSpace(mr2.ReturnData))
                        {
                            throw new Exception("mr2.ReturnData is null, 伺服器回傳空白!");
                        }
                        MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                        if (mr.StatusCode == "201")
                        {
                            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                            string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                            Worker worker = Newtonsoft.Json.JsonConvert.DeserializeObject<Worker>(desc_str);
                            log.WriteLog("5", $"AddWorkerByMiddle Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName})");
                            //log.WriteLog("5", $"AddWorkerByMiddle Create Worker 成功(EmpNo={EmpNo}, EmpName={EmpName}){Environment.NewLine}{desc_str}");
                            success = "true";

                        }
                        else
                        {
                            throw new Exception($"{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}");
                        }
                    }
                    catch (Exception exbs64)
                    {
                        log.WriteErrorLog($"AddWorkerByMiddle Create Worker 失敗:{exbs64.Message}{exbs64.InnerException}");
                    }
                }
                else
                {
                    log.WriteErrorLog($"AddWorkerByMiddle Call Boxman Api {_url} 失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                errmsg = $"AddWorkerByMiddle 失敗:{ex.Message}{ex.InnerException}";
                log.WriteErrorLog(errmsg);
            }
        }

        private string GetGUIDByEmpNo(string EmpNo)
        {
            string rst = "";
            try
            {

                string sql = $@"

select u.guid
from workernames n
join oracleusers u on n.personnumber = u.personnumber
and n.batchno =  u.batchno 
where lastname = '{EmpNo}'
and u.batchno = (
select max(batchno)
from batch
)
";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                rst = sqlite.ExecuteScalarA(sql);
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"取得User GUID 失敗:{ex}");
            }
            return rst;
        }

        public List<OracleEmployee2> OracleEmployeesIncludeAssignmentsCollection;

        public List<OracleEmployee2> GetAllOracleEmployeesIncludeAssignments(out bool Success)
        {
            Success = false;
            OracleEmployeesIncludeAssignmentsCollection = new List<OracleEmployee2>();
            int offset = 0;
            var url = "";
            var hasData = true;
            while (hasData)
            {

                url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/emps?expand=assignments&limit=500&offset={offset}";
                try
                {


                    //  //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                    MiddleReturn mr = HttpGetFromOracleAP(url);
                    if (!string.IsNullOrWhiteSpace(mr.ErrorMessage))
                    {
                        throw new Exception($"GetAllOracleEmployeesIncludeAssignments 失敗:{mr.ErrorMessage}");
                    }
                    var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                    var desc_str = Encoding.UTF8.GetString(bs64_bytes);

                    OracleEmployeeIncludeAssignmentsCollection2 emps = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeIncludeAssignmentsCollection2>(desc_str);
                    if (emps != null && emps.Count > 0)
                    {
                        OracleEmployeesIncludeAssignmentsCollection.AddRange(emps.Items.ToList());
                        if (emps.HasMore)
                        {
                            hasData = true;
                            offset += 500;
                        }
                        else
                        {
                            hasData = false;
                        }
                    }
                    else
                    {
                        hasData = false;
                    }
                }
                catch (Exception ex)
                {
                    log.WriteErrorLog($"GetAllOracleEmployees 呼叫 api 失敗:api:{url}\r\n{ex.Message}{ex.InnerException}");
                    throw;
                }

            }
            Success = true;
            return OracleEmployeesIncludeAssignmentsCollection;
        }

        public List<OracleEmployee> GetAllOracleEmployees()
        {
            List<OracleEmployee> rst = new List<OracleEmployee>();
            int offset = 0;
            var url = "";
            var hasData = true;
            while (hasData)
            {

                url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/emps?limit=500&offset={offset}";
                try
                {


                    //  //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                    MiddleReturn mr = HttpGetFromOracleAP(url);
                    if (!string.IsNullOrWhiteSpace(mr.ErrorMessage))
                    {
                        throw new Exception($"GetAllOracleEmployees 失敗:{mr.ErrorMessage}");
                    }
                    var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                    var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                    OracleEmployees emps = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployees>(desc_str);
                    if (emps != null && emps.Count > 0)
                    {
                        rst.AddRange(emps.Items.ToList());
                        if (emps.HasMore)
                        {
                            hasData = true;
                            offset += 500;
                        }
                        else
                        {
                            hasData = false;
                        }
                    }
                    else
                    {
                        hasData = false;
                    }
                }
                catch (Exception ex)
                {
                    log.WriteErrorLog($"GetAllOracleEmployees 呼叫 api 失敗:api:{url}\r\n{ex.Message}{ex.InnerException}");
                    throw;
                }

            }

            return rst;
        }


        public void UpdateLineManager(string EmployeeAssignmentsSelfUrl,
            string PersonId, string AssignmentId, out bool success, out string errmsg)
        {
            success = false;
            errmsg = "";
            try
            {
                var content1 = new
                {
                    ManagerAssignmentId = AssignmentId, //如果在Create Worker中已獲取並保存，
                                                        //则此步可不用执行；
                                                        //若未保存，则需要执行查询獲取。 
                    ActionCode = "MANAGER_CHANGE",
                    ManagerId = PersonId, //ManagerId为Manager這個用戶所對應的PersonId
                    ManagerType = "LINE_MANAGER"
                };
                var mr3 = HttpPatchFromOracleAP(EmployeeAssignmentsSelfUrl, content1);
                if (mr3.StatusCode == "200")
                {
                    //log.WriteLog("5", $"更新 Line Manager 為 {ManagerEmpNo} 成功!");
                    //byte[] bs64_bytes1 = Convert.FromBase64String(mr2.ReturnData);
                    //string desc_str1 = Encoding.UTF8.GetString(bs64_bytes1);
                    success = true;
                }
                else
                {
                    errmsg = $"{mr3.ErrorMessage}{mr3.ReturnData}";
                }
            }
            catch (Exception ex)
            {
                errmsg = $"{ex.Message}";
            }

        }

        public void GetEmployeePersonIDAssignmentIDByEmpNo(string EmpNo,
            out string PersonID, out string AssignmentID)
        {
            PersonID = "";
            AssignmentID = "";

            //這個 &expand=assignments 是取得 employee 後
            //json結構裡有個 assignments 的子結構
            //子結構就可以用 expand=xxx 來直接取得
            //不然子結構是一串url，就還要再打這個 url 才能取得
            var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/emps?q=LastName=\"{EmpNo}\"&expand=assignments";
            MiddleReturn mr = HttpGetFromOracleAP(url);
            var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            var desc_str = Encoding.UTF8.GetString(bs64_bytes);
            OracleEmployeeIncludeAssignmentsCollection asms = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeIncludeAssignmentsCollection>(desc_str);
            if (asms.Count > 0)
            {
                foreach (var item in asms.Items)
                {
                    PersonID = item.PersonId;
                    foreach (var assignment in item.Assignments)
                    {
                        AssignmentID = assignment.AssignmentId;
                    }
                }
            }
        }

        /// <summary>
        /// 在用 /hcmRestApi/resources/emps?q=LastName="800924"&expand=assignments
        /// 取得的Employee Json 結構裡會比用 /hcmRestApi/resources/emps?q=LastName="800924"
        /// 多出一個 assignments 節點，裡面的 links 在 rel=self, name=assignments 的 href 就代表這個 assignment
        /// 所以要更新 assignment 就要取這個 href 然後呼叫 patch http 方法, 參數就是跟 assignment 一樣，但只需
        /// 列出需修改的屬性
        /// 所以這個 url 是用來更新用的
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <returns></returns>
        public string GetEmployeeAssignmentsSelfURLByEmpNo(string EmpNo)
        {

            var rst = "";
            //這個 &expand=assignments 是取得 employee 後
            //json結構裡有個 assignments 的子結構
            //子結構就可以用 expand=xxx 來直接取得
            //不然子結構是一串url，就還要再打這個 url 才能取得
            var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/emps?q=LastName=\"{EmpNo}\"&expand=assignments";
            MiddleReturn mr = HttpGetFromOracleAP(url);
            var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            var desc_str = Encoding.UTF8.GetString(bs64_bytes);
            OracleEmployeeIncludeAssignmentsCollection asms = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeIncludeAssignmentsCollection>(desc_str);
            if (asms.Count > 0)
            {
                foreach (var item in asms.Items)
                {
                    foreach (var assignment in item.Assignments)
                    {
                        foreach (var link in assignment.Links)
                        {
                            if (link.Rel == "self" && link.Name == "assignments")
                            {
                                rst = link.Href;
                                break;
                            }
                        }
                    }
                }
            }

            return rst;
        }
        Dictionary<string, string> JobLevelCollection;
        public Dictionary<string, string> GetOracleJobLevelCollection()
        {
            if (JobLevelCollection == null)
            {
                JobLevelCollection = new Dictionary<string, string>();
                try
                {
                    var JobLevelName = "職員";
                    var jobid = GetJobIDByChinese(JobLevelName);
                    JobLevelCollection.Add(JobLevelName, jobid);
                    JobLevelName = "室/處長";
                    jobid = GetJobIDByChinese(JobLevelName);
                    JobLevelCollection.Add(JobLevelName, jobid);
                    JobLevelName = "部長";
                    jobid = GetJobIDByChinese(JobLevelName);
                    JobLevelCollection.Add(JobLevelName, jobid);
                    JobLevelName = "營運長";
                    jobid = GetJobIDByChinese(JobLevelName);
                    JobLevelCollection.Add(JobLevelName, jobid);
                    JobLevelName = "執行長/總經理";
                    jobid = GetJobIDByChinese(JobLevelName);
                    JobLevelCollection.Add(JobLevelName, jobid);
                }
                catch (Exception ex)
                {
                }
            }
            return JobLevelCollection;
        }





        private OracleEmployeeAssignmentSelf2 GetOracleEmployeeAssignmentSelf2ByURL(string EmpNo, string url)
        {
            OracleEmployeeAssignmentSelf2 rst = null;
            try
            {
                var mrass = HttpGetFromOracleAP(url);
                if (string.IsNullOrWhiteSpace(mrass.ReturnData))
                {
                    throw new Exception($"取得 Assignment Self Object 失敗! ReturnData 空白!");
                }
                var bs64_bytes2 = Convert.FromBase64String(mrass.ReturnData);
                var desc_str2 = Encoding.UTF8.GetString(bs64_bytes2);
                //OracleEmployeeAssignmentsModel
                rst = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeAssignmentSelf2>(desc_str2);
            }
            catch (Exception ex)
            {
                throw new Exception($"取得OracleEmployeeAssignment失敗:{ex.Message}");
            }
            return rst;
        }

        List<Employee_workRelationships_Assignment> EmployeeAssignmentCollection;
        private Employee_workRelationships_Assignment GetEmployee_workRelationships_assignments_ByPersonNumber(string PersonNumber)
        {
            Employee_workRelationships_Assignment rst = null;
            try
            {
                if (EmployeeAssignmentCollection == null)
                {
                    EmployeeAssignmentCollection = new List<Employee_workRelationships_Assignment>();
                }
                var asms = from mn in EmployeeAssignmentCollection
                           where mn.PersonNumber == PersonNumber
                           select mn;
                foreach (var asm in asms)
                    rst = asm;

                if (rst == null)
                {
                    var manager_url = $"{Oracle_Domain}/hcmRestApi/resources/latest/workers?q=PersonNumber={PersonNumber}&expand=workRelationships.assignments";
                    var mr2 = HttpGetFromOracleAP(manager_url);
                    if (string.IsNullOrWhiteSpace(mr2.ReturnData))
                    {
                        throw new Exception($"取得 workRelationships.assignments 失敗:{mr2.StatusCode} {mr2.ErrorMessage}");
                    }
                    var bs64_bytes = Convert.FromBase64String(mr2.ReturnData);
                    var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                    OracleEmployeeAssignmentSelfModel asmn = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeAssignmentSelfModel>(desc_str);

                    if (asmn.Count > 0)
                    {
                        rst = asmn.Items[0];
                        EmployeeAssignmentCollection.Add(rst);
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"取得Employee_workRelationships_Assignment失敗:{ex.Message}");
            }

            return rst;
        }


        public bool UpdateUserNameByOracleEmployee2(OracleEmployee2 oraEmp, string UserName)
        {

            bool rst = false;
            foreach (var link in oraEmp.Links)
            {
                if (link.Rel == "self" && link.Name == "emps")
                {
                    var url = link.Href;
                    var par = new
                    {
                        UserName = UserName
                    };
                    MiddleReturn mr2 = HttpPatchFromOracleAP(url, par);
                    if (mr2.StatusCode == "200")
                    {
                        rst = true;
                        log.WriteLog("5", $"{oraEmp.LastName} 更新 UserName 成功:{UserName}");
                        //byte[] bs64_bytes = Convert.FromBase64String(mr2.ReturnData);
                        //string desc_str = Encoding.UTF8.GetString(bs64_bytes);

                    }
                    break;
                }
            }
            return rst;
        }


        /// <summary>
        /// 更新 Default Expense Account, 主管員編, Job Level
        /// 這是給新建帳號使用的
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <param name="deptNo"></param>
        /// <returns></returns>
        public bool 更新DefaultExpenseAccount_JobLevel_LineManager(string EmpNo, string DeptNo, string ManagerEmpNo,
            string JobLevelName)
        {
            bool rst = false;
            var deptNo = DeptNo.Substring(0, 9);
            //Line Manager, 
            log.WriteLog("5", $"準備更新 Job ID, Default Expense Account (UpdateDeptNoByEmployeeApi)");
            try
            {

                //正式環境User 的 Default Expense Account 的規則一樣比照Stage(DEV1), default account =6288099
                //完整  0001.< 員工所屬profit center >.< 員工所屬Department > .6288099.0000.000000000.0000000.0000
                //範例: 如 0001.POS.POS000000.6288099.0000.000000000.0000000.0000
                //取得EmployeeAssignment自已的url
                string EmployeeAssignmentsSelfUrl = GetEmployeeAssignmentsSelfURLByEmpNo(EmpNo);
                if (string.IsNullOrWhiteSpace(EmployeeAssignmentsSelfUrl))
                {
                    throw new Exception("Get Employee Assignments URL失敗:(空白)");
                }

                //先 GET 資料來看看
                OracleEmployeeAssignmentSelf2 empAsm = null;
                //OracleEmployeeAssignmentsModel asm2 = null;
                var jobid_in_oracle = "";
                var defExpenseAccount1_in_oracle = "";
                var ManagerAssignmentId1 = "";
                var ManagerID1 = "";
                try
                {
                    //取得 Employee Assignment
                    //var mrass = HttpGetFromOracleAP(EmployeeAssignmentsUrl);
                    //var bs64_bytes2 = Convert.FromBase64String(mrass.ReturnData);
                    //var desc_str2 = Encoding.UTF8.GetString(bs64_bytes2);
                    //asm2 = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeAssignmentSelf2>(desc_str2);


                    //取得 Employee Assignment
                    empAsm = GetOracleEmployeeAssignmentSelf2ByURL(EmpNo, EmployeeAssignmentsSelfUrl);

                    jobid_in_oracle = empAsm.JobId;
                    defExpenseAccount1_in_oracle = empAsm.DefaultExpenseAccount;
                    ManagerAssignmentId1 = empAsm.ManagerAssignmentId;
                    ManagerID1 = empAsm.ManagerId;
                    //if (asm2.Count > 0)
                    //{
                    //    var _asm = asm2.Items[0];
                    //    foreach (var asmm in _asm.Assignments)
                    //    {
                    //        jobid1 = asmm.JobId;
                    //        defExpenseAccount1 = asmm.DefaultExpenseAccount;
                    //        ManagerAssignmentId1 = asmm.ManagerAssignmentId;
                    //    }

                    //}
                }
                catch (Exception exass)
                {
                    log.WriteErrorLog($"先查資料時，取得 JobID, DefaultExpenseAccount, ManagerAssignmentId 失敗:{exass.Message}");
                }


                // job level
                var jobLevelName = JobLevelName;
                if (string.IsNullOrWhiteSpace(jobLevelName))
                {
                    jobLevelName = "職員";
                }

                //job level 
                //var jobid = GetJobIDByChinese(joblevel);
                var jobid = "";
                try
                {
                    jobid = JobLevelCollection[jobLevelName];
                }
                catch (Exception exlv)
                {
                }

                if (string.IsNullOrWhiteSpace(jobid))
                {
                    //重新準備 Job ID
                    log.WriteErrorLog($"重新準備 Job ID");
                    JobLevelCollection = null;
                    GetOracleJobLevelCollection();
                }
                if (string.IsNullOrWhiteSpace(jobid))
                {
                    log.WriteErrorLog($"還是取不到 Job Level 的 ID! 改用 api 傳中文取");
                    jobid = GetJobIDByChinese(jobLevelName);
                }

                // Default Expense Account
                var profit_center = deptNo.Substring(0, 3);
                var Default_Expense_Account = $"0001.{profit_center}.{deptNo}.6288099.0000.000000000.0000000.0000";

                if (jobid_in_oracle != jobid)
                {
                    log.WriteLog("5", $"準備更新 jobid");

                    var content = new
                    {
                        JobId = jobid
                    };
                    var mr = HttpPatchFromOracleAP(EmployeeAssignmentsSelfUrl, content);
                    if (mr.StatusCode == "200")
                    {
                        rst = true;
                        log.WriteLog("5", $"更新 JobId 成功!");
                        //byte[] bs64_bytes = Convert.FromBase64String(mr2.ReturnData);
                        //string desc_str = Encoding.UTF8.GetString(bs64_bytes);

                    }
                }
                else
                {
                    log.WriteLog("5", $"jobid 相同，不需更新");
                }

                if (defExpenseAccount1_in_oracle != Default_Expense_Account)
                {
                    log.WriteLog("5", $"準備更新 Default Expense Account!");

                    var content = new
                    {
                        DefaultExpenseAccount = Default_Expense_Account
                    };
                    var mr = HttpPatchFromOracleAP(EmployeeAssignmentsSelfUrl, content);
                    if (mr.StatusCode == "200")
                    {
                        rst = true;
                        log.WriteLog("5", $"更新 DefaultExpenseAccount 成功!");
                        //byte[] bs64_bytes = Convert.FromBase64String(mr2.ReturnData);
                        //string desc_str = Encoding.UTF8.GetString(bs64_bytes);

                    }
                }
                else
                {
                    log.WriteLog("5", $"Default_Expense_Account 相同，不需更新");
                }



                // Line Manager 主管
                if (string.IsNullOrWhiteSpace(ManagerEmpNo))
                {
                    log.WriteLog("5", $"傳入的主管員編為空，不更新 Line Manager");
                }
                else
                {
                    log.WriteLog("5", $"準備 Get ManagerEmpNo={ManagerEmpNo} 的 Employee 物件!");
                    var manager = GetOracleEmployeeByEmpNo(ManagerEmpNo);
                    if (manager == null)
                    {
                        log.WriteLog("5", $"取不到 Manager({ManagerEmpNo}) 的 Employee 物件");
                    }
                    else
                    {
                        ////取得workRelationships.assignments
                        //var manager_url = $"{Oracle_Domain}/hcmRestApi/resources/latest/workers?q=PersonNumber={manager.PersonNumber}&expand=workRelationships.assignments";
                        //var mr2 = HttpGetFromOracleAP(manager_url);
                        //if (string.IsNullOrWhiteSpace(mr2.ReturnData))
                        //{
                        //    throw new Exception($"在取得 Line Manager的 workRelationships.assignments 時失敗:{mr2.StatusCode} {mr2.ErrorMessage}");
                        //}
                        //var bs64_bytes = Convert.FromBase64String(mr2.ReturnData);
                        //var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        //OracleEmployeeAssignmentSelfModel asmn = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployeeAssignmentSelfModel>(desc_str);


                        //取得workRelationships.assignments
                        Employee_workRelationships_Assignment asm = GetEmployee_workRelationships_assignments_ByPersonNumber(manager.PersonNumber);

                        var AssignmentID = "";
                        foreach (var relas in asm.WorkRelationships)
                        {
                            foreach (var ass in relas.Assignments)
                            {
                                AssignmentID = $"{ass.AssignmentId}";
                                break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(AssignmentID))
                        {
                            throw new Exception($"在取得 Line Manager 的 AssignmentID 時沒取到資料!");
                        }

                        var PersonID = $"{manager.PersonId}";

                        var content1 = new
                        {
                            ManagerAssignmentId = AssignmentID, //如果在Create Worker中已獲取並保存，
                                                                //则此步可不用执行；
                                                                //若未保存，则需要执行查询獲取。 
                            ActionCode = "MANAGER_CHANGE",
                            ManagerId = PersonID, //ManagerId为Manager這個用戶所對應的PersonId
                            ManagerType = "LINE_MANAGER"
                        };
                        var mr3 = HttpPatchFromOracleAP(EmployeeAssignmentsSelfUrl, content1);
                        if (mr3.StatusCode == "200")
                        {
                            rst = true;
                            log.WriteLog("5", $"EmpNO={EmpNo} 更新 Line Manager 為 {ManagerEmpNo} 成功!");
                            //byte[] bs64_bytes1 = Convert.FromBase64String(mr2.ReturnData);
                            //string desc_str1 = Encoding.UTF8.GetString(bs64_bytes1);

                        }


                    }


                }
            }
            catch (Exception exall)
            {
                log.WriteErrorLog($"UpdateDeptNoByEmployeeApi ERROR:{exall.Message}");
                throw;
            }



            return rst;
        }



        /// <summary>
        /// 用 Employee api 更新 username
        /// 還是要先 get, 因為需要它的 self url
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <param name="UserName">要更新成的UserName</param>
        /// <returns></returns>
        public bool 先檢查Oracle的UserName不一樣才更新(string EmpNo, string UserName)
        {
            bool rst = false;
            log.WriteLog("5", $"準備更新 {EmpNo} 的 UserName為  {UserName}. (Into UpdateUserNameByEmployeeApi)");
            try
            {
                var emp = GetOracleEmployeeByEmpNo(EmpNo);
                if (emp == null)
                {
                    log.WriteErrorLog($"取得 {EmpNo} oracle employee 物件失敗!");
                }
                else
                {
                    var username = $"{emp.UserName}";
                    if (username.CompareTo(UserName) == 0)
                    {
                        //不需更新
                        rst = true;
                    }
                    else
                    {
                        //需更新
                        foreach (var link in emp.Links)
                        {
                            if (link.Rel == "self" && link.Name == "emps")
                            {
                                var url = link.Href;
                                var par = new
                                {
                                    UserName = UserName
                                };
                                MiddleReturn mr2 = HttpPatchFromOracleAP(url, par);
                                if (mr2.StatusCode == "200")
                                {
                                    rst = true;
                                    log.WriteLog("5", $"{EmpNo} 更新 UserName 成功:{UserName}");
                                    //byte[] bs64_bytes = Convert.FromBase64String(mr2.ReturnData);
                                    //string desc_str = Encoding.UTF8.GetString(bs64_bytes);

                                }
                                break;
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"UpdateUserNameByEmployeeApi 更新 UserName 失敗:{ex.Message}");
            }

            return rst;
        }

        /// <summary>
        /// 用 user api 更新員工編號
        /// </summary>
        /// <param name="OldEmpNo"></param>
        /// <param name="NewUserName"></param>
        public bool UpdateUserName(string OldEmpNo, string NewUserName)
        {
            bool rst = false;
            if (!string.IsNullOrWhiteSpace(OldEmpNo))
            {
                if (!string.IsNullOrWhiteSpace(NewUserName))
                {
                    try
                    {
                        // ORACLE STAGE
                        // for  oracle ap
                        string guid = GetGUIDByEmpNo(OldEmpNo);
                        if (string.IsNullOrWhiteSpace(guid))
                        {
                            throw new Exception("guid 空白!");
                        }
                        var par = new
                        {
                            Username = NewUserName
                        };
                        MiddleModel2 send_model2 = new MiddleModel2();
                        string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/userAccounts/{guid}";
                        send_model2.URL = url;
                        send_model2.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(par);
                        send_model2.Method = "PATCH";
                        //string username = this.UserName;
                        //string password = this.Password;
                        send_model2.UserName = this.UserName;
                        send_model2.Password = this.Password;
                        string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                        send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                        //CredentialCache myCred = new CredentialCache();
                        //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                        //send_model2.Cred = myCred;
                        send_model2.Timeout = 1000 * 60 * 30;


                        // for BOXMAN API
                        MiddleModel send_model = new MiddleModel();
                        var _url = $"{Oracle_AP}/api/Middle/Call/";
                        send_model.URL = _url;
                        send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                        send_model.Method = "POST";
                        send_model.Timeout = 1000 * 60 * 30;
                        var ret = ApiOperation.CallApi(
                                new ApiRequestSetting()
                                {
                                    Data = send_model,
                                    MethodRoute = "api/Middle/Call",
                                    MethodType = "POST",
                                    TimeOut = 1000 * 60 * 30
                                }
                                );
                        if (ret.Success)
                        {
                            string receive_str = ret.ResponseContent;
                            try
                            {
                                MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                                MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                                byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                                string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                                OracleUser usr = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleUser>(desc_str);
                                if (usr != null)
                                {
                                    if (usr.Username.CompareTo(NewUserName) == 0)
                                    {
                                        rst = true;
                                    }
                                }

                            }
                            catch (Exception exbs64)
                            {
                                log.WriteErrorLog($"Get Workers By Page 失敗:{exbs64.Message}{exbs64.InnerException}");
                            }
                        }
                        else
                        {
                            log.WriteErrorLog($"Call Boxman api {_url} 失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteErrorLog($"Call UpdateEmpNo 失敗:{ex.Message}. {ex.InnerException}");

                    }
                }
            }
            return rst;
        }


        /// <summary>
        /// 同步 oracle users 回來
        /// </summary>
        /// <param name="BatchNo"></param>
        public void SyncUsers(string BatchNo)
        {
            try
            {
                log.WriteLog("4", "準備同步 oracle user 回來");
                bool hasData = true;
                int iCurrent_page = 1;
                int iRecCntPerPage = 100;
                List<OracleUser> all_users = new List<OracleUser>();
                while (hasData)
                {
                    var par = new
                    {
                        iCurrent_page = iCurrent_page,
                        iRecCntPerPage = iRecCntPerPage
                    };
                    //var ret = ApiOperation.CallApi<string>("api/BoxmanOracleEmployee/BoxmanGetUsersByPagesAsync", WebRequestMethods.Http.Post, par);
                    DateTime dt_start = DateTime.Now;
                    var ret = ApiOperation.CallApi<string>(new ApiRequestSetting()
                    {
                        MethodRoute = "api/BoxmanOracleEmployee/BoxmanGetUsersByPagesAsync",
                        Data = par,
                        MethodType = "POST",
                        TimeOut = 1000 * 60 * 20
                    }
                    );
                    //var diff = DateTime.Now.Subtract(dt_start).TotalSeconds;
                    //Console.WriteLine($"api call use {diff} sec(s).");



                    if (string.IsNullOrWhiteSpace(ret.ErrorMessage))
                    {
                        OracleApiReturnObj<OracleUser> rtnobj = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleApiReturnObj<OracleUser>>(ret.Data);
                        if (rtnobj == null)
                        {
                            throw new Exception("解析失敗!");
                        }
                        hasData = rtnobj.HasMore;
                        all_users.AddRange(rtnobj.Items);
                        iCurrent_page++;

                    }
                    else
                    {
                        string errmsg = $"呼叫 api/BoxmanOracleEmployee/BoxmanGetUsersByPagesAsync 失敗:{ret.StatusCode} {ret.StatusDescription} {ret.ErrorMessage}";
                        //Console.WriteLine(errmsg);
                        throw new Exception(errmsg);
                    }
                }

                log.WriteLog("4", $"準備寫入 {all_users.Count} 筆 user 到 SQLite");
                SaveOracleUsersIntoSQLite(all_users, BatchNo);
                //string content = Newtonsoft.Json.JsonConvert.SerializeObject(all_users);
                //string all_oracle_users_json_obj_file_path = $@"{AppDomain.CurrentDomain.BaseDirectory}all_oracle_users_json_obj_file_path.json";
                //System.IO.File.WriteAllText(all_oracle_users_json_obj_file_path, content);
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"SyncUsers 失敗:{ex.Message}{ex.InnerException}");
            }
        }


        public void SyncWorkers(string BatchNo)
        {
            try
            {
                // Get all workers

                //var par = new
                //{
                //    companyCustNo = "H1660610201",
                //    shipNoList = new object[] { }
                //};

                log.WriteLog("4", $"準備同步 oracle Worker 回來");

                bool hasData = true;
                int iCurrent_page = 1;
                int iRecCntPerPage = 100;
                List<Worker> Workers = new List<Worker>();
                int current_cnt = 0;
                bool hasMore = true;
                while ((hasMore) && (current_cnt < 100))
                {
                    current_cnt++;


                    // ORACLE STAGE
                    // for  oracle ap
                    MiddleModel2 send_model2 = new MiddleModel2();
                    string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/workers/?limit={iRecCntPerPage}";
                    if (iCurrent_page > 1)
                    {
                        int offect = iRecCntPerPage * (iCurrent_page - 1);// iRecCntPerPage * (iCurrent_page - 1);

                        url = $"{url}&offset={offect}";

                    }
                    log.WriteLog("4", $"準備呼叫 {url}");
                    send_model2.URL = url;
                    //send_model2.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(par);
                    send_model2.Method = "GET";
                    //string username = this.UserName;
                    //string password = this.Password;
                    send_model2.UserName = this.UserName;
                    send_model2.Password = this.Password;
                    string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                    send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                    //CredentialCache myCred = new CredentialCache();
                    //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                    //send_model2.Cred = myCred;
                    send_model2.Timeout = 1000 * 60 * 30;


                    // for BOXMAN API
                    MiddleModel send_model = new MiddleModel();
                    var _url = $"{Oracle_AP}/api/Middle/Call/";
                    send_model.URL = _url;
                    send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                    send_model.Method = "POST";
                    send_model.Timeout = 1000 * 60 * 30;
                    var ret = ApiOperation.CallApi(
                            new ApiRequestSetting()
                            {
                                Data = send_model,
                                MethodRoute = "api/Middle/Call",
                                MethodType = "POST",
                                TimeOut = 1000 * 60 * 30
                            }
                            );
                    if (ret.Success)
                    {
                        string receive_str = ret.ResponseContent;
                        try
                        {
                            MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                            if (!string.IsNullOrWhiteSpace(mr2.ErrorMessage))
                            {
                                var _errmsg = $"SyncWorkers 失敗:{mr2.StatusCode} {mr2.StatusDescription} {mr2.ReturnData} {mr2.ErrorMessage}";
                                throw new Exception(_errmsg);
                            }

                            MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                            if (!string.IsNullOrWhiteSpace(mr.ErrorMessage))
                            {
                                var _errmsg = $"SyncWorkers 失敗:{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}";
                                throw new Exception(_errmsg);
                            }

                            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                            string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                            OracleResponseWorkersObj obj = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleResponseWorkersObj>(desc_str);
                            Workers.AddRange(obj.Items);
                            iCurrent_page++;

                            hasMore = obj.HasMore;
                        }
                        catch (Exception exbs64)
                        {
                            log.WriteErrorLog($"Call {url} 失敗:{exbs64.Message}{exbs64.InnerException}");
                        }
                    }
                    else
                    {
                        log.WriteErrorLog($"Call Boxman api {_url} 失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                    }

                }

                log.WriteLog("4", $"準備寫入 {Workers.Count} 筆 worker 資料到 SQLite");
                SaveOracleWorkersIntoSQLite(Workers, BatchNo);

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"Get Workers 失敗:{ex.Message}{ex.InnerException}");
            }

        }




        /// <summary>
        /// 經由 TEST BOXMAN AP => ORACLE AP => ORACLE CLOUD
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public MiddleReturn HttpGetFromOracleAP(string url)
        {
            try
            {
                // ORACLE STAGE
                // for  oracle ap
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                //string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/Suppliers/?limit={iRecCntPerPage}";
                MiddleModel2 send_model2 = new MiddleModel2();
                send_model2.URL = url;
                //send_model2.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(par);
                send_model2.Method = "GET";
                //string username = this.UserName;
                //string password = this.Password;
                send_model2.UserName = this.UserName;
                send_model2.Password = this.Password;
                string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                //CredentialCache myCred = new CredentialCache();
                //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                //send_model2.Cred = myCred;
                send_model2.Timeout = 1000 * 60 * 30;

                // for  53                
                MiddleModel send_model = new MiddleModel();
                send_model.URL = $"{Oracle_AP}/api/Middle/Call/";
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;

                // for BOXMAN API
                MiddleModel send_model53 = new MiddleModel();
                send_model53.URL = $"{ap53URLBase}/api/Middle/Call/";
                //send_model53.URL = $"{ap53位置}/api/Middle/TestMiddle/";
                send_model53.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model);
                send_model53.Method = "POST";
                //send_model53.Method = "GET";
                send_model53.Timeout = 1000 * 60 * 30;

                var _MethodRoute = "api/Middle/Call";
                var _MethodType = "POST";
                //var _MethodRoute = "api/Middle/TestMiddle";
                //var _MethodType = "GET";


                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model53,
                            MethodRoute = _MethodRoute,
                            MethodType = _MethodType,
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    if (string.IsNullOrWhiteSpace(receive_str))
                    {
                        throw new Exception("HttpGetFromOracleAP 取得空白的回應!");
                    }
                    string mr2rec = "";
                    try
                    {
                        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                        mr2rec = mr2.ReturnData;
                    }
                    catch (Exception exmr2)
                    {
                        throw new Exception($"HttpGetFromOracleAP 轉成 MiddleReturn mr2 失敗:{exmr2.Message}{exmr2.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }
                    string mr3rec = "";
                    try
                    {
                        MiddleReturn mr3 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2rec);
                        mr3rec = mr3.ReturnData;
                    }
                    catch (Exception exmr3)
                    {
                        throw new Exception($"HttpGetFromOracleAP 轉成 MiddleReturn mr3 失敗:{exmr3.Message}{exmr3.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }


                    try
                    {
                        MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr3rec);
                        return mr;
                    }
                    catch (Exception exmr)
                    {
                        throw new Exception($"HttpGetFromOracleAP 轉成 MiddleReturn mr 失敗:{exmr.Message}{exmr.InnerException}{Environment.NewLine}收到的資料:{mr2rec}");
                    }
                }
                else
                {
                    throw new Exception($"HttpGetFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }



        /// <summary>
        /// 經由 TEST BOXMAN AP => ORACLE AP => ORACLE CLOUD
        /// </summary>
        /// <param name="url"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public MiddleReturn HttpPatchFromOracleAP(string url, object content)
        {
            try
            {
                // ORACLE STAGE
                // for  oracle ap
                MiddleModel2 send_model2 = new MiddleModel2();
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                //string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/Suppliers/?limit={iRecCntPerPage}";


                send_model2.URL = url;
                send_model2.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(content);
                send_model2.Method = "PATCH";
                //string username = this.UserName;
                //string password = this.Password;
                send_model2.UserName = this.UserName;
                send_model2.Password = this.Password;
                string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                //CredentialCache myCred = new CredentialCache();
                //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                //send_model2.Cred = myCred;
                send_model2.Timeout = 1000 * 60 * 30;



                // for  53               
                MiddleModel send_model = new MiddleModel();
                send_model.URL = $"{Oracle_AP}/api/Middle/Call/";
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;


                // for BOXMAN API
                MiddleModel send_model53 = new MiddleModel();
                send_model53.URL = $"{ap53URLBase}/api/Middle/Call/";
                send_model53.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model);
                send_model53.Method = "POST";
                send_model53.Timeout = 1000 * 60 * 30;
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model53,
                            MethodRoute = "api/Middle/Call",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    if (string.IsNullOrWhiteSpace(receive_str))
                    {
                        throw new Exception("HttpPatchFromOracleAP 取得空白的回應!");
                    }
                    string mr2rec = "";
                    try
                    {
                        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                        mr2rec = mr2.ReturnData;
                    }
                    catch (Exception exmr2)
                    {
                        throw new Exception($"HttpPatchFromOracleAP 轉成 MiddleReturn mr2 失敗:{exmr2.Message}{exmr2.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }

                    string mr3rec = "";
                    try
                    {
                        MiddleReturn mr3 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2rec);
                        mr3rec = mr3.ReturnData;
                    }
                    catch (Exception exmr3)
                    {
                        throw new Exception($"HttpGetFromOracleAP 轉成 MiddleReturn mr3 失敗:{exmr3.Message}{exmr3.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }


                    try
                    {
                        MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr3rec);
                        return mr;
                    }
                    catch (Exception exmr)
                    {
                        throw new Exception($"HttpPatchFromOracleAP 轉成 MiddleReturn mr 失敗:{exmr.Message}{exmr.InnerException}{Environment.NewLine}收到的資料:{mr2rec}");
                    }
                }
                else
                {
                    throw new Exception($"HttpPatchFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public MiddleReturn HttpPostFromOracleAP(string url, object content)
        {
            try
            {
                // ORACLE STAGE
                // for  oracle ap
                MiddleModel2 send_model2 = new MiddleModel2();
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                //string url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/Suppliers/?limit={iRecCntPerPage}";


                send_model2.URL = url;
                send_model2.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(content);
                send_model2.Method = "POST";
                //string username = this.UserName;
                //string password = this.Password;
                send_model2.UserName = this.UserName;
                send_model2.Password = this.Password;
                string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                //CredentialCache myCred = new CredentialCache();
                //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                //send_model2.Cred = myCred;
                send_model2.Timeout = 1000 * 60 * 30;



                // for  53                
                MiddleModel send_model = new MiddleModel();
                send_model.URL = $"{Oracle_AP}/api/Middle/Call/";
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;


                // for BOXMAN API
                MiddleModel send_model53 = new MiddleModel();
                send_model53.URL = $"{ap53URLBase}/api/Middle/Call/";
                send_model53.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model);
                send_model53.Method = "POST";
                send_model53.Timeout = 1000 * 60 * 30;
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model53,
                            MethodRoute = "api/Middle/Call",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    if (string.IsNullOrWhiteSpace(receive_str))
                    {
                        throw new Exception("HttpPatchFromOracleAP 取得空白的回應!");
                    }
                    string mr2rec = "";
                    try
                    {
                        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                        mr2rec = mr2.ReturnData;
                    }
                    catch (Exception exmr2)
                    {
                        throw new Exception($"HttpPatchFromOracleAP 轉成 MiddleReturn mr2 失敗:{exmr2.Message}{exmr2.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }

                    string mr3rec = "";
                    try
                    {
                        MiddleReturn mr3 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2rec);
                        mr3rec = mr3.ReturnData;
                    }
                    catch (Exception exmr3)
                    {
                        throw new Exception($"HttpGetFromOracleAP 轉成 MiddleReturn mr3 失敗:{exmr3.Message}{exmr3.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }


                    try
                    {
                        MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr3rec);
                        return mr;
                    }
                    catch (Exception exmr)
                    {
                        throw new Exception($"HttpPatchFromOracleAP 轉成 MiddleReturn mr 失敗:{exmr.Message}{exmr.InnerException}{Environment.NewLine}收到的資料:{mr2rec}");
                    }
                }
                else
                {
                    throw new Exception($"HttpPatchFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }




        void CreateSQLiteFileIfNotExists_Supplier_1()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Supplier_1";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleSupplier";
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
    [BatchNo] nvarchar(30) ,
	[SupplierId] nvarchar(20) NOT NULL, 
	[SupplierPartyId] nvarchar(20), 
	[Supplier] nvarchar(100),
	[SupplierNumber] nvarchar(100),
	[AlternateName] nvarchar(100),
	[SupplierTypeCode] nvarchar(100),
	[SupplierType] nvarchar(100),
	[Status] nvarchar(100)
)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (SupplierNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_Supplier_2_address()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Supplier_2_address";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleSupplierAddr";
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
    [BatchNo] nvarchar(30) ,
	[SupplierNumber] nvarchar(100),
	[SupplierAddressId] nvarchar(100),
	[AddressName] nvarchar(100),
	[CountryCode] nvarchar(100),
	[Country] nvarchar(100),
	[AddressLine1] nvarchar(100)
)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (SupplierNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_Supplier_3_site()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Supplier_3_site";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleSupplierSite";
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
    [BatchNo] nvarchar(30) ,
	[SupplierNumber] nvarchar(100),

	[SupplierSiteId] nvarchar(100),
	[SupplierSite] nvarchar(100),
	[SupplierAddressId] nvarchar(100),
	[SupplierAddressName] nvarchar(100)

)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (SupplierNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_Supplier_4_Assignment()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Supplier_4_Assignment";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleSupplierAssignment";
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
    [BatchNo] nvarchar(30) ,
	[SupplierNumber] nvarchar(100),

	[AssignmentId] nvarchar(100),
	[Status] nvarchar(20) 

)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (SupplierNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_Supplier_5_Payee()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Supplier_5_Payee";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleSupplierPayee";
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
    [BatchNo] nvarchar(30) ,
	[SupplierNumber] nvarchar(100),

	[PayeeId] nvarchar(100),
	[PayeePartyIdentifier] nvarchar(100),
	[PartyName] nvarchar(100),
	[PayeePartyNumber] nvarchar(100),
	[PayeePartySiteIdentifier] nvarchar(100),
	[SupplierSiteCode] nvarchar(100),
	[SupplierSiteIdentifier] nvarchar(100),
	[PayeePartySiteNumber] nvarchar(100)

)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (SupplierNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_Supplier_5_BankAccount()
        {

            //5.2   Get Supplier Bank Account(
            //  第一個用5.1步驟獲得的 PayeePartyIdentifier，
            //  第二個用5.1步驟獲得的 PayeePartySiteIdentifier)


            string fn_name = "CreateSQLiteFileIfNotExists_Supplier_5_BankAccount";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleSupplierBankAccount";
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
    [BatchNo] nvarchar(30) ,
	[SupplierNumber] nvarchar(100),

	[BankAccountId] nvarchar(100),
	[AccountNumber] nvarchar(100),
	[AccountName] nvarchar(100),
	[BankName] nvarchar(100),
	[BranchName] nvarchar(100)

)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (SupplierNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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


        void CreateSQLiteFileIfNotExists_HR_Oracle_Mapping()
        {

            // 新舊員編對應表
            string fn_name = "CreateSQLiteFileIfNotExists_HR_Oracle_Mapping";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "HR_Oracle_Mapping";
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
	[OEmpNo] nvarchar(20), 
	[NEmpNo] nvarchar(20)
)
";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    string file_path = $"{AppDomain.CurrentDomain.BaseDirectory}/Jobs/ORSyncOracleData/rcode_mapping.csv";
                    string[] mapping_table = System.IO.File.ReadAllLines(file_path);
                    for (int idx = 1; idx < mapping_table.Length; idx++)
                    {
                        var vals = mapping_table[idx];
                        string[] onempno = vals.Split(',');
                        var oldEmpno = onempno[0];
                        var newEmpNo = onempno[1];
                        sql = $@"
INSERT INTO [HR_Oracle_Mapping]
		([OEmpNo]
		,[NEmpNo])
	VALUES
(:0, :1);
";
                        try
                        {
                            int cnt1 = sqlite.ExecuteByCmd(sql, oldEmpno, newEmpNo);
                        }
                        catch (Exception exins)
                        {
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"{fn_name} 失敗:{ex.Message}{ex.InnerException}");
            }


        }




        public MiddleReturn GetOracleSupplierByEmpNo(string EmpNo)
        {
            string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
            log.WriteLog("5", $"{EmpNo} supplier url:{url}");
            return HttpGetFromOracleAP(url);
        }

        // 2-Get Supplier Address(第一步得到的 Supplier ID)
        public MiddleReturn GetOracleSupplierAddrByEmpNo(string EmpNo, string BatchNo, string SupplierId = "")
        {
            string _SupplierId = SupplierId;
            if (string.IsNullOrWhiteSpace(_SupplierId))
            {

                string sql = $@"
select SupplierId
from  [OracleSupplier]
where batchno = :0
and SupplierNumber = :1
";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                DataTable tb;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EmpNo))
                {
                    _SupplierId = $"{row["SupplierId"]}";
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(_SupplierId))
            {
                string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers/{_SupplierId}/child/addresses";
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                log.WriteLog("5", $"{EmpNo} addresses url:{url}");
                return HttpGetFromOracleAP(url);
            }
            else
            {
                throw new Exception($"EmpNo:{EmpNo} 找不到 SupplierId");
            }

        }

        // 3-Get Supplier Site(第一步得到的 Supplier ID)
        public MiddleReturn GetOracleSupplierSiteByEmpNo(string EmpNo, string BatchNo, string SupplierId = "")
        {
            string _SupplierId = SupplierId;
            if (string.IsNullOrWhiteSpace(_SupplierId))
            {

                string sql = $@"
select SupplierId
from  [OracleSupplier]
where batchno = :0
and SupplierNumber = :1
";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                DataTable tb;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EmpNo))
                {
                    _SupplierId = $"{row["SupplierId"]}";
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(_SupplierId))
            {
                string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers/{_SupplierId}/child/sites";
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                log.WriteLog("5", $"{EmpNo} sites url:{url}");
                return HttpGetFromOracleAP(url);
            }
            else
            {
                throw new Exception($"EmpNo:{EmpNo} 找不到 SupplierId");
            }

        }

        //4-Get Supplier Assignment(第一個用第一步獲得的 Supplier ID，第二個用第三步獲得的 SupplierSiteId)
        public MiddleReturn GetOracleSupplierAssignmentByEmpNo(string EmpNo, string BatchNo
            , string SupplierId = "", string SupplierSiteId = "")
        {
            string _SupplierId = SupplierId;
            string _SupplierSiteId = SupplierSiteId;

            string sql = "";
            //SQLiteUtl sqlite = new SQLiteUtl(db_file);
            DataTable tb;
            if (string.IsNullOrWhiteSpace(_SupplierId))
            {

                sql = $@"
select SupplierId
from  [OracleSupplier]
where batchno = :0
and SupplierNumber = :1
";
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EmpNo))
                {
                    _SupplierId = $"{row["SupplierId"]}";
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(_SupplierSiteId))
            {

                sql = $@"
select SupplierSiteId
from  [OracleSupplierSite]
where batchno =:0
and SupplierNumber = :1
";

                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EmpNo))
                {
                    _SupplierSiteId = $"{row["SupplierSiteId"]}";
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(_SupplierId))
            {
                string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers/{_SupplierId}/child/sites/{_SupplierSiteId}/child/assignments";
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                log.WriteLog("5", $"{EmpNo} assignments url:{url}");
                return HttpGetFromOracleAP(url);
            }
            else
            {
                throw new Exception($"EmpNo:{EmpNo} 找不到 SupplierId");
            }

        }

        // 5-1.Get Supplier Bank Account-5.1   Get Payee Party ID(第一步獲得的SupplierPartyID)
        public MiddleReturn GetOracleSupplierPayeeByEmpNo(string EmpNo, string BatchNo, string SupplierPartyId = "")
        {
            //5-1.Get Supplier Bank Account-5.1   Get Payee Party ID(第一步獲得的 SupplierPartyID)
            string _SupplierPartyId = SupplierPartyId;
            if (string.IsNullOrWhiteSpace(_SupplierPartyId))
            {

                string sql = $@"
select SupplierPartyId
from  [OracleSupplier]
where batchno = :0
and SupplierNumber = :1
";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                DataTable tb;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EmpNo))
                {
                    _SupplierPartyId = $"{row["SupplierPartyId"]}";
                    break;
                }
            }



            if (!string.IsNullOrWhiteSpace(_SupplierPartyId))
            {
                string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/paymentsExternalPayees?finder=ExternalPayeeSearch;PayeePartyIdentifier={_SupplierPartyId},Intent=Supplier";
                //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                return HttpGetFromOracleAP(url);
            }
            else
            {
                throw new Exception($"EmpNo:{EmpNo} 找不到 SupplierId");
            }

        }

        // 5.2   Get Supplier Bank Account
        //  (第一個用5.1步驟獲得的 PayeePartyIdentifier，
        //    第二個用5.1步驟獲得的 PayeePartySiteIdentifier)
        public MiddleReturn GetOracleSupplierBankAccountByEmpNo(string EmpNo, string BatchNo,
            string PayeePartyIdentifier = "", string PayeePartySiteIdentifier = "")
        {

            string _PayeePartyIdentifier = PayeePartyIdentifier;
            string _PayeePartySiteIdentifier = PayeePartySiteIdentifier;

            if (string.IsNullOrWhiteSpace(_PayeePartyIdentifier) || string.IsNullOrWhiteSpace(_PayeePartySiteIdentifier))
            {

                string sql = $@"
select PayeePartyIdentifier, PayeePartySiteIdentifier
from  [OracleSupplierPayee]
where batchno = :0
and SupplierNumber = :1
";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                DataTable tb;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo, EmpNo))
                {
                    _PayeePartyIdentifier = $"{row["PayeePartyIdentifier"]}";
                    _PayeePartySiteIdentifier = $"{row["PayeePartySiteIdentifier"]}";
                    break;
                }

            }



            if (!string.IsNullOrWhiteSpace(_PayeePartyIdentifier))
            {
                if (!string.IsNullOrWhiteSpace(_PayeePartySiteIdentifier))
                {

                    string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/payeeBankAccountsLOV?finder=AvailablePayeeBankAccountsFinder;PaymentFunction=\"PAYABLES_DISB\",PayeePartyId={_PayeePartyIdentifier},SupplierSiteId={_PayeePartySiteIdentifier}";
                    //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                    log.WriteLog("5", $"{EmpNo} 5.2   Get Supplier Bank Account url:{url}");
                    return HttpGetFromOracleAP(url);
                }
                else
                {
                    throw new Exception($"EmpNo:{EmpNo} PayeePartySiteIdentifier is null");
                }
            }
            else
            {
                throw new Exception($"EmpNo:{EmpNo} PayeePartyIdentifier is null");
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

                    db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                    if (!Directory.Exists(db_path))
                    {
                        Directory.CreateDirectory(db_path);
                    }
                    db_file = $"{db_path}OracleData.sqlite";
                    //SQLiteUtl sqlite = new SQLiteUtl(db_file);


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

                    }
                    catch (Exception exhr)
                    {
                        log.WriteErrorLog($"GetOldEmpNo 失敗:{exhr.Message}{exhr.InnerException}");
                    }


                }
            }
            catch (Exception ex)
            {
                rst = empNo;
            }

            return rst;
        }




        public string UpdateBankInfo(
             string SupplierNumber,
                                       string BankAccountNumber,
                                       string BankName,
                                       string BankBranchName,
                                       string UpdateBankAccountName,
                                       string UpdateBankAccountNumber,
                                       string UpdateBankName,
                                       string UpdateBankBranchName)
        {
            log.WriteLog("5", $"準備更新員工供應商帳號資料 EmpNo={SupplierNumber}");
            var rst = "";
            try
            {
                var par4 = new
                {
                    SupplierNumber = SupplierNumber,
                    BankAccountNumber = BankAccountNumber,
                    BankName = BankName,
                    BankBranchName = BankBranchName,
                    UpdateBankAccountName = UpdateBankAccountName,
                    UpdateBankAccountNumber = UpdateBankAccountNumber,
                    UpdateBankName = UpdateBankName,
                    UpdateBankBranchName = UpdateBankBranchName,
                    user = "Job",
                    dept = "ec"
                };

                // for  53                
                MiddleModel send_model = new MiddleModel();
                send_model.URL = $"{Oracle_AP}/api/OracleApi/SupplierUpdateBankAccounts";
                send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(par4);
                send_model.Method = "POST";
                send_model.Timeout = 1000 * 60 * 30;


                // for BOXMAN API
                MiddleModel send_model53 = new MiddleModel();
                send_model53.URL = $"{ap53URLBase}/api/Middle/Call/";
                send_model53.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model);
                send_model53.Method = "POST";
                send_model53.Timeout = 1000 * 60 * 30;
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = send_model53,
                            MethodRoute = "api/Middle/Call",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    if (string.IsNullOrWhiteSpace(receive_str))
                    {
                        throw new Exception("HttpPatchFromOracleAP 取得空白的回應!");
                    }
                    string mr2rec = "";
                    try
                    {
                        MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                        mr2rec = mr2.ReturnData;
                    }
                    catch (Exception exmr2)
                    {
                        throw new Exception($"HttpPatchFromOracleAP 轉成 MiddleReturn mr2 失敗:{exmr2.Message}{exmr2.InnerException}{Environment.NewLine}收到的資料={receive_str}");
                    }

                    string mr3rec = "";
                    try
                    {
                        MiddleReturn mr3 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2rec);
                        mr3rec = mr3.ReturnData;
                        if (mr3.StatusCode == "200")
                        {
                            log.WriteLog("5", $"帳號更新成功!");
                            //UpdateSupplierAccountReturnModel rtnObj = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateSupplierAccountReturnModel>(mr3rec);
                            //var msg = $"帳號資料已更新為: 戶名:{rtnObj.BankAccountName}, 帳號:{rtnObj.BankAccountNumber}, 銀行代號:{rtnObj.BankNumber},銀行名稱:{rtnObj.BankName},分行代號:{rtnObj.BankBranchNumber},分行名稱:{rtnObj.BankBranchName}";
                            //log.WriteLog("5", msg);
                            rst = "帳號更新成功!";
                        }
                        else
                        {
                            var err = $"帳號更新失敗! {mr3.ReturnData}{mr3.ErrorMessage}";
                            log.WriteErrorLog(err);
                            throw new Exception(err);
                        }

                    }
                    catch (Exception exmr3)
                    {
                        var err = $"HttpGetFromOracleAP 轉成 MiddleReturn mr3 失敗:{exmr3.Message}{exmr3.InnerException}{Environment.NewLine}收到的資料={receive_str}";
                        throw new Exception(err);
                    }


                }
                else
                {
                    throw new Exception($"HttpPatchFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                throw;
            }


            return rst;
        }



        /// <summary>
        /// 取得 oracle 的銀行資料
        /// </summary>
        /// <returns></returns>
        public IEnumerable<OracleApiChkBankBranchesMd> GetOracleBankData()
        {
            bool rst = false;
            try
            {
                // for BOXMAN API
                var par = new
                {
                    dept = "ec"
                };
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = par,
                            MethodRoute = "api/OREMP/GetOracleBankData",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    if (string.IsNullOrWhiteSpace(receive_str))
                    {
                        throw new Exception("HttpGetFromOracleAP 取得空白的回應!");
                    }

                    ResultObject<IEnumerable<OracleApiChkBankBranchesMd>> mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultObject<IEnumerable<OracleApiChkBankBranchesMd>>>(receive_str);
                    return mr2.Data;



                }
                else
                {
                    throw new Exception($"HttpGetFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"HttpGetFromOracleAP 呼叫api失敗:{ex.Message}. {ex.InnerException}");
            }
            return null;
        }



        /// <summary>
        /// 同步oracle的銀行名稱/分行名稱資料回來，供修改銀行帳號資料時使用
        /// </summary>
        /// <returns></returns>
        public bool SyncOracleBankData()
        {
            bool rst = false;
            try
            {
                // for BOXMAN API
                var par = new
                {
                    dept = "ec"
                };
                var ret = ApiOperation.CallApi(
                        new ApiRequestSetting()
                        {
                            Data = par,
                            MethodRoute = "api/OREMP/GetOracleBankData",
                            MethodType = "POST",
                            TimeOut = 1000 * 60 * 30
                        }
                        );
                if (ret.Success)
                {
                    string receive_str = ret.ResponseContent;
                    if (string.IsNullOrWhiteSpace(receive_str))
                    {
                        throw new Exception("HttpGetFromOracleAP 取得空白的回應!");
                    }
                    string mr2rec = "";
                    try
                    {
                        ResultObject<IEnumerable<OracleApiChkBankBranchesMd>> mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultObject<IEnumerable<OracleApiChkBankBranchesMd>>>(receive_str);
                        IEnumerable<OracleApiChkBankBranchesMd> bankdata = mr2.Data;
                        CreateSQLiteFileIfNotExists_OracleBankDataBranchData();
                        if (bankdata == null)
                            throw new Exception("取得 Oracle Bank / Branch Data 失敗!");

                        foreach (var bank in bankdata)
                        {
                            var sql = "";
                            try
                            {

                                sql = $@"DELETE FROM OracleBankData
WHERE BANK_NAME = :0
AND BANK_NUMBER = :1
AND BANK_BRANCH_NAME  = :2
AND BRANCH_NUMBER  = :3
";
                                int cnt = sqlite.ExecuteByCmd(sql,
                                    bank.BANK_NAME,
                                    bank.BANK_NUMBER,
                                    bank.BANK_BRANCH_NAME,
                                    bank.BRANCH_NUMBER
                                          );

                                // insert
                                sql = $@"
INSERT INTO [OracleBankData]
		(
   BANK_NAME ,
BANK_NUMBER ,
BANK_BRANCH_NAME  ,
BRANCH_NUMBER 
)
	VALUES(
:0
,:1
,:2
,:3
)";
                                cnt = sqlite.ExecuteByCmd(sql,
                                  bank.BANK_NAME,
                                  bank.BANK_NUMBER,
                                  bank.BANK_BRANCH_NAME,
                                  bank.BRANCH_NUMBER
                                        );

                            }
                            catch (Exception exx)
                            {
                                var _msg = $@"BANK_NAME={bank.BANK_NAME}, BANK_NUMBER={bank.BANK_NUMBER}, BANK_BRANCH_NAME={bank.BANK_BRANCH_NAME}, BRANCH_NUMBER={bank.BRANCH_NUMBER}";
                                throw new Exception($"寫入 oracle bank data,{_msg} 失敗:{exx.Message}{exx.InnerException}");
                            }
                        }
                    }
                    catch (Exception exmr2)
                    {
                        throw new Exception($"同步 oracle bank data 失敗:{exmr2.Message}{exmr2.InnerException}");
                    }


                }
                else
                {
                    throw new Exception($"HttpGetFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
                }
            }
            catch (Exception ex)
            {
            }
            return rst;
        }



        //public IEnumerable<OracleApiChkBankBranchesMd> GetBankDataFromOracle()
        //{
        //    var par = new
        //    {
        //        dept = "ec"
        //    };

        //    var ret = ApiOperation.CallApi(
        //            new ApiRequestSetting()
        //            {
        //                Data = par,
        //                MethodRoute = "api/OREMP/GetOracleBankData",
        //                MethodType = "POST",
        //                TimeOut = 1000 * 60 * 30
        //            }
        //            );
        //    if (ret.Success)
        //    {
        //        string receive_str = ret.ResponseContent;
        //        if (string.IsNullOrWhiteSpace(receive_str))
        //        {
        //            throw new Exception("HttpGetFromOracleAP 取得空白的回應!");
        //        }
        //        string mr2rec = "";
        //        try
        //        {
        //            ResultObject<IEnumerable<OracleApiChkBankBranchesMd>> mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultObject<IEnumerable<OracleApiChkBankBranchesMd>>>(receive_str);
        //            return mr2.Data;
        //        }
        //        catch (Exception exmr2)
        //        {
        //            throw new Exception($"HttpGetFromOracleAP 轉成 MiddleReturn mr2 失敗:{exmr2.Message}{exmr2.InnerException}{Environment.NewLine}收到的資料={receive_str}");
        //        }


        //    }
        //    else
        //    {
        //        throw new Exception($"HttpGetFromOracleAP 呼叫api失敗:{ret.ErrorMessage}. {ret.ErrorException}");
        //    }
        //}

        /// <summary>
        /// 更新員工供應商銀行資料
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <param name="EmpName"></param>
        /// <param name="TW_ID"></param>
        /// <param name="BankNumber"></param>
        /// <param name="BankName"></param>
        /// <param name="ACCOUNT_NUMBER"></param>
        /// <param name="ACCOUNT_NAME"></param>
        /// <param name="BranchNumber"></param>
        /// <param name="BranchName"></param>
        /// <param name="OldBankName"></param>
        /// <param name="OldBranchName"></param>
        /// <param name="OldAccountNumber"></param>
        public void UpdateSupplierBankDataByEmpNo(
                 string EmpNo, // 員編
                 string EmpName, // 中文姓名
                 string TW_ID, // 帳戶身分證號
                 string BankNumber, // 銀行代碼
                 string BankName, // 銀行名稱
                 string ACCOUNT_NUMBER, // 銀行收款帳號
                 string ACCOUNT_NAME, // 銀行帳戶戶名
                 string BranchNumber, // 銀行分行代碼
                 string BranchName, // 銀行分行名稱
                 string OldBankName,  //舊的銀行名稱
                 string OldBranchName,  //舊的分行名稱
                 string OldAccountNumber //舊的銀行帳號
              )
        {
            //準備update  
            OREmpBankData par1 = new OREmpBankData()
            {
                EmpNo = EmpNo, // 員編
                EmpName = EmpName, // 中文姓名
                TW_ID = TW_ID, // 帳戶身分證號
                BankNumber = BankNumber, // 銀行代碼
                ACCOUNT_NUMBER = ACCOUNT_NUMBER, // 銀行收款帳號
                ACCOUNT_NAME = ACCOUNT_NAME, // 銀行帳戶戶名
                BranchNumber = BranchNumber, // 銀行分行代碼
                ApplyMan = "Job",
                ReDoStep = "",
                BankName = BankName,
                BranchName = BranchName,
                OldBankName = OldBankName,
                OldBranchName = OldBranchName,
                OldAccountNumber = OldAccountNumber
            };

            //var send_model = "";
            var ret1 = ApiOperation.CallApi(
                    new ApiRequestSetting()
                    {
                        Data = par1,
                        MethodRoute = "api/OREMP/UpdateBanksData",
                        MethodType = "POST",
                        TimeOut = 1000 * 60 * 120
                    }
                    );
            if (ret1.Success)
            {
                string receive_str = ret1.ResponseContent;
                try
                {
                    //GenFileResult mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<GenFileResult>(receive_str);

                    //log.WriteErrorLog(string.Join(Environment.NewLine, mr2.ErrorMessages.ToArray()));
                    //foreach (var file in mr2.Data)
                    //{
                    //    byte[] content = Convert.FromBase64String(file.FileBase64Str);
                    //    var path = $"I:/Alvin/Oracle供應商Supplier/產生supplier匯入檔給顧問/{folder}";
                    //    var zipFolder = $"{path}/zip/";
                    //    if (!Directory.Exists(zipFolder))
                    //    {
                    //        Directory.CreateDirectory(zipFolder);
                    //    }
                    //    //var sort_idx = index.ToString("D4");
                    //    var zipFilename = $"{zipFolder}{file.MailFileName}";
                    //    var csvFolder = $"{path}/csv/";
                    //    if (!Directory.Exists(csvFolder))
                    //    {
                    //        Directory.CreateDirectory(csvFolder);
                    //    }
                    //    var tmp = $"{Path.GetFileNameWithoutExtension(file.MailFileName)}.csv";
                    //    var csvFilename = $"{csvFolder}{index}-{tmp}";
                    //    System.IO.File.WriteAllBytes(zipFilename, content);
                    
                    //    //FileInfo fileToDecompress = new FileInfo(zipFilename);
                    //    //using (FileStream originalFileStream = fileToDecompress.OpenRead())
                    //    //{
                    //    //    string currentFileName = fileToDecompress.FullName;
                    //    //    string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);
                    //    //    newFileName = csvFilename;

                    //    //    using (FileStream decompressedFileStream = File.Create(newFileName))
                    //    //    {
                    //    //        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    //    //        {
                    //    //            decompressionStream.CopyTo(decompressedFileStream);
                    //    //            //Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
                    //    //        }
                    //    //    }
                    //    //}
                    //}

                }
                catch (Exception exb)
                {
                    log.WriteErrorLog($"AddWorkerByMiddle Create Worker 失敗:{exb.Message}{exb.InnerException}");
                }
            }
            else
            {
                log.WriteErrorLog($"呼叫 api/OREMP/EMPACCFILES 失敗:{ret1.ResponseContent}{ret1.ErrorMessage}. {ret1.ErrorException}");
            }

        }



        /// <summary>
        /// 建立員工供應商
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <param name="EmpName"></param>
        /// <param name="TW_ID"></param>
        /// <param name="BankNumber"></param>
        /// <param name="ACCOUNT_NUMBER"></param>
        /// <param name="ACCOUNT_NAME"></param>
        /// <param name="BranchNumber"></param>
        /// <returns></returns>
        public ApiResultModel CreateSupplier(
          string EmpNo, // 員編
          string EmpName, // 中文姓名
          string TW_ID, // 帳戶身分證號
          string BankNumber, // 銀行代碼
          string ACCOUNT_NUMBER, // 銀行收款帳號
          string ACCOUNT_NAME, // 銀行帳戶戶名
          string BranchNumber, // 銀行分行代碼
            string DoStep // 要打哪些
            )
        {
            ApiResultModel rst = new ApiResultModel();
            List<OREmpNoData> suppliers = new List<OREmpNoData>();
            suppliers.Add(new OREmpNoData()
            {
                EmpNo = EmpNo, // 員編
                EmpName = $"{EmpName}", // 中文姓名
                TW_ID = TW_ID, // 帳戶身分證號
                BankNumber = BankNumber, // 銀行代碼
                ACCOUNT_NUMBER = ACCOUNT_NUMBER, // 銀行收款帳號
                ACCOUNT_NAME = ACCOUNT_NAME, // 銀行帳戶戶名
                BranchNumber = BranchNumber, // 銀行分行代碼
                ApplyMan = "Job",
                ReDoStep = DoStep
            });
            var par = new
            {
                Data = suppliers
            };
            var ppar = Newtonsoft.Json.JsonConvert.SerializeObject(par);
            //log.WriteLog("4", $"準備推送員工供應商 {EmpNo} {EmpName}, 先暫停 3 秒鐘{Environment.NewLine}Payload={ppar}");
            Thread.Sleep(1000); //改暫停1秒
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
                    //log.WriteLog("5", $"{EmpNo},{EmpName} Supplier push 成功! {model.ErrorCode} {model.ErrorMessages}");
                    rst.Result = model.Result;
                }
                else
                {
                    rst.ErrorCode = model.ErrorCode;
                    rst.ErrorMessages = model.ErrorMessages;
                    //log.WriteErrorLog($"{EmpNo},{EmpName}  Supplier push 失敗! {ret.Data} {ret.ErrorMessage} {ret.ErrorException}");
                }
                /*
{"Result":false,"ErrorCode":null,"ErrorMessages":["Supplier:ERROR","SupplierAddress:ERROR","SupplierSite:ERROR","SupplierSiteAssignment:ERROR","SupplierBank:ERROR"]}

                 * 
                 * 
                 * */

            }
            else
            {
                rst.ErrorMessages = new string[] { ret.ErrorMessage };
                //log.WriteLog("5", $"{EmpNo},{EmpName}  Supplier push 失敗! {ret.Data} {ret.ErrorMessage} {ret.ErrorException}");
            }

            return rst;
        }

        public bool IsOracleAccExists(string EmpNo)
        {
            bool rst = false;
            var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/emps?q=LastName=\"{EmpNo}\"";
            try
            {


                //  //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                MiddleReturn mr = HttpGetFromOracleAP(url);
                var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                OracleEmployees emps = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployees>(desc_str);
                if (emps.Count > 0)
                {
                    //employee = emps.Items[0];
                    rst = true;
                }

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"呼叫 api 失敗:api:{url}\r\n{ex.Message}{ex.InnerException}");
            }
            return rst;
        }


        //public bool UpdateOracleUserNameByUrl(string url, string UserName)
        //{
        //    bool rst = false;

        //    return rst;
        //}

        /// <summary>
        /// 用 oracle employee api 傳入員編取得 employee
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <returns></returns>
        public OracleEmployee GetOracleEmployeeByEmpNo(string EmpNo)
        {
            OracleEmployee employee = null;

            log.WriteLog("5", $"準備取得 EmpNo={EmpNo} 的 oracle employee 物件. (Into GetOracleEmployeeByEmpNo)");

            var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/emps?q=LastName=\"{EmpNo}\"";
            try
            {


                //  //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                MiddleReturn mr = HttpGetFromOracleAP(url);
                if (string.IsNullOrWhiteSpace(mr.ReturnData))
                {
                    throw new Exception($"{mr.StatusCode} {mr.ErrorMessage}");
                }
                var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                OracleEmployees emps = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleEmployees>(desc_str);

                if (emps != null && emps.Count > 0)
                {
                    employee = emps.Items[0];
                }

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"GetOracleEmployeeByEmpNo 呼叫 api 失敗:api:{url}\r\n{ex.Message}{ex.InnerException}");
                throw;
            }

            return employee;
        }

        /// <summary>
        /// 用員編取得 Assignment Number 給 create worker 用
        /// </summary>
        /// <param name="EmpNo"></param>
        /// <returns></returns>
        public string GetAssignmentNumberByEmpNo(string EmpNo)
        {
            OracleEmployee emp = GetOracleEmployeeByEmpNo(EmpNo);
            var assignment_number = GetAssignmentNumberByPersonNumber(emp.PersonNumber);
            return assignment_number;
        }

        /// <summary>
        /// 用 person number取得 assignment number
        /// </summary>
        /// <param name="PersonNumber"></param>
        /// <returns></returns>
        public string GetAssignmentNumberByPersonNumber(string PersonNumber)
        {
            string rst = "";
            var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/workers?q=PersonNumber={PersonNumber}&expand=workRelationships.assignments";
            try
            {


                //  //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                MiddleReturn mr = HttpGetFromOracleAP(url);
                var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                OracleReturnWorkers workers = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleReturnWorkers>(desc_str);
                foreach (var worker in workers.Items)
                {
                    foreach (var relationship in worker.WorkRelationships)
                    {
                        foreach (var assignment in relationship.Assignments)
                        {
                            rst = assignment.AssignmentNumber;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"呼叫 api 失敗:api:{url}\r\n{ex.Message}{ex.InnerException}");
                throw;
            }
            return rst;
        }

        /// <summary>
        /// Job Level 
        /// 用中文取得 oracle 對應的 id
        /// 職員	0.
        /// 室/處長	1.
        /// 部長	2.
        /// 營運長	3.
        /// 執行長/總經理	4.
        /// </summary>
        /// <param name="ChineseName"></param>
        /// <returns></returns>
        public string GetJobIDByChinese(string ChineseName)
        {
            var cname = ChineseName;
            if (string.IsNullOrWhiteSpace(cname))
            {
                cname = "職員";
            }
            string rst = "";
            //var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/workers?q=PersonNumber={PersonNumber}&expand=workRelationships.assignments";
            var url = $"{Oracle_Domain}/hcmRestApi/resources/11.13.18.05/jobs?q=Name=\"{cname}\";SetId=0";
            try
            {


                //  //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                MiddleReturn mr = HttpGetFromOracleAP(url);
                if (string.IsNullOrWhiteSpace(mr.ReturnData))
                {
                    throw new Exception($"取 Job ID 失敗:{mr.StatusCode}{mr.ErrorMessage}");
                }
                var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                OracleJobIDReturnModel ids = Newtonsoft.Json.JsonConvert.DeserializeObject<OracleJobIDReturnModel>(desc_str);
                foreach (var jobid in ids.Items)
                {
                    rst = jobid.JobId;
                }
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"Get Job ID 失敗:api:{url}\r\n{ex.Message}{ex.InnerException}");
                throw new Exception($"Get Job ID 失敗:{ex.Message}{ex.InnerException}");
            }

            return rst;
        }


        public SupplierBankData GetSupplierDataByEmpNo(string EmpNo)
        {
            SupplierBankData rst = null;
            string url = "";
            // step 1.       Get Supplier
            MiddleReturn mr = GetOracleSupplierByEmpNo(EmpNo);
            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            string desc_str = Encoding.UTF8.GetString(bs64_bytes);

            SupplierResponseModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierResponseModel>(desc_str);
            if (obj.Count == 0) return rst;
            var SupplierId = obj.Items[0].SupplierId;
            var SupplierNumber = obj.Items[0].SupplierNumber;
            var SupplierPartyId = obj.Items[0].SupplierPartyId;

            //Step 2.       Get Supplier Address(第一步得到的Supplier ID)
            //  url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers/{SupplierId}/child/addresses";
            //log.WriteLog("5", $"{EmpNo} addresses url:{url}");
            //mr = HttpGetFromOracleAP(url);
            //bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            //desc_str = Encoding.UTF8.GetString(bs64_bytes);
            //SupplierAddrReturnModel objAddr = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierAddrReturnModel>(desc_str);
            //var AddressName = "";
            //if (objAddr.Count > 0)
            //{
            //    AddressName = objAddr.Items[0].AddressName;
            //}

            //Step 3.       Get Supplier Site(第一步得到的Supplier ID)
            //url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers/{SupplierId}/child/sites";
            ////string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
            //log.WriteLog("5", $"{EmpNo} sites url:{url}");
            //mr = HttpGetFromOracleAP(url);
            //bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            //desc_str = Encoding.UTF8.GetString(bs64_bytes);
            //SupplierSiteReturnModel obj1 = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierSiteReturnModel>(desc_str);
            //var _SupplierSiteId = "";
            //if (obj.Count > 0)
            //{
            //    _SupplierSiteId = obj1.Items[0].SupplierSiteId;
            //}

            //Step 4.       Get Supplier Assignment(第一個用第一步獲得的Supplier ID，第二個用第三步獲得的SupplierSiteId)
            //if (!string.IsNullOrWhiteSpace(_SupplierSiteId))
            //{
            //    url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers/{SupplierId}/child/sites/{_SupplierSiteId}/child/assignments";
            //    //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
            //    log.WriteLog("5", $"{EmpNo} assignments url:{url}");
            //    mr = HttpGetFromOracleAP(url);
            //    bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            //    desc_str = Encoding.UTF8.GetString(bs64_bytes);

            //    SupplierAssignmentReturnModel objAssignment = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierAssignmentReturnModel>(desc_str);
            //    var AssignmentId = "";
            //    if (objAssignment.Count > 0)
            //    {
            //        AssignmentId = objAssignment.Items[0].AssignmentId;
            //    }
            //}

            //5.       Get Supplier Bank Account
            //                5.1   Get Payee Party ID(第一步獲得的SupplierPartyID)
            url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/paymentsExternalPayees?finder=ExternalPayeeSearch;PayeePartyIdentifier={SupplierPartyId},Intent=Supplier";
            //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
            mr = HttpGetFromOracleAP(url);
            bs64_bytes = Convert.FromBase64String(mr.ReturnData);
            desc_str = Encoding.UTF8.GetString(bs64_bytes);
            SupplierPayeeReturnModel objPayee = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierPayeeReturnModel>(desc_str);
            var PayeePartyIdentifier = "";
            var PayeePartySiteIdentifier = "";
            //if (objPayee.Count > 0)
            foreach (var item in objPayee.Items)
            {
                PayeePartyIdentifier = item.PayeePartyIdentifier;
                PayeePartySiteIdentifier = $"{item.PayeePartySiteIdentifier}";

                //Step 5.2   Get Supplier Bank Account(第一個用5.1步驟獲得的PayeePartyIdentifier，
                //第二個用5.1步驟獲得的PayeePartySiteIdentifier)
                if ((!string.IsNullOrWhiteSpace(PayeePartyIdentifier)) && (!string.IsNullOrWhiteSpace(PayeePartySiteIdentifier)))
                {

                    url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/payeeBankAccountsLOV?finder=AvailablePayeeBankAccountsFinder;PaymentFunction=\"PAYABLES_DISB\",PayeePartyId={PayeePartyIdentifier},SupplierSiteId={PayeePartySiteIdentifier}";
                    //string url = $"{Oracle_Domain}/fscmRestApi/resources/11.13.18.05/suppliers?q=SupplierNumber '{EmpNo}'";
                    mr = HttpGetFromOracleAP(url);
                    bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                    desc_str = Encoding.UTF8.GetString(bs64_bytes);
                    var BankAccountId = "";
                    SupplierBankAccountReturnModel objBankAccount = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierBankAccountReturnModel>(desc_str);
                    if (objBankAccount.Count > 0)
                    {
                        rst = new SupplierBankData();
                        BankAccountId = objBankAccount.Items[0].BankAccountId;
                        rst.AccountNumber = objBankAccount.Items[0].AccountNumber;
                        rst.AccountName = objBankAccount.Items[0].AccountName;
                        rst.BankName = objBankAccount.Items[0].BankName;
                        rst.BranchName = objBankAccount.Items[0].BranchName;
                        break;
                    }
                }

            }



            return rst;
        }

        public void SyncOneSupplierByEmpNo(string EmpNo, bool ForceSync = false)
        {
            try
            {
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);



                // 因為同步一個供應商 約需5分鐘
                // 假設有1000個員工就要5000分鐘約3.5天
                // 太久了  所以要改成有同步過的就不同步
                // 因為也不會改(有要改再說)
                // 要同步之前 先查一下自己有沒有資料
                // 沒有再同步
                string SupplierNumber = "";
                string SupplierPartyId = "";
                string SupplierSiteId = "";
                string SupplierId = "";
                string PayeePartyIdentifier = "";
                string PayeePartySiteIdentifier = "";
                bool has_this_supplier = false;
                string sql = "";
                string _BatchNo = "Supplier"; //因為同步太慢了 就不分批 只要有資料就記下來備查
                bool need_sync = false;
                // 1. supplier
                try
                {
                    if (ForceSync)
                    {
                        need_sync = true;
                    }
                    else
                    {

                        sql = $@"
select count(1) from  [OracleSupplier]
	WHERE [SupplierNumber] = :0
";
                        int cnts = int.Parse(sqlite.ExecuteScalarA(sql, EmpNo));
                        if (cnts == 0)
                        {
                            need_sync = true;
                        }
                        else
                        {
                            sql = $@"
select count(1) from  [OracleSupplierAddr]
	WHERE [SupplierNumber] = :0
";
                            cnts = int.Parse(sqlite.ExecuteScalarA(sql, EmpNo));
                            if (cnts == 0)
                            {
                                need_sync = true;
                            }
                            else
                            {
                                sql = $@"
select count(1) from  [OracleSupplierSite]
	WHERE [SupplierNumber] = :0
";
                                cnts = int.Parse(sqlite.ExecuteScalarA(sql, EmpNo));
                                if (cnts == 0)
                                {
                                    need_sync = true;
                                }
                                else
                                {
                                    sql = $@"
select count(1) from  [OracleSupplierAssignment]
	WHERE [SupplierNumber] = :0
";
                                    cnts = int.Parse(sqlite.ExecuteScalarA(sql, EmpNo));
                                    if (cnts == 0)
                                    {
                                        need_sync = true;
                                    }
                                    else
                                    {
                                        sql = $@"
select count(1) from  [OracleSupplierBankAccount]
	WHERE [SupplierNumber] = :0
";
                                        cnts = int.Parse(sqlite.ExecuteScalarA(sql, EmpNo));
                                        if (cnts == 0)
                                        {
                                            need_sync = true;
                                        }
                                    }
                                }
                            }
                        }
                    }


                    if (need_sync)
                    {
                        log.WriteLog("5", $"準備從oracle同步員工供應商回來:{EmpNo}");
                        MiddleReturn mr = GetOracleSupplierByEmpNo(EmpNo);
                        var statuscode = mr.StatusCode;
                        if (!string.IsNullOrWhiteSpace(mr.ErrorMessage))
                        {
                            log.WriteErrorLog($"取 oracle supplier 失敗:{mr.ErrorMessage}{mr.ReturnData}");
                        }
                        else
                        {

                            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                            string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                            if (string.IsNullOrWhiteSpace(desc_str))
                            {
                                throw new Exception("取得  oracle supplier 失敗, 回傳空白!");
                            }
                            SupplierResponseModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierResponseModel>(desc_str);

                            if (obj.Count > 0)
                            {
                                foreach (var item in obj.Items)
                                {
                                    SupplierId = item.SupplierId;
                                    SupplierNumber = item.SupplierNumber;
                                    SupplierPartyId = item.SupplierPartyId;

                                    sql = $@"
UPDATE [OracleSupplier]
	SET [BatchNo] = :0
		,[SupplierId] = :1
		,[SupplierPartyId] = :2
		,[Supplier] = :3
		,[AlternateName] = :4
		,[SupplierTypeCode] = :5
		,[SupplierType] = :6
		,[Status] = :7
	WHERE [SupplierNumber] = :8
";
                                    int cntu = sqlite.ExecuteByCmd(sql,
                                             _BatchNo,
                                             item.SupplierId,
                                             item.SupplierPartyId,
                                             item.Supplier,
                                             item.AlternateName,
                                             item.SupplierTypeCode,
                                             item.SupplierType,
                                             item.Status,
                                             item.SupplierNumber
                                             );
                                    if (cntu == 0)
                                    {

                                        sql = $@"
INSERT INTO [OracleSupplier]
		(
    [BatchNo] ,
	[SupplierId] ,
	[SupplierPartyId] ,
	[Supplier] ,
	[SupplierNumber] ,
	[AlternateName] ,
	[SupplierTypeCode] ,
	[SupplierType] ,
	[Status] 
)
	VALUES(
:0
,:1
,:2
,:3
,:4
,:5
,:6
,:7
,:8
)";
                                        int cnt = sqlite.ExecuteByCmd(sql, _BatchNo,
                                                  item.SupplierId,
                                                  item.SupplierPartyId,
                                                  item.Supplier,
                                                  item.SupplierNumber,
                                                  item.AlternateName,
                                                  item.SupplierTypeCode,
                                                  item.SupplierType,
                                                  item.Status
                                                  );
                                    }

                                    has_this_supplier = true;
                                }
                            }
                            log.WriteLog("5", $"同步了 {obj.Count} 筆");

                        }

                    }



                }
                catch (Exception ex)
                {
                    log.WriteErrorLog($"取得 1.Supplier:{EmpNo} 失敗:{ex.Message}{ex.InnerException}");
                }


                // 2. supplier address
                // 2-Get Supplier Address(第一步得到的Supplier ID)
                if (has_this_supplier)
                {

                    try
                    {
                        if (string.IsNullOrWhiteSpace(SupplierId))
                        {
                            throw new Exception($"SupplierId is null!");
                        }

                        MiddleReturn mr = GetOracleSupplierAddrByEmpNo(EmpNo, _BatchNo, SupplierId);
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        if (string.IsNullOrWhiteSpace(desc_str))
                        {
                            throw new Exception("取得  oracle supplier address 失敗, 回傳空白!");
                        }
                        SupplierAddrReturnModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierAddrReturnModel>(desc_str);

                        if (obj.Count > 0)
                        {
                            foreach (var item in obj.Items)
                            {

                                sql = $@"
UPDATE [OracleSupplierAddr]
	SET [BatchNo] = :0
		,[SupplierAddressId] = :1
		,[AddressName] = :2
		,[CountryCode] = :3
		,[Country] = :4
		,[AddressLine1] = :5
	WHERE [SupplierNumber] = :6
";
                                int cntu = sqlite.ExecuteByCmd(sql,
                                       _BatchNo,
                                       item.SupplierAddressId,
                                       item.AddressName,
                                       item.CountryCode,
                                       item.Country,
                                       item.AddressLine1,
                                       SupplierNumber
                                       );


                                if (cntu == 0)
                                {

                                    sql = $@"
INSERT INTO [OracleSupplierAddr]
		(
    [BatchNo] ,
	[SupplierNumber] ,
	[SupplierAddressId] ,
	[AddressName] ,
	[CountryCode] ,
	[Country] ,
	[AddressLine1]  
)
	VALUES(
:0
,:1
,:2
,:3
,:4
,:5
,:6 )
";
                                    int cnt = sqlite.ExecuteByCmd(sql, _BatchNo,
                                              SupplierNumber,
                                              item.SupplierAddressId,
                                              item.AddressName,
                                              item.CountryCode,
                                              item.Country,
                                              item.AddressLine1
                                              );
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteErrorLog($"取得 2.Supplier addr:{EmpNo} 失敗:{ex.Message}{ex.InnerException}");
                    }
                }


                // 3. supplier site
                // 3-Get Supplier Site(第一步得到的Supplier ID)
                if (has_this_supplier)
                {

                    try
                    {
                        if (string.IsNullOrWhiteSpace(SupplierId))
                        {
                            throw new Exception($"SupplierId is null!");
                        }


                        MiddleReturn mr = GetOracleSupplierSiteByEmpNo(EmpNo, _BatchNo, SupplierId);
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        if (string.IsNullOrWhiteSpace(desc_str))
                        {
                            throw new Exception($"取得 {EmpNo} 的 Supplier Site 失敗, 回傳空白!");
                        }
                        SupplierSiteReturnModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierSiteReturnModel>(desc_str);

                        if (obj.Count > 0)
                        {
                            foreach (var item in obj.Items)
                            {
                                var _SupplierSiteId = $"{item.SupplierSiteId}";
                                if (!string.IsNullOrWhiteSpace(_SupplierSiteId))
                                {
                                    SupplierSiteId = _SupplierSiteId;
                                }

                                sql = $@"
UPDATE [OracleSupplierSite]
	SET [BatchNo] = :0
		,[SupplierSiteId] = :1
		,[SupplierSite] = :2
		,[SupplierAddressId] = :3
		,[SupplierAddressName] = :4
	WHERE 	[SupplierNumber] = :5
";
                                int cntu = sqlite.ExecuteByCmd(sql,
                                       _BatchNo,
                                       item.SupplierSiteId,
                                       item.SupplierSite,
                                       item.SupplierAddressId,
                                       item.SupplierAddressName,
                                       SupplierNumber
                                       );

                                if (cntu == 0)
                                {

                                    sql = $@"
INSERT INTO [OracleSupplierSite]
		(
    [BatchNo] ,
	[SupplierNumber] ,

	[SupplierSiteId] ,
	[SupplierSite],
	[SupplierAddressId] ,
	[SupplierAddressName]  
)
	VALUES(
:0
,:1
,:2
,:3
,:4
,:5
)
";
                                    int cnt = sqlite.ExecuteByCmd(sql,
                                              _BatchNo,
                                              SupplierNumber,
                                              item.SupplierSiteId,
                                              item.SupplierSite,
                                              item.SupplierAddressId,
                                              item.SupplierAddressName
                                              );
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteErrorLog($"取得 3.Supplier Site:{EmpNo} 失敗:{ex.Message}{ex.InnerException}");
                    }
                }


                // 4. supplier Assignment
                // 4-Get Supplier Assignment( 第一個用第一步獲得的 Supplier ID，
                //     第二個用第三步獲得的 SupplierSiteId)
                if (has_this_supplier)
                {

                    try
                    {
                        if (string.IsNullOrWhiteSpace(SupplierId))
                        {
                            throw new Exception($"SupplierId is null!");
                        }

                        if (string.IsNullOrWhiteSpace(SupplierSiteId))
                        {
                            throw new Exception($"SupplierSiteId is null!");
                        }


                        MiddleReturn mr = GetOracleSupplierAssignmentByEmpNo(EmpNo, _BatchNo, SupplierId, SupplierSiteId);
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        if (string.IsNullOrWhiteSpace(desc_str))
                        {
                            throw new Exception($"取得 {EmpNo} 的 Supplier Assignment 失敗, 回傳空白!");
                        }
                        SupplierAssignmentReturnModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierAssignmentReturnModel>(desc_str);

                        if (obj.Count > 0)
                        {
                            foreach (var item in obj.Items)
                            {
                                sql = $@"
UPDATE [OracleSupplierAssignment]
	SET [BatchNo] = :0
		,[AssignmentId] = :1
		,[Status] = :2
	WHERE
		[SupplierNumber] =  :3
";
                                int cntu = sqlite.ExecuteByCmd(sql,
                                        _BatchNo,
                                        item.AssignmentId,
                                        item.Status,
                                        SupplierNumber
                                        );
                                if (cntu == 0)
                                {

                                    sql = $@"
INSERT INTO [OracleSupplierAssignment]
		(
    [BatchNo] ,
	[SupplierNumber] ,
	[AssignmentId] ,
	[Status] 
)
	VALUES(
:0
,:1
,:2
,:3

)
";
                                    int cnt = sqlite.ExecuteByCmd(sql,
                                              _BatchNo,
                                              SupplierNumber,
                                              item.AssignmentId,
                                              item.Status
                                              );
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteErrorLog($"取得 4.Supplier Assignment:{EmpNo} 失敗:{ex.Message}{ex.InnerException}");
                    }
                }

                // 5-1. supplier Payee
                //5-1.Get Supplier Bank Account-5.1   Get Payee Party ID(第一步獲得的 SupplierPartyID)
                if (has_this_supplier)
                {

                    try
                    {
                        if (string.IsNullOrWhiteSpace(SupplierPartyId))
                        {
                            throw new Exception($"SupplierPartyId is null!");
                        }

                        MiddleReturn mr = GetOracleSupplierPayeeByEmpNo(EmpNo, _BatchNo, SupplierPartyId);
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        if (string.IsNullOrWhiteSpace(desc_str))
                        {
                            throw new Exception($"取得 {EmpNo} 的 Supplier Payee 失敗, 回傳空白!");
                        }
                        SupplierPayeeReturnModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierPayeeReturnModel>(desc_str);

                        if (obj.Count > 0)
                        {
                            foreach (var item in obj.Items)
                            {

                                //  (第一個用5.1步驟獲得的 PayeePartyIdentifier，
                                //    第二個用5.1步驟獲得的 PayeePartySiteIdentifier)
                                var _PayeePartyIdentifier = $"{item.PayeePartyIdentifier}";
                                var _PayeePartySiteIdentifier = $"{item.PayeePartySiteIdentifier}";
                                if (!string.IsNullOrWhiteSpace(_PayeePartyIdentifier))
                                {
                                    PayeePartyIdentifier = _PayeePartyIdentifier;
                                    if (!string.IsNullOrWhiteSpace(_PayeePartySiteIdentifier))
                                    {
                                        PayeePartySiteIdentifier = _PayeePartySiteIdentifier;

                                        sql = $@"
UPDATE [OracleSupplierPayee]
	SET [BatchNo] = :0
		,[PayeeId] = :1
		,[PayeePartyIdentifier] = :2
		,[PartyName] = :3
		,[PayeePartyNumber] = :4
		,[PayeePartySiteIdentifier] = :5
		,[SupplierSiteCode] = :6
		,[SupplierSiteIdentifier] = :7
		,[PayeePartySiteNumber] = :8
	WHERE
		[SupplierNumber] = :9
";
                                        int cntu = sqlite.ExecuteByCmd(sql,
                                               _BatchNo,
                                               item.PayeeId,
                                               item.PayeePartyIdentifier,
                                               item.PartyName,
                                               item.PayeePartyNumber,
                                               item.PayeePartySiteIdentifier,
                                               item.SupplierSiteCode,
                                               item.SupplierSiteIdentifier,
                                               item.PayeePartySiteNumber,
                                               SupplierNumber
                                               );
                                        if (cntu == 0)
                                        {

                                            sql = $@"
INSERT INTO [OracleSupplierPayee]
		(
    [BatchNo] ,
	[SupplierNumber] ,

	[PayeeId] ,
	[PayeePartyIdentifier] ,
	[PartyName] ,
	[PayeePartyNumber] ,
	[PayeePartySiteIdentifier] ,
	[SupplierSiteCode] ,
	[SupplierSiteIdentifier] ,
	[PayeePartySiteNumber]
)
	VALUES(
:0
,:1
,:2
,:3
,:4
,:5
,:6
,:7
,:8
,:9

)
";
                                            int cnt = sqlite.ExecuteByCmd(sql, _BatchNo,
                                                      SupplierNumber,
                                                      item.PayeeId,
                                                      item.PayeePartyIdentifier,
                                                      item.PartyName,
                                                      item.PayeePartyNumber,
                                                      item.PayeePartySiteIdentifier,
                                                      item.SupplierSiteCode,
                                                      item.SupplierSiteIdentifier,
                                                      item.PayeePartySiteNumber
                                                      );
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteErrorLog($"取得 5-1.Supplier Payee:{EmpNo} 失敗:{ex.Message}{ex.InnerException}");
                    }
                }

                // 5.2   Get Supplier Bank Account
                //  (第一個用5.1步驟獲得的 PayeePartyIdentifier，
                //    第二個用5.1步驟獲得的 PayeePartySiteIdentifier)
                if (has_this_supplier)
                {

                    try
                    {
                        if (string.IsNullOrWhiteSpace(PayeePartyIdentifier))
                        {
                            throw new Exception($"PayeePartyIdentifier is null!");
                        }
                        if (string.IsNullOrWhiteSpace(PayeePartySiteIdentifier))
                        {
                            throw new Exception($"PayeePartySiteIdentifier is null!");
                        }



                        MiddleReturn mr = GetOracleSupplierBankAccountByEmpNo(EmpNo, _BatchNo,
                            PayeePartyIdentifier, PayeePartySiteIdentifier);
                        byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                        string desc_str = Encoding.UTF8.GetString(bs64_bytes);
                        if (string.IsNullOrWhiteSpace(desc_str))
                        {
                            throw new Exception($"取得 {EmpNo} 的 Supplier Bank Account 失敗, 回傳空白!");
                        }
                        SupplierBankAccountReturnModel obj = Newtonsoft.Json.JsonConvert.DeserializeObject<SupplierBankAccountReturnModel>(desc_str);

                        if (obj.Count > 0)
                        {
                            foreach (var item in obj.Items)
                            {
                                sql = $@"
UPDATE [OracleSupplierBankAccount]
	SET [BatchNo] = :0
		,[BankAccountId] = :1
		,[AccountNumber] = :2
		,[AccountName] = :3
		,[BankName] = :4
		,[BranchName] = :5
	WHERE 
		[SupplierNumber] = :6
";
                                int cntu = sqlite.ExecuteByCmd(sql,
                                        _BatchNo,
                                        item.BankAccountId, // 這個沒有用
                                        item.AccountNumber, // 這個就是帳號
                                        item.AccountName, //戶名
                                        item.BankName, //銀行名稱
                                        item.BranchName, //分行名稱
                                        SupplierNumber
                                        );

                                if (cntu == 0)
                                {

                                    sql = $@"
INSERT INTO [OracleSupplierBankAccount]
		(
    [BatchNo] ,
	[SupplierNumber] ,

	[BankAccountId] ,
	[AccountNumber] ,
	[AccountName] ,
	[BankName] ,
	[BranchName]  
)
	VALUES(
:0
,:1
,:2
,:3
,:4
,:5
,:6 

)
";
                                    int cnt = sqlite.ExecuteByCmd(sql, _BatchNo,
                                              SupplierNumber,

                                              item.BankAccountId,
                                              item.AccountNumber,
                                              item.AccountName,
                                              item.BankName,
                                              item.BranchName
                                              );
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteErrorLog($"取得 5.2   Get Supplier Bank Account:{EmpNo} 失敗:{ex.Message}{ex.InnerException}");
                    }
                }

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"SyncOneSupplierByEmpNo 同步一個供應商失敗:{ex.Message}{ex.InnerException}");
            }
        }

        /// <summary>
        /// 飛騰要先同步 這樣才知道有哪些 supplier 要查
        /// </summary>
        /// <param name="_BatchNo"></param>
        public void SyncSuppliers(string BatchNo)
        {

            CreateSQLiteFileIfNotExists_Supplier_1();
            CreateSQLiteFileIfNotExists_Supplier_2_address();
            CreateSQLiteFileIfNotExists_Supplier_3_site();
            CreateSQLiteFileIfNotExists_Supplier_4_Assignment();
            CreateSQLiteFileIfNotExists_Supplier_5_Payee();
            CreateSQLiteFileIfNotExists_Supplier_5_BankAccount();
            CreateSQLiteFileIfNotExists_HR_Oracle_Mapping();

            try
            {
                string sql = $@"
SELECT [BatchNo]
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
		,[Workplacename]
		,[idno]
		,[salaccountname]
		,[bankcode]
		,[bankbranch]
		,[salaccountid]
		,[jobstatus]
	FROM [Supplier] s
	where BatchNo = :0
	and s.r_code like '8%'	
";

                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                DataTable tb;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, BatchNo))
                {
                    //供應商不要分批次
                    //只要有資料就記下來
                    //供下次查詢
                    //因為同步一次的時間要好幾天

                    string NewEmpNo = $"{row["r_code"]}";
                    string EmpNo = NewEmpNo;

                    // stage 上用的是舊員編
                    EmpNo = GetOldEmpNo(NewEmpNo);

                    SyncOneSupplierByEmpNo(EmpNo);
                }

                //SaveOracleSuppliersIntoSQLite(Suppliers, BatchNo);

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"Get Supplier 失敗:{ex.Message}{ex.InnerException}");
            }

        }



        /// <summary>
        /// 把 oracle worker 存進 sqlite
        /// </summary>
        /// <param name="Workers"></param>
        void SaveOracleWorkersIntoSQLite(List<Worker> Workers, string BatchNo)
        {
            try
            {
                // 檢查一下 如果 sqlite 檔案沒有就自動建立
                CreateSQLiteFileIfNotExists_Batch();
                CreateSQLiteFileIfNotExists_OracleWorkers();
                CreateSQLiteFileIfNotExists_WorkerLinks();
                CreateSQLiteFileIfNotExists_WorkerProperties();
                CreateSQLiteFileIfNotExists_WorkerNames();
                CreateSQLiteFileIfNotExists_WorkerNamesLink();
                InsertIntoWorkersTable(Workers, BatchNo);
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"SaveOracleWorkersIntoSQLite 失敗:{ex.Message}{ex.InnerException}");
            }
        }

        void InsertIntoWorkersTable(List<Worker> Workers, string BatchNo)
        {
            try
            {
                //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string sql = "";

                //SQLiteUtl sqlite = new SQLiteUtl(db_file);

                foreach (Worker worker in Workers)
                {
                    sql = $@"
INSERT INTO [OracleWorkers]
		([BatchNo]
		,[PersonId]
		,[PersonNumber]
		,[CorrespondenceLanguage]
		,[BloodType]
		,[DateOfBirth]
		,[DateOfDeath]
		,[CountryOfBirth]
		,[RegionOfBirth]
		,[TownOfBirth]
		,[ApplicantNumber]
		,[CreatedBy]
		,[CreationDate]
		,[LastUpdatedBy]
		,[LastUpdateDate])
	VALUES
		(
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
:14
);
";
                    sqlite.ExecuteByCmd(sql,
                        BatchNo,
                        worker.PersonId,
                        worker.PersonNumber,
                        worker.CorrespondenceLanguage,
                        worker.BloodType,
                        worker.DateOfBirth,
                        worker.DateOfDeath,
                        worker.CountryOfBirth,
                        worker.RegionOfBirth,
                        worker.TownOfBirth,
                        worker.ApplicantNumber,
                        worker.CreatedBy,
                        worker.CreationDate,
                        worker.LastUpdatedBy,
                        worker.LastUpdateDate
                        );

                    foreach (WorkerLink link in worker.Links)
                    {
                        try
                        {
                            sql = $@"

INSERT INTO [WorkerLinks]
		([BatchNo]
        ,[PersonId]
		,[PersonNumber]
		,[Rel]
		,[Href]
		,[Name]
		,[Kind])
	VALUES
		(
:0
,:1
,:2
,:3
,:4
,:5
,:6
)
";
                            sqlite.ExecuteByCmd(sql,
                                    BatchNo,
                                    worker.PersonId,
                                    worker.PersonNumber,
                                    link.Rel,
                                    link.Href,
                                    link.Name,
                                    link.Kind
                                );

                            // 抓取 worker names 寫入 workerNames table
                            try
                            {
                                if (link.Name == "names")
                                {
                                    string url = link.Href;
                                    MiddleReturn mr = HttpGetFromOracleAP(url);
                                    if (!string.IsNullOrWhiteSpace(mr.ErrorMessage))
                                    {
                                        throw new Exception($"Get Worker names 失敗:{mr.ErrorMessage}");
                                    }
                                    var bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                                    var desc_str = Encoding.UTF8.GetString(bs64_bytes);
                                    if (string.IsNullOrWhiteSpace(desc_str))
                                    {
                                        throw new Exception("取得  Worker names 失敗, 回傳空白!");
                                    }
                                    WorkerNamesResponseObj obj = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkerNamesResponseObj>(desc_str);
                                    if (obj.Count > 0)
                                    {
                                        WorkerName work_Name = obj.Items[0];
                                        sql = $@"

INSERT INTO [WorkerNames]
		([BatchNo]
        ,[PersonId]
		,[PersonNumber]
		,[PersonNameId]
		,[EffectiveStartDate]
		,[EffectiveEndDate]
		,[LegislationCode]
		,[LastName]
		,[FirstName]
		,[Title]
		,[MiddleNames]
		,[DisplayName]
		,[OrderName]
		,[ListName]
		,[FullName]
		,[NameLanguage]
		,[CreatedBy]
		,[CreationDate]
		,[LastUpdatedBy]
		,[LastUpdateDate]
)
	VALUES
		(
:0
,:1
,:2
,:3
,:4
,:5
,:6
,:7
,:8
,:9
,:10
,:11
,:12
,:13
,:14
,:15
,:16
,:17
,:18
,:19

)
";
                                        int cnt_names = sqlite.ExecuteByCmd(sql,
                                            BatchNo,
                                            worker.PersonId,
                                            worker.PersonNumber,
                                            work_Name.PersonNameId,
                                            work_Name.EffectiveStartDate,
                                            work_Name.EffectiveEndDate,
                                            work_Name.LegislationCode,
                                            work_Name.LastName,
                                            work_Name.FirstName,
                                            work_Name.Title,
                                            work_Name.MiddleNames,
                                            work_Name.DisplayName,
                                            work_Name.OrderName,
                                            work_Name.ListName,
                                            work_Name.FullName,
                                            work_Name.NameLanguage,
                                            work_Name.CreatedBy,
                                            work_Name.CreationDate,
                                            work_Name.LastUpdatedBy,
                                            work_Name.LastUpdateDate
                                            );

                                        foreach (var work_name_link in work_Name.Links)
                                        {
                                            if (work_name_link.Rel == "self" && work_name_link.Name == "names")
                                            {
                                                try
                                                {
                                                    sql = $@"
INSERT INTO [WorkerNamesLink]
(
BatchNo ,
PersonId ,
PersonNumber ,
PersonNameId ,
Href 
)
VALUES
(
:0
,:1
,:2
,:3
,:4
)
";
                                                    int cnt_names_link = sqlite.ExecuteByCmd(sql,
                                                         BatchNo,
                                                         worker.PersonId,
                                                         worker.PersonNumber,
                                                         work_Name.PersonNameId,
                                                         work_name_link.Href
                                                         );
                                                }
                                                catch (Exception exWorkNameLink)
                                                {
                                                    log.WriteErrorLog($"Insert WorkerNameLink 失敗:{exWorkNameLink.Message}{exWorkNameLink.InnerException}");

                                                }
                                            }
                                        }
                                    }




                                    //                                    // ORACLE STAGE
                                    //                                    // for  oracle ap
                                    //                                    MiddleModel2 send_model2 = new MiddleModel2();
                                    //                                    send_model2.URL = url;
                                    //                                    //send_model2.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(par);
                                    //                                    send_model2.Method = "GET";
                                    //                                    //string username = this.UserName;
                                    //                                    //string password = this.Password;
                                    //                                    send_model2.UserName = this.UserName;
                                    //                                    send_model2.Password = this.Password;
                                    //                                    string usernamePassword = send_model2.UserName + ":" + send_model2.Password;
                                    //                                    send_model2.AddHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword)));
                                    //                                    //CredentialCache myCred = new CredentialCache();
                                    //                                    //myCred.Add(new Uri(send_model2.URL), "Basic", new NetworkCredential(username, password));
                                    //                                    //send_model2.Cred = myCred;
                                    //                                    send_model2.Timeout = 1000 * 60 * 30;



                                    //                                    // for BOXMAN API
                                    //                                    MiddleModel send_model = new MiddleModel();
                                    //                                    var _url = $"{Oracle_AP}/api/Middle/Call/";
                                    //                                    send_model.URL = _url;
                                    //                                    send_model.SendingData = Newtonsoft.Json.JsonConvert.SerializeObject(send_model2);
                                    //                                    send_model.Method = "POST";
                                    //                                    send_model.Timeout = 1000 * 60 * 30;
                                    //                                    var ret = ApiOperation.CallApi(
                                    //                                            new ApiRequestSetting()
                                    //                                            {
                                    //                                                Data = send_model,
                                    //                                                MethodRoute = "api/Middle/Call",
                                    //                                                MethodType = "POST",
                                    //                                                TimeOut = 1000 * 60 * 30
                                    //                                            }
                                    //                                            );
                                    //                                    if (ret.Success)
                                    //                                    {
                                    //                                        string receive_str = ret.ResponseContent;
                                    //                                        try
                                    //                                        {
                                    //                                            MiddleReturn mr2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(receive_str);
                                    //                                            if (!string.IsNullOrWhiteSpace(mr2.ErrorMessage))
                                    //                                            {
                                    //                                                var _errmsg = $"呼叫 {url} 失敗:{mr2.StatusCode} {mr2.StatusDescription} {mr2.ReturnData} {mr2.ErrorMessage}";
                                    //                                                throw new Exception(_errmsg);
                                    //                                            }

                                    //                                            MiddleReturn mr = Newtonsoft.Json.JsonConvert.DeserializeObject<MiddleReturn>(mr2.ReturnData);
                                    //                                            if (!string.IsNullOrWhiteSpace(mr.ErrorMessage))
                                    //                                            {
                                    //                                                var _errmsg = $"呼叫 {url} 失敗:{mr.StatusCode} {mr.StatusDescription} {mr.ReturnData} {mr.ErrorMessage}";
                                    //                                                throw new Exception(_errmsg);
                                    //                                            }

                                    //                                            byte[] bs64_bytes = Convert.FromBase64String(mr.ReturnData);
                                    //                                            string desc_str = Encoding.UTF8.GetString(bs64_bytes);

                                    //                                            WorkerNamesResponseObj obj = Newtonsoft.Json.JsonConvert.DeserializeObject<WorkerNamesResponseObj>(desc_str);
                                    //                                            if (obj.Count > 0)
                                    //                                            {
                                    //                                                WorkerName work_Name = obj.Items[0];
                                    //                                                sql = $@"

                                    //INSERT INTO [WorkerNames]
                                    //		([BatchNo]
                                    //        ,[PersonId]
                                    //		,[PersonNumber]
                                    //		,[PersonNameId]
                                    //		,[EffectiveStartDate]
                                    //		,[EffectiveEndDate]
                                    //		,[LegislationCode]
                                    //		,[LastName]
                                    //		,[FirstName]
                                    //		,[Title]
                                    //		,[MiddleNames]
                                    //		,[DisplayName]
                                    //		,[OrderName]
                                    //		,[ListName]
                                    //		,[FullName]
                                    //		,[NameLanguage]
                                    //		,[CreatedBy]
                                    //		,[CreationDate]
                                    //		,[LastUpdatedBy]
                                    //		,[LastUpdateDate]
                                    //)
                                    //	VALUES
                                    //		(
                                    //:0
                                    //,:1
                                    //,:2
                                    //,:3
                                    //,:4
                                    //,:5
                                    //,:6
                                    //,:7
                                    //,:8
                                    //,:9
                                    //,:10
                                    //,:11
                                    //,:12
                                    //,:13
                                    //,:14
                                    //,:15
                                    //,:16
                                    //,:17
                                    //,:18
                                    //,:19

                                    //)
                                    //";
                                    //                                                int cnt_names = sqlite.ExecuteByCmd(sql,
                                    //                                                    BatchNo,
                                    //                                                    worker.PersonId,
                                    //                                                    worker.PersonNumber,
                                    //                                                    work_Name.PersonNameId,
                                    //                                                    work_Name.EffectiveStartDate,
                                    //                                                    work_Name.EffectiveEndDate,
                                    //                                                    work_Name.LegislationCode,
                                    //                                                    work_Name.LastName,
                                    //                                                    work_Name.FirstName,
                                    //                                                    work_Name.Title,
                                    //                                                    work_Name.MiddleNames,
                                    //                                                    work_Name.DisplayName,
                                    //                                                    work_Name.OrderName,
                                    //                                                    work_Name.ListName,
                                    //                                                    work_Name.FullName,
                                    //                                                    work_Name.NameLanguage,
                                    //                                                    work_Name.CreatedBy,
                                    //                                                    work_Name.CreationDate,
                                    //                                                    work_Name.LastUpdatedBy,
                                    //                                                    work_Name.LastUpdateDate
                                    //                                                    );

                                    //                                                foreach (var work_name_link in work_Name.Links)
                                    //                                                {
                                    //                                                    if (work_name_link.Rel == "self" && work_name_link.Name == "names")
                                    //                                                    {
                                    //                                                        try
                                    //                                                        {
                                    //                                                            sql = $@"
                                    //INSERT INTO [WorkerNamesLink]
                                    //(
                                    //BatchNo ,
                                    //PersonId ,
                                    //PersonNumber ,
                                    //PersonNameId ,
                                    //Href 
                                    //)
                                    //VALUES
                                    //(
                                    //:0
                                    //,:1
                                    //,:2
                                    //,:3
                                    //,:4
                                    //)
                                    //";
                                    //                                                            int cnt_names_link = sqlite.ExecuteByCmd(sql,
                                    //                                                                 BatchNo,
                                    //                                                                 worker.PersonId,
                                    //                                                                 worker.PersonNumber,
                                    //                                                                 work_Name.PersonNameId,
                                    //                                                                 work_name_link.Href
                                    //                                                                 );
                                    //                                                        }
                                    //                                                        catch (Exception exWorkNameLink)
                                    //                                                        {
                                    //                                                            log.WriteErrorLog($"Insert WorkerNameLink 失敗:{exWorkNameLink.Message}{exWorkNameLink.InnerException}");

                                    //                                                        }
                                    //                                                    }
                                    //                                                }
                                    //                                            }

                                    //                                        }
                                    //                                        catch (Exception exWorkNames)
                                    //                                        {
                                    //                                            log.WriteErrorLog($"呼叫 {url} 失敗:{exWorkNames.Message}{exWorkNames.InnerException}");
                                    //                                        }
                                    //                                    }
                                    //                                    else
                                    //                                    {
                                    //                                        log.WriteErrorLog($"Call Boxman Api {_url} 失敗:{ret.ErrorMessage} {ret.ErrorException}");
                                    //                                    }


                                }
                            }
                            catch (Exception wnEx)
                            {
                                log.WriteErrorLog($"寫入 Worker names 失敗:{wnEx}");
                            }
                        }
                        catch (Exception exlink)
                        {
                            log.WriteErrorLog($"寫入 WorkerLinks 失敗:{exlink}");
                        }


                        if (link.Properties != null)
                        {
                            try
                            {
                                sql = $@"
INSERT INTO [WorkerProperties]
		([BatchNo]
        ,[PersonId]
		,[PersonNumber]
		,[ChangeIndicator])
	VALUES
		(
:0
,:1
,:2
,:3
)
";
                                sqlite.ExecuteByCmd(sql,
                                     BatchNo,
                                     worker.PersonId,
                                     worker.PersonNumber,
                                     link.Properties.ChangeIndicator
                         );
                            }
                            catch (Exception exProp)
                            {
                                log.WriteErrorLog($"寫入 WorkerProperties 失敗:{exProp}");
                            }

                        }
                    }
                }


                sql = $@"
                INSERT INTO [Batch]
                		([BatchNo])
                	VALUES
                		(:0)
                ";
                int cnt = sqlite.ExecuteByCmd(sql, BatchNo);

                //int cnt = 0;

                // 留下 3 天的資料
                string B4BatchNo = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd HH:mm:ss.fff");
                sql = $@"
DELETE FROM  [Batch] WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

                sql = $@"
                DELETE FROM   [OracleWorkers]  WHERE [BatchNo] <= :0
                ";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

                sql = $@"
                DELETE FROM   [WorkerLinks]  WHERE [BatchNo] <= :0
                ";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

                sql = $@"
                DELETE FROM   [WorkerNames]  WHERE [BatchNo] <= :0
                ";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

                sql = $@"
DELETE FROM  [WorkerNamesLink] WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

                sql = $@"
DELETE FROM  [WorkerProperties] WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"InsertIntoTable 失敗:{ex.Message}{ex.InnerException}");
            }

        }


        /// <summary>
        /// 建立放oracle bank資料的table
        /// </summary>
        void CreateSQLiteFileIfNotExists_OracleBankDataBranchData()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_OracleBankDataBranchData";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleBankData";
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
BANK_NAME nvarchar(100),
BANK_NUMBER nvarchar(10),
BANK_BRANCH_NAME nvarchar(100),
BRANCH_NUMBER nvarchar(10)

)";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                }



            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"{fn_name} 失敗:{ex.Message}{ex.InnerException}");
            }
        }


        /// <summary>
        /// 自動建立 oracle workers table
        /// </summary>
        void CreateSQLiteFileIfNotExists_OracleWorkers()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_OracleWorkers";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleWorkers";
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
PersonId nvarchar(100),
PersonNumber nvarchar(100),
CorrespondenceLanguage nvarchar(50),
BloodType nvarchar(50),
DateOfBirth nvarchar(50),
DateOfDeath nvarchar(50),
CountryOfBirth nvarchar(50),
RegionOfBirth nvarchar(50),
TownOfBirth nvarchar(100),
ApplicantNumber nvarchar(100),
CreatedBy nvarchar(100),
CreationDate nvarchar(100),
LastUpdatedBy nvarchar(100),
LastUpdateDate nvarchar(100)
)";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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
        /// create oracle worker links table
        /// </summary>
        void CreateSQLiteFileIfNotExists_WorkerLinks()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_WorkerLinks";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "WorkerLinks";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    int cnt = 0;
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) ,
PersonId nvarchar(100),
PersonNumber nvarchar(50),
Rel nvarchar(100),
Href nvarchar(4000),
Name nvarchar(100),
Kind nvarchar(100)
)";

                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_WorkerProperties()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_WorkerProperties";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "WorkerProperties";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table links
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) ,
PersonId nvarchar(100),
PersonNumber nvarchar(50),
ChangeIndicator nvarchar(4000)
)";
                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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

        void CreateSQLiteFileIfNotExists_WorkerNames()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_WorkerNames";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "WorkerNames";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table links
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) ,
PersonId nvarchar(100),
PersonNumber nvarchar(50),
PersonNameId nvarchar(100),
EffectiveStartDate nvarchar(50),
EffectiveEndDate nvarchar(50),
LegislationCode nvarchar(10),
LastName nvarchar(100),
FirstName nvarchar(100),
Title nvarchar(100),
MiddleNames nvarchar(100),
DisplayName nvarchar(100),
OrderName nvarchar(100),
ListName nvarchar(100),
FullName nvarchar(100),
NameLanguage nvarchar(100),
CreatedBy nvarchar(100),
CreationDate nvarchar(100),
LastUpdatedBy nvarchar(100),
LastUpdateDate nvarchar(100)
)";
                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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
        void CreateSQLiteFileIfNotExists_WorkerNamesLink()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_WorkerNamesLink";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "WorkerNamesLink";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table links
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) ,
PersonId nvarchar(100),
PersonNumber nvarchar(50),
PersonNameId nvarchar(100),
Href nvarchar(4000)
)";
                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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


        void SaveOracleUsersIntoSQLite(List<OracleUser> all_users, string BatchNo)
        {
            try
            {
                // 檢查一下 如果 sqlite 檔案沒有就自動建立
                CreateSQLiteFileIfNotExists_Batch();
                CreateSQLiteFileIfNotExists_OracleUsers();
                CreateSQLiteFileIfNotExists_Links();
                CreateSQLiteFileIfNotExists_Properties();
                InsertIntoOracleUsersTable(all_users, BatchNo);
            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"SaveOracleUsersIntoSQLite 失敗:{ex.Message}{ex.InnerException}");
            }
        }

        void InsertIntoOracleUsersTable(List<OracleUser> all_users, string BatchNo)
        {
            try
            {
                //string BatchNo = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string sql = "";

                //SQLiteUtl sqlite = new SQLiteUtl(db_file);

                foreach (OracleUser user in all_users)
                {
                    sql = $@"
INSERT INTO [OracleUsers]
		([BatchNo]
		,[UserId]
		,[Username]
		,[SuspendedFlag]
		,[PersonId]
		,[PersonNumber]
		,[CredentialsEmailSentFlag]
		,[GUID]
		,[CreatedBy]
		,[CreationDate]
		,[LastUpdatedBy]
		,[LastUpdateDate])
	VALUES
		(
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
:11);
";
                    sqlite.ExecuteByCmd(sql,
                        BatchNo,
                        user.UserId,
                        user.Username,
                        user.SuspendedFlag,
                        user.PersonId,
                        user.PersonNumber,
                        user.CredentialsEmailSentFlag,
                        user.GUID,
                        user.CreatedBy,
                        user.CreationDate,
                        user.LastUpdatedBy,
                        user.LastUpdateDate
                        );

                    foreach (OracleApiReturnObjLink link in user.Links)
                    {
                        sql = $@"

INSERT INTO [Links]
		([BatchNo]
        ,[PersonId]
		,[PersonNumber]
		,[Rel]
		,[Href]
		,[Name]
		,[Kind])
	VALUES
		(
:0
,:1
,:2
,:3
,:4
,:5
,:6
)
";
                        sqlite.ExecuteByCmd(sql,
                                BatchNo,
                                user.PersonId,
                                user.PersonNumber,
                                link.Rel,
                                link.Href,
                                link.Name,
                                link.Kind
                            );

                        if (link.Properties != null)
                        {
                            sql = $@"
INSERT INTO [Properties]
		([BatchNo]
        ,[PersonId]
		,[PersonNumber]
		,[ChangeIndicator])
	VALUES
		(
:0
,:1
,:2
,:3
)
";
                            sqlite.ExecuteByCmd(sql,
                                 BatchNo,
                                 user.PersonId,
                                 user.PersonNumber,
                                 link.Properties.ChangeIndicator
                     );
                        }
                    }
                }


                sql = $@"
                INSERT INTO [Batch]
                		([BatchNo])
                	VALUES
                		(:0)
                ";
                int cnt = sqlite.ExecuteByCmd(sql, BatchNo);
                //int cnt = 0;

                // 留下 3 天的資料
                string B4BatchNo = DateTime.Today.AddDays(-3).ToString("yyyy-MM-dd HH:mm:ss.fff");
                sql = $@"
DELETE FROM  [Batch] WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);
                sql = $@"
DELETE FROM   [OracleUsers]  WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);
                sql = $@"
DELETE FROM  [Links] WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);
                sql = $@"
DELETE FROM  [Properties] WHERE [BatchNo] <= :0
";
                cnt = sqlite.ExecuteByCmd(sql, B4BatchNo);

            }
            catch (Exception ex)
            {
                log.WriteErrorLog($"InsertIntoTable 失敗:{ex.Message}{ex.InnerException}");
            }
        }



        /// <summary>
        /// 如果 sqlite 檔案沒有就自動建立
        /// </summary>
        void CreateSQLiteFileIfNotExists_Batch()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Batch";
            try
            {

                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "Batch";
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
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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
        /// 自動建立 oracle users table
        /// </summary>
        void CreateSQLiteFileIfNotExists_OracleUsers()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_OracleUsers";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "OracleUsers";
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
UserId nvarchar(100),
Username nvarchar(100),
SuspendedFlag nvarchar(10),
PersonId nvarchar(50),
PersonNumber nvarchar(50),
CredentialsEmailSentFlag nvarchar(50),
GUID nvarchar(100),
CreatedBy nvarchar(100),
CreationDate nvarchar(50),
LastUpdatedBy nvarchar(100),
LastUpdateDate nvarchar(50)
)";

                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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
        /// 自動建立 oracle users links table
        /// </summary>
        void CreateSQLiteFileIfNotExists_Links()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Links";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "Links";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    int cnt = 0;
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) ,
PersonId nvarchar(50),
PersonNumber nvarchar(50),
Rel nvarchar(100),
Href nvarchar(4000),
Name nvarchar(100),
Kind nvarchar(100)
)";

                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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
        /// 自動建立 oracle users properties table
        /// </summary>
        void CreateSQLiteFileIfNotExists_Properties()
        {
            string fn_name = "CreateSQLiteFileIfNotExists_Properties";
            try
            {
                db_path = $@"{AppDomain.CurrentDomain.BaseDirectory}Jobs\ORSyncOracleData\";
                if (!Directory.Exists(db_path))
                {
                    Directory.CreateDirectory(db_path);
                }
                db_file = $"{db_path}OracleData.sqlite";
                //SQLiteUtl sqlite = new SQLiteUtl(db_file);
                string table_name = "Properties";
                string sql = $"SELECT * FROM sqlite_master WHERE type='table' AND name=:0 ";
                DataTable tb = null;
                bool hasNoTable = true;
                foreach (DataRow row in sqlite.QueryOkWithDataRows(out tb, sql, table_name))
                {
                    hasNoTable = false;
                }
                if (hasNoTable)
                {
                    // create table links
                    //string sqlite_sql = $"DROP TABLE IF EXISTS {table_name}";
                    //sqlite.ExecuteScalarA(sqlite_sql);
                    string sqlite_sql = $@"CREATE TABLE [{table_name}] (
BatchNo nvarchar(30) ,
PersonId nvarchar(50),
PersonNumber nvarchar(50),
ChangeIndicator nvarchar(4000)
)";
                    int cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index ON {table_name} (PersonNumber);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);
                    sqlite_sql = $"CREATE INDEX  {table_name}_index1 ON {table_name} (BatchNo);";
                    cnt = sqlite.ExecuteByCmd(sqlite_sql, null);

                }
                else
                {
                    // 如果有 就保留 3 批
                    sql = $"SELECT BatchNo FROM Batch ORDER BY BatchNo DESC";
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



    }



}
