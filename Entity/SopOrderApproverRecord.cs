using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_order_approver_record")]
    public partial class SopOrderApproverRecord
    {
        public SopOrderApproverRecord()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "companyid")]
        public string Companyid { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "departmentid")]
        public string Departmentid { get; set; }

        /// <summary>
        /// Desc:审批状态
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "approval_status")]
        public string ApprovalStatus { get; set; }

        /// <summary>
        /// Desc:审批节点
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "approval_node")]
        public string ApprovalNode { get; set; }

        /// <summary>
        /// Desc:sop_order对应id
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "sop_order_id")]
        public int? SopOrderId { get; set; }

        /// <summary>
        /// Desc:审批用户-->创建用户
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "approver")]
        public string Approver { get; set; }

        /// <summary>
        /// Desc:审批时间-->创建时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "approval_time")]
        public DateTime? ApprovalTime { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "remark")]
        public string Remark { get; set; }
    }
}
