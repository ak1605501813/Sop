using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_pa_contact")]
    public partial class SopPaContact
    {
        public SopPaContact()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "companyid")]
        public string Companyid { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "departmentid")]
        public string Departmentid { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "sop_order_id")]
        public int? SopOrderId { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "email")]
        public string Email { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "remark")]
        public string Remark { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "createuser")]
        public string Createuser { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "createdate")]
        public DateTime? Createdate { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifier")]
        public string Modifier { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "modifydate")]
        public DateTime? Modifydate { get; set; }

        /// <summary>
        /// Desc:1:pa联系人，0:海外代理联系人
        /// Default:
        /// Nullable:True
        /// </summary>           
        [SugarColumn(ColumnName = "flag")]
        public int Flag { get; set; }
    }
}
