using Microsoft.AspNetCore.Mvc;
using MstSopService.DTO;
using MstSopService.Entity;
using System.Collections.Generic;

namespace MstSopService.IService
{
    public interface IFileManagementService
    {
        IActionResult GetArchitectureData();
        IActionResult Save(List<FilesShare> input);
        IActionResult FilesShareRename(FilesShareRenameInput input);
        IActionResult FilesShareDelete(IdxsPublicInput input);
        IActionResult FilesShareCopy(FilesShareCopyInput input);
        IActionResult FilesShareMoveTo(FilesShareMoveToInput input);
        IActionResult ChangeFilePermissions(ChangeFilePermissionsInput input);
    }
}
