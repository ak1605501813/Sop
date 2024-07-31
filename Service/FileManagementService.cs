using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MstAuth.Extensions;
using MstCore;
using MstCore.Service;
using MstDB;
using MstSopService.Caches;
using MstSopService.DTO;
using MstSopService.Entity;
using MstSopService.IService;
using MstSopService.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MstSopService.Service
{
    public class FileManagementService : IFileManagementService
    {
        PrincipalUser currentUser;
        IDatabase DB;
        IGetPermissionUtil _util;
        public FileManagementService(IHttpContextAccessor httpContextAccessor, MstCacheService mstCacheService, IGetPermissionUtil util)
        {
            currentUser = httpContextAccessor.CurrentUser();
            DB = Database.Instance(mstCacheService) as SugarRepository;
            _util=util;
        }
        public IActionResult GetArchitectureData()
        {
            try
            {
                var sqlWhererole = _util.GetPermissionUtilCommonCnopay();
                //只能给登录用户看到公开或者自己创建的私有文件信息
                var filesShareData = DB.SqlSugarClient().Queryable<FilesShareDTO>().Where(sqlWhererole.Sql, sqlWhererole.ParamsDict).Where(x=>x.IsPrivate==false || x.Createuser==currentUser.Userid).Select(x=>new ArchitectureDataOut 
                {
                    Id=x.Id,
                    Pid=x.Pid,
                    FileName=x.FileName,
                    FileNameen=x.FileNameen,
                    Type=x.Type,
                    Extended=x.Extended,
                    IsPrivate=x.IsPrivate,
                    FilePath=x.FilePath,
                    Remark=x.Remark
                }).ToList();
                if (filesShareData.Count() > 0)
                {
                    var tree = new List<ArchitectureDataOut>();
                    return MstResult.Success(ToTreeTool.BulidTreeByFilesShareDTO(filesShareData, tree, 0));
                }
                return MstResult.Success(filesShareData);
            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult Save(List<FilesShare> input)
        {
            try
            {
                foreach (var item in input) 
                {
                    item.Createuser = currentUser.Userid;
                    item.Createdate = DateTime.Now;
                    item.Companyid = currentUser.Ccode;
                    item.Departmentid = currentUser.subCcode;
                }
                DB.SqlSugarClient().Insertable<FilesShare>(input).ExecuteCommand();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult FilesShareRename(FilesShareRenameInput input) 
        {
            try
            {
                DB.SqlSugarClient().Updateable<FilesShare>().SetColumns(x => x.FileName == input.FileName).Where(x => x.Id == input.Id).ExecuteCommand();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult FilesShareDelete(IdxsPublicInput input) 
        {
            try
            {
                foreach (var id in input.Idxs)
                {
                    var filesShare = DB.SqlSugarClient().Queryable<FilesShare>().ToChildList(x => x.Pid, id);
                    if (filesShare.Count()>0) 
                    {
                        var fids=filesShare.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<FilesShare>().Where(x => fids.Contains(x.Id)).ExecuteCommand();
                    }
                    
                }
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult FilesShareCopy(FilesShareCopyInput input) 
        {
            try 
            {
                DB.SqlSugarClient().BeginTran();
                //var parentLevel = DB.SqlSugarClient().Queryable<FilesShare>().Where(x=>x.Id==id).First();
                var filesShare = DB.SqlSugarClient().Queryable<FilesShare>().ToChildList(x => x.Pid, input.Id);
                var parentLevel= filesShare.Where(x => x.Id == input.Id).FirstOrDefault();
                if (parentLevel!=null) 
                {
                    parentLevel.Id = 0;
                    int parid = DB.SqlSugarClient().Insertable<FilesShare>(parentLevel).ExecuteReturnIdentity();
                    var childSubsets = filesShare.Where(x => x.Pid == input.Id).ToList();
                    if (childSubsets.Count() > 0)
                    {
                        InsertSubsets(filesShare, childSubsets, parid);
                    }
                }
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }

        private void InsertSubsets(List<FilesShare> filesShare, List<FilesShare> subsets, int parentId)
        {
            foreach (var item in subsets)
            {
                item.Pid = parentId; // 设置子集的父级 ID
                var flagId = item.Id;
                item.Id = 0;
                int childId = DB.SqlSugarClient().Insertable<FilesShare>(item).ExecuteReturnIdentity();
                // 如果当前子集还有子集，则递归处理
                var childSubsets = filesShare.Where(x => x.Pid == flagId).ToList();
                if (childSubsets.Count > 0)
                {
                    InsertSubsets(filesShare, childSubsets, childId);
                }
            }
        }
        public IActionResult FilesShareMoveTo(FilesShareMoveToInput input) 
        {
            try
            {
                DB.SqlSugarClient().Updateable<FilesShare>().SetColumns(x => x.Pid == input.Pid).Where(x => x.Id == input.Id).ExecuteCommand();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult ChangeFilePermissions(ChangeFilePermissionsInput input) 
        {
            try
            {
                var shares = DB.SqlSugarClient().Queryable<FilesShare>().ToChildList(x => x.Pid, input.Id);
                if (shares.Count() > 0)
                {
                    var pid=shares.Where(x=>x.Id==input.Id).FirstOrDefault().Pid;
                    if (pid!=0) 
                    {
                        return MstResult.Error("操作失败");
                    }
                    var ids = shares.Select(x => x.Id).ToList();
                    DB.SqlSugarClient().Updateable<FilesShare>().SetColumns(x => x.IsPrivate == input.IsPrivate).Where(x=>ids.Contains(x.Id)).ExecuteCommand();
                }
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
