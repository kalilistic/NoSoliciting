using System.Text;
using JKang.IpcServiceFramework;
using JKang.IpcServiceFramework.Services;
using Newtonsoft.Json;

namespace NoSoliciting.Interface {
    public class BetterIpcSerialiser : IIpcMessageSerializer {
        private static readonly JsonSerializerSettings Settings = new() {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        public byte[] SerializeRequest(IpcRequest request) {
            var json = JsonConvert.SerializeObject(request, Formatting.None, Settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public IpcResponse? DeserializeResponse(byte[] binary) {
            var json = Encoding.UTF8.GetString(binary);
            return JsonConvert.DeserializeObject<IpcResponse>(json, Settings);
        }

        public IpcRequest? DeserializeRequest(byte[] binary) {
            var json = Encoding.UTF8.GetString(binary);
            return JsonConvert.DeserializeObject<IpcRequest>(json, Settings);
        }

        public byte[] SerializeResponse(IpcResponse response) {
            var json = JsonConvert.SerializeObject(response, Formatting.None, Settings);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
