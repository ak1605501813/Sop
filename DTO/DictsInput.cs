namespace MstSopService.DTO
{
    public class DictsInput
    {
        public string[] Codes { get; set; }
        /// <summary>
        /// 10:空运   20:海运
        /// </summary>
        public string OwerType { get; set; }
    }
}
