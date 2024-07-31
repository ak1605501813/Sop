using MstCoreV3._redis;
using MstSopService.DTOs;
using MstSopService.IService;
using MstSopService.Tools;
using System;
using System.Collections.Generic;

namespace MstSopService.Service
{
    public class CodeNameConversionService: ICodeNameConversionService
    {
        HttpTool _httpTool;
        public CodeNameConversionService(HttpTool httpTool) 
        {
            _httpTool = httpTool;
        }
        public List<CodeNamesDTO> GetCodeNamesDatas() 
        {
            //ReDataTemplateDTO<CodeNamesDTO>
            RedisCacheHelper2 redis = new RedisCacheHelper2();
            if (redis.Exist("MstSopService: CN", "CN")) 
            {
                var CNRData=redis.GetCache("MstSopService: CN", "CN");
                return ConvertObject<List<CodeNamesDTO>>(CNRData);
            }
            var data= _httpTool.ObtainCodeNamesData();
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            redis.SetCache("MstSopService: CN", "CN", str);//存所有
            redis.SetExpire("MstSopService: CN", DateTime.Now.AddDays(2));
            return data;
        }

        public static T ConvertObject<T>(object obj) where T : new()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return data;
        }
    }
}
