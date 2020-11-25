using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using NPitaya.Constants;
using NPitaya.Protos;
using Json = PitayaSimpleJson.SimpleJson;

namespace NPitaya.Models
{
    public class PitayaSession
    {
        Int64 _id;
        string _frontendId;
        Dictionary<string, object> _data;
        string _rawData;
        public string RawData => _rawData;
        public string Uid { get; private set; }
        readonly RpcClient _rpcClient;

        internal PitayaSession(Session sessionProto, RpcClient rpcClient)
        {
            _rpcClient = rpcClient;
            _id = sessionProto.Id;
            Uid = sessionProto.Uid;
            _rawData = sessionProto.Data.ToStringUtf8();
            if (!string.IsNullOrEmpty(_rawData))
                _data = Json.DeserializeObject<Dictionary<string, object>>(_rawData);
        }

        internal PitayaSession(Session sessionProto, RpcClient rpcClient, string frontendId)
            : this(sessionProto, rpcClient)
        {
            _frontendId = frontendId;
        }

        public override string ToString()
        {
            return $"ID: {_id}, UID: {Uid}, Data: {_rawData}";
        }

        public void Set(string key, object value)
        {
            _data[key] = value;
            _rawData = Json.SerializeObject(_data);
        }

        public object GetObject(string key)
        {
            if (!_data.ContainsKey(key))
            {
                throw new Exception($"key not found in session, parameter: {key}");
            }

            return _data[key];
        }

        public string GetString(string key)
        {
            return GetObject(key) as string;
        }

        public int GetInt(string key)
        {
            var obj = GetObject(key);
            return obj is int ? (int) obj : 0;
        }

        public double GetDouble(string key)
        {
            var obj = GetObject(key);
            return obj is double ? (double) obj : 0;
        }

        public Task PushToFrontend()
        {
            if (string.IsNullOrEmpty(_frontendId))
            {
                return Task.FromException(new Exception("cannot push to frontend, frontendId is invalid!"));
            }
            return SendRequestToFront(Routes.SessionPushRoute, true);
        }

        public Task Bind(string uid)
        {
            if (Uid != "")
            {
                return Task.FromException(new Exception("session already bound!"));
            }
            Uid = uid;
            // TODO only if server type is backend
            // TODO bind callbacks
            if (!string.IsNullOrEmpty(_frontendId))
            {
                return BindInFrontend();
            }

            return Task.CompletedTask;
        }

        public Task Push(object pushMsg, string route)
        {
            return _rpcClient.SendPushToUser(_frontendId, "", route, Uid, pushMsg);
        }
        public Task Push(object pushMsg, string svType, string route)
        {
            return _rpcClient.SendPushToUser("", svType, route, Uid, pushMsg);
        }

        public Task Push(object pushMsg, string svType, string svId, string route)
        {
            return _rpcClient.SendPushToUser(svId, svType, route, Uid, pushMsg);
        }

        public Task Kick()
        {
            return _rpcClient.SendKickToUser(_frontendId, "", new KickMsg
            {
                UserId = Uid
            });
        }
        public Task Kick(string svType)
        {
            return _rpcClient.SendKickToUser("", svType, new KickMsg
            {
                UserId = Uid
            });
        }

        Task BindInFrontend()
        {
            return SendRequestToFront(Routes.SessionBindRoute, false);
        }

        Task SendRequestToFront(string route, bool includeData)
        {
            var sessionProto = new Session
            {
                Id = _id,
                Uid = Uid
            };
            if (includeData)
            {
                sessionProto.Data = ByteString.CopyFromUtf8(_rawData);
            }
            return _rpcClient.Rpc<Response>(_frontendId, Route.FromString(route), sessionProto.ToByteArray());
        }
    }
}