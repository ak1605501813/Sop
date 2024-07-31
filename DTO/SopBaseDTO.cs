using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_base")]
    public class SopBaseDTO: SopBase
    {
        [SugarColumn(IsIgnore = true)]
        public string _state { set; get; }
        [SugarColumn(IsIgnore = true)]
        public List<FileManage> FileManages { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<SopContactList> Contacts { get; set; }
    }
}
