using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MstSopService.DTO;
using MstSopService.Entity;
using MstSopService.IService;
using System.Collections.Generic;

namespace MstSopService.Controllers
{
    /// <summary>
    /// 文件管理API
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FileManagementController : ControllerBase
    {
        IFileManagementService _fileManagementService;
        public FileManagementController(IFileManagementService fileManagementService) 
        {
            _fileManagementService= fileManagementService;
        }
        /// <summary>
        /// 获取文件管理Tree架构
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("GetArchitectureData")]

        public IActionResult GetArchitectureData()
        {
            return _fileManagementService.GetArchitectureData();
        }
        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("Save")]
        public IActionResult Save(List<FilesShare> input)
        {
            return _fileManagementService.Save(input);
        }
        /// <summary>
        /// 重命名文件/文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("FilesShareRename")]
        public IActionResult FilesShareRename(FilesShareRenameInput input)
        {
            return _fileManagementService.FilesShareRename(input);
        }
        /// <summary>
        /// 删除文件/文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("FilesShareDelete")]
        public IActionResult FilesShareDelete(IdxsPublicInput input)
        {
            return _fileManagementService.FilesShareDelete(input);
        }
        /// <summary>
        /// Copy文件/文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("FilesShareCopy")]
        public IActionResult FilesShareCopy(FilesShareCopyInput input)
        {
            return _fileManagementService.FilesShareCopy(input);
        }
        /// <summary>
        /// Move to文件/文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("FilesShareMoveTo")]
        public IActionResult FilesShareMoveTo(FilesShareMoveToInput input)
        {
            return _fileManagementService.FilesShareMoveTo(input);
        }
        /// <summary>
        /// 修改文件夹权限
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ChangeFilePermissions")]
        public IActionResult ChangeFilePermissions(ChangeFilePermissionsInput input)
        {
            return _fileManagementService.ChangeFilePermissions(input);
        }
    }
}
