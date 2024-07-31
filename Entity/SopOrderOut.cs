using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_order_out")]
    public partial class SopOrderOut
    {
        public SopOrderOut()
        {


        }
        /// <summary>
        /// Desc:内码
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// Desc:集团码
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "companyid")]
        public string Companyid { get; set; }

        /// <summary>
        /// Desc:公司码
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "departmentid")]
        public string Departmentid { get; set; }

        /// <summary>
        /// Desc:sop_order_id
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "sop_order_id")]
        public int? SopOrderId { get; set; }

        /// <summary>
        /// Desc:货物类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "goods_type")]
        public string GoodsType { get; set; }

        /// <summary>
        /// Desc:dg申报
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "dg_")]
        public string Dg { get; set; }

        /// <summary>
        /// Desc:操作模式
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "op_mode")]
        public string OpMode { get; set; }

        /// <summary>
        /// Desc:起运地
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "origin")]
        public string Origin { get; set; }

        /// <summary>
        /// Desc:申报方
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "declaration")]
        public string Declaration { get; set; }

        /// <summary>
        /// Desc:目的国
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "dest_contury")]
        public string DestContury { get; set; }

        /// <summary>
        /// Desc:属性1
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr1")]
        public string Gcattr1 { get; set; }

        /// <summary>
        /// Desc:属性2
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr2")]
        public string Gcattr2 { get; set; }

        /// <summary>
        /// Desc:属性3
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr3")]
        public string Gcattr3 { get; set; }

        /// <summary>
        /// Desc:属性4
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr4")]
        public string Gcattr4 { get; set; }
        /// <summary>
        /// Desc:属性5
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr5")]
        public string Gcattr5 { get; set; }
        /// <summary>
        /// Desc:危标挂网
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "dg_special_packging")]
        public string DgSpecialPackging { get; set; }

        /// <summary>
        /// Desc:备注
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "remark")]
        public string Remark { get; set; }

        /// <summary>
        /// Desc:录入人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "createuser")]
        public string Createuser { get; set; }

        /// <summary>
        /// Desc:录入时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "createdate")]
        public DateTime? Createdate { get; set; }

        /// <summary>
        /// Desc:修改人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifier")]
        public string Modifier { get; set; }

        /// <summary>
        /// Desc:修改时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifydate")]
        public DateTime? Modifydate { get; set; }
        /// <summary>
        /// Desc:货物类型对应字段
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "goods_type_hierarchical")]
        public string GoodsTypeHierarchical { get; set; }
        /// <summary>
        /// Desc:操作模式对应字段
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "op_mode_hierarchical")]
        public string OpModeHierarchical { get; set; }
        /// <summary>
        /// Desc:起运地对应字段
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "origin_hierarchical")]
        public string OriginHierarchical { get; set; }
        /// <summary>
        /// Desc:目的国对应字段
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "dest_contury_hierarchical")]
        public string DestConturyHierarchical { get; set; }

    }
}
