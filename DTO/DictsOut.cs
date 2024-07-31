using MstSopService.Entity;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    public class DictsOut
    {
        public string Code { get; set; }
        public List<DictsSpare> Dictinfos { get; set; }
    }
    public class DictsSpare
    {
        public int Idx { get; set; }
        public string Dictid { get; set; }
        public string Code { get; set; }
        public string Cname { get; set; }
        public string Ename { get; set; }
        public string HierarchicalFields { set; get; }
    }

}
