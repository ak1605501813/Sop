using System.Collections.Generic;

namespace MstSopService.DTO
{
    public class DeptOrPersonnelOut
    {
        public string Type { get; set; }
        public string Image { get; set; }
        public string PositionNames { get; set; }
        public string Desc { get; set; }
        public string Origin { get; set; }
        public string OnJob { get; set; }
        public string NameEN { get; set; }
        public int UsersCount { get; set; }
        public int OnJobCount { get; set; }
        public int QuitJobCount { get; set; }
        public string CompanyId { get; set; }
         public string Name { get; set; }
        public string Key { get; set; }
        public string Pid { get; set; }
        public List<DeptOrPersonnelOut> Children { get; set; }
    
    }
}
