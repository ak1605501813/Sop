using DocumentFormat.OpenXml.EMMA;
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
using NPoco.RowMappers;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MstSopService.Service
{
    public class ShippingFrameDlagramService : IShippingFrameDlagramService
    {
        PrincipalUser currentUser;
        IDatabase DB;
        IGetPermissionUtil _util;
        HttpTool _httpTool;
        public ShippingFrameDlagramService(IHttpContextAccessor httpContextAccessor, MstCacheService mstCacheService,
            IGetPermissionUtil util, HttpTool httpTool)
        {
            currentUser = httpContextAccessor.CurrentUser();
            DB = Database.Instance(mstCacheService) as SugarRepository;
            _util= util;
            _httpTool= httpTool;
        }
        public IActionResult GetFrameData()
        {
            try
            {
                var sqlWhererole = _util.GetPermissionUtilCommonCnopay();
                var any = sqlWhererole.Sql.Contains("idx");
                var sopBases = new List<FrameDataOut>();
                if (any)//判断是否配置了权限
                {
                    var nodes = DB.SqlSugarClient().Queryable<SopBaseDTO>().Where(sqlWhererole.Sql, sqlWhererole.ParamsDict).Select(x => new FrameDataOut()
                    {
                        Departmentid=x.Departmentid,
                        Idx = x.Idx,
                        SopName = x.SopName,
                        SopNameen = x.SopNameen,
                        Pid = x.Pid,
                        Orderid = x.Orderid
                    }).ToList();
                    if (nodes.Count()>0) 
                    {
                        
                        var depids = nodes.Select(x => x.Departmentid).Distinct().ToList();
                        var sopBaseData = DB.SqlSugarClient().Queryable<SopBaseDTO>().Where(x => depids.Contains(x.Departmentid)).Select(x => new FrameDataOut()
                        {
                            Departmentid = x.Departmentid,
                            Idx = x.Idx,
                            SopName = x.SopName,
                            SopNameen=x.SopNameen,
                            Pid = x.Pid,
                            Orderid = x.Orderid
                        }).ToList();
                        sopBases=ObtainCorrespondingData(nodes, sopBaseData);
                    }
                }
                else 
                {
                    sopBases = DB.SqlSugarClient().Queryable<SopBaseDTO>().Where(sqlWhererole.Sql, sqlWhererole.ParamsDict).Select(x => new FrameDataOut()
                    {
                        Departmentid = x.Departmentid,
                        Idx = x.Idx,
                        SopName = x.SopName,
                        SopNameen = x.SopNameen,
                        Pid = x.Pid,
                        Orderid = x.Orderid
                    }).ToList();
                }
                
                if (sopBases.Count()>0) 
                {
                    var tree = new List<FrameDataOut>();
                    var trees = ToTreeTool.BulidTreeBySopBaseDTO(sopBases, tree, 0);
                    foreach (var item in trees)
                    {
                       List<int> ids= new List<int>();
                       GetTreeIds(item, ref ids);
                       item.NumberOfAttachments=DB.SqlSugarClient().Queryable<FileManage>().Where(x => ids.Contains(x.RelationId.Value)&&x.RelationTableName== "sop_base").Count();
                    }
                    return MstResult.Success(trees);
                }
                return MstResult.Success(sopBases);
            }
            catch (Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }
        private void GetTreeIds(FrameDataOut node, ref List<int> ids)
        {
            ids.Add(node.Idx);
            if (node.Subsets != null)
            {
                foreach (var item in node.Subsets) 
                {
                    GetTreeIds(item, ref ids);
                }
            }
        }

        List<int> sopids = new List<int>();
        List<FrameDataOut> sopBases = new List<FrameDataOut>();
        public List<FrameDataOut> ObtainCorrespondingData(List<FrameDataOut> nodes, List<FrameDataOut> sopBaseData) 
        {
            foreach (var node in nodes)
            {
                //找上级数据
                if (node.Pid == 0)
                {
                    sopBases.Add(node);
                    sopids.Add(node.Idx);
                }
                else 
                {
                    sopBases.Add(node);
                    sopids.Add(node.Idx);
                    var flag = node;
                    var any = true;
                    do 
                    {
                        var parentlevel = sopBaseData.Where(x => x.Idx == flag.Pid).First();
                        if (!sopids.Contains(parentlevel.Idx))
                        {
                            sopBases.Add(parentlevel);
                            sopids.Add(parentlevel.Idx);
                        }
                        else 
                        {
                            any = false;
                        }
                        flag = parentlevel;
                    } while (any&&flag.Pid!=0);
                }
                //找下级数据
                var subsets = sopBaseData.Where(x => x.Pid == node.Idx).ToList();
                if (subsets.Count()>0) 
                {
                    ObtainSubsetCorrespondingData(subsets, sopBaseData);
                }
            }
            return sopBases;
        }

        public void ObtainSubsetCorrespondingData(List<FrameDataOut> nodes, List<FrameDataOut> sopBaseData) 
        {
            foreach (var item in nodes) 
            {
                if (!sopids.Contains(item.Idx))
                {
                    sopBases.Add(item);
                    sopids.Add(item.Idx);
                }
                var subsets = sopBaseData.Where(x => x.Pid == item.Idx).ToList();
                if (subsets.Count()>0) 
                {
                    ObtainSubsetCorrespondingData(subsets, sopBaseData);
                }
            }
        }
        public IActionResult Get(int id) 
        {
            try 
            {
                var sopBase = DB.SqlSugarClient().Queryable<SopBaseDTO>().Where(x=>x.Idx==id).First();
                var fils = DB.SqlSugarClient().Queryable<FileManage>().Where(x => x.RelationId.Value==id && x.RelationTableName == "sop_base").ToList();

                sopBase.FileManages = fils;
                /*if (sopBase.Type=="20")//10编辑框   20联系人 
                {
                    sopBase.Contacts = DB.SqlSugarClient().Queryable<SopContactList>().Where(x => x.SopBaseId.Value == id).ToList();
                }*/
                sopBase.Contacts = DB.SqlSugarClient().Queryable<SopContactList>().Where(x => x.SopBaseId.Value == id).ToList();
                return MstResult.Success(sopBase);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult Save(SopBaseDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                input.Createuser = currentUser.Userid;
                input.Createdate = DateTime.Now;
                input.Companyid = currentUser.Ccode;
                input.Departmentid = currentUser.subCcode;
                int mainid=DB.SqlSugarClient().Insertable<SopBaseDTO>(input).ExecuteReturnIdentity();
                if (input.FileManages!=null) 
                {
                    foreach (var item in input.FileManages) 
                    {
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = "sop_base";
                        item.RelationId = mainid;
                    }
                    DB.SqlSugarClient().Insertable<FileManage>(input.FileManages).ExecuteCommand();
                }
                if (input.Type=="20"&&input.Contacts!=null)
                {
                    foreach (var item in input.Contacts)
                    {
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.SopBaseId = mainid;
                    }
                    DB.SqlSugarClient().Insertable<SopContactList>(input.Contacts).ExecuteCommand();
                }
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success(mainid);
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
        public IActionResult Modify(SopBaseDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                input.Modifier = currentUser.Userid;
                input.Modifydate= DateTime.Now;
                if (input.FileManages == null)
                {
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => input.Idx==x.RelationId.Value && x.RelationTableName == "sop_base").ExecuteCommand();
                }
                else 
                {
                    var addFileData=input.FileManages.Where(x => x.Idx == 0).ToList();
                    var fileData = input.FileManages.Where(x => x.Idx != 0).ToList();
                    if (fileData.Count() > 0)
                    {
                        var fidxs=fileData.Select(x => x.Idx).ToList();
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => input.Idx == x.RelationId.Value && !fidxs.Contains(x.Idx)&& x.RelationTableName == "sop_base").ExecuteCommand();
                    }
                    else 
                    {
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => input.Idx == x.RelationId.Value && x.RelationTableName == "sop_base").ExecuteCommand();
                    }
                    if (addFileData.Count()>0) 
                    {
                        foreach (var item in addFileData) 
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = "sop_base";
                            item.RelationId = input.Idx;
                        }
                        DB.SqlSugarClient().Insertable<FileManage>(addFileData).ExecuteCommand();
                    }
                }
                if (input.Contacts == null)
                {
                    //DB.SqlSugarClient().Deleteable<SopContactList>().Where(x => input.Idx == x.SopBaseId.Value).ExecuteCommand();
                }
                else
                {
                    var addContactsData = input.Contacts.Where(x => x.Id == 0).ToList();
                    var contactsData = input.Contacts.Where(x => x.Id != 0).ToList();
                    if (contactsData.Count() > 0)
                    {
                        var cidxs = contactsData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopContactList>().Where(x => input.Idx == x.SopBaseId.Value && !cidxs.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in contactsData) 
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = DateTime.Now;
                        }
                        DB.SqlSugarClient().Updateable<SopContactList>(contactsData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser,x.SopBaseId }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopContactList>().Where(x => input.Idx == x.SopBaseId.Value).ExecuteCommand();
                    }
                    if (addContactsData.Count() > 0)
                    {
                        foreach (var item in addContactsData)
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.SopBaseId = input.Idx;
                        }
                        DB.SqlSugarClient().Insertable<SopContactList>(addContactsData).ExecuteCommand();
                    }
                }
                DB.SqlSugarClient().Updateable<SopBaseDTO>(input).IgnoreColumns(x=>new {x.Companyid,x.Departmentid,x.Createdate,x.Createuser }).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex) 
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
        public IActionResult DELETE(IdxsPublicInput input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                if (input.Idxs.Length>0) 
                {
                    foreach (var id in input.Idxs)
                    {
                        List<int> ids = new List<int>() { id };
                        var sopBases = DB.SqlSugarClient().Queryable<SopBaseDTO>().ToChildList( x => x.Pid, id);
                        if (sopBases.Count() > 0)
                        {
                            var idxs = sopBases.Select(x => x.Idx).ToList();
                            ids.AddRange(idxs);
                        }
                        DB.SqlSugarClient().Deleteable<SopContactList>().Where(x => ids.Contains(x.SopBaseId.Value)).ExecuteCommand();
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => ids.Contains(x.RelationId.Value) && x.RelationTableName == "sop_base").ExecuteCommand();
                        DB.SqlSugarClient().Deleteable<SopBase>().Where(x => ids.Contains(x.Idx)).ExecuteCommand();
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
        public IActionResult GetMappingRelationship() 
        {
            try
            {
                var filesShares = DB.SqlSugarClient().Queryable<SopOrderAttributeDTO>().ToList();
                if (filesShares.Count() > 0)
                {
                    var tree = new List<SopOrderAttributeDTO>();
                    var dictinfos = new List<Dictinfo>();
                    
                    var dicts=filesShares.Where(x => !string.IsNullOrEmpty(x.Dictid)).Select(x => x.Dictid).ToList();
                    if (dicts.Count()>0) 
                    {
                        dictinfos=DB.SqlSugarClient().Queryable<Dictinfo>().Where(x=>dicts.Contains(x.Dictid)).ToList();
                        if (dicts.Contains("OceanShip"))
                        {
                            var shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21477).ToList();
                            if (shippingData.Count() > 0)
                            {
                                var shipDicts = shippingData.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "OceanShip",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(shipDicts);
                            }
                        }
                        if (dicts.Contains("OceanPOL"))
                        {
                            var oceanPOL = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21453).ToList();
                            if (oceanPOL.Count() > 0)
                            {
                                var shipDicts = oceanPOL.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "OceanPOL",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(shipDicts);
                            }
                        }

                        if (dicts.Contains("AirPOL"))
                        {
                            var airPOL = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21545).ToList();
                            if (airPOL.Count() > 0)
                            {
                                var shipDicts = airPOL.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "AirPOL",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(shipDicts);
                            }
                        }

                        if (dicts.Contains("OceanTypeOfGoods"))
                        {
                            var oceanTypeOfGoods = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21486).ToList();
                            if (oceanTypeOfGoods.Count() > 0)
                            {
                                var shipDicts = oceanTypeOfGoods.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "OceanTypeOfGoods",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(shipDicts);
                            }
                        }
                        if (dicts.Contains("AirTypeOfGoods"))
                        {
                            var airTypeOfGoods = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21552).ToList();
                            if (airTypeOfGoods.Count() > 0)
                            {
                                var shipDicts = airTypeOfGoods.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "AirTypeOfGoods",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(shipDicts);
                            }
                        }
                        
                        if (dicts.Contains("OceanDestination")) 
                        {
                            //子集数据
                            var childsData = DB.SqlSugarClient().Queryable<SopBase>().ToChildList(it => it.Pid, 21457);
                            if (childsData.Count() > 0)
                            {
                                List<int> delSubscript = new List<int>();
                                foreach (var item in childsData)
                                {
                                    var flag = item;
                                    var any = true;
                                    do
                                    {
                                        var parentlevel = childsData.Where(x => x.Idx == flag.Pid).FirstOrDefault();
                                        if (parentlevel != null)
                                        {
                                            delSubscript.Add(parentlevel.Idx);
                                            flag = parentlevel;
                                        }
                                        else
                                        {
                                            any=false;
                                        }

                                    } while (any);
                                }
                                if (delSubscript.Count() > 0)
                                {
                                    delSubscript.Add(21457);
                                    childsData = childsData.Where(x => !delSubscript.Contains(x.Idx)).ToList();
                                }
                                var pods=childsData.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "OceanDestination",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(pods);
                            }

                        }
                        if (dicts.Contains("AirDestination"))
                        {
                            //子集数据
                            var childsData = DB.SqlSugarClient().Queryable<SopBase>().ToChildList(it => it.Pid, 21546);
                            if (childsData.Count() > 0)
                            {
                                List<int> delSubscript = new List<int>();
                                foreach (var item in childsData)
                                {
                                    var flag = item;
                                    var any = true;
                                    do
                                    {
                                        var parentlevel = childsData.Where(x => x.Idx == flag.Pid).FirstOrDefault();
                                        if (parentlevel != null)
                                        {
                                            delSubscript.Add(parentlevel.Idx);
                                            flag = parentlevel;
                                        }
                                        else
                                        {
                                            any = false;
                                        }

                                    } while (any);
                                }
                                if (delSubscript.Count() > 0)
                                {
                                    delSubscript.Add(21546);
                                    childsData = childsData.Where(x => !delSubscript.Contains(x.Idx)).ToList();
                                }
                                var pods = childsData.Map(it => new Dictinfo()
                                {
                                    Idx = it.Idx,
                                    Dictid = "AirDestination",
                                    Code = it.Idx.ToString(),
                                    Cname = it.SopName,
                                    Ename = it.SopNameen
                                });
                                dictinfos.AddRange(pods);
                            }

                        }
                    }
                    return MstResult.Success(ToTreeTool.BulidTreeByOrderAttributeDTO(filesShares, tree, dictinfos, 0));
                }
                return MstResult.Success(filesShares);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult JointQuery(ArrayInput input) 
        {
            try 
            {
                //获取框架图所有数据
                var sopbases=DB.SqlSugarClient().Queryable<SopBase>().Select(x => new FrameDataOut()
                {
                    Departmentid = x.Departmentid,
                    Idx = x.Idx,
                    SopName=x.SopName,
                    SopNameen=x.SopNameen,
                    Pid= x.Pid,
                    Orderid= x.Orderid,
                }).ToList();

                var filteredSopBases = sopbases.Where(x => input.Ids.Contains(x.Idx)).ToList();
                sopBases.AddRange(filteredSopBases);
                sopids.AddRange(filteredSopBases.Select(x=>x.Idx).ToList());
                var rootData = new List<FrameDataOut>();
                var retData = new List<FrameDataOut>();
                foreach (var sopbase in filteredSopBases) 
                {
                    //找下级数据
                    var subsets = sopbases.Where(x => x.Pid == sopbase.Idx).ToList();
                    if (subsets.Count() > 0)
                    {
                        ObtainSubsetCorrespondingData(subsets, sopbases);
                    }
                }
                //寻找上级
                foreach (var node in filteredSopBases) 
                {
                    var ids = filteredSopBases.Where(x => x.Idx != node.Idx).Select(x => x.Idx).ToList();
                    //找上级数据
                    if (node.Pid == 0)
                    {
                        rootData.Add(node);
                    }
                    else
                    {
                        var flag = node;
                        var any = true;
                        do
                        {
                            var parentlevel = sopBases.Where(x => x.Idx == flag.Pid).FirstOrDefault();
                            if (parentlevel!=null) 
                            {
                                if (ids.Contains(parentlevel.Idx))
                                {
                                    any=false;
                                }
                                else
                                {
                                    any = true;
                                }
                            }
                            flag = parentlevel;
                        } while (any && flag != null);
                        if (any) 
                        {
                            rootData.Add(node);
                        }
                    }
                }
                var rootIds = rootData.Select(x => x.Idx).ToList();
                foreach (var id in input.Ids) 
                {
                    if (rootIds.Contains(id)) 
                    {
                        var root = rootData.Where(x => x.Idx == id).FirstOrDefault();
                        var tree = new List<FrameDataOut>();
                        root.Subsets = ToTreeTool.BulidTreeBySopBaseDTO(sopBases, tree, id);
                        retData.Add(root);
                    }
                }
                return MstResult.Success(retData);
            }
            catch(Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
