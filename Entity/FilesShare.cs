using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("files_share")]
    public partial class FilesShare
    {
        public FilesShare()
        {


        }
        /// <summary>
        /// Desc:内码
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// Desc:集团码
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "companyid")]
        public string Companyid { get; set; }

        /// <summary>
        /// Desc:公司码
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "departmentid")]
        public string Departmentid { get; set; }

        /// <summary>
        /// Desc:父节点
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "pid")]
        public int? Pid { get; set; }

        /// <summary>
        /// Desc:文件/文件夹名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "file_name")]
        public string FileName { get; set; }
        /// <summary>
        /// Desc:文件/文件夹名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "file_nameen")]
        public string FileNameen { get; set; }

        /// <summary>
        /// Desc:类型
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Desc:扩展名
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "extended")]
        public string Extended { get; set; }

        /// <summary>
        /// Desc:是否私有
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "is_private")]
        public bool? IsPrivate { get; set; }

        /// <summary>
        /// Desc:文件路径
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "file_path")]
        public string FilePath { get; set; }

        /// <summary>
        /// Desc:属性1
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr1")]
        public string Gcattr1 { get; set; }

        /// <summary>
        /// Desc:属性2
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr2")]
        public string Gcattr2 { get; set; }

        /// <summary>
        /// Desc:属性3
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr3")]
        public string Gcattr3 { get; set; }

        /// <summary>
        /// Desc:属性4
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr4")]
        public string Gcattr4 { get; set; }

        /// <summary>
        /// Desc:属性5
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "gcattr5")]
        public string Gcattr5 { get; set; }

        /// <summary>
        /// Desc:备注
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "remark")]
        public string Remark { get; set; }

        /// <summary>
        /// Desc:录入人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "createuser")]
        public string Createuser { get; set; }

        /// <summary>
        /// Desc:录入时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "createdate")]
        public DateTime? Createdate { get; set; }

        /// <summary>
        /// Desc:修改人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifier")]
        public string Modifier { get; set; }

        /// <summary>
        /// Desc:修改时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifydate")]
        public DateTime? Modifydate { get; set; }

    }
}
