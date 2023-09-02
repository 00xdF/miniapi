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
//注册sqlsugar orm服务
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

//Todo : 首页
app.MapGet("/", () =>
{
    return Results.Content("<html><body><h1>This is MinimalAPI Test Page!</h1></body></html>", "text/html");
});

//Todo : 获取商品
app.MapGet("/getGoods", (ISqlSugarClient db) =>
{
    var res = db.Queryable<Goods>();
    return res.Select(x => x).ToArray();
});


//Todo : 初始化->通过DI注入的参数获取数据库操作对象，我们将ISqlSugarClient添加到了scope作用域中
app.MapGet("/init", (ISqlSugarClient db) =>
{
    //create database by codefirst model
    db.DbMaintenance.CreateDatabase();
    //如果是开发环境 先删除所有的表重新创建
    if (app.Environment.IsDevelopment())
    {
        var tables = db.DbMaintenance.GetTableInfoList();
        foreach(var i in tables)
        {
            db.DbMaintenance.DropTable(i.Name);
        }
    }
    //通过反射生成实体表
    Type[] entitys = Assembly.LoadFrom(AppContext.BaseDirectory + "xianyu_miniapi.dll").GetTypes()
    .Where(e => e.Namespace == "xianyu_miniapi.Model").ToArray();
    //通过实体类去创建表 CodeFirst
    db.CodeFirst.InitTables(entitys);
    List<User> list = new();
    list.Add(new User
    {
        UserName = "admin",
        PassWord = "admin"
    });
    return db.Insertable(list).ExecuteCommand();
});


#region 静态文件访问服务
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
//开发者环境下开启目录浏览
if (app.Environment.IsDevelopment())
{
    app.UseDirectoryBrowser(requestPath);
}
#endregion


#region 建立Socket主机
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

//这里加锁的目的是让该接口在任意时间段只能建立一个请求
//因为每次发送数据给设备进行数据处理会消耗大量的性能，并发会导致性能下降
//且这里只限个人使用，没有并发需求
Object visitLock = new object();
app.MapGet("/test",  (string keyword,int low,int high) =>
{
    lock(visitLock){
        try
        {
            bool res = WebSocketHandler.sendMessage($"{keyword},{low},{high}").Result;
            if (res)
            {
                return new Status(200, $"成功处理请求！");
                
            }
            else
            {
                return new Status(201, $"成功发送请求,但客户端请求处理失败，请求超时(3000ms)");
            }
        }
        catch (Exception e)
        {
            return new Status(400, $"客户端未连接!{e.Message}");
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