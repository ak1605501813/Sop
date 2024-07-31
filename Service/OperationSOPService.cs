using Aspose.Words.Drawing;
using Aspose.Words.Fonts;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MstAuth.Extensions;
using MstCore;
using MstCore.Service;
using MstCoreV3.Minio;
using MstDB;
using MstDBComman.Models;
using MstSopService.Caches;
using MstSopService.DTO;
using MstSopService.Entity;
using MstSopService.IService;
using MstSopService.Tools;
using OfficeOpenXml;
using ServiceStack;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using A = DocumentFormat.OpenXml.Drawing;
using Color = DocumentFormat.OpenXml.Wordprocessing.Color;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Style = DocumentFormat.OpenXml.Wordprocessing.Style;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using DocumentFormat.OpenXml.EMMA;

namespace MstSopService.Service
{
    public class OperationSOPService: IOperationSOPService
    {
        PrincipalUser currentUser;
        IDatabase DB;
        ICodeNameConversionService _codeNameConversion;
        IGetPermissionUtil _util;
        HttpTool _httpTool;
        string lang = "";
        public OperationSOPService(IHttpContextAccessor httpContextAccessor, MstCacheService mstCacheService,
            ICodeNameConversionService codeNameConversion,IGetPermissionUtil util, HttpTool httpTool)
        {
            currentUser = httpContextAccessor.CurrentUser();
            DB = Database.Instance(mstCacheService) as SugarRepository;
            _codeNameConversion = codeNameConversion;
            _util= util;
            _httpTool= httpTool;
            lang = httpContextAccessor?.HttpContext?.User.FindFirst("lang")?.Value;
        }
        private string GetWhere(List<int> mainIds)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" and id not in (");
            foreach (var mainId in mainIds)
            {
                sb.Append($"{mainId},");
            }
            string sqlWhere = sb.ToString();
            sqlWhere = sqlWhere.Substring(0, sqlWhere.Length - 1) + ")";
            return sqlWhere;
        }
        public object ObtainSOPData(QueryDescriptor model)
        {
            var sqlWhererole = _util.GetPermissionUtilCommonCnopay();
            try
            {
                #region 搜索条件
                /*
                     
                
                if (model.Conditions != null && model.Conditions.Count > 0)
                {
                    model.Conditions = model.Conditions.Where(i => i.Value != null).ToList();//特殊处理
                    
                    model.Conditions = model.Conditions.Where(i => !keys.Contains(i.Key)).ToList();
                }
                
                 
                 */
                string[] keys = { "shipper_en", "shipper_ch" };
                List<QueryCondition> specialConditions = new List<QueryCondition>();
                int totalCount = 0;
                var isManager = model.Conditions.Where(i => i.Key == "isManager").FirstOrDefault();
                var sign = model.Conditions.Where(i => i.Key == "sign").FirstOrDefault();
                var status = model.Conditions.Where(i => i.Key == "status").FirstOrDefault();
                var queryTags = model.Conditions.Where(i => i.Key == "queryTags").FirstOrDefault();//查询操作人员修改未在审批中
                bool flag = false;
                if (isManager != null)
                {
                    flag = isManager.Value.ToString() == "1" ? true : false;
                }
                bool flag1 = false;
                if (sign != null)
                {
                    flag1 = sign.Value.ToString() == "1" ? true : false;
                }
                if (status != null) 
                {
                    if (flag&&status.Value.ToString() == "60") 
                    {
                        status.Value = "20";
                    }
                }
                if (model.Conditions != null && model.Conditions.Count > 0)
                {
                    specialConditions = model.Conditions.Where(i => keys.Contains(i.Key)).ToList();
                    model.Conditions = model.Conditions.Where(i => i.Value != null&&i.Key!= "isManager" && i.Key != "sign"&&i.Key!= "queryTags").ToList();//特殊处理
                    model.Conditions=model.Conditions.Where(i => !keys.Contains(i.Key)).ToList();
                }
                var special = "";
                if (specialConditions.Count() > 0)
                {
                    special = $"and ({VariableModelTool.MysqlStr(specialConditions).Substring(5)})";
                }
                #endregion
                //+ $" or visible_member like '%{currentUser.User}%' 
                var sql = $"1=1 " + special + VariableModelTool.MysqlStr(model.Conditions);
                if (queryTags != null)
                {
                    if (queryTags.Value.ToString() == "1") 
                    {
                        var primaryIds=DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.Flag == true && x.Status == "20").Select(x => x.PrimaryId).ToList();
                        if (primaryIds.Count()>0)
                        {
                            sql += GetWhere(primaryIds);
                        }
                        sql += " and status='50' ";
                    }
                }
                List<SopOrderListDTO> listSel = null;
                var retList = DB.SqlSugarClient().Queryable<SopOrderListDTO>().Where(sqlWhererole.Sql, sqlWhererole.ParamsDict).Where(sql).Where(x=>x.Flag== flag1);
                if (model.OrderBys.Count() > 0)
                {
                    var orderBys = DB.ParseOrderBy(model.OrderBys);
                    retList = retList.OrderBy(orderBys);
                }
                if (model.PageIndex > 0 && model.PageSize > 0)
                {
                    listSel = retList.ToPageList(model.PageIndex, model.PageSize, ref totalCount);
                }
                else
                {
                    listSel = retList.ToList();
                    totalCount = listSel.Count();
                }
                
                if (listSel.Count() > 0)
                {
                   
                    var cnData = _codeNameConversion.GetCodeNamesDatas();//code and Name
                    List<string> codes=new List<string>() { "2400", "2500" };
                    var dics = DB.SqlSugarClient().Queryable<Dictinfo>().Where(x => codes.Contains(x.Dictid)).ToList();
                    var ids = listSel.Select(x => x.Id).ToList();
                    var sopApprovers = DB.SqlSugarClient().Queryable<SopOrderApproverRecordDTO>().Where(x => ids.Contains(x.SopOrderId.Value)).ToList();
                    foreach (var item in listSel) 
                    {
                        if (flag) 
                        {
                            if (item.Status=="20")
                            {
                                item.Status = "60";
                            }
                        }
                        PublishInput publish = new PublishInput()
                        {
                            Id = item.Id,
                            IsPublish = item.IsPublish,
                            VisibleMember = item.VisibleMember,
                            PublishTime = item.PublishTime,
                            PublishMember = item.PublishMember,
                        };
                        if (!string.IsNullOrEmpty(publish.PublishMember))
                        {
                            publish.PublishMemberName = cnData.Where(x => x.UserId == publish.PublishMember).FirstOrDefault()?.NameEn;
                        }
                        item.PublishMsg = publish;
                        item.SopOrderApproverRecords = sopApprovers.Where(x => x.SopOrderId == item.Id).OrderBy(x=>x.ApprovalTime).ToList();
                        if (item.SopOrderApproverRecords != null)
                        {
                            foreach (var item1 in item.SopOrderApproverRecords)
                            {
                                if (!string.IsNullOrEmpty(item1.Approver))
                                {

                                    item1.ApproverName = cnData.Where(x => x.UserId == item1.Approver).FirstOrDefault()?.NameEn;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(item.Createuser)) 
                        {
                            item.CreateuserName = cnData.Where(x => x.UserId == item.Createuser).FirstOrDefault()?.NameEn;
                        }
                        if (!string.IsNullOrEmpty(item.Modifier))
                        {
                            item.ModifierName = cnData.Where(x => x.UserId == item.Modifier).FirstOrDefault()?.NameEn;
                        }
                        if (!string.IsNullOrEmpty(item.OwerType))
                        {
                            item.OwerTypeName = dics.Where(x =>x.Dictid=="2400" &&x.Code == item.OwerType).FirstOrDefault()?.Ename;
                        }
                        if (!string.IsNullOrEmpty(item.BizType))
                        {
                            var bizTypeArr = item.BizType.Split(",");
                            var bizTypeCopy = item.BizType;
                            foreach (var bizType in bizTypeArr) 
                            {
                                var typeName = dics.Where(x => x.Dictid == "2500" && x.Code == bizType).FirstOrDefault()?.Ename;
                                bizTypeCopy = bizTypeCopy.Replace(bizType, typeName);
                            }
                            item.BizTypeName = bizTypeCopy;
                        }
                        item.SalesidName = item.Salesid;
                        /*if (!string.IsNullOrEmpty(item.Salesid))
                        {
                            item.SalesidName = cnData.Where(x => x.UserId == item.Salesid).FirstOrDefault()?.NameEn;
                        }*/
                        if (!string.IsNullOrEmpty(item.EId))
                        {
                            item.EIdName = cnData.Where(x => x.UserId == item.EId).FirstOrDefault()?.NameEn;
                        }
                        if (!string.IsNullOrEmpty(item.CsId))
                        {
                            item.CsIdName = cnData.Where(x => x.UserId == item.CsId).FirstOrDefault()?.NameEn;
                        }
                    }
                    var data60 = listSel.Where(x => x.Status == "60").ToList();
                    if (data60.Count()>0)
                    {
                        listSel.RemoveAll(x => x.Status == "60");
                        listSel.InsertRange(0, data60);
                    }
                    
                }
                return new { data = listSel, total = totalCount };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public int Save(SopOrderDetailsDTO input)
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                input.Createuser = currentUser.Userid;
                input.Createdate = DateTime.Now;
                input.Companyid = currentUser.Ccode;
                input.Departmentid = currentUser.subCcode;
                input.Flag = false;
                input.Status = input._state.ToUpper() == "SUBMIT" ? "20" : "10";
                MstCore.Helper.CodeParams codeParams = new MstCore.Helper.CodeParams();
                codeParams.codeRule = $"SOP&CURYEAR2&CURMONTH&____";
                codeParams.fldName = "sop_base_code";
                codeParams.tblName = "sop_base_header";
                var codes = VariableModelTool.GetCode(codeParams);
                input.SopCode = codes;
                int mainid = DB.SqlSugarClient().Insertable<SopOrderDetailsDTO>(input).ExecuteReturnIdentity();
                var now = DateTime.Now;
                List<SopOrderAttachmentDTO> attachments = new List<SopOrderAttachmentDTO>();
                List<FileManage> files = new List<FileManage>();
                if (input.ContactInformations != null)
                {
                    foreach (var item in input.ContactInformations)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                    }
                    DB.SqlSugarClient().Insertable<SopOrderContactDTO>(input.ContactInformations).ExecuteCommand();
                }
                if (input.PreAlertList != null)
                {
                    foreach (var item in input.PreAlertList)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.Flag = 1;
                    }
                    DB.SqlSugarClient().Insertable<SopPaContactDTO>(input.PreAlertList).ExecuteCommand();
                }
                if (input.OverseasAgentContactList != null)
                {
                    foreach (var item in input.OverseasAgentContactList)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.Flag = 0;
                    }
                    DB.SqlSugarClient().Insertable<SopPaContactDTO>(input.OverseasAgentContactList).ExecuteCommand();
                }
                if (input.Quotations != null)
                {
                    foreach (var item in input.Quotations)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "10";
                    }
                    attachments.AddRange(input.Quotations);
                }
                if (input.SettlementModes != null)
                {
                    foreach (var item in input.SettlementModes)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "20";
                    }
                    attachments.AddRange(input.SettlementModes);
                }
                if (input.GuaranteeLetters != null)
                {
                    foreach (var item in input.GuaranteeLetters)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "30";
                    }
                    attachments.AddRange(input.GuaranteeLetters);
                }
                if (input.HBLOrderTypes != null)
                {
                    foreach (var item in input.HBLOrderTypes)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "40";
                    }
                    attachments.AddRange(input.HBLOrderTypes);
                }
                if (attachments.Count() > 0)
                {
                    DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(attachments).ExecuteCommand();
                }
                //出口数据
                if (input.Exports != null)
                {
                    foreach (var item in input.Exports) 
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int outid = DB.SqlSugarClient().Insertable<SopOrderOutDTO>(item).ExecuteReturnIdentity();
                        /*字段所对应得附件*/
                        if (item.TypeOfGoods != null)
                        {
                            foreach (var item1 in item.TypeOfGoods)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_TypeOfGoods";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.TypeOfGoods);
                        }
                        if (item.ModeOfOperation != null)
                        {
                            foreach (var item1 in item.ModeOfOperation)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_ModeOfOperation";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.ModeOfOperation);
                        }
                        if (item.Pol != null)
                        {
                            foreach (var item1 in item.Pol)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Pol";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Pol);
                        }
                        if (item.Destination != null)
                        {
                            foreach (var item1 in item.Destination)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Destination";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Destination);
                        }
                    }
                    
                }
                

                if (input.CarrierList != null)
                {
                    foreach (var item in input.CarrierList)
                    {
                        item.SopOrderOutId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int carrId = DB.SqlSugarClient().Insertable<SopOrderOutCarrier>(item).ExecuteReturnIdentity();
                        /*字段所对应得附件*/
                        if (item.FileCarrier != null)
                        {
                            foreach (var item1 in item.FileCarrier)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                item1.RelationId = carrId;
                            }
                            files.AddRange(item.FileCarrier);
                        }
                    }

                }
                if (input.Import != null)
                {
                    input.Import.SopOrderId = mainid;
                    input.Import.Createuser = currentUser.Userid;
                    input.Import.Createdate = now;
                    input.Import.Companyid = currentUser.Ccode;
                    input.Import.Departmentid = currentUser.subCcode;
                    DB.SqlSugarClient().Insertable<SopOrderInDTO>(input.Import).ExecuteCommand();
                }
                else
                {
                    var saveData = new SopOrderInDTO()
                    {
                        SopOrderId = mainid,
                        Createuser = currentUser.Userid,
                        Createdate = now,
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderInDTO>(saveData).ExecuteCommand();
                }
                if (input.TrailerCustomsDeclaration != null)
                {
                    input.TrailerCustomsDeclaration.SopOrderId = mainid;
                    input.TrailerCustomsDeclaration.Createuser = currentUser.Userid;
                    input.TrailerCustomsDeclaration.Createdate = now;
                    input.TrailerCustomsDeclaration.Companyid = currentUser.Ccode;
                    input.TrailerCustomsDeclaration.Departmentid = currentUser.subCcode;
                    int traId = DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(input.TrailerCustomsDeclaration).ExecuteReturnIdentity();
                    //字段对应附件
                    if (input.TrailerCustomsDeclaration.Region != null)
                    {
                        foreach (var item in input.TrailerCustomsDeclaration.Region)
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_trailer_declaration_Region";
                            item.RelationId = traId;
                        }
                        files.AddRange(input.TrailerCustomsDeclaration.Region);
                    }
                }
                else
                {
                    var saveData = new SopOrderTrailerDeclarationDTO()
                    {
                        SopOrderId = mainid,
                        Createuser = currentUser.Userid,
                        Createdate = now,
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(saveData).ExecuteCommand();
                }
                if (input.OrderAttachments != null)
                {
                    foreach (var item in input.OrderAttachments)
                    {
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_attachment";
                        item.RelationId = mainid;
                    }
                    files.AddRange(input.OrderAttachments);
                }

                if (files.Count() > 0)
                {
                    DB.SqlSugarClient().Insertable<FileManage>(files).ExecuteCommand();
                }
                DB.SqlSugarClient().CommitTran();
                return mainid;
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
        #region Save出口一对一备份（20240725）
        /*public int Save(SopOrderDetailsDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                input.Createuser = currentUser.Userid;
                input.Createdate = DateTime.Now;
                input.Companyid = currentUser.Ccode;
                input.Departmentid = currentUser.subCcode;
                input.Flag = false;
                input.Status = input._state.ToUpper() == "SUBMIT" ? "20" : "10";
                MstCore.Helper.CodeParams codeParams = new MstCore.Helper.CodeParams();
                codeParams.codeRule = $"SOP&CURYEAR2&CURMONTH&____";
                codeParams.fldName = "sop_base_code";
                codeParams.tblName = "sop_base_header";
                var codes = VariableModelTool.GetCode(codeParams);
                input.SopCode = codes;
                int mainid = DB.SqlSugarClient().Insertable<SopOrderDetailsDTO>(input).ExecuteReturnIdentity();
                var now=DateTime.Now;
                List<SopOrderAttachmentDTO> attachments=new List<SopOrderAttachmentDTO>();
                List<FileManage> files = new List<FileManage>();
                if (input.ContactInformations!=null) 
                {
                    foreach (var item in input.ContactInformations) 
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                    }
                    DB.SqlSugarClient().Insertable<SopOrderContactDTO>(input.ContactInformations).ExecuteCommand();
                }
                if (input.PreAlertList != null) 
                {
                    foreach (var item in input.PreAlertList)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.Flag = 1;
                    }
                    DB.SqlSugarClient().Insertable<SopPaContactDTO>(input.PreAlertList).ExecuteCommand();
                }
                if (input.OverseasAgentContactList != null) 
                {
                    foreach (var item in input.OverseasAgentContactList)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.Flag = 0;
                    }
                    DB.SqlSugarClient().Insertable<SopPaContactDTO>(input.OverseasAgentContactList).ExecuteCommand();
                }
                if (input.Quotations != null)
                {
                    foreach (var item in input.Quotations)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "10";
                    }
                    attachments.AddRange(input.Quotations);
                }
                if (input.SettlementModes != null)
                {
                    foreach (var item in input.SettlementModes)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "20";
                    }
                    attachments.AddRange(input.SettlementModes);
                }
                if (input.GuaranteeLetters != null)
                {
                    foreach (var item in input.GuaranteeLetters)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "30";
                    }
                    attachments.AddRange(input.GuaranteeLetters);
                }
                if (input.HBLOrderTypes != null)
                {
                    foreach (var item in input.HBLOrderTypes)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "40";
                    }
                    attachments.AddRange(input.HBLOrderTypes);
                }
                if (attachments.Count() > 0) 
                {
                    DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(attachments).ExecuteCommand();
                }
                if (input.Export != null)
                {
                    input.Export.SopOrderId = mainid;
                    input.Export.Createuser = currentUser.Userid;
                    input.Export.Createdate = now;
                    input.Export.Companyid = currentUser.Ccode;
                    input.Export.Departmentid = currentUser.subCcode;
                    int outid = DB.SqlSugarClient().Insertable<SopOrderOutDTO>(input.Export).ExecuteReturnIdentity();
                    *//*字段所对应得附件*//*
                    if (input.Export.TypeOfGoods != null) 
                    {
                        foreach (var item in input.Export.TypeOfGoods) 
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_out_TypeOfGoods";
                            item.RelationId = outid;
                        }
                        files.AddRange(input.Export.TypeOfGoods);
                    }
                    if (input.Export.ModeOfOperation != null)
                    {
                        foreach (var item in input.Export.ModeOfOperation)
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_out_ModeOfOperation";
                            item.RelationId = outid;
                        }
                        files.AddRange(input.Export.ModeOfOperation);
                    }
                    if (input.Export.Pol != null)
                    {
                        foreach (var item in input.Export.Pol)
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_out_Pol";
                            item.RelationId = outid;
                        }
                        files.AddRange(input.Export.Pol);
                    }
                    if (input.Export.Destination != null)
                    {
                        foreach (var item in input.Export.Destination)
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_out_Destination";
                            item.RelationId = outid;
                        }
                        files.AddRange(input.Export.Destination);
                    }
                    if (input.Export.CarrierList != null)
                    {
                        foreach (var item in input.Export.CarrierList)
                        {
                            item.SopOrderOutId = outid;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            int carrId = DB.SqlSugarClient().Insertable<SopOrderOutCarrier>(item).ExecuteReturnIdentity();
                            *//*字段所对应得附件*//*
                            if (item.FileCarrier!=null)
                            {
                                foreach (var item1 in item.FileCarrier)
                                {
                                    item1.Createuser = currentUser.Userid;
                                    item1.Createdate = DateTime.Now;
                                    item1.Companyid = currentUser.Ccode;
                                    item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                    item1.RelationId = carrId;
                                }
                                files.AddRange(item.FileCarrier);
                            }
                        }
                       
                    }
                }
                else 
                {
                    var saveData = new SopOrderOutDTO()
                    {
                        SopOrderId = mainid,
                        Createuser = currentUser.Userid,
                        Createdate = now,
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderOutDTO>(saveData).ExecuteCommand();
                }
                if (input.Import != null)
                {
                    input.Import.SopOrderId = mainid;
                    input.Import.Createuser = currentUser.Userid;
                    input.Import.Createdate = now;
                    input.Import.Companyid = currentUser.Ccode;
                    input.Export.Departmentid = currentUser.subCcode;
                    DB.SqlSugarClient().Insertable<SopOrderInDTO>(input.Import).ExecuteCommand();
                }
                else 
                {
                    var saveData = new SopOrderInDTO()
                    {
                        SopOrderId = mainid,
                        Createuser = currentUser.Userid,
                        Createdate = now,
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderInDTO>(saveData).ExecuteCommand();
                }
                if (input.TrailerCustomsDeclaration != null)
                {
                    input.TrailerCustomsDeclaration.SopOrderId = mainid;
                    input.TrailerCustomsDeclaration.Createuser = currentUser.Userid;
                    input.TrailerCustomsDeclaration.Createdate = now;
                    input.TrailerCustomsDeclaration.Companyid = currentUser.Ccode;
                    input.TrailerCustomsDeclaration.Departmentid = currentUser.subCcode;
                    int traId=DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(input.TrailerCustomsDeclaration).ExecuteReturnIdentity();
                    //字段对应附件
                    if (input.TrailerCustomsDeclaration.Region!=null) 
                    {
                        foreach (var item in input.TrailerCustomsDeclaration.Region)
                        {
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_trailer_declaration_Region";
                            item.RelationId = traId;
                        }
                        files.AddRange(input.TrailerCustomsDeclaration.Region);
                    }
                }
                else 
                {
                    var saveData = new SopOrderTrailerDeclarationDTO()
                    {
                        SopOrderId=mainid,
                        Createuser=currentUser.Userid,
                        Createdate=now,
                        Companyid=currentUser.Ccode,
                        Departmentid=currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(saveData).ExecuteCommand();
                }
                if(input.OrderAttachments!=null) 
                {
                    foreach (var item in input.OrderAttachments)
                    {
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_attachment";
                        item.RelationId = mainid;
                    }
                    files.AddRange(input.OrderAttachments);
                }

                if (files.Count()>0) 
                {
                    DB.SqlSugarClient().Insertable<FileManage>(files).ExecuteCommand();
                }
                DB.SqlSugarClient().CommitTran();
                return mainid;
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }*/
        #endregion

        public IActionResult Modify(SopOrderDetailsDTO input)
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                if (input._state.ToUpper() == "SUBMIT")
                {
                    input.Status = "20";

                    /*记录操作日志*/
                    var appRecord = new SopOrderApproverRecord()
                    {
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                        ApprovalStatus = "10",//通过
                        ApprovalNode = "10",//提交节点
                        Approver = currentUser.Userid,
                        ApprovalTime = DateTime.Now
                    };
                    if (input.Id == 0)
                    {
                        var mainid = Save(input);
                        /*生成合同模板，上传到minio*/
                        input.Id = mainid;
                        OutputTemplateContent(input);
                        appRecord.SopOrderId = mainid;
                        DB.SqlSugarClient().Insertable<SopOrderApproverRecord>(appRecord).ExecuteCommand();
                        return MstResult.Success(mainid);
                    }
                    else
                    {
                        /*生成合同模板，上传到minio*/
                        OutputTemplateContent(input);
                        appRecord.SopOrderId = input.Id;
                        DB.SqlSugarClient().Insertable<SopOrderApproverRecord>(appRecord).ExecuteCommand();
                    }
                }
                var now = DateTime.Now;
                List<SopOrderAttachmentDTO> attachments = new List<SopOrderAttachmentDTO>();
                //Pa联系人
                if (input.PreAlertList != null)
                {
                    var saveData = input.PreAlertList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.PreAlertList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1 && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopPaContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.Flag }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Flag = 1;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1).ExecuteCommand();
                }
                //海外代理商联系方式
                if (input.OverseasAgentContactList != null)
                {
                    var saveData = input.OverseasAgentContactList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.OverseasAgentContactList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0 && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopPaContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.Flag }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Flag = 0;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0).ExecuteCommand();
                }

                if (input.ContactInformations != null)
                {
                    var saveData = input.ContactInformations.Where(x => x.Id == 0).ToList();
                    var modifyData = input.ContactInformations.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopOrderContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                }
                if (input.Quotations != null)
                {
                    foreach (var item in input.Quotations)
                    {
                        item.AttachmentType = "10";
                    }
                    attachments.AddRange(input.Quotations);
                }
                if (input.SettlementModes != null)
                {
                    foreach (var item in input.SettlementModes)
                    {
                        item.AttachmentType = "20";
                    }
                    attachments.AddRange(input.SettlementModes);
                }
                if (input.GuaranteeLetters != null)
                {
                    foreach (var item in input.GuaranteeLetters)
                    {
                        item.AttachmentType = "30";
                    }
                    attachments.AddRange(input.GuaranteeLetters);
                }
                if (input.HBLOrderTypes != null)
                {
                    foreach (var item in input.HBLOrderTypes)
                    {
                        item.AttachmentType = "40";
                    }
                    attachments.AddRange(input.HBLOrderTypes);
                }
                if (attachments.Count() > 0)
                {
                    var saveData = attachments.Where(x => x.Id == 0).ToList();
                    var modifyData = attachments.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var noDeleteIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id && !noDeleteIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopOrderAttachmentDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.AttachmentType }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                }


                /*
                 出口   删除所有出口信息，添加新的出口信息
                 附件统一添加
                 */
                List<FileManage> files = new List<FileManage>();
                var dbOutIds=DB.SqlSugarClient().Queryable<SopOrderOut>().Where(x => x.SopOrderId == input.Id).Select(x => x.Id).ToList();
                if (dbOutIds.Count() > 0) 
                {
                    List<string> exportTabNames = new List<string>()
                    {
                        "sop_order_out_TypeOfGoods",
                        "sop_order_out_ModeOfOperation",
                        "sop_order_out_Pol",
                        "sop_order_out_Destination"
                    };
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => dbOutIds.Contains(x.RelationId.Value) && exportTabNames.Contains(x.RelationTableName)).ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderOut>().Where(x => dbOutIds.Contains(x.Id)).ExecuteCommand();
                }
                if (input.Exports != null)
                {
                   
                    foreach (var item in input.Exports)
                    {
                        item.Id = 0;
                        item.SopOrderId = input.Id;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int outid = DB.SqlSugarClient().Insertable<SopOrderOutDTO>(item).ExecuteReturnIdentity();
                        /*字段所对应的附件*/
                        if (item.TypeOfGoods != null)
                        {
                            foreach (var item1 in item.TypeOfGoods)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_TypeOfGoods";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.TypeOfGoods);
                        }
                        if (item.ModeOfOperation != null)
                        {
                            foreach (var item1 in item.ModeOfOperation)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_ModeOfOperation";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.ModeOfOperation);
                        }
                        if (item.Pol != null)
                        {
                            foreach (var item1 in item.Pol)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Pol";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Pol);
                        }
                        if (item.Destination != null)
                        {
                            foreach (var item1 in item.Destination)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Destination";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Destination);
                        }
                    }
                   
                }
                /*
                 船信息   删除所有出口船信息  ，添加新的出口船信息  
                 */
                var dbCarrierIds = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Id).Select(x => x.Id).ToList();
                if (dbCarrierIds.Count() > 0)
                {
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => dbCarrierIds.Contains(x.RelationId.Value) && x.RelationTableName== "sop_order_out_carrier_Carrier").ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => dbCarrierIds.Contains(x.Id)).ExecuteCommand();
                }

                if (input.CarrierList != null)
                {
                    foreach (var item in input.CarrierList)
                    {
                        item.SopOrderOutId = input.Id;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int carrId = DB.SqlSugarClient().Insertable<SopOrderOutCarrier>(item).ExecuteReturnIdentity();
                        /*字段所对应得附件*/
                        if (item.FileCarrier != null)
                        {
                            foreach (var item1 in item.FileCarrier)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                item1.RelationId = carrId;
                            }
                            files.AddRange(item.FileCarrier);
                        }
                    }

                }
                
                input.Import.Modifier = currentUser.Userid;
                input.Import.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderInDTO>(input.Import).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();

                //删除TrailerCustomsDeclaration 属性对应字段
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.TrailerCustomsDeclaration.Id && x.RelationTableName == "sop_order_trailer_declaration_Region").ExecuteCommand();
                if (input.TrailerCustomsDeclaration.Region != null)
                {
                    foreach (var item in input.TrailerCustomsDeclaration.Region)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_trailer_declaration_Region";
                        item.RelationId = input.TrailerCustomsDeclaration.Id;
                    }
                    files.AddRange(input.TrailerCustomsDeclaration.Region);
                }

                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.Id && x.RelationTableName == "sop_order_attachment").ExecuteCommand();
                if (input.OrderAttachments != null)
                {
                    foreach (var item in input.OrderAttachments)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_attachment";
                        item.RelationId = input.Id;
                    }
                    files.AddRange(input.OrderAttachments);
                }
                if (files.Count() > 0)
                {
                    DB.SqlSugarClient().Insertable<FileManage>(files).ExecuteCommand();
                }
                input.TrailerCustomsDeclaration.Modifier = currentUser.Userid;
                input.TrailerCustomsDeclaration.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderTrailerDeclarationDTO>(input.TrailerCustomsDeclaration).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                input.Modifier = currentUser.Userid;
                input.Modifydate = DateTime.Now;
                DB.SqlSugarClient().Updateable<SopOrderDetailsDTO>(input).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.SopCode, x.Flag, x.ModifySopCode }).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
        #region 出口一对一备份Modify(20240725)
        /*public IActionResult Modify(SopOrderDetailsDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                if (input._state.ToUpper() == "SUBMIT")
                {
                    input.Status = "20";
                    
                    *//*记录操作日志*//*
                    var appRecord = new SopOrderApproverRecord()
                    {
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                        ApprovalStatus = "10",//通过
                        ApprovalNode="10",//提交节点
                        Approver= currentUser.Userid,
                        ApprovalTime= DateTime.Now
                    };
                    if (input.Id == 0)
                    {
                        var mainid = Save(input);
                        *//*生成合同模板，上传到minio*//*
                        input.Id = mainid;
                        OutputTemplateContent(input);
                        appRecord.SopOrderId = mainid;
                        DB.SqlSugarClient().Insertable<SopOrderApproverRecord>(appRecord).ExecuteCommand();
                        return MstResult.Success(mainid);
                    }
                    else 
                    {
                        *//*生成合同模板，上传到minio*//*
                        OutputTemplateContent(input);
                        appRecord.SopOrderId = input.Id;
                        DB.SqlSugarClient().Insertable<SopOrderApproverRecord>(appRecord).ExecuteCommand();
                    }
                }
                var now =DateTime.Now;
                List<SopOrderAttachmentDTO> attachments = new List<SopOrderAttachmentDTO>();
                //Pa联系人
                if (input.PreAlertList != null)
                {
                    var saveData = input.PreAlertList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.PreAlertList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id &&x.Flag==1&& !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopPaContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser,x.Flag }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Flag = 1;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else 
                {
                    DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id&&x.Flag==1).ExecuteCommand();
                }
                //海外代理商联系方式
                if (input.OverseasAgentContactList != null)
                {
                    var saveData = input.OverseasAgentContactList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.OverseasAgentContactList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0 && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopPaContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.Flag }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Flag = 0;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0).ExecuteCommand();
                }

                if (input.ContactInformations != null)
                {
                    var saveData = input.ContactInformations.Where(x => x.Id == 0).ToList();
                    var modifyData = input.ContactInformations.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopOrderContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                    }
                    else 
                    {
                        DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else 
                {
                    DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x=>x.SopOrderId==input.Id).ExecuteCommand();
                }
                if (input.Quotations != null)
                {
                    foreach (var item in input.Quotations)
                    {
                        item.AttachmentType = "10";
                    }
                    attachments.AddRange(input.Quotations);
                }
                if (input.SettlementModes != null)
                {
                    foreach (var item in input.SettlementModes)
                    {
                        item.AttachmentType = "20";
                    }
                    attachments.AddRange(input.SettlementModes);
                }
                if (input.GuaranteeLetters != null)
                {
                    foreach (var item in input.GuaranteeLetters)
                    {
                        item.AttachmentType = "30";
                    }
                    attachments.AddRange(input.GuaranteeLetters);
                }
                if (input.HBLOrderTypes != null)
                {
                    foreach (var item in input.HBLOrderTypes)
                    {
                        item.AttachmentType = "40";
                    }
                    attachments.AddRange(input.HBLOrderTypes);
                }
                if (attachments.Count() > 0)
                {
                    var saveData = attachments.Where(x => x.Id == 0).ToList();
                    var modifyData= attachments.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var noDeleteIds=modifyData.Select(x=>x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id && !noDeleteIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData) 
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopOrderAttachmentDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser,x.AttachmentType }).ExecuteCommand();
                    }
                    else 
                    {
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(saveData).ExecuteCommand();
                    }
                }
                else 
                {
                    DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                }
                input.Export.Modifier = currentUser.Userid;
                input.Export.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderOutDTO>(input.Export).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                *//* 还需处理字段所绑定的附件 *//*
                if (input.Export.CarrierList != null)//在sava时  export已经自动生成，不会为null所以可以直接处理子集
                {
                    var saveData = input.Export.CarrierList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.Export.CarrierList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var mCarIds = modifyData.Select(x => x.Id).ToList();
                        var carIds = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Export.Id && !mCarIds.Contains(x.Id)).Select(x => x.Id).ToList();
                        if (carIds.Count() > 0)
                        {
                            DB.SqlSugarClient().Deleteable<FileManage>().Where(x => carIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ExecuteCommand();
                            DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => carIds.Contains(x.Id)).ExecuteCommand();
                        }
                        //先把附件删除
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => mCarIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ExecuteCommand();
                        List<FileManage> saveFiles=new List<FileManage>();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                            if (item.FileCarrier!=null) 
                            {
                                foreach (var item1 in item.FileCarrier) 
                                {
                                    item1.Createuser = currentUser.Userid;
                                    item1.Createdate = DateTime.Now;
                                    item1.Companyid = currentUser.Ccode;
                                    item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                    item1.RelationId = item.Id;
                                }
                                saveFiles.AddRange(item.FileCarrier);
                            }
                        }
                        if (saveFiles.Count()>0) 
                        {
                            DB.SqlSugarClient().Insertable<FileManage>(saveFiles).ExecuteCommand();
                        }
                        DB.SqlSugarClient().Updateable<SopOrderOutCarrierDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                    }
                    else 
                    {
                        var carIds=DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Export.Id).Select(x => x.Id).ToList();
                        if (carIds.Count()>0)
                        {
                            DB.SqlSugarClient().Deleteable<FileManage>().Where(x => carIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ExecuteCommand();
                            DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Export.Id).ExecuteCommand();
                        }
                    }
                    if (saveData.Count()>0) 
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderOutId = input.Export.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            int carrId=DB.SqlSugarClient().Insertable<SopOrderOutCarrierDTO>(item).ExecuteReturnIdentity();
                            if (item.FileCarrier!=null) 
                            {
                                foreach (var item1 in item.FileCarrier)
                                {
                                    item1.Createuser = currentUser.Userid;
                                    item1.Createdate = DateTime.Now;
                                    item1.Companyid = currentUser.Ccode;
                                    item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                    item1.RelationId = carrId;
                                }
                                DB.SqlSugarClient().Insertable<FileManage>(item.FileCarrier).ExecuteCommand();
                            }
                        }
                    }
                }
                else 
                {
                    var carIds = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Export.Id).Select(x => x.Id).ToList();
                    if (carIds.Count() > 0)
                    {
                        List<long> carIdsLong = carIds.Select(i => (long)i).ToList();
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => carIdsLong.Contains(x.Idx) && x.RelationTableName == "sop_order_out_carrier").ExecuteCommand();
                        DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Export.Id).ExecuteCommand();
                    }
                }
                //删除Export 属性对应附件
                List<string> exportTabNames=new List<string>() 
                {
                    "sop_order_out_TypeOfGoods",
                    "sop_order_out_ModeOfOperation",
                    "sop_order_out_Pol",
                    "sop_order_out_Destination"
                };
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.Export.Id && exportTabNames.Contains(x.RelationTableName)).ExecuteCommand();
                List<FileManage> saveExportFile=new List<FileManage>();
                if (input.Export.TypeOfGoods != null)
                {
                    foreach (var item in input.Export.TypeOfGoods)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_out_TypeOfGoods";
                        item.RelationId = input.Export.Id;
                    }
                    saveExportFile.AddRange(input.Export.TypeOfGoods);
                }
                if (input.Export.ModeOfOperation != null)
                {
                    foreach (var item in input.Export.ModeOfOperation)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_out_ModeOfOperation";
                        item.RelationId = input.Export.Id;
                    }
                    saveExportFile.AddRange(input.Export.ModeOfOperation);
                }
                if (input.Export.Pol != null)
                {
                    foreach (var item in input.Export.Pol) 
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_out_Pol";
                        item.RelationId = input.Export.Id;
                    }
                    saveExportFile.AddRange(input.Export.Pol);
                }
                if (input.Export.Destination != null)
                {
                        foreach (var item in input.Export.Destination)
                        {
                            item.Idx = 0;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_out_Destination";
                            item.RelationId = input.Export.Id;
                        }
                        saveExportFile.AddRange(input.Export.Destination);
                }
                if (saveExportFile.Count()>0) 
                {
                    DB.SqlSugarClient().Insertable<FileManage>(saveExportFile).ExecuteCommand();
                }

                input.Import.Modifier = currentUser.Userid;
                input.Import.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderInDTO>(input.Import).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();

                //删除TrailerCustomsDeclaration 属性对应字段
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.TrailerCustomsDeclaration.Id && x.RelationTableName == "sop_order_trailer_declaration_Region").ExecuteCommand();
                if (input.TrailerCustomsDeclaration.Region != null)
                {
                    foreach (var item in input.TrailerCustomsDeclaration.Region)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_trailer_declaration_Region";
                        item.RelationId = input.TrailerCustomsDeclaration.Id;
                    }
                    DB.SqlSugarClient().Insertable<FileManage>(input.TrailerCustomsDeclaration.Region).ExecuteCommand();
                }

                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.Id && x.RelationTableName == "sop_order_attachment").ExecuteCommand();
                if (input.OrderAttachments != null) 
                {
                    foreach (var item in input.OrderAttachments)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_attachment";
                        item.RelationId = input.Id;
                    }
                    DB.SqlSugarClient().Insertable<FileManage>(input.OrderAttachments).ExecuteCommand();
                }

                input.TrailerCustomsDeclaration.Modifier = currentUser.Userid;
                input.TrailerCustomsDeclaration.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderTrailerDeclarationDTO>(input.TrailerCustomsDeclaration).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                input.Modifier = currentUser.Userid;
                input.Modifydate = DateTime.Now;
                DB.SqlSugarClient().Updateable<SopOrderDetailsDTO>(input).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser ,x.SopCode,x.Flag,x.ModifySopCode}).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex) 
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }*/
        #endregion

        #region 出口一对一备份DELETE(20240725)
        /*public IActionResult DELETE(IdxsPublicInput input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
               

                //删除SopOrderTrailerDeclaration
                var count =DB.SqlSugarClient().Queryable<SopOrder>().Where(x => input.Idxs.Contains(x.Id) && x.Status == "10").Count();
                if (input.Idxs.Length!=count) 
                {
                    DB.SqlSugarClient().RollbackTran();
                    return MstResult.Error("操作失败");
                }
              
                *//* 删除order对应附件 *//*
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => input.Idxs.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_attachment").ExecuteCommand();

                var otdIds =DB.SqlSugarClient().Queryable<SopOrderTrailerDeclaration>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).Select(x => x.Id).ToList();
                
                if (otdIds.Count()>0) 
                {
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => otdIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_trailer_declaration_Region").ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderTrailerDeclaration>().Where(x => otdIds.Contains(x.SopOrderId.Value)).ExecuteCommand();
                }
                //没有字段带有附件，直接删除
                DB.SqlSugarClient().Deleteable<SopOrderIn>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                var outIds = DB.SqlSugarClient().Queryable<SopOrderOut>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).Select(x => x.Id).ToList();
                if (outIds.Count()>0) 
                {
                    //删除字段对应附件（4个）
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => otdIds.Contains(x.RelationId.Value) && x.RelationTableName.Contains("sop_order_out") && x.RelationTableName!= "sop_order_out_carrier_Carrier").ExecuteCommand();
                    var carrIds = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => outIds.Contains(x.SopOrderOutId.Value)).Select(x => x.Id).ToList();
                    if (carrIds.Count()>0) 
                    {
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => carrIds.Contains(x.RelationId.Value) && x.RelationTableName.Contains("sop_order_out_carrier_Carrier")).ExecuteCommand();
                        DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => outIds.Contains(x.SopOrderOutId.Value)).ExecuteCommand();
                    }
                    DB.SqlSugarClient().Deleteable<SopOrderOut>().Where(x => outIds.Contains(x.SopOrderId.Value)).ExecuteCommand();
                }
                //没有字段带有附件，直接删除
                //删除联系人
                DB.SqlSugarClient().Deleteable<SopOrderContact>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                //删除pa联系人，删除海外代理商联系方式
                DB.SqlSugarClient().Deleteable<SopPaContact>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                //删除4类型
                DB.SqlSugarClient().Deleteable<SopOrderAttachment>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                DB.SqlSugarClient().Deleteable<SopOrder>().Where(x => input.Idxs.Contains(x.Id)).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex) 
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }*/
        #endregion
        public IActionResult DELETE(IdxsPublicInput input)
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                var count = DB.SqlSugarClient().Queryable<SopOrder>().Where(x => input.Idxs.Contains(x.Id) && x.Status == "10").Count();
                if (input.Idxs.Length != count)
                {
                    DB.SqlSugarClient().RollbackTran();
                    return MstResult.Error("操作失败");
                }
                //删除Out对应信息以及字段对应附件
                var outIds = DB.SqlSugarClient().Queryable<SopOrderOut>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).Select(x => x.Id).ToList();
                if (outIds.Count() > 0)
                {
                    //删除字段对应附件（4个）
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => outIds.Contains(x.RelationId.Value) && x.RelationTableName.Contains("sop_order_out") && x.RelationTableName != "sop_order_out_carrier_Carrier").ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderOut>().Where(x => outIds.Contains(x.Id)).ExecuteCommand();
                }
                //删除Carrier对应信息以及字段对应附件
                var carrierIds = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => input.Idxs.Contains(x.SopOrderOutId.Value)).Select(x => x.Id).ToList();
                if (carrierIds.Count() > 0)
                {
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => carrierIds.Contains(x.RelationId.Value) && x.RelationTableName.Contains("sop_order_out_carrier_Carrier")).ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => carrierIds.Contains(x.Id)).ExecuteCommand();
                }
                //删除SopOrderTrailerDeclaration对应信息以及字段对应附件
                var otdIds = DB.SqlSugarClient().Queryable<SopOrderTrailerDeclaration>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).Select(x => x.Id).ToList();
                if (otdIds.Count() > 0)
                {
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => otdIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_trailer_declaration_Region").ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderTrailerDeclaration>().Where(x => otdIds.Contains(x.SopOrderId.Value)).ExecuteCommand();
                }
                /*没有字段带有附件，直接删除*/
                //删除In对应信息
                DB.SqlSugarClient().Deleteable<SopOrderIn>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                //删除联系人
                DB.SqlSugarClient().Deleteable<SopOrderContact>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                //删除pa联系人，删除海外代理商联系方式
                DB.SqlSugarClient().Deleteable<SopPaContact>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                //删除4类型
                DB.SqlSugarClient().Deleteable<SopOrderAttachment>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                /* 删除order对应附件 */
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => input.Idxs.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_attachment").ExecuteCommand();
                /* 删除审批记录 */
                DB.SqlSugarClient().Deleteable<SopOrderApproverRecord>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                /* 删除提交生成的文件 */
                DB.SqlSugarClient().Deleteable<SopOrderContentText>().Where(x => input.Idxs.Contains(x.SopOrderId.Value)).ExecuteCommand();
                /* 删除order对应信息 */
                DB.SqlSugarClient().Deleteable<SopOrder>().Where(x => input.Idxs.Contains(x.Id)).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
      
        public IActionResult Get(int id) 
        {
            try
            {
                var order=DB.SqlSugarClient().Queryable<SopOrderDetailsDTO>().Where(x => x.Id == id).First();
                if (order!=null) 
                {
                    var contacts=DB.SqlSugarClient().Queryable<SopPaContactDTO>().Where(x => x.SopOrderId == id).ToList();
                    order.PreAlertList = contacts.Where(x => x.Flag == 1).ToList();
                    order.OverseasAgentContactList = contacts.Where(x => x.Flag == 0).ToList();
                    order.OrderAttachments= DB.SqlSugarClient().Queryable<FileManage>().Where(x => x.RelationId==id && x.RelationTableName == "sop_order_attachment").ToList();
                    PublishInput publish = new PublishInput()
                    {
                        Id = order.Id,
                        IsPublish = order.IsPublish,
                        VisibleMember = order.VisibleMember,
                        PublishTime = order.PublishTime,
                        PublishMember=order.PublishMember,
                    };
                    var cnData = _codeNameConversion.GetCodeNamesDatas();//code and Name
                    if (!string.IsNullOrEmpty(publish.PublishMember))
                    {
                        publish.PublishMemberName = cnData.Where(x => x.UserId == publish.PublishMember).FirstOrDefault()?.NameEn;
                    }
                    order.PublishMsg = publish;
                    var sopApprovers= DB.SqlSugarClient().Queryable<SopOrderApproverRecordDTO>().Where(x => x.SopOrderId == id).OrderBy(x => x.ApprovalTime).ToList();
                    if (sopApprovers != null) 
                    {
                        foreach (var item in sopApprovers) 
                        {
                            if (!string.IsNullOrEmpty(item.Approver))
                            {

                                item.ApproverName = cnData.Where(x => x.UserId == item.Approver).FirstOrDefault()?.NameEn;
                            }
                        }
                    }
                    order.SopOrderApproverRecords = sopApprovers;
                    order.GeneratedFiles = DB.SqlSugarClient().Queryable<SopOrderContentText>().Where(x => id==x.SopOrderId.Value&&x.IsDelete==false).OrderBy(x=>x.Createdate,SqlSugar.OrderByType.Desc).ToList();
                    order.ContactInformations=DB.SqlSugarClient().Queryable<SopOrderContactDTO>().Where(x => x.SopOrderId == id).ToList();
                    
                    var attachment=DB.SqlSugarClient().Queryable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == id).ToList();
                    if (attachment.Count()>0) 
                    {
                        order.Quotations = attachment.Where(x => x.AttachmentType == "10").ToList();
                        order.SettlementModes = attachment.Where(x => x.AttachmentType == "20").ToList();
                        order.GuaranteeLetters = attachment.Where(x => x.AttachmentType == "30").ToList();
                        order.HBLOrderTypes = attachment.Where(x => x.AttachmentType == "40").ToList();
                    }
                    var orderOuts = DB.SqlSugarClient().Queryable<SopOrderOutDTO>().Where(x => x.SopOrderId == id).ToList();
                    if (orderOuts.Count()>0)
                    {
                        var outIds= orderOuts.Select(x=>x.Id).ToList();
                        /*带出字段对应附件*/
                        var exportFiles = DB.SqlSugarClient().Queryable<FileManage>().Where(x => outIds.Contains(x.RelationId.Value) && x.RelationTableName.Contains("sop_order_out")).ToList();
                        if (exportFiles.Count() > 0)
                        {
                            foreach (var item in orderOuts) 
                            {
                                item.TypeOfGoods = exportFiles.Where(x => x.RelationTableName == "sop_order_out_TypeOfGoods" &&x.RelationId==item.Id).ToList();
                                item.ModeOfOperation = exportFiles.Where(x => x.RelationTableName == "sop_order_out_ModeOfOperation" && x.RelationId == item.Id).ToList();
                                item.Pol = exportFiles.Where(x => x.RelationTableName == "sop_order_out_Pol" && x.RelationId == item.Id).ToList();
                                item.Destination = exportFiles.Where(x => x.RelationTableName == "sop_order_out_Destination" && x.RelationId == item.Id).ToList();
                            }
                            
                        }
                        order.Exports = orderOuts;
                    }


                    var orderCarriers = DB.SqlSugarClient().Queryable<SopOrderOutCarrierDTO>().Where(x => x.SopOrderOutId == id).ToList();
                    if (orderCarriers.Count() > 0)
                    {
                        var carIds = orderCarriers.Select(x => x.Id).ToList();
                        var carrFiles = DB.SqlSugarClient().Queryable<FileManage>().Where(x => carIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ToList();
                        if (carrFiles.Count()>0) 
                        {
                            foreach (var item in orderCarriers)
                            {
                                item.FileCarrier= carrFiles.Where(x => x.RelationId == item.Id).ToList();
                            }
                        }
                        order.CarrierList = orderCarriers;
                    }
                    
                    order.Import=DB.SqlSugarClient().Queryable<SopOrderInDTO>().Where(x => x.SopOrderId == id).First();
                    var trailerCustomsDeclaration = DB.SqlSugarClient().Queryable<SopOrderTrailerDeclarationDTO>().Where(x => x.SopOrderId == id).First();
                    if (trailerCustomsDeclaration!=null)
                    {
                        trailerCustomsDeclaration.Region = DB.SqlSugarClient().Queryable<FileManage>().Where(x => x.RelationId == trailerCustomsDeclaration.Id && x.RelationTableName.Contains("sop_order_trailer_declaration_Region")).ToList();
                        order.TrailerCustomsDeclaration = trailerCustomsDeclaration;
                    }
                   
                }
                return MstResult.Success(order);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult ObtainCorrespondingAttachments(string attrId) 
        {
            try 
            {
                var sopBase=DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.AttrId.Contains(attrId)).OrderBy(x=>x.Orderid).Select(x => new FrameDataOut()
                {
                    Idx = x.Idx,
                    SopName = x.SopName,
                    SopNameen = x.SopNameen,
                    Pid = x.Pid,
                }).ToList();
                var outList = new List<CorrespondingAttachmentsOut>();
                if (sopBase.Count()>0) 
                {
                    //获取所有得框架图数据为了在内存中处理得到层级目录
                    var sopBases = DB.SqlSugarClient().Queryable<SopBase>().Select(x => new FrameDataOut()
                    {
                        Idx = x.Idx,
                        SopName = x.SopName,
                        SopNameen = x.SopNameen,
                        Pid = x.Pid,
                    }).ToList();
                    //获取对应附件
                    var idxs = sopBase.Select(x => x.Idx).ToList();
                    var FileManages=DB.SqlSugarClient().Queryable<FileManage>().Where(x => idxs.Contains(x.RelationId.Value) && x.RelationTableName == "sop_base").ToList();
                    foreach (var item in sopBase) 
                    {
                        var sopBaseIdxs = new List<int>();
                        //找上级数据
                        if (item.Pid == 0)
                        {
                            sopBaseIdxs.Add(item.Idx);
                        }
                        else
                        {
                            sopBaseIdxs.Add(item.Idx);
                            var flag = item;
                            var any = true;
                            do
                            {
                                var parentlevel = sopBases.Where(x => x.Idx == flag.Pid).First();
                                sopBaseIdxs.Add(parentlevel.Idx);
                                flag = parentlevel;
                            } while (any && flag.Pid != 0);
                        }
                        StringBuilder nameBuilder=new StringBuilder();
                        StringBuilder nameenBuilder = new StringBuilder();
                        for (int i = (sopBaseIdxs.Count()-1); i >= 0; i--) 
                        {
                            var sopName= sopBases.Where(x=>x.Idx== sopBaseIdxs[i]).FirstOrDefault().SopName;
                            var sopNameen = sopBases.Where(x => x.Idx == sopBaseIdxs[i]).FirstOrDefault().SopNameen;
                            nameBuilder.Append(sopNameen+"/");
                            nameenBuilder.Append(sopNameen+"/");
                        }
                        var name = nameBuilder.ToString().Substring(0, nameBuilder.Length - 1);
                        var nameen = nameBuilder.ToString().Substring(0, nameenBuilder.Length - 1);
                        var caOut = new CorrespondingAttachmentsOut()
                        {
                            SopName = name,
                            SopNameen=nameen,
                            FileManages= FileManages.Where(x=>x.RelationId==item.Idx).ToList()
                        };
                        outList.Add(caOut);
                    }
                }
                return MstResult.Success(outList);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public IActionResult DeleteContentVersion(IdPublicInput input)
        {
            try 
            {
                DB.SqlSugarClient().Updateable<SopOrderContentText>().SetColumns(x => x.IsDelete == true).Where(x=>x.Id==input.Id).ExecuteCommand();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult UploadContentVersion(SopOrderContentTextDTO input) 
        {
            try
            {
                var version = DB.SqlSugarClient().Queryable<SopOrderContentText>().Where(x => x.SopOrderId == input.SopOrderId).Count();
                input.FileName = $"v{version + 1}";
                input.Createuser = currentUser.Userid;
                input.Createdate = DateTime.Now;
                input.Companyid = currentUser.Ccode;
                input.IsDelete = false;
                DB.SqlSugarClient().Insertable<SopOrderContentTextDTO>(input).ExecuteCommand();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult RevokeSubmit(IdPublicInput input) 
        {
            try 
            {
                var any = DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.Status == "20" && x.Id == input.Id).Any();
                if (any) 
                {
                    DB.SqlSugarClient().Updateable<SopOrder>().SetColumns(x => x.Status == "10").Where(x => x.Id == input.Id).ExecuteCommand();
                    return MstResult.Success("操作成功");
                } 
                
                return MstResult.Error("不是提交状态，撤销失败！");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public string Preview(SopOrderDetailsDTO input) 
        {
            try
            {
                input._state = "preview";
                return OutputTemplateContent(input);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult ApprovalResults(SopOrderApproverRecordDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                var any = DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.Id == input.SopOrderId && x.Status == "20").Any();
                if (!any) 
                {
                    DB.SqlSugarClient().RollbackTran();
                    return MstResult.Error("此状态下不支持审核操作！");
                }
                
                string status = "";
                if (input.ApprovalStatus == "10")//通过
                {
                    status = "30";//审批通过
                } else if (input.ApprovalStatus == "20")//驳回
                {
                    status = "10";//未提交
                } else if (input.ApprovalStatus == "30")//作废
                {
                    status = "40";//作废
                }
                else
                {
                    DB.SqlSugarClient().RollbackTran();
                    return MstResult.Error("入参错误");
                }
                DB.SqlSugarClient().Updateable<SopOrder>().SetColumns(x => x.Status == status).Where(x => x.Id == input.SopOrderId).ExecuteCommand();
                input.Companyid = currentUser.Ccode;
                input.Departmentid = currentUser.subCcode;
                //ApprovalStatus = "10";//通过
                //ApprovalNode = "10";//提交节点
                input.Approver = currentUser.Userid;
                input.ApprovalTime = DateTime.Now;
                DB.SqlSugarClient().Insertable<SopOrderApproverRecordDTO>(input).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
        public IActionResult Publish(PublishInput input) 
        {
            try 
            {
                DB.SqlSugarClient().Updateable<SopOrder>().SetColumnsIF(input.IsPublish==true,x=>x.Status=="50")
                    .SetColumnsIF(input.IsPublish == false, x => x.Status == "30")
                    .SetColumns(x => x.IsPublish == input.IsPublish)
                    .SetColumns(x => x.VisibleMember == input.VisibleMember)
                    .SetColumns(x => x.PublishTime == DateTime.Now)
                    .SetColumns(x=>x.PublishMember==currentUser.Userid)
                    .Where(x => x.Id == input.Id).ExecuteCommand();
                return MstResult.Success("操作成功");
            } catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public IActionResult DeptOrPersonnel() 
        {
            try
            {
                var sqlWhererole = _util.GetPermissionUtilCommonCnopay();
                var dict = sqlWhererole.ParamsDict;
                // 找到 '@' 符号的位置
                int startIndex = sqlWhererole.Sql.IndexOf('@');
                // 找到第一个空格的位置，从 '@' 符号之后搜索
                int endIndex = sqlWhererole.Sql.IndexOf(' ', startIndex);
                // 如果没有找到空格，则取字符串末尾
                if (endIndex == -1)
                {
                    endIndex = sqlWhererole.Sql.Length;
                }
                // 提取部门代码
                string filterDepartmentCode2 = sqlWhererole.Sql.Substring(startIndex + 1, endIndex - startIndex - 1);
                if (dict.ContainsKey(filterDepartmentCode2))
                {
                    var codes=dict[filterDepartmentCode2].ToString();
                    var perData=_httpTool.ObtainDeptOrPersonnel(codes);
                    return MstResult.Success(perData);
                }
                return MstResult.Success(new List<DeptOrPersonnelOut>());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult DeptOrFreightRatePersonnel() 
        {
            try
            {
                var sqlWhererole = _util.GetPermissionUtilCommonCnopay();
                var dict = sqlWhererole.ParamsDict;
                string pattern = @"in \((.*?)\)";
                Match match = Regex.Match(sqlWhererole.Sql, pattern);
                if (match.Success)
                {
                    string values = match.Groups[1].Value;
                    values=values.Replace("@", "");
                    string[] userIdKeys = values.Split(',');
                    List<string> userIdValues = new List<string>();
                    for (int i = 0; i < userIdKeys.Length; i++)
                    {
                        if (dict.ContainsKey(userIdKeys[i].Trim())) 
                        {
                            userIdValues.Add(dict[userIdKeys[i].Trim()].ToString());
                        }
                    }
                    var codes=string.Join(",", userIdValues);
                    var perData = _httpTool.ObtainDeptOrFreightRatePersonnel(codes);
                    return MstResult.Success(perData);
                }

                
                return MstResult.Success(new List<DeptOrPersonnelOut>());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public List<DictsOut> ObtainDestinationCountryData(string Code) 
        {
            try
            {
                List<DictsOut> dictList = new List<DictsOut>();
                //父级数据
                List<SopBase> parentsData = null;
                if (Code == "10")
                {
                    parentsData = DB.SqlSugarClient().Queryable<SopBase>().ToParentList(it => it.Pid, 21546);
                }
                else 
                {
                    parentsData = DB.SqlSugarClient().Queryable<SopBase>().ToParentList(it => it.Pid, 21457);
                }
                
                var parent = parentsData.Where(x => x.Pid == 0).FirstOrDefault();
                if (parent == null) 
                {
                    return dictList;
                }
                var parentIdxs = new List<int>();
                parentIdxs.Add(parent.Idx);
                bool flaga=true;
                do 
                {
                    var subset = parentsData.Where(x => x.Pid == parent.Idx).FirstOrDefault();
                    if (subset != null)
                    {
                        parent = subset;
                        parentIdxs.Add(parent.Idx);
                    }
                    else 
                    {
                        flaga = false;
                    }
                } while (flaga);
                //上级列表idx
                string attrParentId = string.Join(",", parentIdxs);
                List<SopBase> childsData = null;
                //子集数据
                if (Code == "10")
                {
                    childsData = DB.SqlSugarClient().Queryable<SopBase>().ToChildList(it => it.Pid, 21546);
                }
                else
                {
                    childsData = DB.SqlSugarClient().Queryable<SopBase>().ToChildList(it => it.Pid, 21457);
                }
                if (childsData.Count()>0) 
                {
                    List<DictsSpare> dicts = new List<DictsSpare>();
                    List<int> delSubscript = new List<int>();
                    foreach (var item in childsData) 
                    {
                        var sopBaseIdxs = new List<int>();
                        sopBaseIdxs.Add(item.Idx);
                        var flag = item;
                        var any = true;
                        do
                        {
                            var parentlevel = childsData.Where(x => x.Idx == flag.Pid).FirstOrDefault();
                            if (parentlevel != null)
                            {
                                sopBaseIdxs.Add(parentlevel.Idx);
                                delSubscript.Add(parentlevel.Idx);
                                flag = parentlevel;
                            }
                            else 
                            {
                                sopBaseIdxs.Reverse();
                                string attrId = string.Join(",", sopBaseIdxs);
                                var dict = new DictsSpare()
                                {
                                    Idx = item.Idx,
                                    Dictid = "Destination",
                                    Code = item.Idx.ToString(),
                                    Cname = item.SopName,
                                    Ename = item.SopNameen,
                                    HierarchicalFields = $"{attrParentId},{attrId}"
                                };
                                dicts.Add(dict);
                                any = false;
                            }
                            
                        } while (any);
                    }
                    if (delSubscript.Count()>0) 
                    {
                        dicts = dicts.Where(x => !delSubscript.Contains(x.Idx)).ToList();
                    }
                    var dictout = new DictsOut
                    {
                        Code = "Destination",
                        Dictinfos = dicts
                    };
                    dictList.Add(dictout);
                   
                }
                return dictList;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IActionResult RegenerateVersion(int Id) 
        {
            try 
            {
                var sopOrderDetail=DB.SqlSugarClient().Queryable<SopOrderDetailsDTO>().Where(x => x.Id == Id).First();
                if (sopOrderDetail != null) 
                {
                    var contacts=DB.SqlSugarClient().Queryable<SopPaContactDTO>().Where(x => x.SopOrderId == Id).ToList();
                    //pa联系人
                    sopOrderDetail.PreAlertList = contacts.Where(x => x.Flag == 1).ToList();
                    //海外代理联系人
                    sopOrderDetail.OverseasAgentContactList= contacts.Where(x => x.Flag == 0).ToList();
                    //联系人
                    sopOrderDetail.ContactInformations = DB.SqlSugarClient().Queryable<SopOrderContactDTO>().Where(x => x.SopOrderId == Id).ToList();
                    List<string> codes = new List<string>() { "10", "20" };
                    var attachments = DB.SqlSugarClient().Queryable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == Id && codes.Contains(x.AttachmentType)).ToList();
                    //处理报价单
                    sopOrderDetail.Quotations = attachments.Where(x => x.AttachmentType == "10").ToList();
                    //处理结算方式
                    sopOrderDetail.SettlementModes = attachments.Where(x => x.AttachmentType == "20").ToList();

                    if (sopOrderDetail.OwerType=="20") 
                    {
                        codes=new List<string>() {"30","40" };
                        attachments=DB.SqlSugarClient().Queryable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == Id&&codes.Contains(x.AttachmentType)).ToList();
                        //处理长期放货保涵
                        sopOrderDetail.GuaranteeLetters = attachments.Where(x => x.AttachmentType == "30").ToList();
                        //处理HBL出单类型
                        sopOrderDetail.HBLOrderTypes = attachments.Where(x => x.AttachmentType == "40").ToList();
                    }
                    sopOrderDetail.Exports = DB.SqlSugarClient().Queryable<SopOrderOutDTO>().Where(x => x.SopOrderId == Id).ToList();
                    if (sopOrderDetail.OwerType == "20") 
                    {
                        sopOrderDetail.CarrierList= DB.SqlSugarClient().Queryable<SopOrderOutCarrierDTO>().Where(x => x.SopOrderOutId == Id).ToList();
                    }
                    sopOrderDetail.Import = DB.SqlSugarClient().Queryable<SopOrderInDTO>().Where(x => x.SopOrderId == Id).First();
                    sopOrderDetail.TrailerCustomsDeclaration = DB.SqlSugarClient().Queryable<SopOrderTrailerDeclarationDTO>().Where(x => x.SopOrderId == Id).First();

                    OutputTemplateContent(sopOrderDetail);
                    return MstResult.Success("操作成功");
                }

                return null;
            }catch (Exception ex) 
            {

                throw new Exception(ex.Message);
            }
        }
        public int OperatorsSave(SopOrderDetailsDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                
                var any=DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.SopCode == input.SopCode && x.Status == "20"&&x.Flag==true).Any();
                if (any) 
                {
                    DB.SqlSugarClient().RollbackTran();
                    return 0;
                }
                input.Createuser = currentUser.Userid;
                input.Createdate = DateTime.Now;
                input.Companyid = currentUser.Ccode;
                input.Departmentid = currentUser.subCcode;
                input.Flag = true;//标记是审批后添加修改得数据
                input.Status = input._state.ToUpper() == "SUBMIT" ? "20" : "10";
                MstCore.Helper.CodeParams codeParams = new MstCore.Helper.CodeParams();
                codeParams.codeRule = $"M&CURYEAR2&CURMONTH&____";
                codeParams.fldName = "sop_base_code";
                codeParams.tblName = "sop_base_header";
                var codes = VariableModelTool.GetCode(codeParams);
                input.ModifySopCode = codes;
                var id=DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.SopCode == input.SopCode && x.Flag == false).Select(x => x.Id).First();
                input.PrimaryId = id;
                input.Id = 0;
                int mainid = DB.SqlSugarClient().Insertable<SopOrderDetailsDTO>(input).ExecuteReturnIdentity();
                var now = DateTime.Now;
                List<SopOrderAttachmentDTO> attachments = new List<SopOrderAttachmentDTO>();
                List<FileManage> files = new List<FileManage>();
                if (input.ContactInformations != null)
                {
                    foreach (var item in input.ContactInformations)
                    {
                        item.Id = 0;
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                    }
                    DB.SqlSugarClient().Insertable<SopOrderContactDTO>(input.ContactInformations).ExecuteCommand();
                }
                if (input.PreAlertList != null)
                {
                    foreach (var item in input.PreAlertList)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.Flag = 1;
                    }
                    DB.SqlSugarClient().Insertable<SopPaContactDTO>(input.PreAlertList).ExecuteCommand();
                }
                if (input.OverseasAgentContactList != null)
                {
                    foreach (var item in input.OverseasAgentContactList)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.Flag = 0;
                    }
                    DB.SqlSugarClient().Insertable<SopPaContactDTO>(input.OverseasAgentContactList).ExecuteCommand();
                }
                if (input.Quotations != null)
                {
                    foreach (var item in input.Quotations)
                    {
                        item.Id = 0;
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "10";
                    }
                    attachments.AddRange(input.Quotations);
                }
                if (input.SettlementModes != null)
                {
                    foreach (var item in input.SettlementModes)
                    {
                        item.Id = 0;
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "20";
                    }
                    attachments.AddRange(input.SettlementModes);
                }
                if (input.GuaranteeLetters != null)
                {
                    foreach (var item in input.GuaranteeLetters)
                    {
                        item.Id = 0;
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "30";
                    }
                    attachments.AddRange(input.GuaranteeLetters);
                }
                if (input.HBLOrderTypes != null)
                {
                    foreach (var item in input.HBLOrderTypes)
                    {
                        item.Id = 0;
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        item.AttachmentType = "40";
                    }
                    attachments.AddRange(input.HBLOrderTypes);
                }
                if (attachments.Count() > 0)
                {
                    DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(attachments).ExecuteCommand();
                }
                //出口数据
                if (input.Exports != null)
                {
                    foreach (var item in input.Exports)
                    {
                        item.SopOrderId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int outid = DB.SqlSugarClient().Insertable<SopOrderOutDTO>(item).ExecuteReturnIdentity();
                        /*字段所对应得附件*/
                        if (item.TypeOfGoods != null)
                        {
                            foreach (var item1 in item.TypeOfGoods)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_TypeOfGoods";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.TypeOfGoods);
                        }
                        if (item.ModeOfOperation != null)
                        {
                            foreach (var item1 in item.ModeOfOperation)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_ModeOfOperation";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.ModeOfOperation);
                        }
                        if (item.Pol != null)
                        {
                            foreach (var item1 in item.Pol)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Pol";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Pol);
                        }
                        if (item.Destination != null)
                        {
                            foreach (var item1 in item.Destination)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Destination";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Destination);
                        }
                    }
                }
                if (input.CarrierList != null)
                {
                    foreach (var item in input.CarrierList)
                    {
                        item.SopOrderOutId = mainid;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int carrId = DB.SqlSugarClient().Insertable<SopOrderOutCarrier>(item).ExecuteReturnIdentity();
                        /*字段所对应得附件*/
                        if (item.FileCarrier != null)
                        {
                            foreach (var item1 in item.FileCarrier)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                item1.RelationId = carrId;
                            }
                            files.AddRange(item.FileCarrier);
                        }
                    }
                }
                if (input.Import != null)
                {
                    input.Import.Id = 0;
                    input.Import.SopOrderId = mainid;
                    input.Import.Createuser = currentUser.Userid;
                    input.Import.Createdate = now;
                    input.Import.Companyid = currentUser.Ccode;
                    input.Import.Departmentid = currentUser.subCcode;
                    DB.SqlSugarClient().Insertable<SopOrderInDTO>(input.Import).ExecuteCommand();
                }
                else
                {
                    var saveData = new SopOrderInDTO()
                    {
                        SopOrderId = mainid,
                        Createuser = currentUser.Userid,
                        Createdate = now,
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderInDTO>(saveData).ExecuteCommand();
                }
                if (input.TrailerCustomsDeclaration != null)
                {
                    input.TrailerCustomsDeclaration.Id = 0;
                    input.TrailerCustomsDeclaration.SopOrderId = mainid;
                    input.TrailerCustomsDeclaration.Createuser = currentUser.Userid;
                    input.TrailerCustomsDeclaration.Createdate = now;
                    input.TrailerCustomsDeclaration.Companyid = currentUser.Ccode;
                    input.TrailerCustomsDeclaration.Departmentid = currentUser.subCcode;
                    int traId = DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(input.TrailerCustomsDeclaration).ExecuteReturnIdentity();
                    if (input.TrailerCustomsDeclaration.Region != null)
                    {
                        foreach (var item in input.TrailerCustomsDeclaration.Region)
                        {
                            item.Idx = 0;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = DateTime.Now;
                            item.Companyid = currentUser.Ccode;
                            item.RelationTableName = $"sop_order_trailer_declaration_Region";
                            item.RelationId = traId;
                        }
                        files.AddRange(input.TrailerCustomsDeclaration.Region);
                    }
                }
                else
                {
                    var saveData = new SopOrderTrailerDeclarationDTO()
                    {
                        SopOrderId = mainid,
                        Createuser = currentUser.Userid,
                        Createdate = now,
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                    };
                    DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(saveData).ExecuteCommand();
                }
                if (input.OrderAttachments != null)
                {
                    foreach (var item in input.OrderAttachments)
                    {
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_attachment";
                        item.RelationId = mainid;
                    }
                    files.AddRange(input.OrderAttachments);
                }
                if (files.Count() > 0)
                {
                    DB.SqlSugarClient().Insertable<FileManage>(files).ExecuteCommand();
                }
                DB.SqlSugarClient().CommitTran();
                return mainid;
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }
        public IActionResult OperatorsModify(SopOrderDetailsDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                if (input._state.ToUpper() == "SUBMIT")
                {
                    input.Status = "20";

                    /*记录操作日志*/
                    var appRecord = new SopOrderApproverRecord()
                    {
                        Companyid = currentUser.Ccode,
                        Departmentid = currentUser.subCcode,
                        ApprovalStatus = "10",//通过
                        ApprovalNode = "10",//提交节点
                        Approver = currentUser.Userid,
                        ApprovalTime = DateTime.Now
                    };
                    if (input.Id == 0)
                    {
                        var mainid = OperatorsSave(input);
                        if (mainid==0) 
                        {
                            DB.SqlSugarClient().RollbackTran();
                            return MstResult.Error("该单号已存在审批中的记录，请勿重复该操作");
                        }
                        /*生成合同模板，上传到minio*/
                        input.Id = mainid;
                        //OutputTemplateContent(input);
                        appRecord.SopOrderId = mainid;
                        DB.SqlSugarClient().Insertable<SopOrderApproverRecord>(appRecord).ExecuteCommand();
                        return MstResult.Success(mainid);
                    }
                    else
                    {
                        var any = DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.SopCode == input.SopCode && x.Status == "20" && x.Flag == true).Any();
                        if (any)
                        {
                            DB.SqlSugarClient().RollbackTran();
                            return MstResult.Error("该单号已存在审批中的记录，请勿重复该操作");
                        }
                        /*生成合同模板，上传到minio*/
                        //OutputTemplateContent(input);
                        appRecord.SopOrderId = input.Id;
                        DB.SqlSugarClient().Insertable<SopOrderApproverRecord>(appRecord).ExecuteCommand();
                    }
                }
                var now = DateTime.Now;
                List<SopOrderAttachmentDTO> attachments = new List<SopOrderAttachmentDTO>();
                //Pa联系人
                if (input.PreAlertList != null)
                {
                    var saveData = input.PreAlertList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.PreAlertList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1 && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopPaContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.Flag }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Flag = 1;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 1).ExecuteCommand();
                }
                //海外代理商联系方式
                if (input.OverseasAgentContactList != null)
                {
                    var saveData = input.OverseasAgentContactList.Where(x => x.Id == 0).ToList();
                    var modifyData = input.OverseasAgentContactList.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0 && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopPaContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.Flag }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Flag = 0;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == input.Id && x.Flag == 0).ExecuteCommand();
                }
                if (input.ContactInformations != null)
                {
                    var saveData = input.ContactInformations.Where(x => x.Id == 0).ToList();
                    var modifyData = input.ContactInformations.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var attaIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id && !attaIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopOrderContactDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderContactDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                }
                if (input.Quotations != null)
                {
                    foreach (var item in input.Quotations)
                    {
                        item.AttachmentType = "10";
                    }
                    attachments.AddRange(input.Quotations);
                }
                if (input.SettlementModes != null)
                {
                    foreach (var item in input.SettlementModes)
                    {
                        item.AttachmentType = "20";
                    }
                    attachments.AddRange(input.SettlementModes);
                }
                if (input.GuaranteeLetters != null)
                {
                    foreach (var item in input.GuaranteeLetters)
                    {
                        item.AttachmentType = "30";
                    }
                    attachments.AddRange(input.GuaranteeLetters);
                }
                if (input.HBLOrderTypes != null)
                {
                    foreach (var item in input.HBLOrderTypes)
                    {
                        item.AttachmentType = "40";
                    }
                    attachments.AddRange(input.HBLOrderTypes);
                }
                if (attachments.Count() > 0)
                {
                    var saveData = attachments.Where(x => x.Id == 0).ToList();
                    var modifyData = attachments.Where(x => x.Id != 0).ToList();
                    if (modifyData.Count() > 0)
                    {
                        var noDeleteIds = modifyData.Select(x => x.Id).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id && !noDeleteIds.Contains(x.Id)).ExecuteCommand();
                        foreach (var item in modifyData)
                        {
                            item.Modifier = currentUser.Userid;
                            item.Modifydate = now;
                        }
                        DB.SqlSugarClient().Updateable<SopOrderAttachmentDTO>(modifyData).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.AttachmentType }).ExecuteCommand();
                    }
                    else
                    {
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                    }
                    if (saveData.Count() > 0)
                    {
                        foreach (var item in saveData)
                        {
                            item.SopOrderId = input.Id;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(saveData).ExecuteCommand();
                    }
                }
                else
                {
                    DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.Id).ExecuteCommand();
                }
                /*
                出口   删除所有出口信息，添加新的出口信息
                附件统一添加
                */
                List<FileManage> files = new List<FileManage>();
                var dbOutIds = DB.SqlSugarClient().Queryable<SopOrderOut>().Where(x => x.SopOrderId == input.Id).Select(x => x.Id).ToList();
                if (dbOutIds.Count() > 0)
                {
                    List<string> exportTabNames = new List<string>()
                    {
                        "sop_order_out_TypeOfGoods",
                        "sop_order_out_ModeOfOperation",
                        "sop_order_out_Pol",
                        "sop_order_out_Destination"
                    };
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => dbOutIds.Contains(x.RelationId.Value) && exportTabNames.Contains(x.RelationTableName)).ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderOut>().Where(x => dbOutIds.Contains(x.Id)).ExecuteCommand();
                }
                if (input.Exports != null)
                {

                    foreach (var item in input.Exports)
                    {
                        item.Id = 0;
                        item.SopOrderId = input.Id;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int outid = DB.SqlSugarClient().Insertable<SopOrderOutDTO>(item).ExecuteReturnIdentity();
                        /*字段所对应的附件*/
                        if (item.TypeOfGoods != null)
                        {
                            foreach (var item1 in item.TypeOfGoods)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_TypeOfGoods";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.TypeOfGoods);
                        }
                        if (item.ModeOfOperation != null)
                        {
                            foreach (var item1 in item.ModeOfOperation)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_ModeOfOperation";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.ModeOfOperation);
                        }
                        if (item.Pol != null)
                        {
                            foreach (var item1 in item.Pol)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Pol";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Pol);
                        }
                        if (item.Destination != null)
                        {
                            foreach (var item1 in item.Destination)
                            {
                                item1.Idx = 0;
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_Destination";
                                item1.RelationId = outid;
                            }
                            files.AddRange(item.Destination);
                        }
                    }

                }
                /*
                 船信息   删除所有出口船信息  ，添加新的出口船信息  
                 */
                var dbCarrierIds = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Id).Select(x => x.Id).ToList();
                if (dbCarrierIds.Count() > 0)
                {
                    DB.SqlSugarClient().Deleteable<FileManage>().Where(x => dbCarrierIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ExecuteCommand();
                    DB.SqlSugarClient().Deleteable<SopOrderOutCarrier>().Where(x => dbCarrierIds.Contains(x.Id)).ExecuteCommand();
                }
                if (input.CarrierList != null)
                {
                    foreach (var item in input.CarrierList)
                    {
                        item.SopOrderOutId = input.Id;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = now;
                        item.Companyid = currentUser.Ccode;
                        item.Departmentid = currentUser.subCcode;
                        int carrId = DB.SqlSugarClient().Insertable<SopOrderOutCarrier>(item).ExecuteReturnIdentity();
                        /*字段所对应得附件*/
                        if (item.FileCarrier != null)
                        {
                            foreach (var item1 in item.FileCarrier)
                            {
                                item1.Createuser = currentUser.Userid;
                                item1.Createdate = DateTime.Now;
                                item1.Companyid = currentUser.Ccode;
                                item1.RelationTableName = $"sop_order_out_carrier_Carrier";
                                item1.RelationId = carrId;
                            }
                            files.AddRange(item.FileCarrier);
                        }
                    }

                }
                input.Import.Modifier = currentUser.Userid;
                input.Import.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderInDTO>(input.Import).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();

                //删除TrailerCustomsDeclaration 属性对应字段
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.TrailerCustomsDeclaration.Id && x.RelationTableName == "sop_order_trailer_declaration_Region").ExecuteCommand();
                if (input.TrailerCustomsDeclaration.Region != null)
                {
                    foreach (var item in input.TrailerCustomsDeclaration.Region)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_trailer_declaration_Region";
                        item.RelationId = input.TrailerCustomsDeclaration.Id;
                    }
                    files.AddRange(input.TrailerCustomsDeclaration.Region);
                }
                DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.Id && x.RelationTableName == "sop_order_attachment").ExecuteCommand();
                if (input.OrderAttachments != null)
                {
                    foreach (var item in input.OrderAttachments)
                    {
                        item.Idx = 0;
                        item.Createuser = currentUser.Userid;
                        item.Createdate = DateTime.Now;
                        item.Companyid = currentUser.Ccode;
                        item.RelationTableName = $"sop_order_attachment";
                        item.RelationId = input.Id;
                    }
                    files.AddRange(input.OrderAttachments);
                }
                if (files.Count() > 0)
                {
                    DB.SqlSugarClient().Insertable<FileManage>(files).ExecuteCommand();
                }
                input.TrailerCustomsDeclaration.Modifier = currentUser.Userid;
                input.TrailerCustomsDeclaration.Modifydate = now;
                DB.SqlSugarClient().Updateable<SopOrderTrailerDeclarationDTO>(input.TrailerCustomsDeclaration).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser }).ExecuteCommand();
                input.Modifier = currentUser.Userid;
                input.Modifydate = DateTime.Now;
                DB.SqlSugarClient().Updateable<SopOrderDetailsDTO>(input).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.SopCode }).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }

        public IActionResult OperatorsApprovalResults(SopOrderApproverRecordDTO input) 
        {
            try
            {
                DB.SqlSugarClient().BeginTran();
                var any = DB.SqlSugarClient().Queryable<SopOrder>().Where(x => x.Id == input.SopOrderId && x.Status == "20").Any();
                if (!any)
                {
                    DB.SqlSugarClient().RollbackTran();
                    return MstResult.Error("此状态下不支持审核操作！");
                }
                string status = "";
                if (input.ApprovalStatus == "10")//通过
                {
                    status = "30";//审批通过
                    /*
                     1.修改数据
                     2.生成新的版本
                     */
                    var sopOrderDetail = DB.SqlSugarClient().Queryable<SopOrderDetailsDTO>().Where(x => x.Id == input.SopOrderId).First();
                    if (sopOrderDetail != null)
                    {
                        var strOrder = JsonConvert.SerializeObject(sopOrderDetail);
                        var entityOrder = JsonConvert.DeserializeObject<SopOrderDetailsDTO>(strOrder);
                        entityOrder.Id = sopOrderDetail.PrimaryId;
                        entityOrder.Modifier = currentUser.Userid;
                        entityOrder.Modifydate = DateTime.Now;
                        DB.SqlSugarClient().Updateable<SopOrderDetailsDTO>(entityOrder).IgnoreColumns(x => new { x.Companyid, x.Departmentid, x.Createdate, x.Createuser, x.PrimaryId, x.Flag, x.ModifySopCode,x.Status,x.SopCode }).ExecuteCommand();
                        //pa联系人
                        var contacts=DB.SqlSugarClient().Queryable<SopPaContactDTO>().Where(x => x.SopOrderId == input.SopOrderId).ToList();
                        sopOrderDetail.PreAlertList = contacts.Where(x => x.Flag == 1).ToList();
                        if (sopOrderDetail.PreAlertList.Count() > 0)
                        {
                            DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == entityOrder.Id&&x.Flag==1).ExecuteCommand();
                            foreach (var item in sopOrderDetail.PreAlertList)
                            {
                                item.Id = 0;
                                item.SopOrderId = entityOrder.Id;
                                item.Flag = 1;
                            }
                            DB.SqlSugarClient().Insertable<SopPaContactDTO>(sopOrderDetail.PreAlertList).ExecuteCommand();
                        }
                        else
                        {
                            DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == entityOrder.Id&&x.Flag==1).ExecuteCommand();
                        }
                        //海外代理商联系方式
                        sopOrderDetail.OverseasAgentContactList = contacts.Where(x => x.Flag == 0).ToList();
                        if (sopOrderDetail.OverseasAgentContactList.Count() > 0)
                        {
                            DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == entityOrder.Id && x.Flag == 0).ExecuteCommand();
                            foreach (var item in sopOrderDetail.PreAlertList)
                            {
                                item.Id = 0;
                                item.SopOrderId = entityOrder.Id;
                                item.Flag = 0;
                            }
                            DB.SqlSugarClient().Insertable<SopPaContactDTO>(sopOrderDetail.OverseasAgentContactList).ExecuteCommand();
                        }
                        else
                        {
                            DB.SqlSugarClient().Deleteable<SopPaContactDTO>().Where(x => x.SopOrderId == entityOrder.Id && x.Flag == 0).ExecuteCommand();
                        }

                        //联系人
                        sopOrderDetail.ContactInformations = DB.SqlSugarClient().Queryable<SopOrderContactDTO>().Where(x => x.SopOrderId == input.SopOrderId).ToList();
                        if (sopOrderDetail.ContactInformations.Count() > 0)
                        {
                            DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == entityOrder.Id).ExecuteCommand();
                            foreach (var item in sopOrderDetail.ContactInformations)
                            {
                                item.Id = 0;
                                item.SopOrderId = entityOrder.Id;
                            }
                            DB.SqlSugarClient().Insertable<SopOrderContactDTO>(sopOrderDetail.ContactInformations).ExecuteCommand();
                        }
                        else
                        {
                            DB.SqlSugarClient().Deleteable<SopOrderContactDTO>().Where(x => x.SopOrderId == entityOrder.Id).ExecuteCommand();
                        }
                        List<string> codes = new List<string>() { "10", "20" };
                        var attachments = DB.SqlSugarClient().Queryable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.SopOrderId && codes.Contains(x.AttachmentType)).ToList();
                        DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == entityOrder.Id && codes.Contains(x.AttachmentType)).ExecuteCommand();
                        //处理报价单
                        sopOrderDetail.Quotations = attachments.Where(x => x.AttachmentType == "10").ToList();
                        if (sopOrderDetail.Quotations.Count() > 0)
                        {
                            foreach (var item in sopOrderDetail.Quotations)
                            {
                                item.Id = 0;
                                item.SopOrderId = entityOrder.Id;
                            }
                            DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(sopOrderDetail.Quotations).ExecuteCommand();
                        }
                        //处理结算方式
                        sopOrderDetail.SettlementModes = attachments.Where(x => x.AttachmentType == "20").ToList();
                        if (sopOrderDetail.SettlementModes.Count() > 0)
                        {
                            foreach (var item in sopOrderDetail.SettlementModes)
                            {
                                item.Id = 0;
                                item.SopOrderId = entityOrder.Id;
                            }
                            DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(sopOrderDetail.SettlementModes).ExecuteCommand();
                        }
                        if (sopOrderDetail.OwerType == "20")//海运
                        {
                            codes = new List<string>() { "30", "40" };
                            attachments = DB.SqlSugarClient().Queryable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == input.SopOrderId && codes.Contains(x.AttachmentType)).ToList();
                            DB.SqlSugarClient().Deleteable<SopOrderAttachmentDTO>().Where(x => x.SopOrderId == entityOrder.Id && codes.Contains(x.AttachmentType)).ExecuteCommand();
                            //处理长期放货保涵
                            sopOrderDetail.GuaranteeLetters = attachments.Where(x => x.AttachmentType == "30").ToList();
                            if (sopOrderDetail.GuaranteeLetters.Count() > 0)
                            {
                                foreach (var item in sopOrderDetail.GuaranteeLetters)
                                {
                                    item.Id = 0;
                                    item.SopOrderId = entityOrder.Id;
                                }
                                DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(sopOrderDetail.GuaranteeLetters).ExecuteCommand();
                            }
                            //处理HBL出单类型
                            sopOrderDetail.HBLOrderTypes = attachments.Where(x => x.AttachmentType == "40").ToList();
                            if (sopOrderDetail.HBLOrderTypes.Count() > 0)
                            {
                                foreach (var item in sopOrderDetail.HBLOrderTypes)
                                {
                                    item.Id = 0;
                                    item.SopOrderId = entityOrder.Id;
                                }
                                DB.SqlSugarClient().Insertable<SopOrderAttachmentDTO>(sopOrderDetail.HBLOrderTypes).ExecuteCommand();
                            }
                        }
                        var files = new List<FileManage>();

                        /*处理出口*/
                        //1.查出新（input.SopOrderId）旧（entityOrder.Id）数据
                        var outData = DB.SqlSugarClient().Queryable<SopOrderOutDTO>().Where(x => x.SopOrderId == input.SopOrderId || x.SopOrderId == entityOrder.Id).ToList();
                        //2.删除旧Out数据
                        var oldOutData= outData.Where(x => x.SopOrderId == entityOrder.Id).ToList();
                        List<string> exportTabNames = new List<string>()
                            {
                                "sop_order_out_TypeOfGoods",
                                "sop_order_out_ModeOfOperation",
                                "sop_order_out_Pol",
                                "sop_order_out_Destination"
                            };
                        if (oldOutData.Count() > 0)
                        {
                            var outIds = oldOutData.Select(x => x.Id).ToList();
                            DB.SqlSugarClient().Deleteable<SopOrderOutDTO>().Where(x => x.SopOrderId == entityOrder.Id).ExecuteCommand();
                            //删除旧Out对应附件
                            var outFiles = DB.SqlSugarClient().Queryable<FileManage>().Where(x => outIds.Contains(x.RelationId.Value) && exportTabNames.Contains(x.RelationTableName)).ToList();
                        }

                        //3.添加新Out数据
                        var exports = outData.Where(x => x.SopOrderId == input.SopOrderId).ToList();
                        if (exports.Count() > 0)
                        {
                            //查出字段对应附件
                            var outIds = exports.Select(x => x.Id).ToList();

                            var outFiles = DB.SqlSugarClient().Queryable<FileManage>().Where(x => outIds.Contains(x.RelationId.Value) && exportTabNames.Contains(x.RelationTableName)).ToList();
                            if (outFiles.Count() > 0)
                            {
                                //先绑定自己附件
                                foreach (var item in exports)
                                {
                                    item.TypeOfGoods = outFiles.Where(x => x.RelationId == item.Id && x.RelationTableName == "sop_order_out_TypeOfGoods").ToList();
                                    item.ModeOfOperation = outFiles.Where(x => x.RelationId == item.Id && x.RelationTableName == "sop_order_out_ModeOfOperation").ToList();
                                    item.Pol = outFiles.Where(x => x.RelationId == item.Id && x.RelationTableName == "sop_order_out_Pol").ToList();
                                    item.Destination = outFiles.Where(x => x.RelationId == item.Id && x.RelationTableName == "sop_order_out_Destination").ToList();
                                }
                                //添加新Out数据和字段对应附件数据

                                foreach (var item in exports)
                                {
                                    item.Id = 0;
                                    item.SopOrderId = entityOrder.Id;
                                    int outid = DB.SqlSugarClient().Insertable<SopOrderOutDTO>(item).ExecuteReturnIdentity();
                                    if (item.TypeOfGoods.Count() > 0)
                                    {
                                        foreach (var item1 in item.TypeOfGoods)
                                        {
                                            item1.Idx = 0;
                                            item1.RelationId = outid;
                                        }
                                        files.AddRange(item.TypeOfGoods);
                                    }
                                    if (item.ModeOfOperation.Count() > 0)
                                    {
                                        foreach (var item1 in item.ModeOfOperation)
                                        {
                                            item1.Idx = 0;
                                            item1.RelationId = outid;
                                        }
                                        files.AddRange(item.ModeOfOperation);
                                    }
                                    if (item.Pol.Count() > 0)
                                    {
                                        foreach (var item1 in item.Pol)
                                        {
                                            item1.Idx = 0;
                                            item1.RelationId = outid;
                                        }
                                        files.AddRange(item.Pol);
                                    }
                                    if (item.Destination.Count() > 0)
                                    {
                                        foreach (var item1 in item.Destination)
                                        {
                                            item1.Idx = 0;
                                            item1.RelationId = outid;
                                        }
                                        files.AddRange(item.Destination);
                                    }
                                }


                            }
                            else
                            {
                                foreach (var item in exports)
                                {
                                    item.Id = 0;
                                    item.SopOrderId = entityOrder.Id;
                                    
                                }
                                DB.SqlSugarClient().Insertable<SopOrderOutDTO>(exports).ExecuteCommand();
                            }
                        }
                        entityOrder.Exports = exports;
                        /*处理船信息*/
                        //1.查出船（input.SopOrderId）旧（entityOrder.Id）数据
                        var carrierData = DB.SqlSugarClient().Queryable<SopOrderOutCarrierDTO>().Where(x => x.SopOrderOutId == input.SopOrderId || x.SopOrderOutId == entityOrder.Id).ToList();
                        
                        //2.删除旧船数据

                        var oldCarrierData = carrierData.Where(x => x.SopOrderOutId == entityOrder.Id).ToList();
                        if (oldCarrierData.Count() > 0)
                        {
                            var oldIds= oldCarrierData.Select(x => x.Id).ToList();
                            DB.SqlSugarClient().Deleteable<FileManage>().Where(x => oldIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ExecuteCommand();
                            DB.SqlSugarClient().Deleteable<SopOrderOutCarrierDTO>().Where(x => oldIds.Contains(x.Id)).ExecuteCommand();
                        }
                        //添加新船数据
                        var carrierList = carrierData.Where(x => x.SopOrderOutId == input.SopOrderId).ToList();
                        if (carrierList.Count() > 0)
                        {
                            //找出新船字段对应附件数据
                            //查出字段对应附件
                            var carrierIds = exports.Select(x => x.Id).ToList();
                            var outFiles = DB.SqlSugarClient().Queryable<FileManage>().Where(x => carrierIds.Contains(x.RelationId.Value) && x.RelationTableName == "sop_order_out_carrier_Carrier").ToList();

                            if (outFiles.Count() > 0)
                            {
                                //先绑定自己附件
                                foreach (var item in carrierList)
                                {
                                    item.FileCarrier = outFiles.Where(x => x.RelationId == item.Id).ToList();
                                }
                                //添加新Carrier数据和字段对应附件数据
                                foreach (var item in carrierList)
                                {
                                    item.Id = 0;
                                    item.SopOrderOutId = entityOrder.Id;
                                    int carrierId = DB.SqlSugarClient().Insertable<SopOrderOutCarrierDTO>(item).ExecuteReturnIdentity();
                                    if (item.FileCarrier != null)
                                    {
                                        foreach (var item1 in item.FileCarrier)
                                        {
                                            item1.Idx = 0;
                                            item1.RelationId = carrierId;
                                        }
                                        files.AddRange(item.FileCarrier);
                                    }
                                }
                            }
                            else 
                            {
                                foreach (var item in carrierList)
                                {
                                    item.Id = 0;
                                    item.SopOrderOutId = entityOrder.Id;
                                }
                                DB.SqlSugarClient().Insertable<SopOrderOutCarrierDTO>(carrierList).ExecuteCommand();
                            }
                        }
                        sopOrderDetail.CarrierList = carrierList;
                        /*处理进口*/
                        sopOrderDetail.Import = DB.SqlSugarClient().Queryable<SopOrderInDTO>().Where(x => x.SopOrderId == input.SopOrderId).First();
                        sopOrderDetail.Import.Id = 0;
                        sopOrderDetail.Import.SopOrderId = entityOrder.Id;
                        DB.SqlSugarClient().Deleteable<SopOrderInDTO>().Where(x => x.SopOrderId == entityOrder.Id).ExecuteCommand();
                        DB.SqlSugarClient().Insertable<SopOrderInDTO>(sopOrderDetail.Import).ExecuteCommand();

                        /*处理拖车+报关*/
                        var trailerCustomsDeclaration = DB.SqlSugarClient().Queryable<SopOrderTrailerDeclarationDTO>().Where(x => x.SopOrderId == input.SopOrderId).First();
                        trailerCustomsDeclaration.Id = 0;
                        trailerCustomsDeclaration.SopOrderId = entityOrder.Id;
                        DB.SqlSugarClient().Deleteable<SopOrderTrailerDeclarationDTO>().Where(x => x.SopOrderId == entityOrder.Id).ExecuteCommand();
                        int tdrId=DB.SqlSugarClient().Insertable<SopOrderTrailerDeclarationDTO>(trailerCustomsDeclaration).ExecuteReturnIdentity();
                        //删除字段对应附件
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == entityOrder.Id && x.RelationTableName == "sop_order_trailer_declaration_Region").ExecuteCommand();
                        //添加字段对应附件
                        var tdrFiles = DB.SqlSugarClient().Queryable<FileManage>().Where(x => x.RelationId == input.SopOrderId && x.RelationTableName == "sop_order_trailer_declaration_Region").ToList();
                        if (tdrFiles.Count > 0)
                        {
                            foreach (var item in tdrFiles)
                            {
                                item.Idx = 0;
                                item.RelationId= tdrId;
                            }
                            files.AddRange(tdrFiles);
                        }
                        sopOrderDetail.TrailerCustomsDeclaration = trailerCustomsDeclaration;
                        if (files.Count() > 0)
                        {
                            DB.SqlSugarClient().Insertable<FileManage>(files).ExecuteCommand();
                        }
                        /*处理order附件*/
                        DB.SqlSugarClient().Deleteable<FileManage>().Where(x => x.RelationId == input.Id && x.RelationTableName == "sop_order_attachment").ExecuteCommand();
                        var orderAttachments = DB.SqlSugarClient().Queryable<FileManage>().Where(x => x.RelationId == input.SopOrderId && x.RelationTableName == "sop_order_attachment").ToList();
                        if (orderAttachments.Count()>0)
                        {
                            foreach (var item in orderAttachments)
                            {
                                item.Idx = 0;
                                item.Createuser = currentUser.Userid;
                                item.Createdate = DateTime.Now;
                                item.Companyid = currentUser.Ccode;
                                item.RelationTableName = $"sop_order_attachment";
                                item.RelationId = entityOrder.Id;
                            }
                            DB.SqlSugarClient().Insertable<FileManage>(orderAttachments).ExecuteCommand();
                        }
                        sopOrderDetail.Id = sopOrderDetail.PrimaryId;
                        OutputTemplateContent(sopOrderDetail);

                    }
                    else 
                    {
                        return MstResult.Error("数据为空！");

                    }
                }
                else if (input.ApprovalStatus == "20")//驳回
                {
                    status = "10";//未提交
                }
                else if (input.ApprovalStatus == "30")//作废
                {
                    status = "40";//作废
                }
                else
                {
                    DB.SqlSugarClient().RollbackTran();
                    return MstResult.Error("入参错误");
                }
                DB.SqlSugarClient().Updateable<SopOrder>().SetColumns(x => x.Status == status).Where(x => x.Id == input.SopOrderId).ExecuteCommand();
                input.Companyid = currentUser.Ccode;
                input.Departmentid = currentUser.subCcode;
                //ApprovalStatus = "10";//通过
                //ApprovalNode = "10";//提交节点
                input.Approver = currentUser.Userid;
                input.ApprovalTime = DateTime.Now;
                DB.SqlSugarClient().Insertable<SopOrderApproverRecordDTO>(input).ExecuteCommand();
                DB.SqlSugarClient().CommitTran();
                return MstResult.Success("操作成功");
            }
            catch (Exception ex)
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }

        }

        public IActionResult CopyOrderData(IdPublicInput input) 
        {
            try 
            {
                DB.SqlSugarClient().BeginTran();
                var sopOrder=DB.SqlSugarClient().Queryable<SopOrder>().Where(x=>x.Id==input.Id).First();
                if (sopOrder != null) 
                {
                    var now = DateTime.Now;
                    /*复制主表信息*/
                    sopOrder.Id = 0;
                    sopOrder.Companyid = currentUser.Ccode;
                    sopOrder.Departmentid = currentUser.subCcode;
                    sopOrder.Createuser = currentUser.Userid;
                    sopOrder.Createdate = now;
                    sopOrder.Remark = "";
                    sopOrder.Status = "10";
                    sopOrder.Modifier = "";
                    sopOrder.Modifydate = null;
                    sopOrder.IsPublish= false;
                    sopOrder.PublishMember = "";
                    sopOrder.PublishTime = null;
                    sopOrder.VisibleMember = "";
                    MstCore.Helper.CodeParams codeParams = new MstCore.Helper.CodeParams();
                    codeParams.codeRule = $"SOP&CURYEAR2&CURMONTH&____";
                    codeParams.fldName = "sop_base_code";
                    codeParams.tblName = "sop_base_header";
                    var codes = VariableModelTool.GetCode(codeParams);
                    sopOrder.SopCode = codes;
                    /*var strOrder = JsonConvert.SerializeObject(sopOrder);
                    var entityOrder = JsonConvert.DeserializeObject<SopOrder>(strOrder);*/

                    int orderId=DB.SqlSugarClient().Insertable<SopOrder>(sopOrder).ExecuteReturnIdentity();
                    /*复制联系人*/
                    var sopOrderContacts = DB.SqlSugarClient().Queryable<SopOrderContact>().Where(x => x.SopOrderId == input.Id).ToList();
                    if (sopOrderContacts.Count()>0) 
                    {
                        foreach (var item in sopOrderContacts) 
                        {
                            item.Id = 0;
                            item.SopOrderId = orderId;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Modifier = "";
                            item.Modifydate = null;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderContact>(sopOrderContacts).ExecuteCommand();
                    }
                    /*复制pa联系人和海外代理联系人*/
                    var sopPaContacts = DB.SqlSugarClient().Queryable<SopPaContact>().Where(x => x.SopOrderId == input.Id).ToList();
                    if (sopPaContacts.Count()>0) 
                    {
                        foreach (var item in sopPaContacts) 
                        {
                            item.Id = 0;
                            item.SopOrderId = orderId;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Modifier = "";
                            item.Modifydate = null;
                        }
                        DB.SqlSugarClient().Insertable<SopPaContact>(sopPaContacts).ExecuteCommand();
                    }
                    /*处理报价单  处理结算方式   长期放货保涵   HBL出单类型*/
                    var sopOrderAttachments = DB.SqlSugarClient().Queryable<SopOrderAttachment>().Where(x => x.SopOrderId == input.Id).ToList();
                    if (sopOrderAttachments.Count()>0) 
                    {
                        foreach (var item in sopOrderAttachments) 
                        {
                            item.Id = 0;
                            item.SopOrderId = orderId;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Modifier = "";
                            item.Modifydate = null;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderAttachment>(sopOrderAttachments).ExecuteCommand();
                    }
                    /*处理进口*/
                    var sopOrderIn = DB.SqlSugarClient().Queryable<SopOrderIn>().Where(x => x.SopOrderId == input.Id).First();
                    if (sopOrderIn != null)
                    {
                        sopOrderIn.Id = 0;
                        sopOrderIn.SopOrderId = orderId;
                        sopOrderIn.Createuser = currentUser.Userid;
                        sopOrderIn.Createdate = now;
                        sopOrderIn.Companyid = currentUser.Ccode;
                        sopOrderIn.Departmentid=currentUser.subCcode;
                        sopOrderIn.Modifier = "";
                        sopOrderIn.Modifydate = null;
                        DB.SqlSugarClient().Insertable<SopOrderIn>(sopOrderIn).ExecuteCommand();
                    }
                    else 
                    {
                        SopOrderIn orderIn = new SopOrderIn()
                        {
                            SopOrderId=orderId,
                            Companyid=currentUser.Ccode,
                            Departmentid=currentUser.subCcode,
                            Createdate=now,
                            Createuser=currentUser.Userid,
                        };
                        DB.SqlSugarClient().Insertable<SopOrderIn>(orderIn).ExecuteCommand();
                    }
                    /*处理拖车+报关*/
                    var sopOrderTrailerDeclaration = DB.SqlSugarClient().Queryable<SopOrderTrailerDeclaration>().Where(x => x.SopOrderId == input.Id).First();
                    if (sopOrderTrailerDeclaration != null)
                    {
                        sopOrderTrailerDeclaration.Id = 0;
                        sopOrderTrailerDeclaration.SopOrderId = orderId;
                        sopOrderTrailerDeclaration.Createuser = currentUser.Userid;
                        sopOrderTrailerDeclaration.Createdate = now;
                        sopOrderTrailerDeclaration.Companyid = currentUser.Ccode;
                        sopOrderTrailerDeclaration.Departmentid = currentUser.subCcode;
                        sopOrderTrailerDeclaration.Modifier = "";
                        sopOrderTrailerDeclaration.Modifydate = null;
                        DB.SqlSugarClient().Insertable<SopOrderTrailerDeclaration>(sopOrderTrailerDeclaration).ExecuteCommand();
                    }
                    else 
                    {
                        SopOrderTrailerDeclaration soptradec= new SopOrderTrailerDeclaration()
                        { 
                            SopOrderId = orderId,
                            Companyid = currentUser.Ccode,
                            Departmentid = currentUser.subCcode,
                            Createdate = now,
                            Createuser = currentUser.Userid,
                        };
                        DB.SqlSugarClient().Insertable<SopOrderTrailerDeclaration>(soptradec).ExecuteCommand();
                    }
                    /*处理出口*/
                    var sopOrderOuts = DB.SqlSugarClient().Queryable<SopOrderOut>().Where(x => x.SopOrderId == input.Id).ToList();
                    if (sopOrderOuts.Count()>0)
                    {
                        foreach (var item in sopOrderOuts)
                        {
                            item.Id = 0;
                            item.SopOrderId = orderId;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Modifier = "";
                            item.Modifydate = null;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderOut>(sopOrderOuts).ExecuteCommand();
                    }
                    /*处理船公司*/
                    var sopOrderOutCarriers = DB.SqlSugarClient().Queryable<SopOrderOutCarrier>().Where(x => x.SopOrderOutId == input.Id).ToList();
                    if (sopOrderOutCarriers.Count() > 0)
                    {
                        foreach (var item in sopOrderOutCarriers) 
                        {
                             item.Id = 0;
                            item.SopOrderOutId = orderId;
                            item.Createuser = currentUser.Userid;
                            item.Createdate = now;
                            item.Companyid = currentUser.Ccode;
                            item.Departmentid = currentUser.subCcode;
                            item.Modifier = "";
                            item.Modifydate = null;
                        }
                        DB.SqlSugarClient().Insertable<SopOrderOutCarrier>(sopOrderOutCarriers).ExecuteCommand();
                    }
                    DB.SqlSugarClient().CommitTran();
                    return MstResult.Success("复制成功");
                }
                DB.SqlSugarClient().RollbackTran();
                return MstResult.Error("找不到数据，复制失败！");
            }catch(Exception ex) 
            {
                DB.SqlSugarClient().RollbackTran();
                throw new Exception(ex.Message);
            }
        }

        public IActionResult ObtainDropdownDataForSeaFreightImports() 
        {
            try 
            {
                /*
                 获取海运进口的船数据 id:21432
                 获取海运进口的货物类型（带出子集，有子集的不需要返回自己本身）id:21434
                 */
                var outData = new ShippingImportDropdownDataOut();
                var shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21432).ToList();
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
                    outData.Carriers = sfdDicts;
                }
                var baseTrees = DB.SqlSugarClient().Queryable<SopBaseTreeDTO>().ToTree(it => it.Subsets, it => it.Pid, 21434);
                if (baseTrees != null)
                {
                    var sopOutBaseTrees=new List<SopBaseTreeDTO>();
                    SubsetRemovalItself(baseTrees, ref sopOutBaseTrees);
                    var sfdDicts = sopOutBaseTrees.Map(it => new DictsSpare()
                    {
                        Idx = it.Idx,
                        Dictid = "TypeOfGoods",
                        Code = it.Idx.ToString(),
                        Cname = it.SopName,
                        Ename = it.SopNameen
                    });
                    outData.GoodsTypes= sfdDicts;
                }
                return MstResult.Success(outData);

            } catch (Exception ex) 
            {
                throw new Exception(ex.Message);
            }
        }
        private void SubsetRemovalItself(List<SopBaseTreeDTO> sopBaseTrees,ref List<SopBaseTreeDTO> sopOutBaseTrees) 
        {
            foreach (var item in sopBaseTrees) 
            {
                if (item.Subsets.Count()==0)
                {
                    sopOutBaseTrees.Add(item);
                }
                else 
                {
                    SubsetRemovalItself(item.Subsets,ref sopOutBaseTrees);
                }
            }
        }
        /// <summary>
        /// 格式化Img标签
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string FixImgTags(string html)
        {
            // 匹配所有没有结束符的<img>标签
            string pattern = @"<img[^>]*>";
            string fixedHtml = Regex.Replace(html, pattern, match =>
            {
                string imgTag = match.Value;
                if (!imgTag.EndsWith("/>"))
                {
                    imgTag = imgTag.TrimEnd('>') + "/>";
                }
                return imgTag;
            });

            return fixedHtml;
        }
        /// <summary>
        /// 转换Rgb
        /// </summary>
        /// <param name="rgb"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static string RGBStringToHex(string rgb)
        {
            // 使用正则表达式提取RGB值
            MatchCollection matches = Regex.Matches(rgb, @"\d+");

            if (matches.Count != 3)
            {
                throw new ArgumentException("Invalid RGB string format");
            }

            int red = int.Parse(matches[0].Value);
            int green = int.Parse(matches[1].Value);
            int blue = int.Parse(matches[2].Value);

            // 将RGB值转换为十六进制
            string hexColor = $"{red:X2}{green:X2}{blue:X2}";

            return hexColor;
        }
        
        private string OutputTemplateContent(SopOrderDetailsDTO input)
        {
            //区分是submit还是preview操作
            bool sign = input._state == "preview" ? true : false;
            string appRoot = AppContext.BaseDirectory;
            string temporaryFiles = "";
            if (sign)
            {
                string previewFiles = Path.Combine(appRoot, "PreviewFiles");
                if (!Directory.Exists(previewFiles))
                {
                    //创建文件夹
                    Directory.CreateDirectory(previewFiles);
                }
                temporaryFiles = Path.Combine(previewFiles, currentUser.Userid);
                if (!Directory.Exists(temporaryFiles))
                {
                    //创建文件夹
                    Directory.CreateDirectory(temporaryFiles);
                }
                // 获取所有文件路径
                string[] files = Directory.GetFiles(temporaryFiles);
                // 删除每个文件
                foreach (string item in files)
                {
                    File.Delete(item);
                }

            }
            else
            {
                temporaryFiles = Path.Combine(appRoot, "TemporaryFiles");
                if (!Directory.Exists(temporaryFiles))
                {
                    //创建文件夹
                    Directory.CreateDirectory(temporaryFiles);
                }
            }
            //获取模板路径
            bool isEn = false;
            if (lang == "en_US")
            {
                isEn = true;
            }
            string TemplateNmae = "";
            if (input.OwerType=="20") 
            {
                TemplateNmae=Path.Combine(appRoot, "SOPTemplateCN.docx");
            }
            else 
            {
                TemplateNmae = Path.Combine(appRoot, "SOPAirTemplateCN.docx");
            }
             
            FileInfo file = new FileInfo(TemplateNmae);
            var fileName = $"{input.Id}{System.DateTime.Now.ToString("yyyyMMddHHmmssfffffff")}.docx";
            var temporaryName = Path.Combine(temporaryFiles, fileName);
            file.CopyTo(temporaryName, true);
            WordprocessingDocument wordDoc = WordprocessingDocument.Open(temporaryName, true);
            var body = wordDoc.MainDocumentPart!.Document.Body;
            #region 用类属性和模板需要替换内容对应
            var cnData = _codeNameConversion.GetCodeNamesDatas();//code and Name
            //List<string> codes = new List<string>() { "2500", "2700", "2800", "2900","3100", "3200", "3300", "4100", "4200", "3500", "3600", "3700", "3800", "3900", "4000", "4100" };
            List<string> codes = new List<string>() { "2500", "2700", "2800", "3100", "3200","3300", "3500", "3600", "3700", "3800", "3900", "4000", "4400" };
            var dics = DB.SqlSugarClient().Queryable<Dictinfo>().Where(x => codes.Contains(x.Dictid)).ToList();
            var pids=new List<int>();
            //货物类型-->2900,3200   pod-->4100   pod-->4200    
            if (input.OwerType == "10")//空运
            {
                //pol:21545，pod:21546,typeofgoods:21552
                pids = new List<int>() {21545, 21546 , 21552 };
            }
            else //海运
            {
               
                //21432:进口船名,Ship:21477,POL:21453,pod:21457,typeofgoods:21486
                pids = new List<int>() { 21432,21446, 21477, 21453, 21457, 21486 };
            }
            var bases = DB.SqlSugarClient().Queryable<SopBase>().Where(x => pids.Contains(x.Pid.Value)).Select(x=>new {Id=x.Idx,SopName=x.SopName, SopNameen = x.SopNameen }).ToList();
            //动态替换文本内容
            var temPro = new TemplatePropertiesDTO()
            {
                DicisinoMaker = input.CustomerName == null ? "" : input.CustomerName,
                Consignee = input.ConsigneeEn == null ? "" : input.ConsigneeEn,
                Shippercn = input.ShipperCh == null ? "" : input.ShipperCh,
                Shipperen = input.ShipperEn == null ? "" : input.ShipperEn,
                //ContactInformation = "",//联系人
                Salesman = input.Salesid == null ? "" : input.Salesid,//销售人员
                Procurement = input.EId == null ? "" : input.EId,//运价
                Cs = input.CsId == null ? "" : input.CsId,//操作人员
                BusinessType = input.BizType == null ? "" : input.BizType,//业务类型 2500
                TradeTerms = input.Incoterm == null ? "" : input.Incoterm,//贸易条款
                Balance = input.PayType == null ? "" : input.PayType,//结算类型 2700
                Protocol = input.HblOrderType == null ? "" : input.HblOrderType,//协议 2800


                /*CarrierList = "",//船公司名sss
                TypeOfGoods = input.Export.GoodsType == null ? "" : input.Export.GoodsType,//货物类型 2900
                ModeOfOperation = input.Export.OpMode == null ? "" : input.Export.OpMode,//操作模式 3300
                DestinationPort = input.Export.Origin == null ? "" : input.Export.Origin,//港口申报-->起运港 4100
                PortOfDestination = input.Export.DestContury == null ? "" : input.Export.DestContury, //港口申报-->目的国 4200(多选)
                OperationMode = input.Import.OpMode == null ? "" : input.Import.OpMode,//操作模式 3500
                ImportMode = input.Import.InType == null ? "" : input.Import.InType,//进口模式
                PortDeclaration = input.Import.DeclarationMethod == null ? "" : input.Import.DeclarationMethod,//港口申报 4000
                RequirementType = input.TrailerCustomsDeclaration.RequirementType == null ? "" : input.TrailerCustomsDeclaration.RequirementType,//拖车报关 3600
                Region = input.TrailerCustomsDeclaration.Origin == null ? "" : input.TrailerCustomsDeclaration.Origin,//起运地 3700
                CustomsDeclarationType = input.TrailerCustomsDeclaration.DeclarationMethod == null ? "" : input.TrailerCustomsDeclaration.DeclarationMethod//报关类型 3900*/
            };
           /* if (!string.IsNullOrEmpty(temPro.Salesman))
            {
                temPro.Salesman = cnData.Where(x => x.UserId == temPro.Salesman).FirstOrDefault()?.NameEn;
            }*/
            if (!string.IsNullOrEmpty(temPro.Procurement))
            {
                var procurementArr = temPro.Procurement.Split(",");
                foreach (var procurement in procurementArr)
                {
                    var procurementName = cnData.Where(x => x.UserId == procurement).FirstOrDefault()?.NameEn;
                    temPro.Procurement = temPro.Procurement.Replace(procurement, procurementName);
                }
                


            }
            if (!string.IsNullOrEmpty(temPro.Cs))
            {
                temPro.Cs = cnData.Where(x => x.UserId == temPro.Cs).FirstOrDefault()?.NameEn;
            }
            if (!string.IsNullOrEmpty(temPro.BusinessType))
            {
                var bizTypeArr = temPro.BusinessType.Split(",");
                foreach (var bizType in bizTypeArr)
                {
                    if (isEn)
                    {
                        var typeName = dics.Where(x => x.Dictid == "2500" && x.Code == bizType).FirstOrDefault()?.Ename;
                        temPro.BusinessType = temPro.BusinessType.Replace(bizType, typeName);
                    }
                    else 
                    {
                        var typeName = dics.Where(x => x.Dictid == "2500" && x.Code == bizType).FirstOrDefault()?.Cname;
                        temPro.BusinessType = temPro.BusinessType.Replace(bizType, typeName);
                    }
                    
                    
                }
            }
            if (!string.IsNullOrEmpty(temPro.Balance))
            {
                if (isEn)
                {
                    temPro.Balance = dics.Where(x => x.Dictid == "2700" && x.Code == temPro.Balance).FirstOrDefault()?.Ename;
                }
                else 
                {
                    temPro.Balance = dics.Where(x => x.Dictid == "2700" && x.Code == temPro.Balance).FirstOrDefault()?.Cname;
                }
            }
            if (!string.IsNullOrEmpty(temPro.Protocol))
            {
                if (isEn)
                {
                    temPro.Protocol = dics.Where(x => x.Dictid == "2800" && x.Code == temPro.Protocol).FirstOrDefault()?.Ename;
                }
                else 
                {
                    temPro.Protocol = dics.Where(x => x.Dictid == "2800" && x.Code == temPro.Protocol).FirstOrDefault()?.Cname;
                }
                
            }
            var pods = ObtainDestinationCountryData(input.OwerType).First();
            /*if (!string.IsNullOrEmpty(temPro.TypeOfGoods))
             {
                 if (isEn)
                 {
                     temPro.TypeOfGoods = bases.Where(x => x.Id.ToString() == temPro.TypeOfGoods).FirstOrDefault()?.SopNameen;
                 }
                 else 
                 {
                     temPro.TypeOfGoods = bases.Where(x => x.Id.ToString() == temPro.TypeOfGoods).FirstOrDefault()?.SopName;
                 }

             }
             if (!string.IsNullOrEmpty(temPro.ModeOfOperation))
             {
                 string code = "3300";
                 if (input.OwerType=="10") 
                 {
                     code = "4400";
                 }
                 if (isEn)
                 {
                     temPro.ModeOfOperation = dics.Where(x => x.Dictid == code && x.Code == temPro.ModeOfOperation).FirstOrDefault()?.Ename;
                 }
                 else 
                 {
                     temPro.ModeOfOperation = dics.Where(x => x.Dictid == code && x.Code == temPro.ModeOfOperation).FirstOrDefault()?.Cname;
                 }

             }
             if (!string.IsNullOrEmpty(temPro.DestinationPort))
             {
                 var bizTypeArr = temPro.DestinationPort.Split(",");
                 foreach (var bizType in bizTypeArr)
                 {
                     if (isEn)
                     {
                         var typeName = bases.Where(x => x.Id.ToString() == bizType).FirstOrDefault()?.SopNameen;
                         temPro.DestinationPort = temPro.DestinationPort.Replace(bizType, typeName);
                     }
                     else 
                     {
                         var typeName = bases.Where(x => x.Id.ToString() == bizType).FirstOrDefault()?.SopName;
                         temPro.DestinationPort = temPro.DestinationPort.Replace(bizType, typeName);
                     }
                 }
             }
             var pods = ObtainDestinationCountryData(input.OwerType).First();
             if (!string.IsNullOrEmpty(temPro.PortOfDestination))
             {
                 var bizTypeArr = temPro.PortOfDestination.Split(",");
                 foreach (var bizType in bizTypeArr)
                 {
                     if (isEn)
                     {
                         var typeName = pods.Dictinfos.Where(x => x.Code == bizType).FirstOrDefault()?.Ename;
                         temPro.PortOfDestination = temPro.PortOfDestination.Replace(bizType, typeName);
                     }
                     else
                     {
                         var typeName = pods.Dictinfos.Where(x => x.Code == bizType).FirstOrDefault()?.Cname;
                         temPro.PortOfDestination = temPro.PortOfDestination.Replace(bizType, typeName);
                     }


                 }
             }

             if (!string.IsNullOrEmpty(temPro.OperationMode))
             {
                 if (isEn)
                 {
                     temPro.OperationMode = dics.Where(x => x.Dictid == "3500" && x.Code == temPro.OperationMode).FirstOrDefault()?.Ename;
                 }
                 else 
                 {
                     temPro.OperationMode = dics.Where(x => x.Dictid == "3500" && x.Code == temPro.OperationMode).FirstOrDefault()?.Cname;
                 }

             }
             if (!string.IsNullOrEmpty(temPro.PortDeclaration))
             {
                 if (isEn)
                 {
                     temPro.PortDeclaration = dics.Where(x => x.Dictid == "4000" && x.Code == temPro.PortDeclaration).FirstOrDefault()?.Ename;
                 }
                 else 
                 {
                     temPro.PortDeclaration = dics.Where(x => x.Dictid == "4000" && x.Code == temPro.PortDeclaration).FirstOrDefault()?.Cname;
                 }


             }
             if (!string.IsNullOrEmpty(temPro.RequirementType))
             {
                 var bizTypeArr = temPro.RequirementType.Split(",");
                 foreach (var bizType in bizTypeArr)
                 {
                     if (isEn)
                     {
                         var typeName = dics.Where(x => x.Dictid == "3600" && x.Code == bizType).FirstOrDefault()?.Ename;
                         temPro.RequirementType = temPro.RequirementType.Replace(bizType, typeName);
                     }
                     else
                     {
                         var typeName = dics.Where(x => x.Dictid == "3600" && x.Code == bizType).FirstOrDefault()?.Cname;
                         temPro.RequirementType = temPro.RequirementType.Replace(bizType, typeName);
                     }

                 }

             }
             if (!string.IsNullOrEmpty(temPro.Region))
             {
                 var bizTypeArr = temPro.Region.Split(",");
                 foreach (var bizType in bizTypeArr)
                 {
                     if (isEn)
                     {
                         var typeName = dics.Where(x => x.Dictid == "3700" && x.Code == bizType).FirstOrDefault()?.Ename;
                         temPro.Region = temPro.Region.Replace(bizType, typeName);
                     }
                     else 
                     {
                         var typeName = dics.Where(x => x.Dictid == "3700" && x.Code == bizType).FirstOrDefault()?.Cname;
                         temPro.Region = temPro.Region.Replace(bizType, typeName);
                     }

                 }
             }
             if (!string.IsNullOrEmpty(temPro.CustomsDeclarationType))
             {
                 if (isEn)
                 {
                     temPro.CustomsDeclarationType = dics.Where(x => x.Dictid == "3900" && x.Code == temPro.CustomsDeclarationType).FirstOrDefault()?.Ename;
                 }
                 else 
                 {
                     temPro.CustomsDeclarationType = dics.Where(x => x.Dictid == "3900" && x.Code == temPro.CustomsDeclarationType).FirstOrDefault()?.Cname;
                 }
             }*/
            var replacements = new Dictionary<string, string>();
            PropertyInfo[] properties = temPro.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var item in properties)
            {
                var value = item.GetValue(temPro).ToString(); // 获取属性值
                replacements.Add("{{" + item.Name + "}}", value);
            }
            #endregion

            #region 获取节点对应数据
            //所有属性字段相关联的框架图数据
            List<SopBase> sopBases = new List<SopBase>();
            List<string> attrIds = new List<string>();
            if (input.Exports != null) 
            {
                foreach (var item in input.Exports) 
                {
                    if (!string.IsNullOrEmpty(item.GoodsTypeHierarchical))
                    {
                        attrIds.Add(item.GoodsTypeHierarchical);
                    }
                    if (!string.IsNullOrEmpty(item.OpModeHierarchical))
                    {
                        attrIds.Add(item.OpModeHierarchical);
                    }
                    if (!string.IsNullOrEmpty(item.OriginHierarchical))
                    {
                        var oh = item.OriginHierarchical.Split(";");
                        attrIds.AddRange(oh);
                    }
                    if (!string.IsNullOrEmpty(item.DestConturyHierarchical))
                    {
                        var dch = item.DestConturyHierarchical.Split(";");
                        attrIds.AddRange(dch);
                    }
                }
            }
           
            if (input.CarrierList != null)
            {
                foreach (var item in input.CarrierList)
                {
                    if (!string.IsNullOrEmpty(item.CarrierHierarchical))
                    {
                        attrIds.Add(item.CarrierHierarchical);
                    }
                }
            }
            if (!string.IsNullOrEmpty(input.TrailerCustomsDeclaration.OriginHierarchical))
            {
                var oh = input.TrailerCustomsDeclaration.OriginHierarchical.Split(";");
                attrIds.AddRange(oh);
            }
            if (attrIds.Count() > 0)
            {
                sopBases = DB.SqlSugarClient().Queryable<SopBase>().Where(x => attrIds.Contains(x.AttrId)).ToList();
            }

            #endregion

            #region 处理联系人
            if (input.ContactInformations != null)
            {
                // 找到文档中的第一个表格
                Table informationsTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().FirstOrDefault();
                // 创建新行
                int row = 0;
                foreach (var item in input.ContactInformations)
                {
                    row++;
                    TableRow newRow = new TableRow();
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.ContactName)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Title)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Dept)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Email)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Tel)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Remark)))));
                    //设置单元格字体大小，不能设置超链接的样式
                    /*foreach (TableCell cell in newRow.Elements<TableCell>())
                    {
                        foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                        {
                            foreach (Run run in paragraph.Elements<Run>())
                            {
                                run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            }
                        }
                    }*/
                    // 将新行添加到第一个表格的末尾
                    informationsTable.Append(newRow);
                }
            }
            #endregion

            #region 处理Pa联系人
            if (input.PreAlertList != null)
            {
                Table quotationsTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().Skip(1).FirstOrDefault();
                int row = 0;
                foreach (var item in input.PreAlertList)
                {
                    row++;
                    TableRow newRow = new TableRow();
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Email)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Remark)))));
                    //设置单元格字体大小，不能设置超链接的样式
                    /*foreach (TableCell cell in newRow.Elements<TableCell>())
                    {
                        foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                        {
                            foreach (Run run in paragraph.Elements<Run>())
                            {
                                run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            }
                        }
                    }*/
                    // 将新行添加到第一个表格的末尾
                    quotationsTable.Append(newRow);
                }

            }
            #endregion

            #region 海外代理商联系方式
            if (input.OverseasAgentContactList != null)
            {
                Table verseassTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().Skip(2).FirstOrDefault();
                int row = 0;
                foreach (var item in input.OverseasAgentContactList)
                {
                    row++;
                    TableRow newRow = new TableRow();
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Email)))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.Remark)))));
                    //设置单元格字体大小，不能设置超链接的样式
                   /* foreach (TableCell cell in newRow.Elements<TableCell>())
                    {
                        foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                        {
                            foreach (Run run in paragraph.Elements<Run>())
                            {
                                run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            }
                        }
                    }*/
                    // 将新行添加到第一个表格的末尾
                    verseassTable.Append(newRow);
                }

            }
            #endregion
            int quoNum = 3;
            int setNum = 4;


            if (input.OwerType == "20")//海运
            {
                #region 处理HBL出单类型
                if (input.HBLOrderTypes != null)
                {
                    Table hblOrderTypesTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().Skip(3).FirstOrDefault();
                    int row = 0;
                    foreach (var item in input.HBLOrderTypes)
                    {
                        row++;
                        TableRow newRow = new TableRow();
                        newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                        newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.FileNamePath)))));
                        var startDate = item.ValidBegin.HasValue ? item.ValidBegin.Value.ToString("yyyy-MM-dd") : "";
                        var endDate = item.ValidEnd.HasValue ? item.ValidEnd.Value.ToString("yyyy-MM-dd") : "";
                        newRow.Append(new TableCell(new Paragraph(new Run(new Text($"{startDate}-{endDate}")))));
                        newRow.Append(new TableCell());
                        string linkAddress = "";
                        if (item.Type == "10") //上传附件
                        {
                            linkAddress = $"https://api.kwesz.com.cn/MstSopService/api/Minio/Download?path={item.FilePath}";
                        }
                        else//link方式 
                        {
                            linkAddress = item.FileNamePath;
                        }
                        try
                        {
                            // 创建超链接关系
                            var hyperLinkShip = wordDoc.MainDocumentPart.AddHyperlinkRelationship(new Uri(linkAddress), true);
                            // 创建超链接
                            Hyperlink hyperlink = new Hyperlink();
                            hyperlink.Anchor = linkAddress; // 设置超链接地址
                            hyperlink.Tooltip = linkAddress; // 设置超链接提示文本
                            hyperlink.Id = hyperLinkShip.Id; // 使用超链接关系的 Id
                            var hyperLinkProp = new RunProperties();
                            hyperLinkProp.Underline = new Underline() { Val = UnderlineValues.Single };
                            hyperLinkProp.Color = new Color() { ThemeColor = ThemeColorValues.Hyperlink };
                            hyperLinkProp.RunStyle = new RunStyle() { Val = "Hyperlink" };
                            // 创建运行并将其添加到超链接中
                            Run linkContent = new Run(new Text("点击访问"));//超链接显示的字
                            linkContent.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            linkContent.Append(hyperLinkProp);
                            hyperlink.Append(linkContent);
                            // 创建段落并将超链接添加到其中
                            Paragraph paragraph1 = new Paragraph();
                            paragraph1.Append(hyperlink);
                            // 将段落添加到第四个单元格中
                            TableCell cell7 = newRow.Elements<TableCell>().ElementAt(3); // 获取第四个单元格
                            cell7.Append(paragraph1);
                        }
                        catch
                        {
                            //不做处理
                        }
                        //设置单元格字体大小，不能设置超链接的样式
                        /*foreach (TableCell cell in newRow.Elements<TableCell>())
                        {
                            foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                            {
                                foreach (Run run in paragraph.Elements<Run>())
                                {
                                    run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                                }
                            }
                        }*/
                        // 将新行添加到第一个表格的末尾
                        hblOrderTypesTable.Append(newRow);
                    }

                }
                #endregion

                #region 处理长期放货保涵
                if (input.GuaranteeLetters != null)
                {
                    Table guaranteeLettersTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().Skip(4).FirstOrDefault();
                    int row = 0;
                    foreach (var item in input.GuaranteeLetters)
                    {
                        row++;
                        TableRow newRow = new TableRow();
                        newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                        newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.FileNamePath)))));
                        var startDate = item.ValidBegin.HasValue ? item.ValidBegin.Value.ToString("yyyy-MM-dd") : "";
                        var endDate = item.ValidEnd.HasValue ? item.ValidEnd.Value.ToString("yyyy-MM-dd") : "";
                        newRow.Append(new TableCell(new Paragraph(new Run(new Text($"{startDate}-{endDate}")))));
                        newRow.Append(new TableCell());
                        string linkAddress = "";
                        if (item.Type == "10") //上传附件
                        {
                            linkAddress = $"https://api.kwesz.com.cn/MstSopService/api/Minio/Download?path={item.FilePath}";
                        }
                        else//link方式 
                        {
                            linkAddress = item.FileNamePath;
                        }
                        try
                        {
                            // 创建超链接关系
                            var hyperLinkShip = wordDoc.MainDocumentPart.AddHyperlinkRelationship(new Uri(linkAddress), true);
                            // 创建超链接
                            Hyperlink hyperlink = new Hyperlink();
                            hyperlink.Anchor = linkAddress; // 设置超链接地址
                            hyperlink.Tooltip = linkAddress; // 设置超链接提示文本
                            hyperlink.Id = hyperLinkShip.Id; // 使用超链接关系的 Id
                            var hyperLinkProp = new RunProperties();
                            hyperLinkProp.Underline = new Underline() { Val = UnderlineValues.Single };
                            hyperLinkProp.Color = new Color() { ThemeColor = ThemeColorValues.Hyperlink };
                            hyperLinkProp.RunStyle = new RunStyle() { Val = "Hyperlink" };
                            // 创建运行并将其添加到超链接中
                            Run linkContent = new Run(new Text("点击访问"));//超链接显示的字
                            linkContent.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            linkContent.Append(hyperLinkProp);
                            hyperlink.Append(linkContent);
                            // 创建段落并将超链接添加到其中
                            Paragraph paragraph1 = new Paragraph();
                            paragraph1.Append(hyperlink);
                            // 将段落添加到第四个单元格中
                            TableCell cell7 = newRow.Elements<TableCell>().ElementAt(3); // 获取第四个单元格
                            cell7.Append(paragraph1);
                        }
                        catch
                        {
                            //不做处理
                        }
                        //设置单元格字体大小，不能设置超链接的样式
                        /*foreach (TableCell cell in newRow.Elements<TableCell>())
                        {
                            foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                            {
                                foreach (Run run in paragraph.Elements<Run>())
                                {
                                    run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                                }
                            }
                        }*/
                        // 将新行添加到第一个表格的末尾
                        guaranteeLettersTable.Append(newRow);
                    }
                }
                #endregion
                quoNum = 5;
                setNum = 6;
            }





            #region 处理报价单
            if (input.Quotations != null)
            {
                Table quotationsTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().Skip(quoNum).FirstOrDefault();
                int row = 0;
                foreach (var item in input.Quotations)
                {
                    row++;
                    TableRow newRow = new TableRow();
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.FileNamePath)))));
                    var startDate = item.ValidBegin.HasValue ? item.ValidBegin.Value.ToString("yyyy-MM-dd") : "";
                    var endDate = item.ValidEnd.HasValue ? item.ValidEnd.Value.ToString("yyyy-MM-dd") : "";
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text($"{startDate}-{endDate}")))));
                    newRow.Append(new TableCell());
                    string linkAddress = "";
                    if (item.Type == "10") //上传附件
                    {
                        linkAddress = $"https://api.kwesz.com.cn/MstSopService/api/Minio/Download?path={item.FilePath}";
                        //https://api.kwesz.com.cn/MstSopService/api/Minio/Upload
                    }
                    else//link方式 
                    {
                        linkAddress = item.FileNamePath;
                    }
                    try
                    {
                        // 创建超链接关系
                        var hyperLinkShip = wordDoc.MainDocumentPart.AddHyperlinkRelationship(new Uri(linkAddress), true);
                        // 创建超链接
                        Hyperlink hyperlink = new Hyperlink();
                        hyperlink.Anchor = linkAddress; // 设置超链接地址
                        hyperlink.Tooltip = linkAddress; // 设置超链接提示文本
                        hyperlink.Id = hyperLinkShip.Id; // 使用超链接关系的 Id
                        var hyperLinkProp = new RunProperties();
                        hyperLinkProp.Underline = new Underline() { Val = UnderlineValues.Single };
                        hyperLinkProp.Color = new Color() { ThemeColor = ThemeColorValues.Hyperlink };
                        hyperLinkProp.RunStyle = new RunStyle() { Val = "Hyperlink" };
                        // 创建运行并将其添加到超链接中
                        Run linkContent = new Run(new Text("点击访问"));//超链接显示的字
                        linkContent.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                        linkContent.Append(hyperLinkProp);
                        hyperlink.Append(linkContent);
                        // 创建段落并将超链接添加到其中
                        Paragraph paragraph1 = new Paragraph();
                        paragraph1.Append(hyperlink);
                        // 将段落添加到第四个单元格中
                        TableCell cell7 = newRow.Elements<TableCell>().ElementAt(3); // 获取第四个单元格
                        cell7.Append(paragraph1);
                    }
                    catch
                    {
                        //不做处理
                    }

                    //设置单元格字体大小，不能设置超链接的样式
                    /*foreach (TableCell cell in newRow.Elements<TableCell>())
                    {
                        foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                        {
                            foreach (Run run in paragraph.Elements<Run>())
                            {
                                run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            }
                        }
                    }*/
                    // 将新行添加到第一个表格的末尾
                    quotationsTable.Append(newRow);
                }

            }
            #endregion

            #region 处理结算方式
            if (input.SettlementModes != null)
            {
                Table settlementModesTable = wordDoc.MainDocumentPart!.Document.Descendants<Table>().Skip(setNum).FirstOrDefault();
                int row = 0;
                foreach (var item in input.SettlementModes)
                {
                    row++;
                    TableRow newRow = new TableRow();
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(row.ToString())))));
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text(item.FileNamePath)))));
                    var startDate = item.ValidBegin.HasValue ? item.ValidBegin.Value.ToString("yyyy-MM-dd") : "";
                    var endDate = item.ValidEnd.HasValue ? item.ValidEnd.Value.ToString("yyyy-MM-dd") : "";
                    newRow.Append(new TableCell(new Paragraph(new Run(new Text($"{startDate}-{endDate}")))));
                    newRow.Append(new TableCell());
                    string linkAddress = "";
                    if (item.Type == "10") //上传附件
                    {
                        linkAddress = $"https://api.kwesz.com.cn/MstSopService/api/Minio/Download?path={item.FilePath}";
                    }
                    else//link方式 
                    {
                        linkAddress = item.FileNamePath;
                    }
                    try
                    {
                        // 创建超链接关系
                        var hyperLinkShip = wordDoc.MainDocumentPart.AddHyperlinkRelationship(new Uri(linkAddress), true);
                        // 创建超链接
                        Hyperlink hyperlink = new Hyperlink();
                        hyperlink.Anchor = linkAddress; // 设置超链接地址
                        hyperlink.Tooltip = linkAddress; // 设置超链接提示文本
                        hyperlink.Id = hyperLinkShip.Id; // 使用超链接关系的 Id
                        var hyperLinkProp = new RunProperties();
                        hyperLinkProp.Underline = new Underline() { Val = UnderlineValues.Single };
                        hyperLinkProp.Color = new Color() { ThemeColor = ThemeColorValues.Hyperlink };
                        hyperLinkProp.RunStyle = new RunStyle() { Val = "Hyperlink" };
                        // 创建运行并将其添加到超链接中
                        Run linkContent = new Run(new Text("点击访问"));//超链接显示的字
                        linkContent.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                        linkContent.Append(hyperLinkProp);
                        hyperlink.Append(linkContent);
                        // 创建段落并将超链接添加到其中
                        Paragraph paragraph1 = new Paragraph();
                        paragraph1.Append(hyperlink);
                        // 将段落添加到第四个单元格中
                        TableCell cell7 = newRow.Elements<TableCell>().ElementAt(3); // 获取第四个单元格
                        cell7.Append(paragraph1);
                    }
                    catch
                    {
                        //不做处理
                    }
                    //设置单元格字体大小，不能设置超链接的样式
                    /*foreach (TableCell cell in newRow.Elements<TableCell>())
                    {
                        foreach (Paragraph paragraph in cell.Elements<Paragraph>())
                        {
                            foreach (Run run in paragraph.Elements<Run>())
                            {
                                run.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "14" }); // 设置字体大小
                            }
                        }
                    }*/
                    // 将新行添加到第一个表格的末尾
                    settlementModesTable.Append(newRow);
                }

            }
            #endregion

            #region 替换文本内容
            var paras = body!.Elements<Paragraph>();
            foreach (var para in paras)
            {
                var runs = para.Elements<Run>();
                string[] copy_text = new string[runs.Count()];
                int pt = 0;
                foreach (var run in runs)
                {
                    var texts = run.Elements<Text>();

                    // 构建数组记录下所有的run，组合内容到copy_text数组
                    foreach (var text in texts)
                    {
                        copy_text[pt] += text.Text;
                    }
                    pt++;
                }
                //将字符串拼接在一块，看看是否存在目标字段
                string str = string.Join("", copy_text);
                //如果存在目标字段，则将范围内的run合并在一起
                if (replacements.Keys.Any(str.Contains))
                {
                    //找到特定字段所在的第一个run的位置
                    int start = 0;
                    while (true)
                    {
                        string sub_str = "";
                        for (int i = start; i < runs.Count(); i++)
                        {
                            sub_str += copy_text[i];
                        }
                        if (!replacements.Keys.Any(sub_str.Contains))
                        {
                            start--;
                            break;
                        }
                        else
                        {
                            start++;
                        }
                    }
                    //找到特定字段所在的最后一个run的位置
                    string inner_str = "";//范围内的字符串
                    int end = runs.Count();
                    while (true)
                    {
                        string sub_str = "";
                        for (int i = start; i < end; i++)
                        {
                            sub_str += copy_text[i];
                        }
                        if (!replacements.Keys.Any(sub_str.Contains))
                        {
                            end++;
                            break;
                        }
                        else
                        {
                            inner_str = sub_str;
                            end--;
                        }
                    }
                    //将范围内的run合并在一起
                    int sel_pt = 0;
                    foreach (var run in runs)
                    {
                        if (sel_pt == start)
                        {
                            var texts = run.Elements<Text>();
                            //将run里面的文字改为inner_str的内容
                            int num = 0;
                            foreach (var mytext in texts)
                            {
                                if (num == 0)
                                {
                                    mytext.Text = inner_str;
                                }
                                else
                                {
                                    mytext.Text = "";
                                }
                                num++;
                            }
                        }
                        else if (sel_pt > start && sel_pt < end)
                        {
                            var texts = run.Elements<Text>();
                            foreach (var mytext in texts)
                            {
                                mytext.Text = "";
                            }
                        }
                        sel_pt++;
                    }
                    //重新遍历一遍，替换目标字段
                    foreach (var run in runs)
                    {
                        var texts = run.Elements<Text>();
                        foreach (var text in texts)
                        {
                            foreach (string key in replacements.Keys)
                            {
                                if (text.Text.Contains(key))
                                {
                                    text.Text = text.Text.Replace(key, replacements[key]);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region 动态插入节点数据


            if (!string.IsNullOrEmpty(input.BizType))
            {
                int num = 1;
                if (input.BizType.Contains("10"))//Export
                {
                    List<Paragraph> pars = new List<Paragraph>();
                    Paragraph exportParagraph = new Paragraph();
                    num++;
                    Run run = new Run(new Text("2.出口   Export"));
                    var rgbys = RGBStringToHex("rgb(0, 189, 248)");
                    // 为运行设置字体颜色
                    run.RunProperties = new RunProperties(
                        new Color() { Val = rgbys }
                    );
                    exportParagraph.ParagraphProperties = new ParagraphProperties(
                      new ParagraphStyleId { Val = "3" }//设置标题样式
                    );


                    exportParagraph.Append(run);
                    pars.Add(exportParagraph);
                    /*
                     空运出口没有船信息，OwerType：20（海运）
                     */
                    if (input.OwerType == "20")
                    {
                        if (input.CarrierList != null)
                        {
                            //var shippingData = DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Pid == 21477).ToList();
                            foreach (var item in input.CarrierList)
                            {
                                Paragraph shipParagraph = new Paragraph();
                                shipParagraph.ParagraphProperties = new ParagraphProperties(
                                   new SpacingBetweenLines() { Line = "360" }//设置行距
                                );

                                var CarrierName = bases.Where(x => x.Id.ToString() == item.Carrier).FirstOrDefault()?.SopNameen;
                                CarrierName = string.IsNullOrEmpty(CarrierName) ? "" : CarrierName;
                                var carPar = new Run(new Text($"船公司名：{CarrierName}"));
                                shipParagraph.Append(carPar);
                                //shipParagraph.Append(new Run(new Break()));//换行
                                var cosPar = new Run(new Text($"\u00A0      价格类型：{item.PriceType}"));
                                shipParagraph.Append(cosPar);
                                pars.Add(shipParagraph);
                                if (!string.IsNullOrEmpty(item.CarrierHierarchical))
                                {
                                    var sopData = sopBases.Where(x => x.AttrId == item.CarrierHierarchical).ToList();
                                    foreach (var item1 in sopData)
                                    {
                                        var htmlData = "<root>" + item1.Detail + "</root>";
                                        var par = ProcessingNodeContent(htmlData, wordDoc);
                                        pars.AddRange(par);
                                    }
                                }
                                /*加上附件*/
                                if (item.FileCarrier != null)
                                {
                                    pars.Add(AddHyperlink(wordDoc, item.FileCarrier));
                                }
                                //pars.Add(new Paragraph());
                            }
                            pars.Add(new Paragraph());
                        }
                    }
                    //添加出口信息
                    if (input.Exports!=null) 
                    {
                        foreach (var item in input.Exports)
                        {
                            string goodsType = "";
                            if (!string.IsNullOrEmpty(item.GoodsType))
                            {
                                if (isEn)
                                {
                                    goodsType = bases.Where(x => x.Id.ToString() == item.GoodsType).FirstOrDefault()?.SopNameen;
                                }
                                else
                                {
                                    goodsType = bases.Where(x => x.Id.ToString() == item.GoodsType).FirstOrDefault()?.SopName;
                                }

                            }
                            Paragraph exportsParagraph = new Paragraph();
                            exportsParagraph.ParagraphProperties = new ParagraphProperties(
                                  new SpacingBetweenLines() { Line = "360" }//设置行距
                               );

                           
                            Run goodsTypeRun=new Run();
                            goodsTypeRun.RunProperties = new RunProperties(new RunFonts() { Ascii = "SimSun", HighAnsi = "SimSun", EastAsia = "SimSun" });
                            goodsTypeRun.Append(new Text($"货物类型：{goodsType}"));
                            exportsParagraph.Append(goodsTypeRun);
                            pars.Add(exportsParagraph);
                            if (!string.IsNullOrEmpty(item.GoodsTypeHierarchical))
                            {
                                var sopData = sopBases.Where(x => x.AttrId == item.GoodsTypeHierarchical).ToList();
                                if (sopData.Count() > 0)
                                {
                                    pars.Add(new Paragraph(new Run(new Break())));
                                    foreach (var item1 in sopData)
                                    {
                                        var htmlData = "<root>" + item1.Detail + "</root>";
                                        var par = ProcessingNodeContent(htmlData, wordDoc);
                                        pars.AddRange(par);
                                    }
                                    
                                }
                            }
                            /*加上附件*/
                            if (item.TypeOfGoods != null)
                            {
                                pars.Add(AddHyperlink(wordDoc, item.TypeOfGoods));
                            }
                            if (input.OwerType == "20")
                            {
                                if (!string.IsNullOrEmpty(item.GoodsType) && item.GoodsType.Contains("21487"))
                                {
                                    string dg = "";
                                    string dgsp = "";
                                    if (!string.IsNullOrEmpty(item.Dg))
                                    {
                                        if (isEn)
                                        {
                                            dg = dics.Where(x => x.Dictid == "3200" && x.Code == item.Dg).FirstOrDefault()?.Ename;
                                        }
                                        else
                                        {
                                            dg = dics.Where(x => x.Dictid == "3200" && x.Code == item.Dg).FirstOrDefault()?.Cname;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(item.DgSpecialPackging))
                                    {
                                        if (isEn)
                                        {
                                            dgsp = dics.Where(x => x.Dictid == "3200" && x.Code == item.DgSpecialPackging).FirstOrDefault()?.Ename;
                                        }
                                        else
                                        {
                                            dgsp = dics.Where(x => x.Dictid == "3200" && x.Code == item.DgSpecialPackging).FirstOrDefault()?.Cname;
                                        }
                                    }
                                    Paragraph dgParagraph = new Paragraph();//设置行距
                                    dgParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                                    dgParagraph.Append(new Run(new Text($"海事申报：{dg}")));
                                    pars.Add(dgParagraph);

                                    Paragraph dgspParagraph = new Paragraph();
                                    dgspParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                                    dgspParagraph.Append(new Run(new Text($"危标挂网：{dgsp}")));
                                    pars.Add(dgspParagraph);
                                }
                                string opMode = "";
                                if (!string.IsNullOrEmpty(item.OpMode))
                                {
                                    string code = "3300";
                                    if (input.OwerType == "10")
                                    {
                                        code = "4400";
                                    }
                                    if (isEn)
                                    {
                                        opMode = dics.Where(x => x.Dictid == code && x.Code == item.OpMode).FirstOrDefault()?.Ename;
                                    }
                                    else
                                    {
                                        opMode = dics.Where(x => x.Dictid == code && x.Code == item.OpMode).FirstOrDefault()?.Cname;
                                    }
                                }
                                Paragraph opModeParagraph = new Paragraph();//设置行距
                                opModeParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                                opModeParagraph.Append(new Run(new Text($"操作模式：{opMode}")));
                                pars.Add(opModeParagraph);
                                if (!string.IsNullOrEmpty(item.OpModeHierarchical))
                                {
                                    var sopData = sopBases.Where(x => x.AttrId == item.OpModeHierarchical).ToList();
                                    if (sopData.Count() > 0)
                                    {
                                        pars.Add(new Paragraph(new Run(new Break())));
                                        foreach (var item1 in sopData)
                                        {
                                            var htmlData = "<root>" + item1.Detail + "</root>";
                                            var par = ProcessingNodeContent(htmlData, wordDoc);
                                            pars.AddRange(par);
                                        }
                                        
                                    }
                                }
                                /*加上附件*/
                                if (item.ModeOfOperation != null)
                                {
                                    pars.Add(AddHyperlink(wordDoc, item.ModeOfOperation));
                                }
                            }
                            string Origin = "";
                            if (!string.IsNullOrEmpty(item.Origin))
                            {
                                Origin= item.Origin;
                                var bizTypeArr = item.Origin.Split(",");
                                foreach (var bizType in bizTypeArr)
                                {
                                    if (isEn)
                                    {
                                        var typeName = bases.Where(x => x.Id.ToString() == bizType).FirstOrDefault()?.SopNameen;
                                        Origin = Origin.Replace(bizType, typeName);
                                    }
                                    else
                                    {
                                        var typeName = bases.Where(x => x.Id.ToString() == bizType).FirstOrDefault()?.SopName;
                                        Origin = Origin.Replace(bizType, typeName);
                                    }
                                }
                            }
                            Paragraph originParagraph=new Paragraph();//设置行距
                            originParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                            originParagraph.Append(new Run(new Text($"起运地：{Origin}")));
                            pars.Add(originParagraph);
                            if (!string.IsNullOrEmpty(item.OriginHierarchical))
                            {
                                var oh = item.OriginHierarchical.Split(";");
                                foreach (var item1 in oh)
                                {
                                    var sopData = sopBases.Where(x => x.AttrId == item1).ToList();
                                    if (sopData.Count() > 0)
                                    {
                                        foreach (var item2 in sopData)
                                        {
                                            var htmlData = "<root>" + item2.Detail + "</root>";
                                            var par = ProcessingNodeContent(htmlData, wordDoc);
                                            pars.AddRange(par);
                                        }
                                        //pars.Add(new Paragraph(new Run(new Break())));
                                        //pars.Add(new Paragraph());
                                    }

                                }
                            }
                            /*加上附件*/
                            if (item.Pol != null)
                            {
                                pars.Add(AddHyperlink(wordDoc, item.Pol));
                            }
                            if (!string.IsNullOrEmpty(item.Origin) && item.Origin.Contains("21455"))
                            {
                                string declaration = "";
                                if (!string.IsNullOrEmpty(item.Declaration))
                                {
                                    if (isEn)
                                    {
                                        declaration = dics.Where(x => x.Dictid == "3100" && x.Code == item.Declaration).FirstOrDefault()?.Ename;
                                    }
                                    else
                                    {
                                        declaration = dics.Where(x => x.Dictid == "3100" && x.Code == item.Declaration).FirstOrDefault()?.Cname;
                                    }
                                }
                                Paragraph outDeclarationParagraph = new Paragraph();//设置行距
                                outDeclarationParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                                outDeclarationParagraph.Append(new Run(new Text($"香港出口报关：{declaration}")));
                                pars.Add(outDeclarationParagraph);
                            }
                            string destContury = "";
                            if (!string.IsNullOrEmpty(item.DestContury))
                            {
                                destContury = item.DestContury;
                                var bizTypeArr = item.DestContury.Split(",");
                                foreach (var bizType in bizTypeArr)
                                {
                                    if (isEn)
                                    {
                                        var typeName = pods.Dictinfos.Where(x => x.Code == bizType).FirstOrDefault()?.Ename;
                                        destContury = destContury.Replace(bizType, typeName);
                                    }
                                    else
                                    {
                                        var typeName = pods.Dictinfos.Where(x => x.Code == bizType).FirstOrDefault()?.Cname;
                                        destContury = destContury.Replace(bizType, typeName);
                                    }

                                }
                            }
                            Paragraph declarationParagraph = new Paragraph();//设置行距
                            declarationParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                            declarationParagraph.Append(new Run(new Text($"目的国：{destContury}")));
                            pars.Add(declarationParagraph);

                            if (!string.IsNullOrEmpty(item.DestConturyHierarchical))
                            {
                                var dch = item.DestConturyHierarchical.Split(";");
                                foreach (var item1 in dch)
                                {
                                    var sopData = sopBases.Where(x => x.AttrId == item1).ToList();
                                    if (sopData.Count() > 0)
                                    {
                                        pars.Add(new Paragraph(new Run(new Break())));
                                        foreach (var item2 in sopData)
                                        {
                                            var htmlData = "<root>" + item2.Detail + "</root>";
                                            var par = ProcessingNodeContent(htmlData, wordDoc);
                                            pars.AddRange(par);
                                        }
                                    }
                                }
                            }
                            /*加上附件*/
                            if (item.Destination != null)
                            {
                                pars.Add(AddHyperlink(wordDoc, item.Destination));
                            }
                            pars.Add(new Paragraph());
                        }
                       
                    }
                    if (pars.Count() > 0)
                    {
                        foreach (var par in pars)
                        {
                            Paragraph lastParagraph = body.Elements<Paragraph>().LastOrDefault();
                            lastParagraph.InsertAfterSelf(par);
                        }
                    }
                }

                if (input.BizType.Contains("20"))//Import
                {
                    Paragraph importParagraphTitle = new Paragraph();
                    List<Paragraph> pars = new List<Paragraph>();
                    num++;

                    Run run = new Run(new Text($"{num}.进口  Import"));
                    //*/ 为运行设置属性：字体颜色
                    var rgbys = RGBStringToHex("rgb(0, 189, 248)");
                    // 为运行设置字体颜色
                    run.RunProperties = new RunProperties(
                        new Color() { Val = rgbys }
                    );
                    importParagraphTitle.ParagraphProperties = new ParagraphProperties(
                      new ParagraphStyleId { Val = "3" }
                    );
                    importParagraphTitle.Append(run);
                    pars.Add(importParagraphTitle);
                    //Paragraph exportParagraph = new Paragraph();

                    //exportParagraph.Append(new Run(new Break(), new Break()));//换行

                    string opMode = "";
                    if (!string.IsNullOrEmpty(input.Import.OpMode))
                    {
                        if (isEn)
                        {
                            opMode = dics.Where(x => x.Dictid == "3500" && x.Code == input.Import.OpMode).FirstOrDefault()?.Ename;
                        }
                        else
                        {
                            opMode = dics.Where(x => x.Dictid == "3500" && x.Code == input.Import.OpMode).FirstOrDefault()?.Cname;
                        }

                    }
                    Paragraph importParagraph = new Paragraph();//设置行距
                    importParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });

                    /*importParagraph.Append(new Run(new Text($"操作模式       {opMode}")));
                    importParagraph.Append(new Run(new Break()));//换行
                    importParagraph.Append(new Run(new Text($"操作模式       {opMode}")));*/

                    if (input.OwerType == "20")//海运
                    {
                       
                        
                        
                        string carrierSopName = "";
                        if (isEn)
                        {
                            carrierSopName = input.Import.Carrier;
                            var carrierArr= carrierSopName.Split(',');
                            foreach (var item in carrierArr) 
                            {
                                var sopName = bases.Where(x => x.Id.ToString() == input.Import.Carrier).FirstOrDefault()?.SopNameen;
                                carrierSopName = carrierSopName.Replace(item,sopName);
                            }
                            
                        }
                        else
                        {
                            carrierSopName = input.Import.Carrier;
                            var carrierArr = carrierSopName.Split(',');
                            foreach (var item in carrierArr)
                            {
                                var sopName = bases.Where(x => x.Id.ToString() == input.Import.Carrier).FirstOrDefault()?.SopName;
                                carrierSopName = carrierSopName.Replace(item, sopName);
                            }
                        }
                        importParagraph.Append(new Run(new Text($"船公司：{carrierSopName}")));
                        importParagraph.Append(new Run(new Break()));//换行
                        var importMode = new Run(new Text($"进口模式：{input.Import.InType}"));
                        importParagraph.Append(importMode);
                        importParagraph.Append(new Run(new Break()));//换行
                        string goodsSopName = "";

                        
                        var goodsBase = DB.SqlSugarClient().Queryable<SopBaseTreeDTO>().ToChildList(it => it.Pid, 21434);
                        var goodsType = goodsBase.Where(x => x.Idx.ToString() == input.Import.GoodsType).FirstOrDefault();
                        if (goodsType != null)
                        {
                            if (isEn)
                            {
                                goodsSopName = goodsType.SopNameen;
                            }
                            else
                            {
                                goodsSopName = goodsType.SopName;
                            }
                        }
                        importParagraph.Append(new Run(new Text($"货物类型：{goodsSopName}")));
                        string dest = "";
                        if (!string.IsNullOrEmpty(input.Import.Dest))
                        {
                            var bizTypeArr = input.Import.Dest.Split(",");
                            dest = input.Import.Dest;
                            foreach (var bizType in bizTypeArr)
                            {
                                if (isEn)
                                {
                                    var typeName = bases.Where(x => x.Id.ToString() == bizType).FirstOrDefault()?.SopNameen;
                                    dest = dest.Replace(bizType, typeName);
                                }
                                else
                                {
                                    var typeName = bases.Where(x => x.Id.ToString() == bizType).FirstOrDefault()?.SopName;
                                    dest = dest.Replace(bizType, typeName);
                                }

                            }
                        }

                        importParagraph.Append(new Run(new Break()));//换行
                        var destMode = new Run(new Text($"抵达国家：{dest}"));
                        importParagraph.Append(destMode);

                        if (!string.IsNullOrEmpty(input.Import.Dest) && input.Import.Dest.Contains("21448"))
                        {
                            importParagraph.Append(new Run(new Break()));//换行
                            string declarationMethod = "";
                            if (!string.IsNullOrEmpty(input.Import.DeclarationMethod))
                            {
                                if (isEn)
                                {
                                    declarationMethod = dics.Where(x => x.Dictid == "4000" && x.Code == input.Import.DeclarationMethod).FirstOrDefault()?.Ename;
                                }
                                else
                                {
                                    declarationMethod = dics.Where(x => x.Dictid == "4000" && x.Code == input.Import.DeclarationMethod).FirstOrDefault()?.Cname;
                                }


                            }
                            var portDeclaration = new Run(new Text($"ROCARS 申报方：{declarationMethod}"));
                            importParagraph.Append(portDeclaration);
                        }
                    }
                    else //空运
                    {
                        importParagraph.Append(new Run(new Break()));//换行
                        var importMode = new Run(new Text($"进口联系窗口：{input.Import.ContactWindow}"));
                        importParagraph.Append(importMode);

                    }
                    pars.Add(importParagraph);
                    pars.Add(new Paragraph());
                    if (pars.Count() > 0)
                    {
                        foreach (var par in pars)
                        {
                            Paragraph lastParagraph = body.Elements<Paragraph>().LastOrDefault();
                            lastParagraph.InsertAfterSelf(par);
                        }
                    }
                }

                if (input.BizType.Contains("30"))//拖车报关
                {
                    var tcd = input.TrailerCustomsDeclaration;
                    Paragraph trailerParagraphTitle = new Paragraph();
                    num++;
                    List<Paragraph> pars = new List<Paragraph>();
                    Run run = new Run(new Text($"{num}.拖车报关 TRUCKING+CUSTOMS CLEARANCE"));
                    var rgbys = RGBStringToHex("rgb(0, 189, 248)");
                    // 为运行设置字体颜色
                    run.RunProperties = new RunProperties(
                        new Color() { Val = rgbys }
                    );
                    trailerParagraphTitle.ParagraphProperties = new ParagraphProperties(
                      new ParagraphStyleId { Val = "3" }
                    );

                    trailerParagraphTitle.Append(run);
                    pars.Add(trailerParagraphTitle);
                    //Paragraph exportParagraph = new Paragraph();

                    Paragraph trailerParagraph = new Paragraph();//设置行距
                    trailerParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                    //exportParagraph.Append(new Run(new Break(), new Break()));//换行

                    if (input.OwerType == "20")
                    {
                        #region 海运专属
                        string requirementType = "";
                        if (!string.IsNullOrEmpty(tcd.RequirementType))
                        {
                            var bizTypeArr = tcd.RequirementType.Split(",");
                            requirementType= tcd.RequirementType;
                            foreach (var bizType in bizTypeArr)
                            {
                                if (isEn)
                                {
                                    var typeName = dics.Where(x => x.Dictid == "3600" && x.Code == bizType).FirstOrDefault()?.Ename;
                                    requirementType = requirementType.Replace(bizType, typeName);
                                }
                                else
                                {
                                    var typeName = dics.Where(x => x.Dictid == "3600" && x.Code == bizType).FirstOrDefault()?.Cname;
                                    requirementType = requirementType.Replace(bizType, typeName);
                                }

                            }

                        }
                        trailerParagraph.Append(new Run(new Text($"需求类型：{requirementType}")));
                        trailerParagraph.Append(new Run(new Break()));//换行
                        //var region = new Run(new Text($"\u00A0               起运地： {temPro.Region}"));
                        string origin = "";
                        if (!string.IsNullOrEmpty(tcd.Origin))
                        {
                            var bizTypeArr = tcd.Origin.Split(",");
                            origin= tcd.Origin;
                            foreach (var bizType in bizTypeArr)
                            {
                                if (isEn)
                                {
                                    var typeName = dics.Where(x => x.Dictid == "3700" && x.Code == bizType).FirstOrDefault()?.Ename;
                                    origin = origin.Replace(bizType, typeName);
                                }
                                else
                                {
                                    var typeName = dics.Where(x => x.Dictid == "3700" && x.Code == bizType).FirstOrDefault()?.Cname;
                                    origin = origin.Replace(bizType, typeName);
                                }

                            }
                        }
                        trailerParagraph.Append(new Run(new Text($"起运地：{origin}")));
                        pars.Add(trailerParagraph);
                        if (!string.IsNullOrEmpty(tcd.OriginHierarchical))
                        {
                            var oh = input.TrailerCustomsDeclaration.OriginHierarchical.Split(";");
                            foreach (var item in oh)
                            {
                                var sopData = sopBases.Where(x => x.AttrId == item).ToList();
                                if (sopData.Count() > 0)
                                {
                                    foreach (var item1 in sopData)
                                    {
                                        var htmlData = "<root>" + item1.Detail + "</root>";
                                        var par = ProcessingNodeContent(htmlData, wordDoc);
                                        pars.AddRange(par);
                                    }
                                }
                            }
                        }
                        /*加上附件*/
                        if (tcd.Region != null)
                        {
                            pars.Add(AddHyperlink(wordDoc, tcd.Region));
                        }
                        Paragraph endingParagraph = new Paragraph();
                        endingParagraph.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                        if (!string.IsNullOrEmpty(tcd.Origin) && tcd.Origin.Contains("20"))
                        {
                            //3100
                            string declarationMethod = "";
                            if (!string.IsNullOrEmpty(tcd.DeclarationMethod))
                            {
                                if (isEn)
                                {
                                    declarationMethod = dics.Where(x => x.Dictid == "3100" && x.Code == tcd.DeclarationMethod).FirstOrDefault()?.Ename;
                                }
                                else
                                {
                                    declarationMethod = dics.Where(x => x.Dictid == "3100" && x.Code == tcd.DeclarationMethod).FirstOrDefault()?.Cname;
                                }
                            }
                            
                            endingParagraph.Append(new Run(new Text($"ROCARS 申报方：{declarationMethod}")));
                            endingParagraph.Append(new Run(new Break()));
                        }
                        string customsType = "";
                        if (!string.IsNullOrEmpty(tcd.CustomsType))
                        {
                            if (isEn)
                            {
                                customsType = dics.Where(x => x.Dictid == "3900" && x.Code == tcd.CustomsType).FirstOrDefault()?.Ename;
                            }
                            else
                            {
                                customsType = dics.Where(x => x.Dictid == "3900" && x.Code == tcd.CustomsType).FirstOrDefault()?.Cname;
                            }
                        }
                        endingParagraph.Append(new Run(new Text($"报关类型：{customsType}")));
                        if (input.TrailerCustomsDeclaration.RequirementType.Contains("20"))//如果起运地选择香港后，还需要带出ROCARS 申报方
                        {
                            endingParagraph.Append(new Run(new Break()));
                            endingParagraph.Append(new Run(new Text($"提货厂名：{tcd.PickUpName}")));
                            endingParagraph.Append(new Run(new Break()));
                            endingParagraph.Append(new Run(new Text($"提货地址：{tcd.PickUpAddr}")));
                            endingParagraph.Append(new Run(new Break()));
                            endingParagraph.Append(new Run(new Text($"联系人及电话：{tcd.PickUpContact}")));
                            string isWeigh = "";
                            if (!string.IsNullOrEmpty(tcd.IsWeigh))
                            {
                                var bizTypeArr = tcd.IsWeigh.Split(",");
                                isWeigh = tcd.IsWeigh;
                                foreach (var bizType in bizTypeArr)
                                {
                                    if (isEn)
                                    {
                                        var typeName = dics.Where(x => x.Dictid == "3800" && x.Code == bizType).FirstOrDefault()?.Ename;
                                        isWeigh = isWeigh.Replace(bizType, typeName);
                                    }
                                    else
                                    {
                                        var typeName = dics.Where(x => x.Dictid == "3800" && x.Code == bizType).FirstOrDefault()?.Cname;
                                        isWeigh = isWeigh.Replace(bizType, typeName);
                                    }

                                }
                            }

                            endingParagraph.Append(new Run(new Break()));
                            endingParagraph.Append(new Run(new Text($"是否过磅：{isWeigh}")));//3800
                            endingParagraph.Append(new Run(new Break()));
                            endingParagraph.Append(new Run(new Text($"其它拖车要求：{tcd.TrailerRequirements}")));
                        }
                        pars.Add(endingParagraph);
                        #endregion
                    }
                    else 
                    {
                        Paragraph airParagraph30 = new Paragraph();
                        airParagraph30.ParagraphProperties = new ParagraphProperties(new SpacingBetweenLines() { Line = "360" });
                        string requirementType = "";
                        if (!string.IsNullOrEmpty(input.TrailerCustomsDeclaration.RequirementType))
                        {
                            var bizTypeArr = input.TrailerCustomsDeclaration.RequirementType.Split(",");
                            requirementType = input.TrailerCustomsDeclaration.RequirementType;
                            foreach (var bizType in bizTypeArr)
                            {
                                if (isEn)
                                {
                                    var typeName = dics.Where(x => x.Dictid == "3600" && x.Code == bizType).FirstOrDefault()?.Ename;
                                    requirementType = requirementType.Replace(bizType, typeName);
                                }
                                else
                                {
                                    var typeName = dics.Where(x => x.Dictid == "3600" && x.Code == bizType).FirstOrDefault()?.Cname;
                                    requirementType = requirementType.Replace(bizType, typeName);
                                }

                            }

                        }
                        airParagraph30.Append(new Run(new Text($"需求类型：{requirementType}")));
                        if (tcd.RequirementType == "10")
                        {
                            airParagraph30.Append(new Run(new Break()));//换行
                            string declarationMethod = "";
                            if (!string.IsNullOrEmpty(tcd.DeclarationMethod))
                            {
                                if (isEn)
                                {
                                    declarationMethod = dics.Where(x => x.Dictid == "3900" && x.Code == tcd.DeclarationMethod).FirstOrDefault()?.Ename;
                                }
                                else
                                {
                                    declarationMethod = dics.Where(x => x.Dictid == "3900" && x.Code == tcd.DeclarationMethod).FirstOrDefault()?.Cname;
                                }
                            }
                            airParagraph30.Append(new Run(new Text($"报关类型：{declarationMethod}")));
                            pars.Add(airParagraph30);
                        }
                        else 
                        {
                            /*airParagraph30.Append(new Run(new Text($"提货厂名       {temPro.RequirementType}")));*/
                            airParagraph30.Append(new Run(new Break()));
                            airParagraph30.Append(new Run(new Text($"提货厂名：{tcd.PickUpName}")));
                            airParagraph30.Append(new Run(new Break()));
                            airParagraph30.Append(new Run(new Text($"提货地址：{tcd.PickUpAddr}")));
                            airParagraph30.Append(new Run(new Break()));
                            airParagraph30.Append(new Run(new Text($"联系人及电话：{tcd.PickUpContact}")));
                            airParagraph30.Append(new Run(new Break()));
                            airParagraph30.Append(new Run(new Text($"其它拖车要求：{tcd.TrailerRequirements}")));
                            pars.Add(airParagraph30);
                        }
                    }
                   
                    /*StylesPart stylePart = wordDoc.MainDocumentPart.StyleDefinitionsPart;
                    if (stylePart != null)
                    {
                        Styles styles = stylePart.Styles;
                        if (styles != null)
                        {
                            foreach (Style style in styles.Elements<Style>())
                            {
                                // 在这里处理样式，例如输出样式的名称
                                string styleName = style.StyleName.Val;
                                Console.WriteLine("Style Name: " + styleName + ": id" + style.StyleId);
                            }
                        }
                    }*/

                    #region 对齐方式和添加样式测试
                    /* // 创建一个新的段落
                     Paragraph newParagraph = new Paragraph(new Run(new Text("测试居右对齐")));

                     // 创建一个新的段落属性对象
                     ParagraphProperties paragraphProperties = new ParagraphProperties();

                     // 创建一个新的对齐方式对象，并设置为居中对齐
                     Justification justification = new Justification() { Val = JustificationValues.Right };

                     // 将对齐方式对象添加到段落属性对象中
                     paragraphProperties.Append(justification);
                     //段落属性对象分配给段落
                     newParagraph.AppendChild(paragraphProperties);
                     pars.Add(newParagraph);*/
                    // 创建一个新的段落
                    /*StylesPart stylePart = wordDoc.MainDocumentPart.StyleDefinitionsPart;

                    if (stylePart != null)
                    {
                        Styles styles = stylePart.Styles;
                        if (styles != null)
                        {
                            foreach (Style style in styles.Elements<Style>())
                            {
                                // 在这里处理样式，例如输出样式的名称
                                string styleName = style.StyleName.Val;
                                Console.WriteLine("Style Name: " + styleName+": id"+ style.StyleId);
                            }
                        }
                    }

                    Paragraph heading1Paragraph = new Paragraph();
                    
                    // 添加一个文本运行到段落中
                    Run run11 = new Run(new Text("这是标题1段落"));
                    Style fourthStyle = stylePart.Styles.Elements<Style>().ElementAtOrDefault(1);
                    // 设置段落的样式为标题1
                    heading1Paragraph.ParagraphProperties = new ParagraphProperties(
                        new ParagraphStyleId { Val = fourthStyle.StyleId }
                    );
                    // 将运行添加到段落中
                    heading1Paragraph.Append(run11);
                    pars.Add(heading1Paragraph);*/
                    /*var htmlStr = "<root>" + DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Idx == 21790).First().Detail + "</root>";
                    var par1 = ProcessingNodeContent(htmlStr, wordDoc);
                    pars.AddRange(par1);*/
                    #endregion
                    if (pars.Count() > 0)
                    {
                        foreach (var par in pars)
                        {
                            Paragraph lastParagraph = body.Elements<Paragraph>().LastOrDefault();
                            lastParagraph.InsertAfterSelf(par);
                        }
                    }
                }
                if (!string.IsNullOrEmpty(input.Remark))
                {
                    List<Paragraph> remarkPars = new List<Paragraph>();
                    Paragraph back = new Paragraph();
                    remarkPars.Add(back);
                    Paragraph remarkTitleParagraph = new Paragraph();
                    num++;
                    Run remarkRun = new Run(new Text($"{num}.备注"));
                    //*/ 为运行设置属性：字体颜色
                    var remarkRgbys = RGBStringToHex("rgb(0, 189, 248)");
                    // 为运行设置字体颜色
                    remarkRun.RunProperties = new RunProperties(
                        new Color() { Val = remarkRgbys }
                    );
                    remarkTitleParagraph.ParagraphProperties = new ParagraphProperties(
                      new ParagraphStyleId { Val = "3" }
                    );
                    remarkTitleParagraph.Append(remarkRun);
                    remarkPars.Add(remarkTitleParagraph);
                    var remarkArr = input.Remark.Split("\n");
                    Paragraph remarkTextParagraph = new Paragraph();
                    foreach (var item in remarkArr)
                    {
                        remarkTextParagraph.Append(new Run(new Text(item)));
                        remarkTextParagraph.Append(new Run(new Break()));
                    }
                    remarkPars.Add(remarkTextParagraph);
                    if (remarkPars.Count() > 0)
                    {
                        foreach (var par in remarkPars)
                        {
                            Paragraph lastParagraph = body.Elements<Paragraph>().LastOrDefault();
                            lastParagraph.InsertAfterSelf(par);
                        }
                    }
                }
                
                var attachments=DB.SqlSugarClient().Queryable<FileManage>().Where(x => x.RelationId == input.Id && x.RelationTableName == "sop_order_attachment").ToList();
                if (attachments.Count > 0)
                {
                    num++;
                    List<Paragraph> attachmentPars = new List<Paragraph>();
                    Paragraph attachmentTitleParagraph = new Paragraph();
                    //attachmentTitleParagraph.Append(new Run(new Break()));
                    Run attachmentRun = new Run(new Text($"{num}.附件"));
                    var attachmentRgbys = RGBStringToHex("rgb(0, 189, 248)");
                    // 为运行设置字体颜色
                    attachmentRun.RunProperties = new RunProperties(
                        new Color() { Val = attachmentRgbys }
                    );
                    attachmentTitleParagraph.ParagraphProperties = new ParagraphProperties(
                      new ParagraphStyleId { Val = "3" }
                    );
                    attachmentTitleParagraph.Append(attachmentRun);
                    attachmentPars.Add(attachmentTitleParagraph);
                    Paragraph attachmentParagraph = new Paragraph();
                    int n = 1;
                    foreach (var item in attachments)
                    {
                        var linkAddress = $"https://api.kwesz.com.cn/MstSopService/api/Minio/Download?path={item.FilePath}";
                        // 创建超链接关系
                        var hyperLinkShip = wordDoc.MainDocumentPart.AddHyperlinkRelationship(new Uri(linkAddress), true);
                        // 创建超链接
                        Hyperlink hyperlink = new Hyperlink();
                        hyperlink.Anchor = linkAddress; // 设置超链接地址
                        hyperlink.Tooltip = "点击下载"; // 设置超链接提示文本
                        hyperlink.Id = hyperLinkShip.Id; // 使用超链接关系的 Id
                        var hyperLinkProp = new RunProperties();
                        hyperLinkProp.Underline = new Underline() { Val = UnderlineValues.Single };
                        hyperLinkProp.Color = new Color() { ThemeColor = ThemeColorValues.Hyperlink };
                        hyperLinkProp.RunStyle = new RunStyle() { Val = "Hyperlink" };
                        // 创建运行并将其添加到超链接中
                        Run linkContent = new Run(new Text($"({n}).{item.FileName}"));//超链接显示的字
                        linkContent.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "20" }); // 设置字体大小
                        linkContent.Append(hyperLinkProp);
                        hyperlink.Append(linkContent);
                        attachmentParagraph.Append(hyperlink);
                        n++;
                        attachmentParagraph.Append(new Run(new Break()));

                    }
                    attachmentPars.Add(attachmentParagraph);
                    if (attachmentPars.Count() > 0)
                    {
                        foreach (var par in attachmentPars)
                        {
                            Paragraph lastParagraph = body.Elements<Paragraph>().LastOrDefault();
                            lastParagraph.InsertAfterSelf(par);
                        }
                    }
                }

                /* 添加附件显示（超链接形式） sop_order_attachment*/

            }
            #endregion

            //保存文档
            wordDoc.Save();
            //释放资源
            wordDoc.Dispose();
            //上传到Minio
            if (!sign)
            {
                var fileUrl = $@"{BucketRootFolder.SopUploader}/{fileName}";
                bool ret = MinioPub.UploadFile(temporaryName, fileUrl, BucketName.FileBucket).GetAwaiter().GetResult();
                if (ret)
                {
                    var version = DB.SqlSugarClient().Queryable<SopOrderContentText>().Where(x => x.SopOrderId == input.Id).Count();
                    var fileMan = new SopOrderContentText()
                    {
                        FileName = $"v{version + 1}",
                        FilePath = fileUrl,
                        Createuser = currentUser.Userid,
                        Createdate = DateTime.Now,
                        Companyid = currentUser.Ccode,
                        SopOrderId = input.Id,
                        IsDelete = false
                    };
                    DB.SqlSugarClient().Insertable<SopOrderContentText>(fileMan).ExecuteCommand();
                }
                else 
                {
                    throw new Exception("模板上传失败");
                }
                File.Delete(temporaryName);
            }
            return temporaryName;
        }
        public IActionResult Test()
        {
            /*
             设置居中案例
              Paragraph newParagraph = new Paragraph(new Run(new Text("测试居右对齐")));

                     // 创建一个新的段落属性对象
                     ParagraphProperties paragraphProperties = new ParagraphProperties();

                     // 创建一个新的对齐方式对象，并设置为居中对齐
                     Justification justification = new Justification() { Val = JustificationValues.Right };

                     // 将对齐方式对象添加到段落属性对象中
                     paragraphProperties.Append(justification);
                     //段落属性对象分配给段落
                     newParagraph.AppendChild(paragraphProperties);
             */
            /*
             添加标题案例
            Paragraph heading1Paragraph = new Paragraph();
                    
                    // 添加一个文本运行到段落中
                    Run run11 = new Run(new Text("这是标题1段落"));
                    Style fourthStyle = stylePart.Styles.Elements<Style>().ElementAtOrDefault(1);
                    // 设置段落的样式为标题1
                    heading1Paragraph.ParagraphProperties = new ParagraphProperties(
                        new ParagraphStyleId { Val = fourthStyle.StyleId }
                    );
                    // 将运行添加到段落中
                    heading1Paragraph.Append(run11);
             */
            /*var htmlStr = "<root>" + DB.SqlSugarClient().Queryable<SopBase>().Where(x => x.Idx == 21790).First().Detail + "</root>";
            htmlStr = htmlStr.Replace("<br>", "");
            htmlStr = htmlStr.Replace("&nbsp;", " ");
            htmlStr = FixImgTags(htmlStr);
            // 加载 HTML 内容到 XDocument
            XDocument doc = XDocument.Parse(htmlStr);

            // 遍历文档根节点的子节点
            foreach (XNode node in doc.Root.Nodes())
            {
                if (node is XElement element)
                {
                    var parStyle = element.FirstAttribute?.Value;
                    Console.WriteLine($"<{element.Name.LocalName}>:");
                    if (!string.IsNullOrEmpty(parStyle))
                    {
                        int num=parStyle.IndexOf(":") + 2;
                        if (num!=1) 
                        {
                            parStyle = parStyle.Substring(num, parStyle.Length - (num+1));
                        }
                        
                    }
                    Console.WriteLine("--------------------");
                    Console.WriteLine("父节点样式" + parStyle);
                    // var rgbys=RGBStringToHex("rgb(254, 52, 52)");
                    //center    left      right
                    // 如果当前节点有子节点，遍历子节点
                    foreach (XNode childNode in element.Nodes())
                    {
                        if (childNode is XElement childElement)//判断p标签里面或者h标签里面是否还有子标签（有就取子节点）
                        {
                            var style = childElement.Name.LocalName == "img" ? childElement.LastAttribute : childElement.FirstAttribute;
                            Console.WriteLine($"  <{childElement.Name.LocalName}>: {childElement.Value.Trim()}对应样式{style}");
                        }
                        else //没有就取当前节点
                        {
                            // 输出当前节点名称和文本内容
                            Console.WriteLine($"<{element.Name.LocalName}>: {childNode}");
                        }
                    }
                    Console.WriteLine("--------------------");
                }
            }*/
            //string htmlData = "<root><p>- 深圳机场2022年6月开始实行预申报报关模式。（提货-入前置仓-过磅出预配-报关审结-入货站仓-理货安检-放行上机）</p><p>   申报毛重与货站过磅重量误差不能超出3%，超过只能申请删除报关单并办理退仓，不能改单。</p><p>- 空运货可提前48小时入仓，国际货站一部直航维持常态48小时车辆审核预约，但是优先审核24小时之内的航班。</p><p>- 因深圳机场底账系统升级：2021年4月1日切换成新模式 ：单票货物如果对应两套或两套以上报关资料的,只能一套报关单证对应一个HAWB。</p><p></p><p><span style=\"color: rgb(207, 19, 34);\">如涉及绿通备案：</span></p><p><span style=\"font-size: 14px;\">绿通需多方授权，客户需与航司/航站楼/销售代理人申请备案；满足两个条件：</span></p><p><span style=\"font-size: 14px;\">①航空公司 航站楼备案；②销售代理人备案。</span></p><p style=\"text-align: start;\"><span style=\"font-size: 14px;\"><strong>差异化安检 ：</strong></span><span style=\"font-size: 14px;\">客户属白名单企业且申请相关备案（航站楼&对应航空公司清单），</span></p><p style=\"text-align: start;\"><span style=\"font-size: 14px;\">                       且地面代理可以直接使用 （但对应的地面代理需有操作资质，地面代理也应为白名单企业清单内）</span></p><p style=\"text-align: start;\"><span style=\"font-size: 14px;\"><strong>绿色通道：</strong></span><span style=\"font-size: 14px;\">客户与航站楼或航空公司申请备案  备案授权销售代理人 ，且提供航空运输鉴定报告正本  UN38.3报告。（如</span>报告在监管区有备案可不携带正本）<span style=\"font-size: 14px;\">）</span></p><p>链接如下：</p><p>\\\\SER-FS1\\share\\Forwarding（AIR）\\SOP系统\\2 SOP系统同步\\2 空运出口\\1 航线注意事项\\1 起运港\\2 绿通备案</p><p></p></root>";
            string htmlData = "<root><p>- 深圳机场2022年6月开始实行预申报报关模式。（提货-入前置仓-过磅出预配-报关审结-入货站仓-理货安检-放行上机）</p><p>   申报毛重与货站过磅重量误差不能超出3%，超过只能申请删除报关单并办理退仓，不能改单。</p><p>- 空运货可提前48小时入仓，国际货站一部直航维持常态48小时车辆审核预约，但是优先审核24小时之内的航班。</p><p>- 因深圳机场底账系统升级：2021年4月1日切换成新模式 ：单票货物如果对应两套或两套以上报关资料的,只能一套报关单证对应一个HAWB。</p><p></p><p><span style=\"color: rgb(207, 19, 34);\">如涉及绿通备案：</span></p><p><span style=\"font-size: 14px;\">绿通需多方授权，客户需与航司/航站楼/销售代理人申请备案；满足两个条件：</span></p><p><span style=\"font-size: 14px;\">①航空公司 航站楼备案；②销售代理人备案。</span></p><p style=\"text-align: start;\"><span style=\"font-size: 14px;\"><strong>差异化安检 ：</strong></span><span style=\"font-size: 14px;\">客户属白名单企业且申请相关备案（航站楼对应航空公司清单），</span></p><p style=\"text-align: start;\"><span style=\"font-size: 14px;\">                       且地面代理可以直接使用 （但对应的地面代理需有操作资质，地面代理也应为白名单企业清单内）</span></p><p style=\"text-align: start;\"><span style=\"font-size: 14px;\"><strong>绿色通道：</strong></span><span style=\"font-size: 14px;\">客户与航站楼或航空公司申请备案  备案授权销售代理人 ，且提供航空运输鉴定报告正本  UN38.3报告。（如</span>报告在监管区有备案可不携带正本）<span style=\"font-size: 14px;\">）</span></p><p>链接如下：</p><p>\\\\SER-FS1\\share\\Forwarding（AIR）\\SOP系统\\2 SOP系统同步\\2 空运出口\\1 航线注意事项\\1 起运港\\2 绿通备案</p><p></p></root>";
            XDocument htmlDocument = XDocument.Parse(htmlData);
            int i = 0;
            /*
             <root><p>- 深圳机场2022年6月开始实行预申报报关模式。（提货-入前置仓-过磅出预配-报关审结-入货站仓-理货安检-放行上机）</p><p>   申报毛重与货站过磅重量误差不能超出3%，超过只能申请删除报关单并办理退仓，不能改单。</p><p>- 空运货可提前48小时入仓，国际货站一部直航维持常态48小时车辆审核预约，但是优先审核24小时之内的航班。</p><p>- 因深圳机场底账系统升级：2021年4月1日切换成新模式 ：单票货物如果对应两套或两套以上报关资料的,只能一套报关单证对应一个HAWB。</p><p></p><p><span style="color: rgb(207, 19, 34);">如涉及绿通备案：</span></p><p><span style="font-size: 14px;">绿通需多方授权，客户需与航司/航站楼/销售代理人申请备案；满足两个条件：</span></p><p><span style="font-size: 14px;">①航空公司& 航站楼备案；②销售代理人备案。</span></p><p style="text-align: start;"><span style="font-size: 14px;"><strong>差异化安检 ：</strong></span><span style="font-size: 14px;">客户属白名单企业且申请相关备案（航站楼&对应航空公司清单），</span></p><p style="text-align: start;"><span style="font-size: 14px;">                       且地面代理可以直接使用 （但对应的地面代理需有操作资质，地面代理也应为白名单企业清单内）</span></p><p style="text-align: start;"><span style="font-size: 14px;"><strong>绿色通道：</strong></span><span style="font-size: 14px;">客户与航站楼或航空公司申请备案 & 备案授权销售代理人 ，且提供航空运输鉴定报告正本 & UN38.3报告。（如</span>报告在监管区有备案可不携带正本）<span style="font-size: 14px;">）</span></p><p>链接如下：</p><p>\\SER-FS1\share\Forwarding（AIR）\SOP系统\2 SOP系统同步\2 空运出口\1 航线注意事项\1 起运港\2 绿通备案</p><p></p></root>XDocument htmlDocument = XDocument.Parse(htmlData)
             */
            return MstResult.Success("操作成功");
        }

        public Paragraph AddHyperlink(WordprocessingDocument wordDoc,List<FileManage> files)
        {
            Paragraph attachmentParagraph = new Paragraph();
            int n = 1;
            foreach (var item in files)
            {
                var linkAddress = $"https://api.kwesz.com.cn/MstSopService/api/Minio/Download?path={item.FilePath}";
                // 创建超链接关系
                var hyperLinkShip = wordDoc.MainDocumentPart.AddHyperlinkRelationship(new Uri(linkAddress), true);
                // 创建超链接
                Hyperlink hyperlink = new Hyperlink();
                hyperlink.Anchor = linkAddress; // 设置超链接地址                hyperlink.Tooltip = "点击下载"; // 设置超链接提示文本
                hyperlink.Id = hyperLinkShip.Id; // 使用超链接关系的 Id
                var hyperLinkProp = new RunProperties();
                hyperLinkProp.Underline = new Underline() { Val = UnderlineValues.Single };
                hyperLinkProp.Color = new Color() { ThemeColor = ThemeColorValues.Hyperlink };
                hyperLinkProp.RunStyle = new RunStyle() { Val = "Hyperlink" };
                // 创建运行并将其添加到超链接中
                Run linkContent = new Run(new Text($"({n}).{item.FileName}"));//超链接显示的字
                linkContent.RunProperties = new RunProperties(new DocumentFormat.OpenXml.Wordprocessing.FontSize() { Val = "20" }); // 设置字体大小
                linkContent.Append(hyperLinkProp);
                hyperlink.Append(linkContent);
                attachmentParagraph.Append(hyperlink);
                n++;
                attachmentParagraph.Append(new Run(new Break()));

            }
            return attachmentParagraph;
        }
        private List<Paragraph> ProcessingNodeContent(string htmlData, WordprocessingDocument wordDoc)
        {
            //创建一个新的 Paragraph 对象
            List<Paragraph> paragraphs = new List<Paragraph>();
            //替换不规则标签,和字符
            htmlData = WebUtility.HtmlDecode(htmlData);
            htmlData = htmlData.Replace("<br>", "");
            htmlData = htmlData.Replace("&nbsp;", " ");
            htmlData = htmlData.Replace("&", "&amp;");
            
            htmlData = FixImgTags(htmlData);
            // 解析 HTML 数据
            XDocument htmlDocument = XDocument.Parse(htmlData);
            // 遍历文档根节点的子节点
            foreach (XNode node in htmlDocument.Root.Nodes())
            {
                if (node is XElement element)
                {
                    Paragraph paragraph = new Paragraph();
                    var parStyle = element.FirstAttribute?.Value;
                    string[] titleArr= { "h1", "h2", "h3" };
                    if (titleArr.Contains(element.Name.LocalName))//添加了标题样式 
                    {
                        var styleId = "";
                        if (element.Name.LocalName=="h1")
                        {
                            styleId = "2";
                        } else if (element.Name.LocalName == "h2")
                        {
                            styleId = "3";
                        }
                        else 
                        {
                            styleId = "4";
                        }
                        paragraph.ParagraphProperties = new ParagraphProperties(
                        new ParagraphStyleId { Val = styleId } );
                    }
                    #region 处理父级对齐属性
                    if (!string.IsNullOrEmpty(parStyle))
                    {
                        int num = parStyle.IndexOf(":") + 2;
                        if (num != 1)
                        {
                            parStyle = parStyle.Substring(num, parStyle.Length - (num + 1));
                            string[] keywords = { "left", "right", "center" };
                            if (keywords.Contains(parStyle))
                            {
                                // 创建一个新的段落属性对象
                                ParagraphProperties paragraphProperties = new ParagraphProperties();
                                // 创建一个新的对齐方式对象
                                Justification justification = new Justification();
                                switch (parStyle)
                                {
                                    case "left":
                                        justification.Val = JustificationValues.Left;
                                        break;
                                    case "right":
                                        justification.Val = JustificationValues.Right;
                                        break;
                                    case "center":
                                        justification.Val = JustificationValues.Center;
                                        break;
                                    default:
                                        break;
                                }
                                // 将对齐方式对象添加到段落属性对象中
                                paragraphProperties.Append(justification);
                                paragraph.AppendChild(paragraphProperties);
                            }
                        }
                    }
                    #endregion
                    // 如果当前节点有子节点，遍历子节点
                    foreach (XNode childNode in element.Nodes())
                    {
                        if (childNode is XElement childElement)//判断p标签里面或者h标签里面是否还有子标签（有就取子节点）
                        {
                            var style = childElement.Name.LocalName == "img" ? childElement.LastAttribute.Value : childElement.FirstAttribute.Value;
                            //Console.WriteLine($"  <{childElement.Name.LocalName}>: {childElement.Value.Trim()}对应样式{style}");
                            if (childElement.Name.LocalName == "img")
                            {
                                //获取base64字符串
                                string base64Pattern = @"data:image\/\w+;base64,([\w\/+=]+)";
                                MatchCollection matches = Regex.Matches(childElement.FirstAttribute.Value, base64Pattern);
                                var base64Str = matches.FirstOrDefault().Groups[1].Value;
                                ImagePart imagePart = wordDoc.MainDocumentPart.AddImagePart(ImagePartType.Jpeg);
                                using (var stream = new MemoryStream(Convert.FromBase64String(base64Str)))
                                {
                                    imagePart.FeedData(stream);
                                }
                                long width = 0;
                                long height = 0;
                                if (style==""||style.Contains("%"))
                                {
                                    int percentage = 1;
                                    if (style != "")
                                    {
                                        style = style.Replace("width: ", "");
                                        style = style.Replace("%;", "");
                                        percentage = (int)Math.Round(double.Parse(style));
                                    }
                                    else 
                                    {
                                        percentage = 100;
                                    }
                                    byte[] imageBytes = Convert.FromBase64String(base64Str);
                                    using (MemoryStream ms = new MemoryStream(imageBytes))
                                    {
                                        Image image = Image.FromStream(ms);
                                        width = image.Width * 9144 / 96 * percentage;
                                        height = image.Height * 9144 / 96 * percentage;
                                    }
                                }
                                else 
                                {
                                    var whArr = style.Split(";");
                                    var widthPx = whArr[0];
                                    widthPx = widthPx.Replace("width: ", "");
                                    widthPx = widthPx.Replace("px", "");
                                    width = (long)(double.Parse(widthPx) * 914400 / 96);
                                    var heightPx = whArr[1];
                                    heightPx = heightPx.Replace("height: ", "");
                                    heightPx = heightPx.Replace("px", "");
                                    height = (long)(double.Parse(heightPx) * 914400 / 96);
                                }
                                paragraph.Append(new Run(AddImageToBody(wordDoc.MainDocumentPart.GetIdOfPart(imagePart), width, height)));
                            }
                            else
                            {
                                /*
                                <span style="color: rgb(216, 68, 147);">
                                  <s>删除线
                                    <u>下划线
                                      <em>斜体
                                        <strong>34</strong>粗体
                                      </em>
                                    </u>
                                  </s>
                                </span>
                                */
                                Run run = new Run(new Text(childElement.Value));
                                // 创建 RunProperties 对象
                                RunProperties runProperties = new RunProperties();
                                if (childElement.ToString().Contains("<strong>")) 
                                {
                                    // 向 RunProperties 添加粗体属性
                                    runProperties.Append(new Bold());
                                }
                                if (childElement.ToString().Contains("<em>"))
                                {
                                    // 向 RunProperties 添加斜体属性
                                    runProperties.Append(new Italic());
                                }
                                if (childElement.ToString().Contains("<u>"))
                                {
                                    // 向 RunProperties 添加下划线属性，这里使用单下划线，UnderlineValues.Single
                                    runProperties.Append(new Underline { Val = UnderlineValues.Single });
                                }
                                if (childElement.ToString().Contains("<s>"))
                                {
                                    // 向 RunProperties 添加删除线属性
                                    runProperties.Append(new Strike());
                                }
                                var color = childElement.FirstAttribute?.Value;
                                if (!string.IsNullOrEmpty(color))//设置颜色，目前先支持rgb形式
                                {
                                    string rgbPattern = @"rgb\((\d{1,3}),\s*(\d{1,3}),\s*(\d{1,3})\)";
                                    if (Regex.Match(color, rgbPattern).Success) 
                                    {
                                        var rgbys = RGBStringToHex(color);
                                        runProperties.Append(new Color() { Val = rgbys });
                                    };
                                    Console.WriteLine(color+":"+Regex.Match(color, rgbPattern).Success);
                                }
                                run.Append(runProperties);
                                paragraph.Append(run);
                            }
                        }
                        else //没有就取当前节点
                        {
                            // 输出当前节点名称和文本内容
                            //Console.WriteLine($"<{element.Name.LocalName}>: {childNode}");
                            paragraph.Append(new Run(new Text(childNode.ToString())));
                        }
                    }
                    paragraphs.Add(paragraph);
                }
            }
            return paragraphs;
        }
        #region 代码备份
        /*
          private Paragraph ProcessingNodeContent(string htmlData, WordprocessingDocument wordDoc)
        {
            //创建一个新的 Paragraph 对象
            Paragraph paragraph = new Paragraph();
            //创建换行
            //Break lineBreak = new Break();
            htmlData = WebUtility.HtmlDecode(htmlData);
            htmlData = htmlData.Replace("<br>", "");
            // 解析 HTML 数据
            XDocument htmlDocument = XDocument.Parse(htmlData);
            string pattern = @"<p>(.*?)</p>";
            MatchCollection matches = Regex.Matches(htmlData, pattern);
            foreach (Match match in matches)
            {
                string base64Pattern = @"data:image\/\w+;base64,([\w\/+=]+)";
                string imgPattern = @"<img[^>]+/>";
                string stylePattern = "style=\"(.*?)\"";
                MatchCollection matches1 = Regex.Matches(match.Groups[1].Value, base64Pattern);
                MatchCollection matches2 = Regex.Matches(match.Groups[1].Value, imgPattern);
                MatchCollection matches3 = Regex.Matches(match.Groups[1].Value, stylePattern);

                if (matches1.Count() == 0)//首先先处理每行没带图片的
                {
                    string content = match.Groups[1].Value;
                    paragraph.Append(new Run(new Break()));
                    paragraph.Append(new Run(new Text(content)));
                }
                else
                {
                    paragraph.Append(new Run(new Break()));
                    string str1 = match.Groups[1].Value;
                    str1 = Regex.Replace(str1, @"<img[^>]+/>", "丨");//把img标签替换成特殊字符
                                                                    //找到每个图片的下标
                    List<int> indices = new List<int>();
                    int index = -1;
                    while ((index = str1.IndexOf("丨", index + 1)) != -1)
                    {
                        indices.Add(index);
                    }
                    int num = 0;//用于记录次数
                    int flagnum = 0;//用于记录图片下标
                    foreach (var item in indices)
                    {
                        if (item == 0)//代表图片 
                        {
                            //base64图片需要处理插入到word
                            flagnum = indices[num] + 1;
                            ImagePart imagePart = wordDoc.MainDocumentPart.AddImagePart(ImagePartType.Jpeg);
                            string base64Str = matches1[num].Groups[1].Value;
                            using (var stream = new MemoryStream(Convert.FromBase64String(base64Str)))
                            {
                                imagePart.FeedData(stream);

                            }
                            long width = 0;
                            long height = 0;
                            var whArr = matches3[num].Groups[1].Value.Split(";");
                            if (whArr.Length > 1)
                            {
                                if (whArr[1] == "")
                                {
                                    var width11 = whArr[0];
                                    width11 = width11.Replace("width: ", "");
                                    width11 = width11.Replace("%", "");
                                    var percentage = (int)Math.Round(double.Parse(width11));
                                    byte[] imageBytes = Convert.FromBase64String(base64Str);
                                    using (MemoryStream ms = new MemoryStream(imageBytes))
                                    {
                                        Image image = Image.FromStream(ms);
                                        width = image.Width * 9144 / 96 * percentage;
                                        height = image.Height * 9144 / 96 * percentage;
                                    }
                                }
                                else
                                {
                                    var width11 = whArr[0];
                                    width11 = width11.Replace("width: ", "");
                                    width11 = width11.Replace("px", "");
                                    width = (long)(double.Parse(width11) * 914400 / 96);
                                    var height11 = whArr[1];
                                    height11 = height11.Replace("height: ", "");
                                    height11 = height11.Replace("px", "");
                                    height = (long)(double.Parse(height11) * 914400 / 96);
                                }

                            }
                            else
                            {
                                byte[] imageBytes = Convert.FromBase64String(base64Str);
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    Image image = Image.FromStream(ms);

                                    width = image.Width * 914400 / 96;
                                    height = image.Height * 914400 / 96;
                                }
                            }
                            AddImageToBody(wordDoc, wordDoc.MainDocumentPart.GetIdOfPart(imagePart), paragraph, width, height);
                        }
                        else
                        {
                            int fg = 0;
                            if (num == 0 || item - indices[num - 1] != 1) //代表文字
                            {
                                var textbox = str1.Substring(flagnum, item - flagnum);

                                paragraph.Append(new Run(new Text(textbox)));
                                fg++;
                            }
                            flagnum = indices[num] + 1;
                            //base64图片需要处理插入到word
                            //matches1[num].Groups[1].Value;
                            ImagePart imagePart = wordDoc.MainDocumentPart.AddImagePart(ImagePartType.Jpeg);
                            string base64Str = matches1[num].Groups[1].Value;

                            using (var stream = new MemoryStream(Convert.FromBase64String(base64Str)))
                            {
                                imagePart.FeedData(stream);

                            }
                            long width = 0;
                            long height = 0;

                            var whArr = matches3[num].Groups[1].Value.Split(";");
                            if (whArr.Length > 1)
                            {
                                if (whArr[1] == "")
                                {
                                    var width11 = whArr[0];
                                    width11 = width11.Replace("width: ", "");
                                    width11 = width11.Replace("%", "");
                                    var percentage = (int)Math.Round(double.Parse(width11));
                                    byte[] imageBytes = Convert.FromBase64String(base64Str);
                                    using (MemoryStream ms = new MemoryStream(imageBytes))
                                    {
                                        Image image = Image.FromStream(ms);
                                        width = (long)(image.Width * 9144 / 96 * percentage);
                                        height = (long)(image.Height * 9144 / 96 * percentage);
                                    }
                                }
                                else
                                {
                                    var width11 = whArr[0];
                                    width11 = width11.Replace("width: ", "");
                                    width11 = width11.Replace("px", "");
                                    width = (long)(double.Parse(width11) * 914400 / 96);
                                    var height11 = whArr[1];
                                    height11 = height11.Replace("height: ", "");
                                    height11 = height11.Replace("px", "");
                                    height = (long)(double.Parse(height11) * 914400 / 96);
                                }
                            }
                            else
                            {
                                byte[] imageBytes = Convert.FromBase64String(base64Str);
                                using (MemoryStream ms = new MemoryStream(imageBytes))
                                {
                                    Image image = Image.FromStream(ms);

                                    width = image.Width * 914400 / 96;
                                    height = image.Height * 914400 / 96;
                                }
                            }
                            AddImageToBody(wordDoc, wordDoc.MainDocumentPart.GetIdOfPart(imagePart), paragraph, width, height);
                        }
                        num++;
                    }
                    if (str1.Length - (indices[indices.Count() - 1]) != 1)
                    {
                        var ending = str1.Substring((indices[indices.Count() - 1] + 1), (str1.Length - indices[indices.Count() - 1] - 1));
                        paragraph.Append(new Run(new Text(ending)));
                    }
                }
            }
            return paragraph;
        }
         */
        #endregion
        private static Drawing AddImageToBody(string relationshipId,long width, long height)
        {
            // 创建一个Drawing对象，用于插入图片
            Drawing drawing = new Drawing();

            // 添加图片到Drawing对象中
            // 这里的图片元素可以是之前你创建的Drawing对象
            drawing.AppendChild(new DW.Inline(new DW.Extent() { Cx = width, Cy = height },
                new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties() { Id = (UInt32Value)1U, Name = "Picture 1" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                new A.Graphic(new A.GraphicData(new PIC.Picture(
                    new PIC.NonVisualPictureProperties(
                        new PIC.NonVisualDrawingProperties() { Id = (UInt32Value)0U, Name = "New Bitmap Image.jpg" },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(new A.Blip() { Embed = relationshipId },
                        new A.Stretch(new A.FillRectangle())),
                    new PIC.ShapeProperties(new A.Transform2D(new A.Offset() { X = 0L, Y = 0L }, new A.Extents() { Cx = 1000000L, Cy = 1000000L }),
                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))))));

            // 将Drawing对象添加到运行中
            //par.Append(new Run(drawing));
            return drawing;
        }
    }
}
