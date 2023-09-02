using SqlSugar;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace xianyu_miniapi.Handle
{
    public static class WebSocketHandler
    {
        private static bool isExecuteOk = false;
        private static WebSocket websocket = null;

        /// <summary>
        /// Todo ： 处理webscoket响应请求
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public static async Task Handle(WebSocket webSocket)
        {
            websocket = webSocket;
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // 处理接收到的消息
                    switch (message)
                    {
                        case "req_connect":
                            Trace.Write($"本地主机请求连接\n");
                            await sendMessage($"WebSocket Host Connected!");
                            break;
                        case "rep_ok":
                            Trace.Write($"响应数据成功！\n");
                            //成功响应 直接从数据库获取数据
                            isExecuteOk = true;
                            break;
                        case "rep_fail":
                            Trace.Write("响应数据失败！");
                            //响应失败 返回失败提示
                            break;
                        default:
                            Trace.WriteLine($"Received message from client: {message}");
                            break;
                    }
                   }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Trace.WriteLine("Client disconnected.");
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }
        }


        /// <summary>
        /// Todo : 通过websocket向已经连接的客户端发送信息
        /// </summary>
        /// <param name="webSocket"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static  Task<bool> sendMessage(string message)
        {
            return  Task.Run(async () =>
            {
                // 向客户端发送响应数据
                var responseData = Encoding.UTF8.GetBytes(message);
                await websocket.SendAsync(new ArraySegment<byte>(responseData), WebSocketMessageType.Text, true, CancellationToken.None);
                int time = 0;
                while (true)
                {
                    //如果执行成功 则返回成功标识
                    if (isExecuteOk)
                    {
                        isExecuteOk = false;
                        return true;
                    }
                    //超时未接到请求 则返回 false
                    if (time >= 3000)
                    {
                        return false;
                    }
                    time += 100;
                    await Task.Delay(100);
                }
            });
        }
    }

}
