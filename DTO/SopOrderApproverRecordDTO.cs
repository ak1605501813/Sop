using MstSopService.Entity;
using SqlSugar;

namespace MstSopService.DTO
{
    [SugarTable("sop_order_approver_record")]
    public class SopOrderApproverRecordDTO: SopOrderApproverRecord
    {
        /// <summary>
        /// Desc:审批用户-->创建用户
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(IsIgnore = true)]
        public string ApproverName { get; set; }
    }
}
