using CameraStream.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace CameraStream.Controllers
{
    public class WebSKController : Controller
    {
        private IWebSocketService _webSocketService;
        public WebSKController(IWebSocketService webSocketService)
        {
            _webSocketService = webSocketService;
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
            var buffer = new byte[10 * 1024 * 1024];
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
    }
}
