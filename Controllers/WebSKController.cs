using CameraStream.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace CameraStream.Controllers
{
    public class WebSKController : Controller
    {
        private IWebSocketService _webSocketService;
        private IWebSocketSingleService _webSocketSingleService;
        public WebSKController(IWebSocketService webSocketService, IWebSocketSingleService webSocketSingleService)
        {
            _webSocketService = webSocketService;
            _webSocketSingleService = webSocketSingleService;
        }
        [Route("/ws/Sub")]
        public async Task Sub()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket;
                if (_webSocketService.GetSub() == null || _webSocketService.GetSub().State != WebSocketState.Open)
                {
                    webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _webSocketService.AddSub(webSocket);
                }
                else
                {
                    webSocket = _webSocketService.GetSub();
                }
                await Stream(webSocket, true);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        [Route("/ws/Pub")]
        public async Task Pub()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket;
                if (_webSocketService.GetPub() == null || _webSocketService.GetPub().State != WebSocketState.Open)
                {
                    webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _webSocketService.AddPub(webSocket);
                }
                else
                {
                    webSocket = _webSocketService.GetPub();
                }
                await Stream(webSocket, false);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        private async Task Stream(WebSocket srcWebSocket, bool isSub)
        {
            try
            {
                var buffer = new byte[1024 * 1024];//1MB
                WebSocketReceiveResult result = await srcWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    ArraySegment<byte> streamData = new ArraySegment<byte>(buffer, 0, result.Count);
                    byte[] imageData = streamData.ToArray();
                    if (!isSub)
                    {
                        WebSocket desWebSocket = isSub ? _webSocketService.GetPub() : _webSocketService.GetSub();
                        if (desWebSocket != null) _ = desWebSocket.SendAsync(new ArraySegment<byte>(imageData, 0, imageData.Length), result.MessageType, result.EndOfMessage, CancellationToken.None).ConfigureAwait(false);
                    }
                    var outputData = new ArraySegment<byte>(buffer);
                    result = await srcWebSocket.ReceiveAsync(outputData, CancellationToken.None);
                }
                await srcWebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch
            {
                await srcWebSocket.CloseAsync(WebSocketCloseStatus.ProtocolError, "result.CloseStatusDescription", CancellationToken.None);
            }
        }

        [Route("/ws/single")]
        public async Task SingleWS()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket;
                if (_webSocketSingleService.Get() == null || _webSocketSingleService.Get().State != WebSocketState.Open)
                {
                    webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _webSocketSingleService.Add(webSocket);
                }
                else
                {
                    webSocket = _webSocketSingleService.Get();
                }
                await Stream(HttpContext, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        private static async Task Stream(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 1024];
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
