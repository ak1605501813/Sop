using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_order_in")]
    public partial class SopOrderIn
    {
        public SopOrderIn()
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
        /// Desc:操作模式
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "op_mode")]
        public string OpMode { get; set; }

        /// <summary>
        /// Desc:进口模式
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "in_type")]
        public string InType { get; set; }

        /// <summary>
        /// Desc:目的地
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "dest")]
        public string Dest { get; set; }

        /// <summary>
        /// Desc:申报方式
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "declaration_method")]
        public string DeclarationMethod { get; set; }

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
        /// Desc:备注
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "remark")]
        public string Remark { get; set; }
        /// <summary>
        /// Desc:联系窗口
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "contact_window")]
        public string ContactWindow { get; set; }

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
        /// Desc:船公司名
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "carrier")]
        public string Carrier { get; set; }
        /// <summary>
        /// Desc:货物类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "goods_type")]
        public string GoodsType { get; set; }
        
    }
}
