using System;
using System.Collections.Generic;
using System.Threading;

namespace chat
{
    class Program
    {
        const int TcpPort = 55555;
        const int UdpPort = 55550;
        public static object locker = new object();//заглушка
        static string Login;      
        private const string GetHistory = "HISTORY";

        static void Main(string[] args)
        {
            List<User> Clients = new List<User>();
            List<String> History = new List<String>();
            UDP udp = new UDP();
            TCP tcp = new TCP();
            bool Chat = true;
            string Message;
            Thread UdpListenThread = null;
            Thread TcpListenThread = null;  

            try
            {
                Console.Write("Имя пользователя: ");                               
                Login = Console.ReadLine();

                udp.Connect(Login, UdpPort, ref History);
                UdpListenThread = new Thread( () => { udp.Receive(ref Clients, ref History, Login, TcpPort, UdpPort); });      
                UdpListenThread.Start();

                TcpListenThread = new Thread( () => { tcp.Listen(ref Clients, ref History, TcpPort); });
                TcpListenThread.Start();
                Thread.Sleep(5000);

                if (Clients.Count != 0)
                {                    
                 tcp.SendHistoryRequest(Clients[0]); 
                }    
                
                while (Chat)
                {
                    
                    Message = Console.ReadLine();
                    if (Message != "0")
                    {
                        tcp.SendMessage(ref Clients, Message);
                        History.Add(Login + " (" + DateTime.Now.ToLongTimeString() + ")" +": " + Message + "\n");
                    }
                    else
                    {
                        TcpListenThread.IsBackground = true;
                        UdpListenThread.IsBackground = true;
                        Console.WriteLine("you left");
                        Chat = false;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Ошибка сети!");
            }
            Console.ReadKey();
        }
      
    }
}
