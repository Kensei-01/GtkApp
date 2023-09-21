// A C# program for Client
using System.Net;
using System.Net.Sockets;
using System.Text;
using Gdk;
using Newtonsoft.Json;

#pragma warning disable IDE0079
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8600
#pragma warning disable CS0618


namespace app
{
    public delegate void ClientHandler(object sender, ClientEventArgs e);

    public class Client
    {
        const int bufferSize = 1024;
        IPHostEntry ipHost;
        IPAddress ipAddr;
        IPEndPoint localEndPoint;
        Socket sender;
        byte[] buffer = new byte[bufferSize];
        public string token = "none";
        public ClientHandler OnConnected;
        public ClientHandler OnDisconnected;
        public Role role = Role.commenter;

        public Client(string token = "none")
        {
            this.token = token;
            OnConnected += OnConnectedEvent;
            OnDisconnected += OnConnectedEvent;
        }

        public void Init(string server, int port, string token)
        {
            try
            {
                ipHost = Dns.GetHostByName(server);
                ipAddr = ipHost.AddressList[0];
                localEndPoint = new IPEndPoint(ipAddr, port);


                sender = new Socket(ipAddr.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine(ipHost.AddressList[0]);

                try
                {
                    sender.Connect(localEndPoint);

                    Console.WriteLine("Socket connected to -> {0} ",
                                sender.RemoteEndPoint.ToString());
                    ClientEventArgs evArg = new ClientEventArgs() { token = token };
                    OnConnected(this, evArg);
                }

                // Manage of Socket's Exceptions
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        void OnConnectedEvent(object sender, ClientEventArgs e)
        {
            // empty cus otherwise null

        }

        public void SendMessage(string message, MessageType type = MessageType.Normal, string username = "<undefined>")
        {
            Message msgObject = new()
            {
                cnt = message ?? "<empty>",
                tok = token,
                t = type,
                username = username,
                role = role
            };

            byte[] msg = Encoding.UTF8.GetBytes(msgObject.asJson());
            try
            {
                int send = sender.Send(msg);
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't send message to server");
            }
        }

        public void SendRequest(Request request, string content, SignalType signalType)
        {
            RequestMessage msgObject = new()
            {
                request = request,
                content = content,
                signalType = signalType
            };

            byte[] msg = Encoding.UTF8.GetBytes(msgObject.asJson());
            try
            {
                int send = sender.Send(msg);
                Thread.Sleep(200);
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't send message to server");
            }
        }

        public Message ReceiveMessage()
        {
            try
            {
                int rec = sender.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer, 0, rec);

                if (message == "")
                {
                    Console.WriteLine("Looks like the server quit unexpectedly...");
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                else
                {
                    return Message.fromJson(message);
                }
                Console.WriteLine($"Message from Server -> {message}");

                return new Message(content: "<null>", type: MessageType.Shutdown);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("The server crashed unexpectedly.");
                return new Message(content: "<null>", type: MessageType.Shutdown);
            }
        }

        public Signal ReceiveSignals()
        {
            try
            {
                int rec = sender.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer, 0, rec);
                if (message == "")
                {
                    Console.WriteLine("Looks like the server quit unexpectedly...");
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                else
                {
                    return Signal.fromJson(message);
                }
                Console.WriteLine($"Message from Server -> {message}");
                return new Signal();
            }
            catch(ObjectDisposedException)
            {
                Console.WriteLine("The server crashed unexpectedly.");
                return new Signal(SignalType.ON_SERVER_CRASH);
            }
        }

        public void Disconnect()
        {
            OnDisconnected(this, new ClientEventArgs() { token = token });
            SendMessage("-1", type: MessageType.Shutdown);
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            Console.WriteLine("disconnected from server");
        }

        public async void ReceiveImage(Gtk.Image img)
        {
            // Receive image data
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[1024]; // Adjust buffer size as needed
                int bytesRead;
                while ((bytesRead = await sender.ReceiveAsync(buffer, SocketFlags.None)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }

                // Create a Pixbuf from the received image data
                memoryStream.Seek(0, SeekOrigin.Begin);
                Pixbuf pixbuf = new Pixbuf(memoryStream);
                img.Pixbuf = pixbuf;
            }
        }

        public void SendImage(string imagePath)
        {
            try
            {
                using (FileStream fileStream = File.OpenRead(imagePath))
                {
                    byte[] buffer = new byte[1024]; // Adjust buffer size as needed
                    int bytesRead;

                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sender.Send(buffer, 0, bytesRead, SocketFlags.None);
                    }
                }

                Console.WriteLine("Image sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }

    // message to be sent to the server
    [Serializable]
    public class Message
    {
        public MessageType t = MessageType.Normal;
        public string cnt = "";
        public string tok = "server";
        public Role role = Role.commenter;
        public string username = "<undefined>";

        public string asJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Message fromJson(string jsonString)
        {
            try
            {
                return (Message)JsonConvert.DeserializeObject(jsonString, typeof(Message)) ?? new Message();
            }
            catch (JsonReaderException)
            {
                Console.WriteLine("Error parsing the json string at the end...");
                return new Message() { t = MessageType.Shutdown, tok = "null" };
            }
        }

        public Message() { }
        public Message(string content, MessageType type)
        {
            cnt = content;
            t = type;
        }
        public Message(string content)
        {
            cnt = content;
        }
    }

    [Serializable]
    public class ProfileMessage
    {
        public string username = "";
        public string joinDate = "";
        public string bio = "";
        public bool success = true;

        public static ProfileMessage fromJson(string jsonString)
        {
            try
            {
                return (ProfileMessage)JsonConvert.DeserializeObject(jsonString, typeof(ProfileMessage)) ?? new ProfileMessage();
            }
            catch (JsonReaderException)
            {
                Console.WriteLine("Error parsing the json string at the end...");
                return new ProfileMessage() { success = false};
            }
        }
    }

    public enum Role : int
    {
        commenter,
        admin,
        owner,
        bot
    }

    public class ClientEventArgs : EventArgs
    {
        public string token = "<null>";

    }

    public enum MessageType : int
    {
        BotClient = -3,
        UsernameExists,
        Shutdown,
        _PH,
        Normal,
        Ban,
        Promotion,
        Unban,
        Demotion,
        ChatRoomCreated,
        RequestJoinCR,
        ChatRoomApproved = 9,
        ChatRoomNotFound,
        UserJoinedCR,
        KickedFromCR,
        ClosedCR,
        ProfileMessage,
        ImageTransfer
    }
}
