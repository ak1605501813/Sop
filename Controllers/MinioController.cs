using Aspose.Words;
using Aspose.Words.Saving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Minio;
using MstCoreV3.Base;
using MstCoreV3.Minio;
using MstCoreV3.Pub;
using MstCoreV3.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace MstSopService.Controllers
{
    /// <summary>
    /// Minio文件操作
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MinioController : ControllerBase
    {
        MinioControllerBase minioControllerBase = new MinioControllerBase();

        /// <summary>
        /// 使用Minio上传文件，返回Minio地址
        /// </summary>
        /// <param name="ifc"></param>
        /// <returns></returns>
        [HttpPost("Upload")]
        public CommonResult Upload(IFormCollection ifc)
        {
            try
            {
                IFormFile filedata = ifc.Files[0];
                string filename = filedata.FileName;
                var size = filedata.Length;
                string localFileDir = $"\\{BucketName.FileBucket}";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    localFileDir = localFileDir.TrimStart('/').TrimStart('\\');
                    if (!Path.IsPathRooted(localFileDir))
                        localFileDir = Path.Combine(AppContext.BaseDirectory, localFileDir);
                }
                localFileDir = localFileDir.Replace("\\", "/");
                var uploadFileName = filename;
                string fileFullPath = Path.Combine(localFileDir, uploadFileName);
                if (!Directory.Exists(localFileDir))
                    Directory.CreateDirectory(localFileDir);
                FileUtil.Save(filedata, fileFullPath);
                string suffix=Path.GetExtension(filename);
                string prefix = filename.Substring(0, filename.IndexOf("."));
                string minioFileFullPath = BucketRootFolder.SopUploader + "/" +prefix + (DateTime.Now.ToString("yyyyMMddHHmmssfffffff")) + suffix;
                bool isSuc = MinioPub.UploadFile(fileFullPath, minioFileFullPath, BucketName.FileBucket).GetAwaiter().GetResult();
                if (isSuc)
                {
                    FileUtil.Delete(fileFullPath);
                    return CommonResult.Ok("上传成功", new
                    {
                        name = filename,
                        path = minioFileFullPath,
                        size=size,
                        suffix=suffix.Substring(1)

                    });
                }
                else
                {
                    return CommonResult.BadRequest("上传失败");
                }
            }
            catch (Exception ex)
            {
                return CommonResult.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 使用Minio上传文件，返回Minio地址,带封面图
        /// </summary>
        /// <param name="ifc"></param>
        /// <returns></returns>
        [HttpPost("UploadWithCover")]
        public CommonResult UploadWithCover(IFormCollection ifc)
        {
            try
            {
                IFormFile filedata = ifc.Files[0];
                string filename = filedata.FileName;

                string appRoot = AppContext.BaseDirectory;
                string temporaryFiles = Path.Combine(appRoot, "TemporaryFiles");
                if (!Directory.Exists(temporaryFiles))
                {
                    //创建文件夹
                    Directory.CreateDirectory(temporaryFiles);
                }
                // 保存上传的文件到服务器上的临时目录
                var temporaryName = Path.Combine(temporaryFiles, $"{System.DateTime.Now.ToString("yyyyMMddHHmmssfffffff")}.docx");

                using (var stream = new FileStream(temporaryName, FileMode.Create))
                {
                    filedata.CopyToAsync(stream);
                }

                // 将 Word 文档转换为图像
                Document doc = new Document(temporaryName);
                ImageSaveOptions options = new ImageSaveOptions(SaveFormat.Png);
                options.Resolution = 300; // 设置分辨率
                using (MemoryStream stream = new MemoryStream())
                {
                    doc.Save(stream, options);

                    // 选择第一页作为封面图
                    stream.Seek(0, SeekOrigin.Begin);
                    using (Image image = Image.FromStream(stream))
                    {
                        // 这里假设你想要生成一个100x100的缩略图作为封面图
                        using (Image thumbnail = image.GetThumbnailImage(600, 800, null, IntPtr.Zero))
                        {
                            // 保存封面图到文件系统
                            var thumbnailPath = Path.Combine(temporaryFiles, "thumbnail_" + filename + ".png");
                            thumbnail.Save(thumbnailPath, ImageFormat.Png);

                            // 在这里你可以使用 thumbnailPath 作为封面图路径，保存到数据库或者其他地方
                        }
                    }
                }
                return CommonResult.BadRequest("上传失败");
                /*IFormFile filedata = ifc.Files[0];
                string filename = filedata.FileName;
                var size = filedata.Length;
                string localFileDir = $"\\{BucketName.FileBucket}";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    localFileDir = localFileDir.TrimStart('/').TrimStart('\\');
                    if (!Path.IsPathRooted(localFileDir))
                        localFileDir = Path.Combine(AppContext.BaseDirectory, localFileDir);
                }
                localFileDir = localFileDir.Replace("\\", "/");
                var uploadFileName = filename;
                string fileFullPath = Path.Combine(localFileDir, uploadFileName);
                if (!Directory.Exists(localFileDir))
                    Directory.CreateDirectory(localFileDir);
                FileUtil.Save(filedata, fileFullPath);
                string suffix = Path.GetExtension(filename);
                string prefix = filename.Substring(0, filename.IndexOf("."));
                string minioFileFullPath = BucketRootFolder.SopUploader + "/" + prefix + (DateTime.Now.ToString("yyyyMMddHHmmssfffffff")) + suffix;
                bool isSuc = MinioPub.UploadFile(fileFullPath, minioFileFullPath, BucketName.FileBucket).GetAwaiter().GetResult();
                if (isSuc)
                {
                    FileUtil.Delete(fileFullPath);
                    return CommonResult.Ok("上传成功", new
                    {
                        name = filename,
                        path = minioFileFullPath,
                        size = size,
                        suffix = suffix.Substring(1)

                    });
                }
                else
                {
                    return CommonResult.BadRequest("上传失败");
                }*/
            }
            catch (Exception ex)
            {
                return CommonResult.BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// 使用Minio下载，返回文件流
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("Download")]
        [AllowAnonymous]
        public IActionResult Download(string path)
        {
            FilePath fp = new FilePath() { Path = path };
            return minioControllerBase.GetMinioFileStream(fp);
        }
    }
}
