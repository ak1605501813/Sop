using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MstCore;
using Microsoft.AspNetCore.Mvc;

namespace MstSopService
{
    /// <summary>
    /// 返回可被其他服务调用的对象，并且返回的对象可以转换为IActionResult
    /// 作者：Steven
    /// </summary>
    public class MstExeResult
    {
        public static IActionResult ToActionResult<T>(MstActionResult<T> mstActionResult)
        {
            JsonResult JsonResult = new JsonResult(new
            {
                Code = mstActionResult.Code,
                Message = mstActionResult.Message,
                Data = mstActionResult.Data,
                MsgType = mstActionResult.MsgType
            });
            return JsonResult;
        }

        public static MstActionResult<object> Result(Hashtable rtnData)
        {
            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = rtnData?["code"]?.ToString(),
                Message = rtnData?["message"]?.ToString(),
                Data = rtnData?["data"],
                MsgType = rtnData?["msgType"].ToString()
            };
            return JsonResult;
        }

        public static MstActionResult<T> Success<T>(T rtnData, string msg = null)
        {

            MstActionResult<T> JsonResult =new MstActionResult<T>
            {
                Code = "200",
                Message = msg ?? "操作成功",
                Data = rtnData,
                MsgType = MsgType.Info
            };
            return JsonResult;

        }

        public static MstActionResult<object> Success(object rtnData = null, string msg = null)
        {

            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = "200",
                Message = msg ?? "操作成功",
                Data = rtnData ?? string.Empty,
                MsgType = MsgType.Info
            };
            return JsonResult;

        }

        public static MstActionResult<object> Success(object rtnData)
        {

            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = "200",
                Message = "",
                Data = rtnData ?? string.Empty,
                MsgType = MsgType.Info
            };
            return JsonResult;

        }

        public static MstActionResult<object> Success3(object rtnData)
        {
            //将object对象转换为json字符
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(rtnData);
            var tjson = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(json);

            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = "200",
                Message = "",
                Data = tjson,
                MsgType = MsgType.Info
            };
            return JsonResult;

        }

        public static MstActionResult<object> Success2(object rtnData, string msg = null)
        {

            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = "200",
                Message = msg,
                Data = rtnData ?? string.Empty,
                MsgType = MsgType.Info
            };
            return JsonResult;

        }
        public static MstActionResult<string> Error(string responseMessage, string responseCode = "400")
        {
            MstActionResult<string> returnJson = new MstActionResult<string>
            {
                Code = responseCode,
                Message = responseMessage ?? "操作失败",
                Data = string.Empty,
                MsgType = MsgType.Error
            };
            return returnJson;
        }

        public static MstActionResult<T> Error<T>(string responseMessage, T rtnData, string responseCode = "400")
        {
            MstActionResult<T> returnJson = new MstActionResult<T>
            {
                Code = responseCode??"400",
                Message = responseMessage ?? "操作失败",
                Data = rtnData,
                MsgType = MsgType.Error
            };
            return returnJson;
        }


        public static MstActionResult<string> Warn(string msg = null, string responseCode = "250")
        {

            MstActionResult<string> JsonResult = new MstActionResult<string>
            {
                Code = responseCode,
                Message = msg,
                Data = string.Empty,
                MsgType = MsgType.Warn
            };
            return JsonResult;

        }

        public static MstActionResult<object> Warn(object rtnData = null, string msg = null, string responseCode = "250")
        {

            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = responseCode,
                Message = msg,
                Data = rtnData ?? string.Empty,
                MsgType = MsgType.Warn
            };
            return JsonResult;

        }

        //unauthorized

        public static MstActionResult<object> UnAuthorized(string responseMessage, string responseCode = "401", object rtnData = null)
        {

            MstActionResult<object> returnJson = new MstActionResult<object>
            {
                Code = responseCode,
                Message = responseMessage ?? string.Empty,
                Data = rtnData ?? string.Empty,
                MsgType = MsgType.UnAuthorized
            };
            return returnJson;
        }


        /// <summary>
        /// 返回数据塑形
        /// </summary>
        /// <param name="rtnData">返回的数据</param>
        /// <param name="noUiprop">不给前端看的字段</param>
        /// <param name="uiprop">给前端看的字段</param>
        /// <returns></returns>
        public static MstActionResult<object> Success(object rtnData, string[] noUiprop, string[] uiprop = null)
        {
            var jso = Json.DefaultJso(new MyNamingPolicy(uiprop, noUiprop));
            var json = System.Text.Json.JsonSerializer.Serialize(rtnData, jso);
            json = Regex.Replace(json, "\"__del.*", "", RegexOptions.IgnoreCase);

            json = Regex.Replace(json, ",\\s+}", "}");

            var obj2 = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);

            MstActionResult<object> JsonResult = new MstActionResult<object>
            {
                Code = "200",
                Message = "",
                Data = obj2 ?? string.Empty,
                MsgType = MsgType.Info
            };

            return JsonResult;

        }
    }
}
