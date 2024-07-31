using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("files_share")]
    public class FilesShareDTO: FilesShare
    {
        [SugarColumn(IsIgnore = true)]
        public List<FilesShareDTO> Subsets { get; set; }
    }
}
