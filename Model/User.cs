using SqlSugar;
using System.Security.Principal;

namespace xianyu_miniapi.Model
{
    [SugarTable("users")]
    public class User
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
    }
}
