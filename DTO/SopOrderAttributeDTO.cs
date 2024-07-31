using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_order_attribute")]
    public class SopOrderAttributeDTO: SopOrderAttribute
    {
        [SugarColumn(IsIgnore = true)]
        public List<SopOrderAttributeDTO> Subsets { get; set; }
    }
}
