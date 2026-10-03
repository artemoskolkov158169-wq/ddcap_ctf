using System;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;

class Program {
    static void Main() {
        try {
            Console.WriteLine("[*] Loading DDCAP-Desktop assembly...");
            Assembly asm = Assembly.LoadFrom(@"D:\ProdCTF\Release\DDCAP-Desktop.exe");
            
            Type configType = asm.GetType("DDCAP.AltayCTF.AppConfig");
            object config = Activator.CreateInstance(configType);
            
            var services = (System.Collections.IList)configType.GetProperty("Services").GetValue(config);
            Type serviceDefType = asm.GetType("DDCAP.AltayCTF.ServiceDefinition");
            object mockSvc = Activator.CreateInstance(serviceDefType);
            serviceDefType.GetProperty("Name").SetValue(mockSvc, "TestService");
            serviceDefType.GetProperty("ListenPort").SetValue(mockSvc, 8080);
            services.Add(mockSvc);

            Type snifferType = asm.GetType("DDCAP.AltayCTF.AltayProxySniffer");
            object sniffer = Activator.CreateInstance(snifferType);
            
            Console.WriteLine("[*] Calling Start()...");
            MethodInfo startMethod = snifferType.GetMethod("Start");
            startMethod.Invoke(sniffer, new object[] { 8080, "http://127.0.0.1:8080", config });
            
            FieldInfo rawSocketsField = snifferType.GetField("_rawSockets", BindingFlags.NonPublic | BindingFlags.Instance);
            var sockets = (System.Collections.IList)rawSocketsField.GetValue(sniffer);
            
            Console.WriteLine("[+] Active Raw Sockets Count: " + sockets.Count);
            if (sockets.Count > 0) {
                Console.WriteLine("[+] SUCCESS: Engine is successfully binding to OS raw sockets.");
            } else {
                Console.WriteLine("[-] FAILURE: No sockets were bound.");
            }
            
            MethodInfo stopMethod = snifferType.GetMethod("Stop");
            stopMethod.Invoke(sniffer, null);
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
