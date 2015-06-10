using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Tester
{
    public class BaseServiceLayerClient
    {
        // injectable service layer client 
        public static IServiceLayerClientImplementation impl;

        // call injectable service layer client
        public void BaseCallMeOneWay(string name, params object[] args)
        {
            string json = JsonConvert.SerializeObject(new {name, args});
            impl.CallMeOneWay(json);
        }

        // call injectable service layer client
        public dynamic BaseCallMe(string name, params object[] args)
        {
            string json = JsonConvert.SerializeObject(new TransferFormat {Name = name, Args = args});
            var result = JsonConvert.DeserializeObject<object>(impl.CallMe(json));
            var exceptionResult = result as Exception;
            if (exceptionResult != null)
                throw exceptionResult;
            return impl.CallMe(json);
        }

        // currently only local implementation
        static BaseServiceLayerClient()
        {
            impl = new ServiceLayerClientImplementationLocal();
        }
    }

    public class ServiceLayerServer
    {
        public static Dictionary<string, Delegate> ExecuteServiceLayerCall = new Dictionary<string, Delegate>();

        static ServiceLayerServer()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var mytype in assembly.GetTypes())
                    foreach (var myinterface in mytype.GetInterfaces())
                        if (myinterface.GetCustomAttributes(typeof (ServiceLayerImplementationAttribute), true).Length > 0)
                            foreach (var method in myinterface.GetMethods())
                                foreach (var myattribute in method.CustomAttributes)
                                    if (myattribute.AttributeType == typeof (ServiceLayerImplementationAttribute))
                                    {
                                        List<Type> args =
                                            new List<Type>(method.GetParameters().Select(p => p.ParameterType));
                                        Type delegateType;
                                        if (method.ReturnType == typeof (void))
                                        {
                                            delegateType = Expression.GetActionType(args.ToArray());
                                        }
                                        else
                                        {
                                            args.Add(method.ReturnType);
                                            delegateType = Expression.GetFuncType(args.ToArray());
                                        }
                                        Delegate d = Delegate.CreateDelegate(delegateType, null, method);
                                        ExecuteServiceLayerCall.Add(method.Name, d);
                                    }
        }
    }

    // injectable Service Layer Client Implementation
    public interface IServiceLayerClientImplementation
    {
        string CallMe(string json);
        void CallMeOneWay(string json);
    }

    // DTO
    public class TransferFormat
    {
        public string Name;
        public object[] Args;
    }

    // Local ServiceLayerClientImplementation
    public class ServiceLayerClientImplementationLocal : IServiceLayerClientImplementation
    {
        public void CallMeOneWay(string json)
        {
            var result = JsonConvert.DeserializeObject<TransferFormat>(json);
            ServiceLayerServer.ExecuteServiceLayerCall[result.Name].DynamicInvoke(result.Args);
        }

        public string CallMe(string json)
        {
            var result = JsonConvert.DeserializeObject<TransferFormat>(json);
            return JsonConvert.SerializeObject(
                        ServiceLayerServer.ExecuteServiceLayerCall[result.Name].DynamicInvoke(result.Args));
        }
    }
}
