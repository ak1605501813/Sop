using System.Collections.Generic;

namespace MstSopService.DTO
{
    public class ReturnSerialize<T>
    {
        public List<T> Data { get; set; }
        public int Total { get; set; }
    }
}
