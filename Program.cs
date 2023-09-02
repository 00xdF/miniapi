using Microsoft.Extensions.FileProviders;
using SqlSugar;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using xianyu_miniapi.Model;
using System.Text;
using System.Diagnostics;
using System.Net.WebSockets;
using xianyu_miniapi.Handle;

var builder = WebApplication.CreateBuilder(args);
//ע��sqlsugar orm����
builder.Services.AddScoped<ISqlSugarClient>(db =>
{
    return new SqlSugarClient(new ConnectionConfig()
    {
        ConnectionString = builder.Configuration.GetConnectionString("mysql"),
        DbType = DbType.MySql,
        IsAutoCloseConnection = true,
    });
});


var app = builder.Build();

//Todo : ��ҳ
app.MapGet("/", () =>
{
    return Results.Content("<html><body><h1>This is MinimalAPI Test Page!</h1></body></html>", "text/html");
});

//Todo : ��ȡ��Ʒ
app.MapGet("/getGoods", (ISqlSugarClient db) =>
{
    var res = db.Queryable<Goods>();
    return res.Select(x => x).ToArray();
});


//Todo : ��ʼ��->ͨ��DIע��Ĳ�����ȡ���ݿ�����������ǽ�ISqlSugarClient��ӵ���scope��������
app.MapGet("/init", (ISqlSugarClient db) =>
{
    //create database by codefirst model
    db.DbMaintenance.CreateDatabase();
    //����ǿ������� ��ɾ�����еı����´���
    if (app.Environment.IsDevelopment())
    {
        var tables = db.DbMaintenance.GetTableInfoList();
        foreach(var i in tables)
        {
            db.DbMaintenance.DropTable(i.Name);
        }
    }
    //ͨ����������ʵ���
    Type[] entitys = Assembly.LoadFrom(AppContext.BaseDirectory + "xianyu_miniapi.dll").GetTypes()
    .Where(e => e.Namespace == "xianyu_miniapi.Model").ToArray();
    //ͨ��ʵ����ȥ������ CodeFirst
    db.CodeFirst.InitTables(entitys);
    List<User> list = new();
    list.Add(new User
    {
        UserName = "admin",
        PassWord = "admin"
    });
    return db.Insertable(list).ExecuteCommand();
});


#region ��̬�ļ����ʷ���
var filePath = $"{AppContext.BaseDirectory}/wwwroot";
if (!Directory.Exists(filePath))
{
    Directory.CreateDirectory(filePath);
}
var fileProvider = new PhysicalFileProvider(filePath); 
var requestPath = "/static";
app.UseStaticFiles(new StaticFileOptions()
{
     FileProvider = fileProvider,
     RequestPath = requestPath
});
//�����߻����¿���Ŀ¼���
if (app.Environment.IsDevelopment())
{
    app.UseDirectoryBrowser(requestPath);
}
#endregion


#region ����Socket����
app.UseWebSockets();
app.Map("/socket", HandleWebSocket);
async Task HandleWebSocket(HttpContext context)
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await xianyu_miniapi.Handle.WebSocketHandler.Handle(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
}
#endregion

//���������Ŀ�����øýӿ�������ʱ���ֻ�ܽ���һ������
//��Ϊÿ�η������ݸ��豸�������ݴ�������Ĵ��������ܣ������ᵼ�������½�
//������ֻ�޸���ʹ�ã�û�в�������
Object visitLock = new object();
app.MapGet("/test",  (string keyword,int low,int high) =>
{
    lock(visitLock){
        try
        {
            bool res = WebSocketHandler.sendMessage($"{keyword},{low},{high}").Result;
            if (res)
            {
                return new Status(200, $"�ɹ���������");
                
            }
            else
            {
                return new Status(201, $"�ɹ���������,���ͻ���������ʧ�ܣ�����ʱ(3000ms)");
            }
        }
        catch (Exception e)
        {
            return new Status(400, $"�ͻ���δ����!{e.Message}");
        }
    }
    
});
app.Run();


class Status
{
    public int Code { get;set; } 
    public string Message { get; set; }
    public Status(int Code,string Message)
    {
        this.Code = Code;
        this.Message = Message;
    }
}