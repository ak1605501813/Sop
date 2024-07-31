using System.Collections.Generic;

namespace MstSopService.DTO
{
    public class ArchitectureDataOut
    {
        public int Id { get; set; }
        public int? Pid { get; set; }
        public string FileName { get; set; }
        public string FileNameen { get; set; }
        public string Type { get; set; }
        public string Extended { get; set; }
        public bool? IsPrivate { get; set; }
        public string FilePath { get; set; }
        public string Remark { get; set; }
        public List<ArchitectureDataOut> Subsets { get; set; }
    }
}
