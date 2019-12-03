using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace chat
{
    class User
    {
        public string Name;
        public IPAddress IpAddr;
        public TcpClient Connection;
        private const int bufLen = 64;
        private const string HistoryKeyWord = "HISTORY";

        public User(string name, IPAddress ipAddr, TcpClient connection)
        {
            Name = name;
            IpAddr = ipAddr;
            Connection = connection;
        }



        public void GetTcpMessages(ref List<String> History, ref List<User> clients)
        {
            NetworkStream OneUserStream = Connection.GetStream(); // метод получения сообщения из потока tcp
            try
            {
                while (true)
                {
                    byte[] byteMessage = new byte[bufLen];
                    StringBuilder MessageBuilder = new StringBuilder();
                    string message;
                    int RecBytes = 0;
                    do
                    {
                        RecBytes = OneUserStream.Read(byteMessage, 0, byteMessage.Length);
                        MessageBuilder.Append(Encoding.UTF8.GetString(byteMessage, 0, RecBytes));
                    }
                    while (OneUserStream.DataAvailable);

                    message = MessageBuilder.ToString();
                    if (message != HistoryKeyWord)
                    {
                        if (Name == null) // первое полученное сообщение от пользователя - его имя
                        {
                            Name = message;
                        }
                        else
                        {
                            Console.WriteLine(Name + "(" + DateTime.Now.ToLongTimeString() + ")"  + ": " + message);
                            History.Add(Name + "(" + DateTime.Now.ToLongTimeString() + ")" + ": " + message + "\n");
                        }
                    }
                    else
                    {
                        HistoryRecieve(ref History, this);
                    }
                }
            }
            catch
            {
                // если пользователь вышел либо закрыл программу(определяется, если не удалось отправить сообщение ) - получаем его адрес и удаляем из списка 
                Console.WriteLine(Name + " left chat ");
                History.Add(Name + " left chat \n");
                var address = ((IPEndPoint)Connection.Client.RemoteEndPoint).Address;
                lock (Program.locker)
                {
                    clients.RemoveAll(X => X.IpAddr.ToString() == address.ToString());
                }
            }
            finally
            {
                if (OneUserStream != null)
                OneUserStream.Close();
                if (Connection != null)
                Connection.Close();
              
            }
        }        

        public void HistoryRecieve(ref List<string> History, User client)
        {
            byte[] HistoryItemBytes;
            foreach (string HistoryItem in History)
            {
                HistoryItemBytes = Encoding.ASCII.GetBytes(HistoryItem);
                client.Connection.GetStream().Write(HistoryItemBytes, 0, HistoryItemBytes.Length);
            }           
        }

    }
}
