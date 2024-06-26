﻿using System.Net.WebSockets;

namespace CameraStream
{
    public static class WebSocketBuilder
    {
        public static IApplicationBuilder UseStreamSocket(this IApplicationBuilder app)
        {
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            };

            app.UseWebSockets(webSocketOptions);


            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await Stream(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });



            return app;
        }

        private static async Task Stream(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[10 * 1024 * 1024];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                ArraySegment<byte> streamData = new ArraySegment<byte>(buffer, 0, result.Count);
                byte[] imageData = streamData.ToArray();
                await webSocket.SendAsync(new ArraySegment<byte>(imageData, 0, imageData.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                var outputData = new ArraySegment<byte>(buffer);
                result = await webSocket.ReceiveAsync(outputData, CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
