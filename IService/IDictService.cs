using Microsoft.AspNetCore.Mvc;
using MstSopService.DTO;

namespace MstSopService.IService
{
    public interface IDictService
    {
        IActionResult GetDict(string code);
        IActionResult GetDicts(DictsInput input);
        IActionResult GetDictsHierarchy(DictsInput input);
        
    }
}
