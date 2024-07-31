using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MstSopService.DTO;
using MstSopService.IService;

namespace MstSopService.Controllers
{
    /// <summary>
    /// 本地字典API
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DictController : ControllerBase
    {
        IDictService _dictService;
        public DictController(IDictService dictService)
        {
            _dictService = dictService;
        }
        /// <summary>
        /// 获取字典信息
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetDict")]
        public IActionResult GetDict(string code)
        {
            return _dictService.GetDict(code);
        }
        /// <summary>
        /// 获取字典信息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetDicts")]
        public IActionResult GetDicts(DictsInput input)
        {
            return _dictService.GetDicts(input);
        }
        /// <summary>
        /// 获取字典信息
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetDictsHierarchy")]
        public IActionResult GetDictsHierarchy(DictsInput input)
        {
            return _dictService.GetDictsHierarchy(input);
        }
    }
}
