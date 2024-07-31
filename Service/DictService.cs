using Microsoft.AspNetCore.Mvc;
using MstCore;
using MstDB;
using MstSopService.Caches;
using MstSopService.DTO;
using MstSopService.Entity;
using MstSopService.IService;
using MstSopService.Tools;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using static Community.CsharpSqlite.Sqlite3;

namespace MstSopService.Service
{
    public class DictService: IDictService
    {
        IDatabase DB;
        HttpTool _httpTool;

        public DictService(MstCacheService mstCacheService, HttpTool httpTool)
        {
            DB = Database.Instance(mstCacheService) as SugarRepository;
            _httpTool= httpTool;
        }
        public IActionResult GetDict(string code)
        {
            try
            {
                var infos = DB.SqlSugarClient().Queryable<Dictinfo>().Where(x => x.Dictid == code).ToList();
                return MstResult.Success(infos);
            }
            catch (Exception ex)
            {
                throw new Exception($"400|GetDict Erro {ex}");
            }
        }
        public IActionResult GetDicts(DictsInput input) 
        {
            try
            {
                var infos = DB.SqlSugarClient().Queryable<Dictinfo>().Where(x => input.Codes.Contains(x.Dictid)).Select(x=>new DictsSpare 
                {
                    Idx = x.Idx,
                    Dictid=x.Dictid,
                    Code=x.Code,
                    Cname=x.Cname,  
                    Ename=x.Ename
                }).ToList();
                if (input.Codes.Contains("InPod")) 
                {
                    var shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21446).ToList();
                    if (shippingData.Count() > 0)
                    {
                        var sssDicts = shippingData.Map(it => new DictsSpare()
                        {
                            Idx = it.Idx,
                            Dictid = "InPod",
                            Code = it.Idx.ToString(),
                            Cname = it.SopName,
                            Ename = it.SopNameen
                        });
                        infos.AddRange(sssDicts);
                    }
                }
                List<DictsOut> dictList = new List<DictsOut>();
                foreach (var code in input.Codes) 
                {
                    var dicts = infos.Where(x => x.Dictid == code).ToList();
                    var dict = new DictsOut
                    {
                        Code = code,
                        Dictinfos = dicts
                    };
                    dictList.Add(dict);
                }
                return MstResult.Success(dictList);
            }
            catch (Exception ex)
            {
                throw new Exception($"400|GetDicts Erro {ex}");
            }
        }
        public IActionResult GetDictsHierarchy(DictsInput input)
        {
            try
            {
                bool isAir=false;
                if (input.OwerType == "10") 
                {
                    isAir = true;
                }
                var infos = DB.SqlSugarClient().Queryable<Dictinfo>().Where(x => input.Codes.Contains(x.Dictid)).Select(x => new DictsSpare
                {
                    Idx = x.Idx,
                    Dictid = x.Dictid,
                    Code = x.Code,
                    Cname = x.Cname,
                    Ename = x.Ename
                }).ToList();
                if (input.Codes.Contains("Ship"))//船名  只有海运有 
                {
                    var shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21477).ToList();
                    if (shippingData.Count()>0) 
                    {
                        var sfdDicts = shippingData.Map(it => new DictsSpare()
                        {
                            Idx = it.Idx,
                            Dictid = "Ship",
                            Code = it.Idx.ToString(),
                            Cname = it.SopName,
                            Ename = it.SopNameen
                        });
                        infos.AddRange(sfdDicts);
                    }
                }
                if (input.Codes.Contains("POL"))//需要区分空海运
                {
                    List<SopBase> shippingData = new List<SopBase>();
                    if (isAir)
                    {
                        shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21545).ToList();
                    }
                    else
                    {
                        shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21453).ToList();
                    }
                    if (shippingData.Count() > 0)
                    {
                        var sfdDicts = shippingData.Map(it => new DictsSpare()
                        {
                            Idx = it.Idx,
                            Dictid = "POL",
                            Code = it.Idx.ToString(),
                            Cname = it.SopName,
                            Ename = it.SopNameen
                        });
                        infos.AddRange(sfdDicts);
                    }
                }
                if (input.Codes.Contains("TypeOfGoods"))//需要区分空海运
                {
                    List<SopBase> shippingData = new List<SopBase>();
                    if (isAir)
                    {
                        shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21552).ToList();
                    }
                    else
                    {
                        shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21486).ToList();
                    }
                    if (shippingData.Count() > 0)
                    {
                        var sfdDicts = shippingData.Map(it => new DictsSpare()
                        {
                            Idx = it.Idx,
                            Dictid = "TypeOfGoods",
                            Code = it.Idx.ToString(),
                            Cname = it.SopName,
                            Ename = it.SopNameen
                        });
                        infos.AddRange(sfdDicts);
                    }
                }
                List<SopOrderAttribute> orderAttrs= new List<SopOrderAttribute>();
                if (isAir)
                {
                    var pl = DB.SqlSugarClient().Queryable<SopOrderAttribute>().Where(x=>x.AttrNameen== "Air").First();
                    orderAttrs = DB.SqlSugarClient().Queryable<SopOrderAttribute>().ToChildList(it => it.Pid, pl.Id);
                }
                else
                {
                    var pl = DB.SqlSugarClient().Queryable<SopOrderAttribute>().Where(x => x.AttrNameen == "Ocean").First();
                    orderAttrs = DB.SqlSugarClient().Queryable<SopOrderAttribute>().ToChildList(it => it.Pid, pl.Id);
                }
                List<DictsOut> dictList = new List<DictsOut>();
                var hasDictids = orderAttrs.Where(x => !string.IsNullOrEmpty(x.Dictid)).ToList();
                foreach (var code in input.Codes)
                {
                    var dicts = infos.Where(x => x.Dictid == code).ToList();
                    /*var item = orderPars.Where(x => x.Dictid.Contains(code)).FirstOrDefault();*/
                    var item = hasDictids.Where(x => x.Dictid.Contains(code)).FirstOrDefault();
                    var sopBaseIdxs = new List<int>();
                    //找上级数据
                    if (item.Pid == 0)
                    {
                        sopBaseIdxs.Add(item.Id);
                    }
                    else
                    {
                        sopBaseIdxs.Add(item.Id);
                        var flag = item;
                        var any = true;
                        do
                        {
                            var parentlevel = orderAttrs.Where(x => x.Id == flag.Pid).First();
                            sopBaseIdxs.Add(parentlevel.Id);
                            flag = parentlevel;
                        } while (any && flag.Pid != 0);
                    }
                    sopBaseIdxs.Reverse();
                    string attrId=string.Join(",", sopBaseIdxs);
                    if (dicts.Count()>0) 
                    {
                        foreach (var item1 in dicts)
                        {
                            item1.HierarchicalFields = $"{attrId},{item1.Idx}";
                        }
                    }
                    
                    var dict = new DictsOut
                    {
                        Code = code,
                        Dictinfos = dicts
                    };
                    dictList.Add(dict);
                }
                return MstResult.Success(dictList);
            }
            catch (Exception ex)
            {
                throw new Exception($"400|GetDicts Erro {ex}");
            }
        }
        
    }
}
