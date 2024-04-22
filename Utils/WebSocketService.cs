using System.Net.WebSockets;

namespace CameraStream.Utils
{
    public interface IWebSocketService
    {
        void AddSub(WebSocket _websocket);
        WebSocket GetSub();
        void AddPub(WebSocket _websocket);
        WebSocket GetPub();
    }
    public class WebSocketService : IWebSocketService
    {
        private WebSocket _websocketSub;
        private WebSocket _websocketPub;
        public void AddSub(WebSocket _websocket)
        {
            this._websocketSub = _websocket;
        }

        public WebSocket GetSub()
        {
            return _websocketSub;
        }
        public void AddPub(WebSocket _websocket)
        {
            this._websocketPub = _websocket;
        }

        public WebSocket GetPub()
        {
            return _websocketPub;
        }
    }
}
