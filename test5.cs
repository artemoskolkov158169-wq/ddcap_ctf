using System;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;

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
            
            var activePortsField = snifferType.GetField("_activePorts", BindingFlags.NonPublic | BindingFlags.Instance);
            var activePorts = (System.Collections.IDictionary)Activator.CreateInstance(activePortsField.FieldType);
            activePorts.Add(8080, mockSvc);
            activePortsField.SetValue(sniffer, activePorts);

            var processingChannelField = snifferType.GetField("_processingChannel", BindingFlags.NonPublic | BindingFlags.Instance);
            var processingChannel = Activator.CreateInstance(processingChannelField.FieldType);
            processingChannelField.SetValue(sniffer, processingChannel);
            
            MethodInfo processMethod = snifferType.GetMethod("ProcessTrafficQueue", BindingFlags.NonPublic | BindingFlags.Instance);
            var isRunningField = snifferType.GetField("_isRunning", BindingFlags.NonPublic | BindingFlags.Instance);
            isRunningField.SetValue(sniffer, true);
            System.Threading.Tasks.Task.Run(new Action(() => { processMethod.Invoke(sniffer, null); }));

            EventInfo evnt = snifferType.GetEvent("PacketCaptured");
            Type delegateType = evnt.EventHandlerType;
            MethodInfo handler = typeof(Program).GetMethod("OnPacketCaptured");
            Delegate del = Delegate.CreateDelegate(delegateType, null, handler);
            evnt.AddEventHandler(sniffer, del);

            Console.WriteLine("[*] Injecting simulated raw IP/TCP packet to test the new parser...");
            
            string httpPayload = "GET /api/flag HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(httpPayload);
            
            byte[] packet = new byte[20 + 20 + payloadBytes.Length];
            
            packet[0] = 0x45;
            packet[9] = 6;
            packet[12] = 192; packet[13] = 168; packet[14] = 1; packet[15] = 100;
            packet[16] = 10; packet[17] = 0; packet[18] = 0; packet[19] = 1;
            
            packet[20] = (byte)(12345 >> 8); packet[21] = (byte)(12345 & 0xFF);
            packet[22] = (byte)(8080 >> 8); packet[23] = (byte)(8080 & 0xFF);
            packet[32] = 0x50;
            
            Array.Copy(payloadBytes, 0, packet, 40, payloadBytes.Length);

            Type mockAsyncResultType = typeof(MockAsyncResult);
            object mockAsyncResult = Activator.CreateInstance(mockAsyncResultType, new object[] { packet });
            
            MethodInfo onReceiveMethod = snifferType.GetMethod("OnReceive", BindingFlags.NonPublic | BindingFlags.Instance);
            onReceiveMethod.Invoke(sniffer, new object[] { mockAsyncResult });

            Thread.Sleep(2000);
            
            isRunningField.SetValue(sniffer, false);
            
            if (captured > 0) {
                Console.WriteLine("[+] TEST PASSED: The new manual IP/TCP parser correctly decoded the raw packet!");
            } else {
                Console.WriteLine("[-] TEST FAILED: Packet was not captured or parser failed.");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
    
    public static void OnPacketCaptured(object packet) {
        Console.WriteLine("[!] Packet Captured Successfully!");
        
        string method = (string)packet.GetType().GetProperty("Method").GetValue(packet);
        string uri = (string)packet.GetType().GetProperty("Uri").GetValue(packet);
        string reqHeaders = (string)packet.GetType().GetProperty("ReqHeaders").GetValue(packet);
        
        Console.WriteLine("    Method : " + method);
        Console.WriteLine("    Uri    : " + uri);
        Console.WriteLine("    Headers: " + reqHeaders.Substring(0, Math.Min(reqHeaders.Length, 20)).Replace("\r", "").Replace("\n", " ") + "...");
        
        captured++;
    }
}

public class MockAsyncResult : IAsyncResult {
    private object _state;
    public object AsyncState { get { return _state; } }
    public WaitHandle AsyncWaitHandle { get { return null; } }
    public bool CompletedSynchronously { get { return true; } }
    public bool IsCompleted { get { return true; } }
    
    public MockAsyncResult(byte[] packet) {
        _state = new object[] { new MockSocket(), packet };
    }
}

public class MockSocket {
    public int EndReceive(IAsyncResult ar) {
        return ((byte[])((object[])ar.AsyncState)[1]).Length;
    }
    public void BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state) {
    }
}
