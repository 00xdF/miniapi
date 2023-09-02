using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace xianyu_miniapi.Model
{
    [SugarTable("xy")]
    public class Goods
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        [SugarColumn(IsNullable = true)]
        public string PublishNick { get; set; }
        [SugarColumn(IsNullable = false)]
        public string Content { get; set; }
        [SugarColumn(IsNullable = false)]
        public string Price { get; set; }
        [SugarColumn(IsNullable = false)]
        public string PubTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public string Url { get; set; }
        [SugarColumn(IsNullable = true)]
        public string Param { get; set; }
        [SugarColumn(IsNullable = true)]
        public string ImgURL { get; set; }
    }
}
