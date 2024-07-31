using MstCore;
using System;
using System.Collections;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using MstSopService.DTOs;
using MstDBComman.Config;
using MstSopService.DTO;
using MstDBComman.Models;

namespace MstSopService.Tools
{
    public class HttpTool
    {
        Dictionary<string, string> headers = new Dictionary<string, string>();
        public HttpTool(IHttpContextAccessor httpContextAccessor)
        {
            SetHeaders(httpContextAccessor);
        }

        private void SetHeaders(IHttpContextAccessor httpContextAccessor)
        {
            Hashtable hkeys = new Hashtable();
            hkeys.Add("Authorization", "");
            foreach (string key in httpContextAccessor.HttpContext.Request.Headers.Keys)
            {
                if (hkeys.Contains(key) && headers.ContainsKey(key) == false)
                {
                    headers.Add(key, httpContextAccessor.HttpContext.Request.Headers[key]);
                }
            }
        }
        /// <summary>
        /// 获取所有在职人员、和组织架构码名信息
        /// </summary>
        public List<CodeNamesDTO> ObtainCodeNamesData()
        {
            try
            {
                //string url = "https://api.kwesz.com.cn/MstPermissionService/api/C2N/DepartmentUser?onjob=true";
                string url = SysConfig.Configuration["C2N"].ToString();
                var resData = HttpWeb.HttpGetJson<Hashtable>(url, headers);
                if (resData["code"].ToString() != "200")
                {
                    throw new Exception($"获取信息失败");
                }
                var data = JsonConvert.DeserializeObject<List<CodeNamesDTO>>(resData["data"].ToString());
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"数据获取失败,具体消息： " + ex);
            }

        }
        /// <summary>
        /// 获取SSS船公司名信息
        /// </summary>
        public List<CarrierInfoDTO> ObtainThenameOfSSSShippingCompany()
        {
            try
            {
                //string url = "https://api.kwesz.com.cn/MstCRMService/api/QuotationCarrierInfo/QuotationCarrierInfo";
                string url = SysConfig.Configuration["CarrierInfo"].ToString();
                headers.Add("MenuId", "16762801136387072");
                headers.Add("ModuleId", "16799212966724608");
                QueryDescriptor descriptor = new QueryDescriptor();
                var resData = HttpWeb.HttpPostJson<Hashtable>(url, descriptor, headers);
                if (resData["code"].ToString() != "200")
                {
                    throw new Exception($"获取信息失败");
                }
                var data = JsonConvert.DeserializeObject<ReturnSerialize<CarrierInfoDTO>>(resData["data"].ToString());
                return data.Data;
            }
            catch (Exception ex)
            {
                throw new Exception($"数据获取失败,具体消息： " + ex);
            }
        }
        /// <summary>
        /// 获取部门人员信息
        /// </summary>
        public List<DeptOrPersonnelOut> ObtainDeptOrPersonnel(string codes)
        {
            try
            {
                //string url = "https://api.kwesz.com.cn/MstPermissionService/api/Open/dept/deptOrPersonnel";
                string url = SysConfig.Configuration["DeptOrPersonnel"].ToString();
                Dictionary<string, string> parameter = new Dictionary<string, string>();
                parameter.Add("companyId", "KWE001");
                parameter.Add("departmentCodes", codes);
                var resData = HttpWeb.HttpPostJson<Hashtable>(url, parameter, headers);
                if (resData["code"].ToString() != "200")
                {
                    throw new Exception($"获取信息失败");
                }
                var test = resData["data"].ToString();
                var data = JsonConvert.DeserializeObject<List<DeptOrPersonnelOut>>(resData["data"].ToString());
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"数据获取失败,具体消息： " + ex);
            }
        }
        /// <summary>
        /// 获取人员信息
        /// </summary>
        public List<DeptOrPersonnelOut> ObtainDeptOrFreightRatePersonnel(string codes)
        {
            try
            {
                //string url = "https://api.kwesz.com.cn/MstPermissionService/api/Open/dept/deptOrPersonnel";
                string url = SysConfig.Configuration["DeptOrPersonnel"].ToString();
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter.Add("companyId", "KWE001");
                parameter.Add("departmentCodes", "");
                parameter.Add("userids", codes);
                parameter.Add("isSelect", true);
                var resData = HttpWeb.HttpPostJson<Hashtable>(url, parameter, headers);
                if (resData["code"].ToString() != "200")
                {
                    throw new Exception($"获取信息失败");
                }
                var test = resData["data"].ToString();
                var data = JsonConvert.DeserializeObject<List<DeptOrPersonnelOut>>(resData["data"].ToString());
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception($"数据获取失败,具体消息： " + ex);
            }
        }
        

    }
}
