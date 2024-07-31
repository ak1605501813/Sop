using NPOI.Util;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    public class FrameDataOut
    {
        public string Departmentid { get; set; }
        public int Idx { get; set; }
        public string SopName { get; set; }
        public string SopNameen { get; set; }
        public int? Pid { get; set; }
        public int? Orderid { get; set; }
        public int NumberOfAttachments { get; set; }
        public List<FrameDataOut> Subsets { get; set; }
    }
}
