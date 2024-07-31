using AutoMapper;
using MstCore;
using MstCore.Helper;
using MstDB;
using MstDBComman.Config;
using MstDBComman.Enums;
using MstDBComman.Models;
using MstSopService.Caches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MstSopService.Tools
{

    /// <summary>
    /// 
    /// </summary>
    public class VariableModelTool
    {
        IDatabase DB;
        IMapper Mapper;
        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="database">数据库</param>
        /// <param name="mapper">IMapper实例</param>
        public VariableModelTool( IDatabase database, IMapper mapper, MstCacheService mstCacheService)
        {
            MstCacheService.Init();
            DB = database;
            this.Mapper = mapper;
        }
        public static string MysqlStrFormmate(string sql)
        {


            sql = sql.Replace("'", "\\'" + "");
            sql = sql.Replace("\"", "\\" + "\"");

            return sql;

        }
        /// <summary>
        /// 查询参数数据变成SQL语句字符串
        /// </summary>
        /// <param name="Conditions"></param>
        /// <returns></returns>
        public static string MysqlStr(List<QueryCondition> Conditions)
        {
            Conditions = Conditions.Where(p => !string.IsNullOrEmpty(p.Value.ToString())).ToList();
            var retSql = "";
            if (Conditions is null)
            {
                return "";
            }
            var s = 0;
            foreach (var item in Conditions)
            {
                if (item.Character == QueryCharacter.And && !StrUtil.IsEmpty(item.Value))
                {
                    retSql += "  and ";
                }
                else
                {
                    if (s==0)
                    {
                        retSql += "  and ";
                    }
                    else
                    {
                        retSql += "  or ";
                    }
                }
                s++;
                switch (item.Operator)
                {
                    case QueryOperator.Equal:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";
                            retSql += $" {item.Key}='{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";
                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.Like:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";
                            retSql += $"  {item.Key} like  '%{MysqlStrFormmate(item.Value.ToString())}%'  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.GreaterThan:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(" + retSql + "1=1)";

                            retSql += $" {item.Key}>'{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.GreaterThanOrEqual:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key}>='{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.LessThan:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key}<'{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.LessThanOrEqual:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key}<='{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.In:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key} in ('{ string.Join( "','",MysqlStrFormmate(item.Value.ToString()).Split(","))}')  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.NotIn:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key} not in ('{ string.Join("','", MysqlStrFormmate(item.Value.ToString()).Split(","))}')  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.LikeLeft:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";
                            retSql += $"  {item.Key} like  '%{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";
                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.LikeRight:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";
                            retSql += $"  {item.Key} like  '{MysqlStrFormmate(item.Value.ToString())}%'  ";
                            retSql += ")";
                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.NoEqual:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key}!='{MysqlStrFormmate(item.Value.ToString())}'  ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.IsNullOrEmpty:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" ({item.Key} is null or {item.Key}='') ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.IsNot:
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" ({item.Key} is not null  and {item.Key}!='') ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.DateRange:
                        var strDate = item.Key.Split("|");
                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key} BETWEEN   {strDate[0]} and  {strDate[1]}   ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    case QueryOperator.NoLike:

                        if (!StrUtil.IsEmpty(item.Value))
                        {
                            retSql += "(";

                            retSql += $" {item.Key}  not like '%{MysqlStrFormmate(item.Value.ToString())}%' ";
                            retSql += ")";

                        }
                        else
                        {
                            retSql += "(" + retSql + "1=1)";
                        }
                        break;
                    default:
                        break;
                }
            }



            return retSql;
        }


        public static string GetCode(CodeParams codeParams)
        {

            //throw new Exception("编码服务器逻辑有问题!");
            string url = StrUtil.obj2str(SysConfig.Params.CodeServiceUrl);
            if (StrUtil.IsEmpty(url)) throw new Exception("The encoding server address is empty");//编码服务器地址为空
            var codeRes = MstCore.HttpWeb.HttpPostJson(url, codeParams);
            var codeHt = StrUtil.Decode<Hashtable>(codeRes);
            return StrUtil.obj2str(codeHt["data"]);

        }



        /// <summary>
        /// 更新列表
        /// </summary>
        /// <typeparam name="TOfEntity">实体类型</typeparam>
        /// <typeparam name="TOfModel">model类型</typeparam>
        /// <param name="entityList">实体列表</param>
        /// <param name="idEntity">实体条件</param>
        /// <param name="modeles">model列表</param>
        /// <param name="idModel">model条件</param>
        /// <param name="whereUpdateModel">更新条件</param>
        /// <param name="entityCreationCallBack">创建Func</param>
        /// <param name="entityUpdateCallBack">更新Func</param>
        /// <param name="noNew">不更新的列</param>
        public void UpdateList<TOfEntity, TOfModel>(List<TOfEntity> entityList, Func<TOfEntity, long> idEntity, List<TOfModel> modeles, Func<TOfModel, long> idModel,
            Func<TOfModel, bool> whereUpdateModel, Func<List<TOfModel>, List<TOfEntity>> entityCreationCallBack, Func<List<TOfEntity>, TOfModel, TOfEntity> entityUpdateCallBack, List<string> noNew)
            where TOfEntity : class, new()
        {
            if (entityList == null)
            {
                throw new ArgumentNullException(nameof(entityList));
            }

            if (modeles == null)
            {
                throw new ArgumentNullException(nameof(modeles));
            }

            var addModels = modeles.Where(m => !whereUpdateModel(m)).ToList();
            var updateModels = modeles.Where(whereUpdateModel).ToList();

            var idList = modeles.Where(whereUpdateModel).Select(idModel); // 更新Id列表
            var entityIdList = entityList.Select(idEntity).ToList(); // 数据库中存在Id列表
            var removedIds = entityIdList.Except(idList).ToList();  // 数据库和model进行差集运算
            var updateIds = entityIdList.Intersect(idList).ToList(); // 两边同时存在的数据

            var updateList = new List<TOfEntity>();
            foreach (var item in updateModels)
            {
                var entity = entityUpdateCallBack(entityList, item);

                // 为null则找不到对应数据更新，添加到新增列表
                if (entity == null)
                {
                    addModels.Add(item);

                    continue;
                }
                updateList.Add(entity);
            }

            DB.Add(entityCreationCallBack(addModels));
            DB.Update(updateList, noNew);
            DB.DeleteIn<TOfEntity>(removedIds.Cast<dynamic>().ToList());
        }


    }
}
