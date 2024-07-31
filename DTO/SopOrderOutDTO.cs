using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_order_out")]
    public class SopOrderOutDTO: SopOrderOut
    {

        [SugarColumn(IsIgnore = true)]
        public List<FileManage> TypeOfGoods { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> ModeOfOperation { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> Pol { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> Destination { get; set; }
    }
}
