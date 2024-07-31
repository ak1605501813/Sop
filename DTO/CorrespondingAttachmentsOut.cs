using MstSopService.Entity;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    public class CorrespondingAttachmentsOut
    {
        public string SopName { get; set; }
        public string SopNameen { get; set; }
        public List<FileManage> FileManages { get; set; }
    }
}
