using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Deft
{
    internal class RouteHandler
    {
        private Func<DeftConnectionOwner, string, string, DeftResponseDTO> handler;
        private DeftRoute route;
        ThreadOptions threadOptions;

        public void Handle(DeftConnectionOwner owner, uint methodIndex, string headersJSON, string bodyJSON)
        {
            Action action = () => HandleInternal(owner, methodIndex, headersJSON, bodyJSON);

            if ((threadOptions & ThreadOptions.ExecuteAsync) != 0)
                Task.Run(action);
            else
                DeftThread.ExecuteOnSelectedTaskQueue(action, DeftConfig.DefaultRouteHandlerTaskQueue);

        }

        private void HandleInternal(DeftConnectionOwner owner, uint methodIndex, string headersJSON, string bodyJSON)
        {
            try
            {
                var result = handler(owner, headersJSON, bodyJSON);
                DeftMethods.Respond(owner.Connection, methodIndex, result);
            }
            catch (Exception e)
            {
                Logger.LogError($"An exception has been thrown while handling request on route {route}, see exception: {e}");
                DeftMethods.Respond(owner.Connection, methodIndex, new DeftResponseDTO()
                {
                    StatusCode = ResponseStatusCode.InternalServerError,
                    HeadersJSON = JsonConvert.SerializeObject(new Dictionary<string, string>() {
                                { "exception-type", e.GetType().Name },
                                { "exception-message", e.Message },
                                { "exception-stacktrace", DeftConfig.RespondWithExceptionStackTrace ? e.StackTrace : null }
                            })
                });
            }
        }

        public static RouteHandler From<TBody, TResponse>(DeftRoute route, Func<DeftConnectionOwner, DeftRequest<TBody>, DeftResponse<TResponse>> handler, ThreadOptions threadOptions)
        {
            return new RouteHandler()
            {
                route = route,
                threadOptions = threadOptions,
                handler = (owner, headersJSON, bodyJSON) =>
                {
                    var deftRequest = new DeftRequest<TBody>();

                    try
                    {
                        if (headersJSON != null)
                            deftRequest.Headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJSON);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Could not deserialize header JSON to Dictionary<string, string>, JSON: {headersJSON} \n Exception: {e}");
                    }

                    try
                    {
                        if (bodyJSON != null)
                            deftRequest.Body = JsonConvert.DeserializeObject<TBody>(bodyJSON);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Could not deserialize body JSON to {typeof(TBody).Name}, JSON: {bodyJSON} \n Exception: {e}");
                    }

                    var response = handler(owner, deftRequest);

                    return new DeftResponseDTO()
                    {
                        StatusCode = response.StatusCode,
                        HeadersJSON = JsonConvert.SerializeObject(response.Headers),
                        BodyJSON = JsonConvert.SerializeObject(response.Body)
                    };
                }
            };
        }
    }

    public enum ThreadOptions : uint
    {
        Default = 0,
        ExecuteAsync = 1,
    }
}