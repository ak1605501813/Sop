using System;

namespace MstSopService.DTO
{
    public class PublishInput
    {
        public int Id { get; set; }
        /// <summary>
        /// Desc:是否发布
        /// Default:
        /// Nullable:True
        /// </summary>           
        public bool? IsPublish { get; set; }
        /// <summary>
        /// Desc:发布选择的人员
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string VisibleMember { get; set; }
        /// <summary>
        /// Desc:发布时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? PublishTime { get; set; }
        /// <summary>
        /// 发布人员
        /// </summary>
        public string PublishMember { get; set; }

        /// <summary>
        /// 发布人员
        /// </summary>
        public string PublishMemberName { get; set; }
    }
}
