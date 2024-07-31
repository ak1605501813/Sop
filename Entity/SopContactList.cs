using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace MstSopService.Entity
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("sop_contact_list")]
    public partial class SopContactList
    {
           public SopContactList(){


           }
           /// <summary>
           /// Desc:内码
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true,ColumnName="id")]
           public int Id {get;set;}

           /// <summary>
           /// Desc:集团码
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="companyid")]
           public string Companyid {get;set;}

           /// <summary>
           /// Desc:公司码
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="departmentid")]
           public string Departmentid {get;set;}

           /// <summary>
           /// Desc:树节点ID
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="sop_base_id")]
           public int? SopBaseId {get;set;}

           /// <summary>
           /// Desc:运输公司
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="carrier")]
           public string Carrier {get;set;}

           /// <summary>
           /// Desc:办公室
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="office")]
           public string Office {get;set;}

           /// <summary>
           /// Desc:航线
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="route")]
           public string Route {get;set;}

           /// <summary>
           /// Desc:目的国
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName= "destination")]
           public string Destination { get;set;}

           /// <summary>
           /// Desc:角色
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="role")]
           public string Role {get;set;}

           /// <summary>
           /// Desc:岗位描述
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="title_desc")]
           public string TitleDesc {get;set;}

           /// <summary>
           /// Desc:联系人
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="contact_name")]
           public string ContactName {get;set;}

           /// <summary>
           /// Desc:联系方式
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="tel")]
           public string Tel {get;set;}

           /// <summary>
           /// Desc:手机号
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="mobile")]
           public string Mobile {get;set;}

           /// <summary>
           /// Desc:邮箱地址
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="email")]
           public string Email {get;set;}

           /// <summary>
           /// Desc:属性1
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="gcattr1")]
           public string Gcattr1 {get;set;}

           /// <summary>
           /// Desc:属性2
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="gcattr2")]
           public string Gcattr2 {get;set;}

           /// <summary>
           /// Desc:属性3
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="gcattr3")]
           public string Gcattr3 {get;set;}

           /// <summary>
           /// Desc:属性4
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="gcattr4")]
           public string Gcattr4 {get;set;}

           /// <summary>
           /// Desc:属性5
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="gcattr5")]
           public string Gcattr5 {get;set;}

           /// <summary>
           /// Desc:备注
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="remark")]
           public string Remark {get;set;}

           /// <summary>
           /// Desc:录入人
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="createuser")]
           public string Createuser {get;set;}

           /// <summary>
           /// Desc:录入时间
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="createdate")]
           public DateTime? Createdate {get;set;}

           /// <summary>
           /// Desc:修改人
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="modifier")]
           public string Modifier {get;set;}

           /// <summary>
           /// Desc:修改时间
           /// Default:
           /// Nullable:True
           /// </summary>           
           [SugarColumn(ColumnName="modifydate")]
           public DateTime? Modifydate {get;set;}

    }
}
