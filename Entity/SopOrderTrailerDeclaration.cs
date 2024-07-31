using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_order_trailer_declaration")]
    public partial class SopOrderTrailerDeclaration
    {
        public SopOrderTrailerDeclaration()
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
        /// Desc:需求类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "requirement_type")]
        public string RequirementType { get; set; }

        /// <summary>
        /// Desc:起运地
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "origin")]
        public string Origin { get; set; }

        /// <summary>
        /// Desc:提货地名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "pick_up_name")]
        public string PickUpName { get; set; }

        /// <summary>
        /// Desc:提货地址
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "pick_up_addr")]
        public string PickUpAddr { get; set; }

        /// <summary>
        /// Desc:联系人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "pick_up_contact")]
        public string PickUpContact { get; set; }

        /// <summary>
        /// Desc:是否过磅
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "is_weigh")]
        public string IsWeigh { get; set; }

        /// <summary>
        /// Desc:拖车要求
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "trailer_requirements")]
        public string TrailerRequirements { get; set; }

        /// <summary>
        /// Desc:报关类型
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
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifydate")]
        public DateTime? Modifydate { get; set; }

        /// <summary>
        /// Desc:起运地对应字段
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "origin_hierarchical")]
        public string OriginHierarchical { get; set; }
        /// <summary>
        /// Desc:报关类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "customs_type")]
        public string CustomsType { get; set; }
        
    }
}
