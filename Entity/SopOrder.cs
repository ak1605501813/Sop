using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_order")]
    public partial class SopOrder
    {
        public SopOrder()
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
        /// Desc:SOP单号
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "sop_code")]
        public string SopCode { get; set; }

        /// <summary>
        /// Desc:所属类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "ower_type")]
        public string OwerType { get; set; }

        /// <summary>
        /// Desc:业务类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "biz_type")]
        public string BizType { get; set; }

        /// <summary>
        /// Desc:客户名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "customer_name")]
        public string CustomerName { get; set; }

        /// <summary>
        /// Desc:cnee(英文)
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "consignee_en")]
        public string ConsigneeEn { get; set; }

        /// <summary>
        /// Desc:cnee(中文)
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "consignee_ch")]
        public string ConsigneeCh { get; set; }

        /// <summary>
        /// Desc:shipper（英文）
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "shipper_en")]
        public string ShipperEn { get; set; }

        /// <summary>
        /// Desc:shipper（中文）
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "shipper_ch")]
        public string ShipperCh { get; set; }

        /// <summary>
        /// Desc:销售人员
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "salesid")]
        public string Salesid { get; set; }

        /// <summary>
        /// Desc:运价人员
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "e_id")]
        public string EId { get; set; }

        /// <summary>
        /// Desc:操作人员
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "cs_id")]
        public string CsId { get; set; }

        /// <summary>
        /// Desc:贸易条款
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "incoterm")]
        public string Incoterm { get; set; }

        /// <summary>
        /// Desc:结算类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "pay_type")]
        public string PayType { get; set; }

        /// <summary>
        /// Desc:状态
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Desc:hbl_order_type
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "hbl_order_type")]
        public string HblOrderType { get; set; }

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
        /// Desc:修改时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifydate")]
        public DateTime? Modifydate { get; set; }
        /// <summary>
        /// Desc:是否发布
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "is_publish")]
        public bool? IsPublish { get; set; }
        /// <summary>
        /// Desc:发布选择的人员
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "visible_member")]
        public string VisibleMember { get; set; }
        /// <summary>
        /// Desc:发布时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "publish_time")]
        public DateTime? PublishTime { get; set; }
        /// <summary>
        /// Desc:发布人员
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "publish_member")]
        public string PublishMember { get; set; }

        /// <summary>
        /// Desc:标记
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "flag")]
        public bool? Flag { get; set; }
        /// <summary>
        /// Desc:原id
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "primary_id")]
        public int PrimaryId { get; set; }
        /// <summary>
        /// Desc:修改单号
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modify_sop_code")]
        public string ModifySopCode { get; set; }
        
    }
}
