using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Deft
{
    internal class ExceptionHandler
    {
        private Func<DeftConnectionOwner, Exception, DeftResponseDTO> handler;
        private ThreadOptions threadOptions;
        private Type exceptionType;
        private Router parent;

        public bool Handles(Exception exception)
        {
            return exceptionType.IsAssignableFrom(exception.GetType());
        }

        public void Handle(DeftConnectionOwner owner, uint methodIndex, Exception exception)
        {
            Action action = () => HandleInternal(owner, methodIndex, exception);

            if ((threadOptions & ThreadOptions.ExecuteAsync) != 0)
                Task.Run(action);
            else
                DeftThread.ExecuteOnSelectedTaskQueue(action, DeftConfig.DefaultRouteHandlerTaskQueue);

        }

        private void HandleInternal(DeftConnectionOwner owner, uint methodIndex, Exception exception)
        {
            try
            {
                var result = handler(owner, exception);
                DeftMethods.Respond(owner.Connection, methodIndex, result);
            }
            catch (Exception e)
            {
                Logger.LogError($"An exception has been thrown while handling exception of type {exceptionType.Name}, see exception: {e}");
                parent.HandleException(owner, methodIndex, e);
            }
        }

        public static ExceptionHandler From<TException, TResponse>(Func<DeftConnectionOwner, TException, DeftResponse<TResponse>> handler, ThreadOptions threadOptions, Router parent) where TException : Exception
        {
            return new ExceptionHandler()
            {
                threadOptions = threadOptions,
                exceptionType = typeof(TException),
                parent = parent,
                handler = (owner, exception) =>
                {
                    var response = handler(owner, (TException)exception);

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
}
