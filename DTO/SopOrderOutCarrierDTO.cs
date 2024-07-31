using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_order_out_carrier")]
    public class SopOrderOutCarrierDTO: SopOrderOutCarrier
    {
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> FileCarrier { get; set; }
    }
}
