using Microsoft.AspNetCore.Mvc;
using MstSopService.DTO;
using MstSopService.DTOs;

namespace MstSopService.IService
{
    public interface IShippingFrameDlagramService
    {
        IActionResult GetFrameData();
        IActionResult Get(int id);
        IActionResult Save(SopBaseDTO input);
        IActionResult Modify(SopBaseDTO input);
        IActionResult DELETE(IdxsPublicInput input);
        IActionResult GetMappingRelationship();
        IActionResult JointQuery(ArrayInput input);

    }
}
