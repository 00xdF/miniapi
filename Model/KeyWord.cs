using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace xianyu_miniapi.Model
{
    [SugarTable("keyword")]
    public class KeyWord
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int ID { get; set; }
        public string AllowedKeyWord { get; set; }
        public string BannedKeyWord { get; set; }
    }
}
