using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Deft
{
    internal class RouteHandler
    {
        private Func<DeftRequest, DeftResponse> handler;
        private ThreadOptions threadOptions;

        public async Task<DeftResponse> Handle(DeftRequest request)
        {
            if (!DeftThread.IsOnDeftThread())
                Logger.LogError("RouteHandler is not on DeftThread!");

            DeftResponse response;

            if ((threadOptions & ThreadOptions.ExecuteAsync) != 0)
                response = await Task.Run(() => handler(request));
            else
            {
                var taskCompletionSource = new TaskCompletionSource<DeftResponse>();
                DeftThread.ExecuteOnSelectedTaskQueue(() =>
                {
                    try
                    {
                        var result = handler(request);
                        taskCompletionSource.SetResult(result);
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }, DeftConfig.DefaultRouteHandlerTaskQueue);

                response = await taskCompletionSource.Task;
            }

            if (!DeftThread.IsOnDeftThread())
                Logger.LogError("RouteHandler is not on DeftThread after handler executed!");

            return response;
        }

        public static RouteHandler From<TBody, TResponse>(Func<DeftConnectionOwner, DeftRequest<TBody>, DeftResponse<TResponse>> handler, ThreadOptions threadOptions)
        {
            return new RouteHandler()
            {
                threadOptions = threadOptions,
                handler = (request) =>
                {
                    var deftRequest = new DeftRequest<TBody>();
                    deftRequest.Headers = request.Headers;

                    try
                    {
                        if (request.BodyJSON != null)
                            deftRequest.Body = JsonConvert.DeserializeObject<TBody>(request.BodyJSON);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Could not deserialize body JSON to {typeof(TBody).Name}, JSON: {request.BodyJSON} \n Exception: {e}");
                    }

                    var response = handler(request.Owner, deftRequest);

                    return new DeftResponse()
                    {
                        StatusCode = response.StatusCode,
                        Headers = response.Headers,
                        BodyJSON = JsonConvert.SerializeObject(response.Body)
                    };
                }
            };
        }
    }
}