using System.Reflection;
using Gtk;

namespace app
{
    public delegate void ProfileDataReceived();
    public partial class Program
    {
        static string ipAddress = "34.125.141.21";
        static string server = ""; // 34.125.141.21
        static readonly string version = "0.0.2 BETA";
        static string username = "undefined2";
        static readonly CssProvider css = new CssProvider();
        static readonly bool fileFound = css.LoadFromData(Style.Data);
        const int messageLength = 500;
        static bool connected = false;
        static Client? client;
        static bool banned = false;
        static string lastMessageAuthor = "";
        static string chatroom = "Global";
        static List<string> ownedChatrooms = new List<string>();
        static List<string> blockedUsers = new List<string>();
        static Dictionary<string, ProfileMessage> cachedProfiles = new Dictionary<string, ProfileMessage>();
        static bool messageTooLong = false;
        static Label start = new Label("");
        static bool infoWindowOpen = false;
        static int blockedMessages = 0;
        static bool mailboxOpened = false;
        static bool profileLoaded = false;
        static ProfileMessage profileMessage = new ProfileMessage();
        static ProfileDataReceived OnProfileDataReceived = new ProfileDataReceived(EmptyEvent);
        static Message currentMessage = new Message();
        
        public static void Main()
        {
            // BOT CODE, ONLY FOR TESTING PURPOSES
            /*
            Bot bot = new Bot("Cock", "yourmomisgaylmfao");

            bot.RequestListenOnChatroom("Global");

            bot.RegisterSignal(SignalType.ON_MESSAGE);
            bot.RegisterSignal(SignalType.ON_USER_JOIN);

            bot.onMessage += OnMessage;
            bot.onUserJoin += OnUserJoin;

            void OnMessage(Signal signal)
            {
                Console.WriteLine($"{signal.user.username} sent a message at {signal.time}. (Content: {signal.content})");
                if (signal.content.StartsWith("hello"))
                { 
                    bot.RequestSendMessage($"Hello there, @{signal.user.username}", signal.user.chatroom);
                }
            }

            static void OnUserJoin(Signal signal)
            {
                Console.WriteLine($"User {signal.user.username} just joined the chatroom.");
            }

            bot.MainLoop();
            */

            
            Application.Init();
            LogIn();
            Application.Run();
            
            
        }

        public static void EmptyEvent()
        {
            Console.WriteLine("Received Profile Data.");
        }

        static void InitScrollView()
        {
            Label beginning_of_chat = new Label($"-Beggining of chat-");
            Style.StylizeWidget(beginning_of_chat, "begnningofchat", css);
            beginning_of_chat.Ypad = 5;
            box.Add(beginning_of_chat);
            box.ShowAll();
        }

        static void Start()
        {
            Init();
            InitScrollView();
        }

        static void Client_OnConnected(object sender, ClientEventArgs e)
        {
            connected = true;
            client?.SendMessage("registering", 0, username);
            Thread receiveMessages = new Thread(ReceiveMessages);
            receiveMessages.Start();
        }

        static void Client_OnDisconnect(object sender, ClientEventArgs e)
        {
            connected = false;
        }

        static async void ReceiveMessages()
        {
            Console.WriteLine("Entered thread for server");

            while (connected)
            {
                Message message = await Task.Run( () => client?.ReceiveMessage() ?? new Message() { t = MessageType.Shutdown, cnt = "<null>" });
                Application.Invoke((sender, args) => HandleMessages(message));
            }
        }

        private static void HandleMessages(Message message)
        {
            switch (message.t)
            {
                case MessageType.UsernameExists:
                    // the requested username already existed, changing it..
                    username = message.cnt;
                    break;

                case MessageType.Normal:
                    // normal message
                    ReceiveMessage(message);
                    Console.WriteLine("Received message from another client");
                    break;

                case MessageType.Ban:
                    // you are banned
                    banned = true;
                    label.Text = $"You are banned. (reason: {message.cnt})";
                    Style.StylizeWidget(label, "error", css);
                    ReceiveMessage(message);
                    break;

                case MessageType.Promotion:
                    // promotion
                    if (client is not null)
                    client.role = Role.admin;
                    ReceiveMessage(message);
                    break;

                case MessageType.Unban:
                    // unban
                    banned = false;
                    label.Text = "yo!";
                    Style.StylizeWidget(label, "normal", css);
                    break;

                case MessageType.Demotion:
                    // demotion
                    client.role = Role.commenter;
                    ReceiveMessage(message);
                    break;

                case MessageType.Shutdown:
                    // shutdown
                    Console.WriteLine("Closed server -1");
                    connected = false;
                    return;

                case MessageType.ChatRoomCreated:
                    /* chatroom created
                     * 
                     * no longer automatically join the room -- updated
                     * 
                    chatroom = message.tok;
                    default_.Text = $"Chatroom: {chatroom}";
                    client.token = chatroom;
                    */
                    ownedChatrooms.Add(message.tok);
                    ReceiveMessage(message);
                    break;

                case MessageType.ChatRoomApproved:
                    // chatroom aprooved
                    if (ownedChatrooms.Contains(message.tok))
                    {
                        if (client?.role != Role.admin)
                        client.role = Role.owner;
                    }
                    else
                    {
                        client.role = Role.commenter;
                    }
                    ExitChatRoom(message.tok);
                    ReceiveMessage(new Message() { cnt = $"Joined Chatroom \"{chatroom}\"", role = Role.admin, username = "server" });
                    break;

                case MessageType.ChatRoomNotFound:
                    // chatroom doesn't exist
                case MessageType.UserJoinedCR:
                    // someone joined your room
                    ReceiveMessage(message);
                    break;

                case MessageType.KickedFromCR:
                    // you were kicked
                    ExitChatRoom();
                    ReceiveMessage(message);
                    break;
                case MessageType.ClosedCR:
                    // chatroom closed
                    ExitChatRoom();
                    ReceiveMessage(message);
                    break;

                case MessageType.ProfileMessage:
                    // we received the profile information for the user
                    // the message.cnt holds a json string in this case
                    // deserialize it
                    profileMessage = ProfileMessage.fromJson(message.cnt);
                    profileLoaded = true;
                    OnProfileDataReceived();
                    if (!cachedProfiles.ContainsKey(profileMessage.username))
                    cachedProfiles.Add(profileMessage.username, profileMessage);
                    break;

                case MessageType.ImageTransfer:
                    // prepare for receiving an image
                    // but still receive the normal message

                    ReceiveMessage(message);
                    break;

                default:
                    Console.WriteLine(message.asJson());
                    break;
            }
        }

        static void ExitChatRoom(string value = "Global")
        {
            chatroom = value;
            start.Text = $"Chatroom: {chatroom}";
            client.token = chatroom;
        }
    }

    public readonly struct Style
    {
        public static string Data
        {
            get
            {
                string exeDirectory = Assembly.GetEntryAssembly().Location;
                string path = Path.GetDirectoryName(exeDirectory);
                // Console.WriteLine(path + "/style.css");
                using StreamReader sr = new(path + "/style.css");
                return sr.ReadToEnd();
            }
        }

        public static void StylizeWidget(Widget widget, string name, CssProvider provider)
        {
            StyleContext styleContext = widget.StyleContext;
            styleContext.AddProvider(provider, StyleProviderPriority.User);
            widget.Name = name;
        }
    }
}
