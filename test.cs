using System;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Text;

class Program {
    static int captured = 0;
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
            
            EventInfo evnt = snifferType.GetEvent("PacketCaptured");
            Type delegateType = evnt.EventHandlerType;
            MethodInfo handler = typeof(Program).GetMethod("OnPacketCaptured");
            Delegate del = Delegate.CreateDelegate(delegateType, null, handler);
            evnt.AddEventHandler(sniffer, del);

            Console.WriteLine("[*] Starting sniffer on port 8080...");
            MethodInfo startMethod = snifferType.GetMethod("Start");
            startMethod.Invoke(sniffer, new object[] { 8080, "http://127.0.0.1:8080", config });
            
            Thread.Sleep(1000);
            
            Console.WriteLine("[*] Sending test attack...");
            try {
                using(WebClient client = new WebClient()) {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    client.UploadString("http://127.0.0.1:8080/api/login", "POST", "admin' OR 1=1--");
                }
            } catch { } // Expected to fail, no server is actually running.
            
            Thread.Sleep(2000);
            
            MethodInfo stopMethod = snifferType.GetMethod("Stop");
            stopMethod.Invoke(sniffer, null);
            
            if (captured > 0) {
                Console.WriteLine("[+] TEST PASSED: Captured " + captured + " packets natively.");
            } else {
                Console.WriteLine("[-] TEST FAILED: No packets captured.");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
    
    public static void OnPacketCaptured(object packet) {
        Console.WriteLine("[!] Packet Captured!");
        captured++;
    }
}
