using System.CodeDom.Compiler;
using System.Reflection;
using System;
using System.Linq;
using System.Text;

namespace Tester
{
    public class CompileInterface
    {
        public static void BuildMe()
        {
            StringBuilder toBuild = new StringBuilder();
            toBuild.AppendFormat("{0} \n{{class ServiceLayer1 : BaseServiceLayerClient, IServiceLayer {{ ",
                DefaultNamespace);
            AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(d => d.GetTypes())
                 .Where(o => o.IsInterface)
                 .ToList()
                 .ForEach(i =>
                    {
                        if (Attribute.IsDefined(i, typeof(ServiceLayerDefinitionAttribute)))
                        foreach (var method in i.GetMethods())
                        { //public string testme(int x) { return BaseCallMe("testme", x); }
                            var isVoid = (method.ReturnType.Name == "void");
                            var methodToCallString = (method.ReturnType.Name == "void")
                                ? "CallMeOneWay" 
                                : "return BaseCallMe";
                            toBuild.AppendFormat("public {0} {0}(", method.ReturnType.Name, method.Name);
                            int count = 0;    
                            foreach (var p in method.GetParameters())
                            {
                                if (count++ != 0)
                                    toBuild.Append(",");
                                toBuild.AppendFormat("{0} {1}", p.ParameterType.Name, p.Name);
                            }
                            toBuild.AppendFormat(") {{ {0},\"{1}\"", methodToCallString, method.Name);
                            foreach (var p in method.GetParameters())
                            {
                                toBuild.AppendFormat(",{0}", p.Name);
                            }
                            toBuild.Append("); }}\n");
                        }

                    }
                );
            toBuild.Append("\n}}");

        }
        public static void Compile(string toBuild)
        {       
            var parameters = new CompilerParameters 
            {
                GenerateExecutable = false,
                OutputAssembly = "ServiceLayer.dll"
            };

            CompilerResults r = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromSource(parameters, toBuild);

            //verify generation
            Console.WriteLine(Assembly.LoadFrom("AutoGen.dll").GetType("B").GetField("k").GetValue(null));
        }

        private static string DefaultNamespace { get { return Assembly.GetExecutingAssembly().GetType("IServiceLayer").Namespace; }}
    }}
