using Microsoft.AspNetCore.Mvc;
using MstDBComman.Models;
using MstSopService.DTO;
using MstSopService.DTOs;
using System.Collections.Generic;

namespace MstSopService.IService
{
    public interface IOperationSOPService
    {
        object ObtainSOPData(QueryDescriptor model);
        int Save(SopOrderDetailsDTO input);
        IActionResult Modify(SopOrderDetailsDTO input);
        IActionResult DELETE(IdxsPublicInput input);
        IActionResult Get(int id);
        IActionResult ObtainCorrespondingAttachments(string attrId);
        IActionResult DeleteContentVersion(IdPublicInput input);
        IActionResult UploadContentVersion(SopOrderContentTextDTO input);
        IActionResult RevokeSubmit(IdPublicInput input);
        string Preview(SopOrderDetailsDTO input);
        IActionResult ApprovalResults(SopOrderApproverRecordDTO input);
        IActionResult Publish(PublishInput input);
        IActionResult DeptOrPersonnel();
        IActionResult DeptOrFreightRatePersonnel();
        List<DictsOut> ObtainDestinationCountryData(string Code);
        IActionResult RegenerateVersion(int Id);
        int OperatorsSave(SopOrderDetailsDTO input);
        IActionResult OperatorsModify(SopOrderDetailsDTO input);
        IActionResult OperatorsApprovalResults(SopOrderApproverRecordDTO input);
        IActionResult CopyOrderData(IdPublicInput input);
        IActionResult ObtainDropdownDataForSeaFreightImports();
        IActionResult Test();
    }
}
