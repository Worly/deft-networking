﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Deft
{
    public static class DeftMethods
    {
        public static Router DefaultRouter { get; private set; } = new Router();

        private static uint sendingMethodIndex = 0;

        private static readonly Dictionary<uint, DeftSentMethod> sentMethods = new Dictionary<uint, DeftSentMethod>();

        public static void SendMethod<TBody, TResponse>(this DeftConnectionOwner connection,
            string methodRoute,
            TBody body, Dictionary<string, string> headers = null,
            Action<DeftResponse<TResponse>> onResponseCallback = null,
            ThreadOptions threadOptions = ThreadOptions.Default)
        {
            if (methodRoute == null)
                throw new ArgumentNullException("methodRoute");

            if (!connection.IsConnected)
            {
                ExecuteResponseActionOnCorrectTaskQueue(threadOptions, () =>
                {
                    Logger.LogError("Cannot send method to connection which is not connected. Hint: check with IsConnected.");
                    try
                    {
                        onResponseCallback(new DeftResponse<TResponse>()
                        {
                            StatusCode = ResponseStatusCode.NotReachable
                        });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Exception has been thrown while executing response callback for DeftMethod {methodRoute}, see exception: {e}");
                    }
                });

                return;
            }

            var methodIndex = sendingMethodIndex++;

            var deftMethod = new DeftRequestDTO()
            {
                MethodIndex = methodIndex,
                HeadersJSON = JsonConvert.SerializeObject(headers),
                BodyJSON = JsonConvert.SerializeObject(body),
                MethodRoute = methodRoute
            };


            Action<ResponseStatusCode, Dictionary<string, string>, object> callback = (statusCode, headers, body) =>
            {
                onResponseCallback?.Invoke(new DeftResponse<TResponse>()
                {
                    StatusCode = statusCode,
                    Headers = headers,
                    Body = body != null ? (TResponse)body : default
                });
            };
            var deftSentMethod = new DeftSentMethod()
            {
                MethodIndex = methodIndex,
                ToConnectionOwner = connection,
                MethodRoute = methodRoute,
                ResponseCallback = callback,
                ThreadOptions = threadOptions,
                ResponseType = typeof(TResponse),
                SentDateTime = DateTime.UtcNow,
                ResponseReceived = false
            };
            sentMethods.Add(deftSentMethod.MethodIndex, deftSentMethod);

            Task.Delay(DeftConfig.MethodTimeoutMs, Deft.CancellationTokenSource.Token).ContinueWith(t =>
            {
                if (!deftSentMethod.ResponseReceived)
                {
                    Logger.LogError($"Method (index: {deftSentMethod.MethodIndex}, route: {deftSentMethod.MethodRoute}) has timed out");
                    CallbackSentMethod(deftSentMethod.MethodIndex, ResponseStatusCode.Timeout, null, null);
                }
            }, TaskContinuationOptions.NotOnCanceled);


            PacketBuilder.SendMethod(connection.Connection, deftMethod);
        }

        internal static void ExecuteResponseActionOnCorrectTaskQueue(ThreadOptions threadOptions, Action action)
        {
            if ((threadOptions & ThreadOptions.ExecuteAsync) != 0)
                Task.Run(action);
            else
                DeftThread.ExecuteOnSelectedTaskQueue(action, DeftConfig.DefaultMethodResponseTaskQueue);
        }

        public static Task<DeftResponse<TResponse>> SendMethodAsync<TBody, TResponse>(this DeftConnectionOwner connection, string methodRoute, TBody body, Dictionary<string, string> headers = null)
        {
            if (methodRoute == null)
                throw new ArgumentNullException("methodRoute");

            var t = new TaskCompletionSource<DeftResponse<TResponse>>();
            connection.SendMethod<TBody, TResponse>(methodRoute, body, headers, r => t.TrySetResult(r), ThreadOptions.ExecuteAsync);

            return t.Task;
        }

        internal static void Respond(DeftConnection connection, uint methodIndex, ResponseStatusCode statusCode)
        {
            Respond(connection, methodIndex, new DeftResponseDTO() { StatusCode = statusCode });
        }

        internal static void Respond(DeftConnection connection, uint methodIndex, DeftResponseDTO responseDTO)
        {
            responseDTO.MethodIndex = methodIndex;

            if (connection == null)
            {
                Logger.LogError("Cannot respond to method because connection is down");
                return;
            }

            PacketBuilder.SendMethodResponse(connection, responseDTO);
        }

        internal static void ReceivedMethod(DeftConnection connection, DeftRequestDTO requestDTO)
        {
            if (connection.Owner == null)
            {
                Logger.LogError($"Could not receive method from connection without an owner, responding with {HttpStatusCode.BadRequest}");
                Respond(connection, requestDTO.MethodIndex, ResponseStatusCode.BadRequest);
                return;
            }

            DefaultRouter.Handle(connection.Owner, requestDTO.MethodIndex, DeftRoute.FromString(requestDTO.MethodRoute), requestDTO.HeadersJSON, requestDTO.BodyJSON);
        }

        internal static void ReceivedResponse(DeftConnection connection, DeftResponseDTO responseDTO)
        {
            if (connection.Owner == null)
            {
                Logger.LogError($"Could not receive method response from connection without an owner");
                return;
            }

            if (!sentMethods.TryGetValue(responseDTO.MethodIndex, out DeftSentMethod deftSentMethod))
            {
                Logger.LogError($"Received method response for unknown or already completed request with index {responseDTO.MethodIndex}");
                return;
            }

            if (deftSentMethod.ToConnectionOwner != connection.Owner)
            {
                Logger.LogError($"Received method response for index {responseDTO.MethodIndex} but from wrong connection, ignoring...");
                return;
            }

            CallbackSentMethod(responseDTO.MethodIndex, responseDTO.StatusCode, responseDTO.HeadersJSON, responseDTO.BodyJSON);
        }


        private static void CallbackSentMethod(uint methodIndex, ResponseStatusCode statusCode, string headersJSON, string bodyJSON)
        {
            if (sentMethods.TryGetValue(methodIndex, out DeftSentMethod sentMethod))
            {
                sentMethod.ResponseReceived = true;
                Dictionary<string, string> headers = null;
                object body = null;
                try
                {
                    if (headersJSON != null)
                        headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJSON);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Could not deserialize header JSON to Dictionary<string, string>, JSON: {headersJSON} \n Exception: {e}");
                }

                try
                {
                    if (bodyJSON != null)
                        body = JsonConvert.DeserializeObject(bodyJSON, sentMethod.ResponseType);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Could not deserialize body JSON to {sentMethod.ResponseType.Name}, JSON: {bodyJSON} \n Exception: {e}");
                }


                ExecuteResponseActionOnCorrectTaskQueue(sentMethod.ThreadOptions, () =>
                {
                    try
                    {
                        sentMethod.ResponseCallback(statusCode, headers, body);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Exception has been thrown while executing response callback for DeftMethod {sentMethod.MethodRoute}, see exception: {e}");
                    }
                });

                sentMethods.Remove(methodIndex);
            }
        }

        private class DeftSentMethod
        {
            public uint MethodIndex { get; set; }
            public DeftConnectionOwner ToConnectionOwner { get; set; }
            public string MethodRoute { get; set; }
            public Action<ResponseStatusCode, Dictionary<string, string>, object> ResponseCallback { get; set; }
            public ThreadOptions ThreadOptions { get; set; }
            public Type ResponseType { get; set; }
            public DateTime SentDateTime { get; set; }
            public bool ResponseReceived { get; set; }
        }
    }
}
