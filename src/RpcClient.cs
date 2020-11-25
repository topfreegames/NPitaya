using System;
using Google.Protobuf;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NPitaya.Serializer;
using NPitaya.Protos;
using System.Text;
using System.Collections.Generic;
using static NPitaya.Utils.Utils;

namespace NPitaya
{
    // RpcClient allows sending RPC messages to other Pitaya servers.
    internal class RpcClient
    {
        readonly IntPtr _pitaya;
        readonly ISerializer _serializer;
        readonly ProtobufSerializer _internalRpcSerializer;

        private readonly Dictionary<Type, PitayaCluster.SendRpcCallback> callbackDict = new Dictionary<Type, PitayaCluster.SendRpcCallback>();
        private readonly PitayaCluster.SendPushCallback pushCallback = new PitayaCluster.SendPushCallback(PushCallback);
        private readonly PitayaCluster.SendKickCallback kickCallback = new PitayaCluster.SendKickCallback(KickCallback);


        public RpcClient(IntPtr pitaya, ISerializer serializer)
        {
            _pitaya = pitaya;
            _serializer = serializer;
            _internalRpcSerializer = BuildInterRpcSerializer(serializer);
        }

        static ProtobufSerializer BuildInterRpcSerializer(ISerializer receivedSerializer)
        {
            if (receivedSerializer.GetType() == typeof(ProtobufSerializer))
            {
                return (ProtobufSerializer) receivedSerializer;
            }

            return new ProtobufSerializer();
        }

        public Task<T> Rpc<T>(string serverId, Route route, object msg)
        {
            return DoRpc<T>(serverId, route, msg, _serializer);
        }

        internal Task<T> Rpc<T>(string serverId, Route route, IMessage msg)
        {
            return DoRpc<T>(serverId, route, msg, _internalRpcSerializer);
        }

        Task<T> DoRpc<T>(string serverId, Route route, object msg, ISerializer serializer)
        {
            return Task.Run(() =>
            {
                if (!callbackDict.ContainsKey(typeof(T))){
                    callbackDict.Add(typeof(T), new PitayaCluster.SendRpcCallback(RpcCallback<T>));
                }
                PitayaCluster.SendRpcCallback callback;
                callbackDict.TryGetValue(typeof(T), out callback);
                var context = new CallbackContext<T>
                {
                    t = new TaskCompletionSource<T>(),
                    serializer = serializer,
                };
                var handle = GCHandle.Alloc(context, GCHandleType.Normal);
                var payload = SerializerUtils.SerializeOrRaw(msg, serializer);

                unsafe
                {
                    fixed (byte* p = payload)
                    {
                        IntPtr request = PitayaCluster.pitaya_buffer_new((IntPtr)p, payload.Length);
                        PitayaCluster.pitaya_send_rpc(
                            _pitaya,
                            serverId,
                            route.ToString(),
                            request,
                            callback,
                            GCHandle.ToIntPtr(handle));
                    }
                }

                return context.t.Task;
            });
        }

        public Task SendKickToUser(string frontendId, string serverKind, KickMsg kick)
        {
            return Task.Run(() =>
            {
                var context = new CallbackContext<bool>
                {
                    t = new TaskCompletionSource<bool>(),
                    serializer = _serializer,
                };
                var handle = GCHandle.Alloc(context, GCHandleType.Normal);

                unsafe
                {
                    var data = kick.ToByteArray();
                    fixed (byte* p = data)
                    {
                        IntPtr kickBuffer = PitayaCluster.pitaya_buffer_new((IntPtr)p, data.Length);
                        PitayaCluster.pitaya_send_kick(_pitaya, frontendId, serverKind, kickBuffer, kickCallback, GCHandle.ToIntPtr(handle));
                    }
                }

                return context.t.Task;
            });
        }

