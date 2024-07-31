using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MstCore;
using MstDBComman.Models;
using MstSopService.DTO;
using MstSopService.IService;
using System;
using System.IO;
using System.Xml.Linq;

namespace MstSopService.Controllers
{
    /// <summary>
    /// 操作SOPAPI
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OperationSOPController : ControllerBase
    {
        IOperationSOPService _operationsService;
        public OperationSOPController(IOperationSOPService operationsService)
        {
            _operationsService=operationsService;
        }
        /// <summary>
        /// 获取SOP数据
        /// </summary>
        /// <param name="model">筛选条件和排序</param>
        /// <returns></returns>
        [HttpPost]
        [Route("ObtainSOPData")]
        public IActionResult ObtainSOPData([FromBody] QueryDescriptor model)
        {
            return MstResult.Success(_operationsService.ObtainSOPData(model));
        }
        /// <summary>
        /// 保存SOP数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Save")]
        public IActionResult Save(SopOrderDetailsDTO input)
        {
            return MstResult.Success(_operationsService.Save(input));
        }
        /// <summary>
        /// 修改SOP数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Modify")]

        public IActionResult Modify(SopOrderDetailsDTO input)
        {
            return _operationsService.Modify(input);
        }
        /// <summary>
        /// 删除SOP数据
        /// </summary>
        /// <param name="input">筛选条件和排序</param>
        /// <returns></returns>
        [HttpPost]
        [Route("DELETE")]
        public IActionResult DELETE(IdxsPublicInput input)
        {
            return _operationsService.DELETE(input);
        }
        /// <summary>
        /// 获取SOP数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Get")]

        public IActionResult Get(int id)
        {
            return _operationsService.Get(id);
        }
        /// <summary>
        /// 获取框架图对应附件信息
        /// </summary>
        /// <param name="attrId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("ObtainCorrespondingAttachments")]

        public IActionResult ObtainCorrespondingAttachments(string attrId)
        {
            return _operationsService.ObtainCorrespondingAttachments(attrId);
        }
        /// <summary>
        /// 删除内容版本
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteContentVersion")]

        public IActionResult DeleteContentVersion(IdPublicInput input)
        {
            return _operationsService.DeleteContentVersion(input);
        }
        /// <summary>
        /// 上传内容版本
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("UploadContentVersion")]

        public IActionResult UploadContentVersion(SopOrderContentTextDTO input)
        {
            return _operationsService.UploadContentVersion(input);
        }

        /// <summary>
        /// 撤销提交
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("RevokeSubmit")]
        public IActionResult RevokeSubmit(IdPublicInput input)
        {
            return _operationsService.RevokeSubmit(input);
        }

        /// <summary>
        /// 预览SOP数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Preview")]
        public IActionResult Preview(SopOrderDetailsDTO input)
        {
            string fileName = _operationsService.Preview(input);
            byte[] bt = null;
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] byteArray = new byte[fs.Length];
                fs.Read(byteArray, 0, byteArray.Length);
                bt = byteArray;
            }
            return File(bt, "application/octet-stream", "preview.docx");
        }

        /// <summary>
        /// 审批结果
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ApprovalResults")]
        public IActionResult ApprovalResults(SopOrderApproverRecordDTO input)
        {
            return _operationsService.ApprovalResults(input);
        }
        /// <summary>
        /// 发布
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Publish")]
        public IActionResult Publish(PublishInput input)
        {
            return _operationsService.Publish(input);
        }
        /// <summary>
        /// 获取对应人员信息
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("DeptOrPersonnel")]
        public IActionResult DeptOrPersonnel()
        {
            return _operationsService.DeptOrPersonnel();
        }

        /// <summary>
        /// 获取对应人员信息(运价人员)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("DeptOrFreightRatePersonnel")]
        public IActionResult DeptOrFreightRatePersonnel()
        {
            return _operationsService.DeptOrFreightRatePersonnel();
        }
        /// <summary>
        /// 获取目的国数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ObtainDestinationCountryData")]
        public IActionResult ObtainDestinationCountryData(string Code)
        {
            return MstResult.Success(_operationsService.ObtainDestinationCountryData(Code));
        }
        /// <summary>
        /// 重新生成版本
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("RegenerateVersion")]
        public IActionResult RegenerateVersion(int Id)
        {
            return MstResult.Success(_operationsService.RegenerateVersion(Id));
        }

        /// <summary>
        /// 操作人员重新保存SOP数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("OperatorsSave")]
        public IActionResult OperatorsSave(SopOrderDetailsDTO input)
        {
            var id = _operationsService.OperatorsSave(input);
            if (id > 0)
            {
                return MstResult.Success(id);
            }
            else 
            {
                return MstResult.Error("该单号已存在审批中的记录，请勿重复该操作");
            }

            
        }
        
        /// <summary>
        /// 操作人员修改SOP数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("OperatorsModify")]

        public IActionResult OperatorsModify(SopOrderDetailsDTO input)
        {
            return _operationsService.OperatorsModify(input);
        }

        /// <summary>
        /// 操作人员修改审批结果
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("OperatorsApprovalResults")]
        public IActionResult OperatorsApprovalResults(SopOrderApproverRecordDTO input)
        {
            return _operationsService.OperatorsApprovalResults(input);
        }
        /// <summary>
        /// 复制数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CopyOrderData")]
        public IActionResult CopyOrderData(IdPublicInput input)
        {
            return _operationsService.CopyOrderData(input);
        }
        /// <summary>
        /// 获取海运进口下拉数据
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ObtainDropdownDataForSeaFreightImports")]
        public IActionResult ObtainDropdownDataForSeaFreightImports()
        {
            return _operationsService.ObtainDropdownDataForSeaFreightImports();
        }
        /// <summary>
        /// Test
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Test")]
        [AllowAnonymous]
        public IActionResult Test()
        {

            return _operationsService.Test();
        }
    }
}
