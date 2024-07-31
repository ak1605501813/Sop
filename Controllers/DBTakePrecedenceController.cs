using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MstDB;
using System.Linq;
using MstCore;

namespace MstSopService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DBTakePrecedenceController : ControllerBase
    {
        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="tblName">数据库表名</param>
        /// <param name="savePath">存放路径</param>
        /// <returns>返回Http状态码</returns>
        [HttpGet]
        [Route("CreateEntity")]
        public IActionResult CreateEntity(string tblName, string savePath)
        {
            string nameSpace = "MstSopService.Entity";
            var db = ((SugarRepository)MstDB.Database.Instance()).DbContext;
            foreach (var item in db.DbMaintenance.GetTableInfoList())
            {
                string entityName = StrUtil.ToCamelName(item.Name);
                db.MappingTables.Add(entityName, item.Name);
                foreach (var col in db.DbMaintenance.GetColumnInfosByTableName(item.Name))
                {
                    db.MappingColumns.Add(StrUtil.ToCamelName(col.DbColumnName), col.DbColumnName, entityName);
                }
            }
            db.DbFirst.IsCreateAttribute().Where(it => tblName.Split(",").Contains(it)).CreateClassFile(savePath, nameSpace);
            return Ok();
        }
    }
}
