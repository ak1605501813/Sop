using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_order_trailer_declaration")]
    public class SopOrderTrailerDeclarationDTO: SopOrderTrailerDeclaration
    {
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> Region { get; set; }
    }
}