        public Task SendPushToUser(
            string frontendId,
            string serverKind,
            string route,
            string uid,
            object pushMsg)
        {
            return Task.Run(() =>
            {
                var context = new CallbackContext<bool>
                {
                    t = new TaskCompletionSource<bool>(),
                    serializer = _serializer,
                };
                var handle = GCHandle.Alloc(context, GCHandleType.Normal);
                var push = new Push
                {
                    Route = route,
                    Uid = uid,
                    Data = ByteString.CopyFrom(SerializerUtils.SerializeOrRaw(pushMsg, _serializer))
                };

                unsafe
                {
                    var data = push.ToByteArray();
                    fixed (byte* p = data)
                    {
                        IntPtr pushBuffer = PitayaCluster.pitaya_buffer_new((IntPtr)p, data.Length);
                        PitayaCluster.pitaya_send_push_to_user(_pitaya, frontendId, serverKind, pushBuffer, pushCallback, GCHandle.ToIntPtr(handle));
                    }
                }

                return context.t.Task;
            });
        }

        class CallbackContext<T>
        {
            public TaskCompletionSource<T> t;
            public ISerializer serializer;
        }

        static void RpcCallback<T>(IntPtr userData, IntPtr err, IntPtr responseBuf)
        {
            var handle = GCHandle.FromIntPtr(userData);
            var context = (CallbackContext<T>)handle.Target;

            if (err != IntPtr.Zero)
            {
                var pitayaError = new PitayaError(
                    PitayaCluster.pitaya_error_code(err),
                    PitayaCluster.pitaya_error_message(err));
                context.t.SetException(new PitayaException($"RPC call failed: ({pitayaError.Code}: {pitayaError.Message})"));
                return;
            }

            Int32 len;
            IntPtr resData = PitayaCluster.pitaya_buffer_data(responseBuf, out len);
            byte[] resBytes = GetDataFromRawPointer(resData, len);

            try
            {
                // An RPC message comes encoded as a Protos.Response protobuf message.
                // The data field of the message is going to be either encoded in protobuf or JSON,
                // which is taken care by our serializer variable

                var rpcResponse = new Protos.Response();
                rpcResponse.MergeFrom(resBytes);

                if (rpcResponse.Error == null)
                {
                    T remoteResponse = (T)context.serializer.Unmarshal(rpcResponse.Data.ToByteArray(), typeof(T));
                    context.t.SetResult(remoteResponse);
                }
                else
                {
                    context.t.SetException(new PitayaException(
                        "rpc failed: code={0} msg={1}",
                        rpcResponse.Error.Code, rpcResponse.Error.Msg
                    ));
                }
            }
            catch (Exception e)
            {
                context.t.SetException(new PitayaException(
                    "failed to parse rpc response: {0} (payload={1})",
                    e.Message, Encoding.UTF8.GetString(resBytes)
                ));
            }
        }

        static void KickCallback(IntPtr userData, IntPtr err, IntPtr kickAnswerBuf)
        {
            var handle = GCHandle.FromIntPtr(userData);
            var context = (CallbackContext<bool>)handle.Target;

            if (err != IntPtr.Zero)
            {
                var pe = new PitayaError(
                    PitayaCluster.pitaya_error_code(err),
                    PitayaCluster.pitaya_error_message(err));
                context.t.SetException(new Exception($"Kick failed: code={pe.Code} message={pe.Message}"));
                return;
            }

            Int32 len;
            IntPtr resData = PitayaCluster.pitaya_buffer_data(kickAnswerBuf, out len);

            var kickAnswer = new KickAnswer();
            kickAnswer.MergeFrom(new CodedInputStream(GetDataFromRawPointer(resData, len)));

            if (!kickAnswer.Kicked)
            {
                context.t.SetException(new Exception($"Kick failed: received kicked=false from server"));
                return;
            }

            context.t.SetResult(true);
        }

        static void PushCallback(IntPtr userData, IntPtr err)
        {
            var handle = GCHandle.FromIntPtr(userData);
            var context = (CallbackContext<bool>)handle.Target;

            if (err != IntPtr.Zero)
            {
                var pe = new PitayaError(
                    PitayaCluster.pitaya_error_code(err),
                    PitayaCluster.pitaya_error_message(err));
                context.t.SetException(new Exception($"Push failed: code={pe.Code}, message={pe.Message}"));
                return;
            }

            context.t.SetResult(true);
        }
    }
}