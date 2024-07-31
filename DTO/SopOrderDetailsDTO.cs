using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_order")]
    public class SopOrderDetailsDTO: SopOrder
    {
        [SugarColumn(IsIgnore = true)]
        public string _state { set; get; }
        /// <summary>
        /// Pa联系人
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<SopPaContactDTO> PreAlertList { set; get; }
        /// <summary>
        /// 海外代理商联系方式
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<SopPaContactDTO> OverseasAgentContactList { set; get; }
        
        /// <summary>
        /// 提交后生成文件
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderContentText> GeneratedFiles { set; get; }
        /// <summary>
        /// 审批记录
        /// </summary>

        [SugarColumn(IsIgnore = true)]
        public List<SopOrderApproverRecordDTO> SopOrderApproverRecords { get; set; }
        /// <summary>
        /// 发布信息
        /// </summary>

        [SugarColumn(IsIgnore = true)]
        public PublishInput PublishMsg { get; set; }
        /// <summary>
        /// 联系人
        /// </summary>

        [SugarColumn(IsIgnore = true)]
        public List<SopOrderContactDTO> ContactInformations { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderAttachmentDTO> Quotations { get; set; }
        
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderAttachmentDTO> SettlementModes { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderAttachmentDTO> GuaranteeLetters { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderAttachmentDTO> HBLOrderTypes { get; set; }
        /// <summary>
        /// 出口信息
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderOutDTO> Exports { get; set; }
        /// <summary>
        /// 船信息
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderOutCarrierDTO> CarrierList { get; set; }
        [SugarColumn(IsIgnore = true)]
        public SopOrderInDTO Import { get; set; }
        [SugarColumn(IsIgnore = true)]
        public SopOrderTrailerDeclarationDTO TrailerCustomsDeclaration { get; set; }
        /// <summary>
        /// sopOrder附件
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> OrderAttachments { get; set; }
    }
}
