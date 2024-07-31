using Microsoft.AspNetCore.Mvc;
using MstSopService.DTO;
using MstSopService.IService;

namespace MstSopService.Controllers
{
    /// <summary>
    /// 运输框架图API
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingFrameDlagramController : ControllerBase
    {
        IShippingFrameDlagramService _shippingFrameDlagram;
        public ShippingFrameDlagramController(IShippingFrameDlagramService shippingFrameDlagram) 
        {
            _shippingFrameDlagram=  shippingFrameDlagram;
        }
        /// <summary>
        /// 获取全部运输框架数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetFrameData")]
        
        public IActionResult GetFrameData()
        {
            return _shippingFrameDlagram.GetFrameData();
        }
        /// <summary>
        /// 获取单条运输框架数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Get")]

        public IActionResult Get(int id)
        {
            return _shippingFrameDlagram.Get(id);
        }
        /// <summary>
        /// 保存单条运输框架数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Save")]

        public IActionResult Save(SopBaseDTO input)
        {
            return _shippingFrameDlagram.Save(input);
        }
        /// <summary>
        /// 修改单条运输框架数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Modify")]

        public IActionResult Modify(SopBaseDTO input)
        {
            return _shippingFrameDlagram.Modify(input);
        }
        /// <summary>
        /// 删除运输框架数据
        /// </summary>
        /// <param name="input">筛选条件和排序</param>
        /// <returns></returns>
        [HttpPost]
        [Route("DELETE")]
        public IActionResult DELETE(IdxsPublicInput input)
        {
            return _shippingFrameDlagram.DELETE(input);
        }

        /// <summary>
        /// 获取对应关系
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetMappingRelationship")]
        public IActionResult GetMappingRelationship()
        {
            return _shippingFrameDlagram.GetMappingRelationship();
        }
        /// <summary>
        /// 联合查询
        /// </summary>
        /// <param name="input">筛选条件和排序</param>
        /// <returns></returns>
        [HttpPost]
        [Route("JointQuery")]
        public IActionResult JointQuery(ArrayInput input)
        {
            return _shippingFrameDlagram.JointQuery(input);
        }
        
    }
}
