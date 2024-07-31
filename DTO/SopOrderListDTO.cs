using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_order")]
    public class SopOrderListDTO:SopOrder
    {
        [SugarColumn(IsIgnore = true)]
        public string CreateuserName { get; set; }

        [SugarColumn(IsIgnore = true)]
        public string ModifierName { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string OwerTypeName { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string BizTypeName { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string SalesidName { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string EIdName { get; set; }
        [SugarColumn(IsIgnore = true)]
        public string CsIdName { get; set; }
        /// <summary>
        /// 发布信息
        /// </summary>

        [SugarColumn(IsIgnore = true)]
        public PublishInput PublishMsg { get; set; }
        /// <summary>
        /// 审批记录
        /// </summary>

        [SugarColumn(IsIgnore = true)]
        public List<SopOrderApproverRecordDTO> SopOrderApproverRecords { get; set; }
    }
}
