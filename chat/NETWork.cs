using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace chat
{
    class UDP
    {
        private UdpClient UdpSender;
        private readonly IPAddress IpAdressBroadcast = IPAddress.Parse("192.168.100.255");//IPAddress.Broadcast;
        private IPEndPoint ipEndPointBroadcast;
        private UdpClient UdpListener = null;      

        public void Connect(string login, int port, ref List<string> history)
        {                      
            try
            {
                UdpSender = new UdpClient(port, AddressFamily.InterNetwork);
                ipEndPointBroadcast = new IPEndPoint(IpAdressBroadcast, port);
                byte[] LoginBytes = Encoding.ASCII.GetBytes(login);
                int sendedData = UdpSender.Send(LoginBytes, LoginBytes.Length, ipEndPointBroadcast);
                if (sendedData == LoginBytes.Length)
                {
                    Console.WriteLine(login + " join ");
                    history.Add(login + " join \n");
                }
                UdpSender.Close();
            }
            catch
            {
                Console.WriteLine("Ошибка подключения.");
            }
        }

        public void Receive(ref List<User> Users, ref List<String> history, string login, int TcpPort, int UdpPort)//если пришел udp-пакет, добавляем отправителя в список, отправляем ему tcp-пакет  с своим логином
        {                                  
            UdpListener = new UdpClient();
            try
            {
                IPEndPoint ClientEndPoint = new IPEndPoint(IPAddress.Any, UdpPort);

                UdpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                UdpListener.ExclusiveAddressUse = false;
                UdpListener.Client.Bind(ClientEndPoint);
                while (true)
                {
                    Byte[] data = UdpListener.Receive(ref ClientEndPoint);
                    string UserName = Encoding.ASCII.GetString(data);
                    Users.Add(new User(UserName, ClientEndPoint.Address, null));

                    TcpClient NewTcp = new TcpClient();
                    NewTcp.Connect(new IPEndPoint(ClientEndPoint.Address, TcpPort));

                    Users[Users.Count - 1].Connection = NewTcp;

                    Console.WriteLine(UserName + " join ");
                    history.Add(UserName + " join \n");
                    StartClientReceive(Users[Users.Count - 1], history, Users);

                    byte[] LoginBytes = Encoding.ASCII.GetBytes(login);
                    NewTcp.GetStream().Write(LoginBytes, 0, LoginBytes.Length);
                }
            }
            catch
            {
                Console.WriteLine("Ошибка подключения.");
            }
            finally
            {
                UdpListener.Close();
            }
        }

        public void StartClientReceive(User client, List<String> History, List<User> clients)
        {
            Thread ClientThread = new Thread(() => { client.GetTcpMessages(ref History, ref clients); });
            ClientThread.Start();
        }

    }


    class TCP
    {
        private const int bufLen = 64;
        private const string askHistory = "HISTORY";
        TcpListener tcpListener;

        public void SendMessage(ref List<User> clients, string Message)
        {
            byte[] MessageBytes = Encoding.ASCII.GetBytes(Message);
            foreach (User client in clients)
            {
                client.Connection.GetStream().Write(MessageBytes, 0, MessageBytes.Length);
            }
        }

        public void Listen(ref List<User> clients, ref List<String> history, int port)//ожидание сообщения
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            try
            {
                while (true)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    IPAddress SenderIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
                    User Sender = clients.Find(x => x.IpAddr == SenderIP);
                    if (Sender == null)// если отправитель неизвестен - создать нового                     
                    {
                        lock (Program.locker) 
                        {      // т.к User - общедоступный класс( для всех потоков) необходимо на время создания обьекта заблокировать все потоки 
                            User item = new User(null, SenderIP, client);
                            clients.Add(item);
                            Sender = item;
                        }
                    }
                    StartClientReceive(Sender, history, clients);
                }
            }
            catch
            {
                Console.WriteLine("Ошибка приема сообщений.");
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        public void StartClientReceive(User client, List<String> History, List<User> clients)
        {
            Thread ClientThread = new Thread(() => { client.GetTcpMessages(ref History, ref clients); });
            ClientThread.IsBackground = true;
            ClientThread.Start();
        }

        public void SendHistoryRequest(User client)
        {
            
            byte[] AskHistoryBytes = Encoding.ASCII.GetBytes(askHistory);
            client.Connection.GetStream().Write(AskHistoryBytes, 0, AskHistoryBytes.Length);
        }
    }
}
