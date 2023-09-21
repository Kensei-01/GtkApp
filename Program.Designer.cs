using GLib;
using Gtk;

namespace app
{
	public partial class Program
	{
        static Box MainLayout = new Box(Orientation.Horizontal, 5);
        static Box layout = new Box(Orientation.Vertical, 0);
        static Window window = new Window($"Welcome");
        static Label label = new Label("yo !");
        static Label characterCount = new Label($"0/{messageLength}");
        static Window emptyWindow = new Window("nothing to see here...");
        static Button quitButton = new Button();
        static Entry inputText = new Entry();
        static ScrolledWindow scrollView = new ScrolledWindow();
        static Box box = new Box(Orientation.Vertical, 5);
        static Label version_ = new Label() { Text = $"version {version}" };
        static Button sendButton = new Button();
        static Button createChatRoom = new Button() { Label = "Create Room" };
        static Button joinChatRoom = new Button() { Label = "Join Room" };
        static Box header = new Box(Orientation.Vertical, 5);
        static EventBox eventArea = new EventBox();


        static void Init()
		{
            InitWindowEvents();
            window.SetDefaultSize(400, 200);
            window.DeleteEvent -= (_, __) => Gtk.Application.Quit();
            window.WindowPosition = WindowPosition.CenterAlways;
            // window.KeepAbove = true;     doesn't work for Mac Silicon
            Style.StylizeWidget(window, "app", css);

            label.Ypad = 10;
            characterCount.Ypad = 2;
            characterCount.Halign = Align.Start;
            Style.StylizeWidget(label, "normal", css);
            Style.StylizeWidget(characterCount, "count", css);


            InitAnyButton(quitButton, "Quit");
            quitButton.Clicked += QuitButton_Clicked;
            Style.StylizeWidget(quitButton, "button", css);

            inputText.Activated += InputText_Activated;
            inputText.ClipboardCut += InputText_ClipboardCut;
            inputText.Changed += InputText_Changed;
            Style.StylizeWidget(inputText, "textbox", css);

            box.Halign = Align.Fill;
            scrollView.Add(box);
            scrollView.SetSizeRequest(100, 450);
            eventArea.VisibleWindow = false;
            eventArea.Events = Gdk.EventMask.AllEventsMask;
            eventArea.DragDataReceived += ScrollViewArea_DragDataReceived;
            eventArea.DragMotion += EventArea_DragMotion;
            Style.StylizeWidget(scrollView, "contentpanel", css);

            version_.Ypad = 5;
            version_.Selectable = true;
            Style.StylizeWidget(version_, "version", css);

            Box buttons = new Box(Orientation.Horizontal, 10);

            buttons.PackStart(createChatRoom, true, true, 0);
            buttons.PackStart(joinChatRoom, true, true, 0);
            buttons.PackStart(quitButton, true, true, 0);
            Style.StylizeWidget(createChatRoom, "button", css);
            Style.StylizeWidget(joinChatRoom, "button", css);
            joinChatRoom.Clicked += JoinChatRoom_Clicked;
            createChatRoom.Clicked += CreateChatRoom_Clicked;

            Box textSection = new Box(Orientation.Horizontal, 5);
            textSection.PackStart(inputText, true, true, 0);
            textSection.PackStart(sendButton, false, true, 0);
            sendButton.WidthRequest = 60;
            sendButton.Clicked += InputText_Activated;
            Image testImage = new Image();
            testImage.File = Directory.GetCurrentDirectory() + "/testImage.png";
            sendButton.Add(testImage);
            Style.StylizeWidget(sendButton, "button", css);

            Separator separator = new Separator(Orientation.Horizontal);
            Style.StylizeWidget(separator, "seperator", css);

            start = new Label($"Chatroom: {chatroom}");
            Style.StylizeWidget(header, "start", css);
            
            header.Add(start);
            layout.Add(header);

            layout.Add(scrollView);
            layout.Add(textSection);
            layout.Add(characterCount);
            // add visual seperator
            layout.Add(separator);
            layout.Add(buttons);
            layout.Add(label);
            layout.Add(version_);

            MainLayout.PackStart(layout, true, true, 0);
            eventArea.Add(MainLayout);
            window.Add(eventArea);
            window.ShowAll();
        }

        private static void EventArea_DragMotion(object o, DragMotionArgs args)
        {
            Console.WriteLine("Drag motion was raised");
        }

        private static void ScrollViewArea_DragDataReceived(object o, DragDataReceivedArgs args)
        {
            // dragged a file into the scroll view Area
            Console.WriteLine("Dragged a file over the area");
        }

        private static void Inbox_Clicked(object sender, EventArgs e)
        {
            if (mailboxOpened) return;
            mailboxOpened = true;
            Button test = new Button("Help me");
            Style.StylizeWidget(test, "button", css);
            MainLayout.PackStart(test, true, true, 0);
            MainLayout.ShowAll();
        }

        private static void CreateChatRoom_Clicked(object sender, EventArgs e)
        {
            client.SendMessage("requesting room", MessageType.ChatRoomCreated, username);
        }

        private static void JoinChatRoom_Clicked(object sender, EventArgs e)
        {
            Window chatRoom = new Window("Enter Code");
            Entry codeEntry = new Entry();
            Button confirm = new Button() { Label = "Confirm"};
            Box contentHolder = new Box(Orientation.Vertical, 10);
            chatRoom.TransientFor = window;
            chatRoom.WindowPosition = WindowPosition.CenterOnParent;
            Style.StylizeWidget(codeEntry, "textbox", css);
            Style.StylizeWidget(confirm, "button", css);
            Style.StylizeWidget(contentHolder, "app", css);
            confirm.Clicked += Confirm_Clicked;

            contentHolder.Add(codeEntry);
            contentHolder.Add(confirm);
            chatRoom.Add(contentHolder);

            chatRoom.ShowAll();

            void Confirm_Clicked(object sender, EventArgs e)
            {
                // send to server
                client.SendMessage(codeEntry.Text, MessageType.RequestJoinCR, username);
                CloseWindow();
            }

            void CloseWindow()
            {
                chatRoom.Close();
                codeEntry.Dispose();
                codeEntry = null;
                confirm.Clicked -= Confirm_Clicked;
                confirm.Dispose();
                confirm = null;
                contentHolder.Dispose();
                contentHolder = null;
                chatRoom.Dispose();
                chatroom = null;
            }
        }


        private static void InputText_Changed(object sender, EventArgs e)
        {
            characterCount.Text = $"{inputText.Text.Length}/{messageLength}";
            if (inputText.Text.Length > messageLength)
            {
                Style.StylizeWidget(characterCount, "error", css);
            }
            else
            {
                Style.StylizeWidget(characterCount, "count", css);
            }
        }

        private static void InputText_ClipboardCut(object sender, EventArgs e)
        {
            Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", true));

            Console.WriteLine($"Copied text to clipboard: {clipboard.WaitForText()}");
        }

        private static void InputText_Activated(object sender, EventArgs e)
        {
            Console.WriteLine($"Sent Message: {inputText.Text}");
            if (inputText.Text == "")
            {
                // the input was empty
                label.Text = "Cannot send empty message!";
            }
            else
            {
                SendMessage(inputText.Text);
                if (connected && !banned && !messageTooLong)
                {
                    label.Text = "yo!";
                    // cus otherwise this overrides the error message
                    messageTooLong = false;
                }

            }
            inputText.Text = "";
        }

        static void InitWindowEvents()
        {
            window.Destroyed += Window_Destroyed;

            scrollView.HscrollbarPolicy = PolicyType.Automatic;
            scrollView.VscrollbarPolicy = PolicyType.Always;

        }

        static void InitAnyButton(Button button, string label)
        {
            quitButton.Label = label;
        }

        private static void Window_Destroyed(object sender, EventArgs e)
        {
            Console.WriteLine("The window has been closed!");
            emptyWindow.SetDefaultSize(250, 200);
            emptyWindow.Add(new Label("Looks like the session ended."));
            emptyWindow.WindowPosition = WindowPosition.Mouse;
            emptyWindow.ShowAll();
        }

        private static void Button_Clicked(object sender, EventArgs e)
        {
            SendMessage("hey!");
        }

        private static void QuitButton_Clicked(object sender, EventArgs e)
        {
            Console.WriteLine("Quitting application...");
            if (connected)
            {
                client.Disconnect();
            }
            Gtk.Application.Quit();
        }


        static void SendMessage(string text)
        {
            // check if message isn't too long
            text = text.Trim();
            if (text.Length > messageLength)
            {
                label.Text = $"Message is too long! Max. characters: {messageLength}";
                messageTooLong = true;
                return;
            }

            Box message = new Box(Orientation.Vertical, 2);
            Label content = new Label(text) { LineWrap = true, LineWrapMode = Pango.WrapMode.Char };
            Label name = new Label(username);
            name.Halign = Align.Start;
            name.Xpad = 2;
            name.Ypad = 2;
            content.Halign = Align.Start;
            content.Xpad = 2;

            if (fileFound)
            {
                Style.StylizeWidget(name, client.role.ToString(), css);
                Style.StylizeWidget(message, "message", css);
                Style.StylizeWidget(content, "normal", css);
            }
            else
            {
                label.Text = "Theere was an error loading the styles for the code.";
            }

            // try send message to server
            if (connected && !banned)
            {
                if (lastMessageAuthor != username)
                {
                    message.Add(name);
                    // don't add the name again if the message belonged to you anyway
                }

                message.Add(content);
                ParseLabelForMentions(content);

                content.Selectable = true;
                box.Add(message);
                box.ShowAll();


                client.SendMessage(text, username: username);

                // automatically scroll down the window

                lastMessageAuthor = username;
            }
            else
            {
                label.Text = "Not connected to the server.";
                Style.StylizeWidget(label, "error", css);
            }
            ScrollWindowDown();
        }


        static void ReceiveMessage(Message message)
        {
            // if the user is blocked, don't even load the message
            if (blockedUsers.Contains(message.username))
            {
                blockedMessages++;
                label.Text = $"Blocked {blockedMessages} incoming message.";
                return;
            }
            blockedMessages = 0;
            Box msg = new Box(Orientation.Vertical, 2);
            Label content = new Label(message.cnt) { LineWrap = true, LineWrapMode = Pango.WrapMode.Char };
            Label name = new Label(message.username);
            name.Halign = Align.Start;
            name.Xpad = 2;
            name.Ypad = 2;
            content.Halign = Align.Start;
            content.Xpad = 2;

            // click the name to open a new window
            name.ButtonPressEvent += Name_ButtonPressEvent;
            name.Selectable = true;
            currentMessage = message;

            if (fileFound)
            {
                Style.StylizeWidget(name, message.role.ToString(), css);
                Style.StylizeWidget(msg, "message", css);
                Style.StylizeWidget(content, "normal", css);

                if (message.role == Role.bot)
                {
                    name.Markup = $"<b>{message.username}</b> <span foreground='blue'>BOT</span>";
                }

            }

            if (message.username != lastMessageAuthor)
            {
                msg.Add(name);
                // same thing as send message here
            }

            msg.Add(content);
            ParseLabelForMentions(content);

            content.Selectable = true;

            if (message.t == MessageType.Ban)
            {
                content.Text = $"You are banned. Admin: {message.username}, Time: {System.DateTime.Now}";
            }

            else if (message.t == MessageType.ChatRoomCreated)
            {
                // chat room has been created,
                // which means we can add a 'join' button to the message
                Button joinButton = new Button() { Label = "join" };
                Button closeButton = new Button() { Label = "close" };
                Box buttonContainer = new Box(Orientation.Horizontal, 5);
                Style.StylizeWidget(joinButton, "button", css);
                Style.StylizeWidget(closeButton, "button", css);
                buttonContainer.PackStart(closeButton, true, true, 0);
                buttonContainer.PackStart(joinButton, true, true, 0);
                msg.Add(buttonContainer);
                joinButton.Clicked += JoinButton_Clicked;
                closeButton.Clicked += new EventHandler(CloseButton_Clicked);

                void CloseButton_Clicked(object sender, EventArgs e)
                {
                    client.SendMessage(message.tok, MessageType.ClosedCR, username);
                    buttonContainer.Destroy();
                    content.Text = $"Closed Chatroom \"{message.tok}\"";
                    var removed = ownedChatrooms.Remove(message.tok);
                    ExitChatRoom();
                    if (client.role != Role.admin)
                    client.role = Role.commenter;
                }
            }

            else if (message.t == MessageType.UserJoinedCR)
            {
                // user joined message, add kick button
                Button kickButton = new Button() { Label = "kick" };
                Style.StylizeWidget(kickButton, "button", css);
                msg.Add(kickButton);
                kickButton.Clicked += KickButton_Clicked;
            }
            void JoinButton_Clicked(object sender, EventArgs e)
            {
                client.SendMessage(message.tok, MessageType.RequestJoinCR, username);
                client.role = Role.owner;
                ownedChatrooms.Add(message.tok);
            }
            void KickButton_Clicked(object sender, EventArgs e)
            {
                client.SendMessage("", MessageType.KickedFromCR, message.tok);
                Button kickButton = sender as Button;
                kickButton.Destroy();
            }

            box.Add(msg);
            box.ShowAll();

            ScrollWindowDown();

            lastMessageAuthor = message.username;
        }


        static Label addLabel(string labelname, string content)
        {
            Label lbl = new Label();
            lbl.UseMarkup = true;
            lbl.Markup = $"<b>{labelname}:</b> <span foreground='gray'>{content}</span>";
            lbl.Halign = Align.Start;
            lbl.LineWrapMode = Pango.WrapMode.Char;
            lbl.Wrap = true;
            Style.StylizeWidget(lbl, "infolabel", css);
            return lbl;
        }

        private static void Info_window_Destroyed(object sender, EventArgs e)
        {
            infoWindowOpen = false;
        }

        static bool IsScheduled = false;
        static void ScrollWindowDown()
        {
            // automatically scroll the window
            // this basically waits for the next update cycle

            if (IsScheduled)
            {
                return;
            }

            Idle.Add(() =>
            {
                ScrollWindow();
                IsScheduled = false;
                return false; // Return false to indicate that the idle callback should not be called again
            });
            IsScheduled = true;
        }

        async static void ScrollWindow()
        {
            await System.Threading.Tasks.Task.Delay(10);
            Adjustment adjustment = scrollView.Vadjustment;
            adjustment.Value = adjustment.Upper - adjustment.PageSize;
            scrollView.Vadjustment = adjustment;
        }


        static void Name_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if (infoWindowOpen) return;
            // the name label was clicked
            Box info_window = new Box(Orientation.Vertical, 0);
            Label info_name = new Label("");
            Label info_date = new Label("");
            Button blockButton = new Button();
            Separator line = new Separator(Orientation.Horizontal);
            Button banButton = new Button();
            Button doneButton = new Button("Done");
            Box info_content = new Box(Orientation.Vertical, 2);

            Label currentLabel = o as Label;

            infoWindowOpen = true;
            info_window.Destroyed += Info_window_Destroyed;

            blockButton.Label = blockedUsers.Contains(currentLabel.Text) ? "Unblock" : "Block";
            blockButton.Clicked += BlockButton_Clicked;
            doneButton.Clicked += DoneButton_Clicked;


            Style.StylizeWidget(blockButton, "button", css);
            Style.StylizeWidget(banButton, "button", css);
            Style.StylizeWidget(line, "seperator", css);
            Style.StylizeWidget(info_content, "app", css);
            Style.StylizeWidget(doneButton, "button", css);

            if (!cachedProfiles.ContainsKey(currentLabel.Text))
            {
                OnProfileDataReceived += DataReceived;
                client.SendMessage($"{currentLabel.Text}", MessageType.ProfileMessage, username);
            }
            else
            {
                profileMessage = cachedProfiles[currentLabel.Text];
                DataReceived();
            }

            void DataReceived()
            {
                if (!profileMessage.success)
                {
                    info_content.Add(addLabel("Error:", "Couldn't load the proper contents"));
                }
                info_date.Text = profileMessage.joinDate;

                info_window.Add(info_content);
                info_content.Add(addLabel("Username", currentLabel.Text));
                info_content.Add(addLabel("Joined", $"{profileMessage.joinDate}"));
                info_content.Add(addLabel("Bio", $"{profileMessage.bio}"));
                if (currentLabel.Text == "server")
                {
                    blockButton.Sensitive = false;
                    banButton.Sensitive = false;
                }
                else
                {
                    info_content.Add(blockButton);
                }
                if (client.role == Role.admin)
                {
                    info_content.Add(line);
                    info_content.Add(banButton);
                    banButton.Label = "Ban";
                }

                MainLayout.PackEnd(info_window, true, true, 0);
                info_content.Add(doneButton);
                info_window.ShowAll();
            }

            void DoneButton_Clicked(object sender, EventArgs e)
            {
                MainLayout.Remove(info_window);
                info_window.Unrealize();
                info_window.Destroy();
                OnProfileDataReceived -= DataReceived;
                profileLoaded = false;
                return;
            }
        }

        static void BlockButton_Clicked(object sender, EventArgs e)
        {
            Button blockbutton = sender as Button;
            if (blockedUsers.Contains(currentMessage.username))
            {
                blockbutton.Label = "Block";
                blockedUsers.Remove(currentMessage.username);
            }
            else
            {
                blockbutton.Label = "Unblock";
                blockedUsers.Add(currentMessage.username);
            }
        }

        static void LogIn()
        {
            Window login = new Window("Choose a nickname");
            login.SetDefaultSize(300, 400);
            Button done = new Button() { Label = "Done" };
            Button quit = new Button() { Label = "Quit" };
            Box content = new Box(Orientation.Vertical, 15);
            Entry username_ = new Entry();
            Label enterName = new Label("Choose Nickname") { Ypad = 10 };
            Label chooseServer = new Label("Server: Local") { Ypad = 1 };

            // server chooser
            Button serverButton = new Button();
            serverButton.Label = "Click to choose";
            // Create the popover
            Popover popover = new Popover(serverButton);
            popover.Position = PositionType.Bottom;

            serverButton.Clicked += OnButtonClicked;

            // Create a box to hold the popover's contents
            Box popoverBox = new Box(Orientation.Vertical, 5);
            var option1 = new Button("Online");
            var option2 = new Button("Local");
            popoverBox.Add(option1);
            popoverBox.Add(option2);

            popover.Add(popoverBox);

            Style.StylizeWidget(done, "button", css);
            Style.StylizeWidget(quit, "button", css);
            Style.StylizeWidget(username_, "textbox", css);
            Style.StylizeWidget(content, "app", css);
            Style.StylizeWidget(enterName, "count", css);
            Style.StylizeWidget(chooseServer, "count", css);
            Style.StylizeWidget(serverButton, "button", css);
            Style.StylizeWidget(popoverBox, "app", css);
            Style.StylizeWidget(popover, "app", css);
            Style.StylizeWidget(option1, "button", css);
            Style.StylizeWidget(option2, "button", css);

            login.Add(content);
            content.Add(enterName);
            content.Add(username_);
            content.Add(done);
            content.Add(quit);
            content.Add(chooseServer);
            content.Add(serverButton);

            login.WindowPosition = WindowPosition.Center;
            login.ShowAll();

            done.Clicked += Done_Clicked;
            username_.Activated += Done_Clicked;
            quit.Clicked += QuitButton_Clicked;
            option1.Clicked += (sender, e) => OnMenuSelected("Online");
            option2.Clicked += (sender, e) => OnMenuSelected("Local");

            void Done_Clicked(object sender, EventArgs e)
            {
                if (username_.Text.Contains(" "))
                {
                    enterName.Text = "Cannot use spaces in Nickname.";
                }
                else if (username_.Text != " " && username_.Text.Length > 1)
                {
                    // success 
                    username = username_.Text;
                    done.Clicked -= Done_Clicked;
                    username_.Activated -= Done_Clicked;
                    quit.Clicked -= QuitButton_Clicked;
                    login.Close();
                    login.Dispose();
                    login = null;
                    done.Dispose();
                    done = null;
                    quit.Dispose();
                    quit = null;
                    content.Dispose();
                    content = null;
                    username_.Dispose();
                    username_ = null;
                    enterName.Dispose();
                    enterName = null;
                    window.Title = $"Welcome, {username}!";
                    // instantiate the client after the user has logged in
                    client = new Client(chatroom);
                    client.OnConnected += Client_OnConnected;
                    client.OnDisconnected += Client_OnDisconnect;
                    client.Init(server, 5050, "none");
                    Start();
                }
                else
                {
                    enterName.Text = "Nickname too short.";
                }
            }

            void OnButtonClicked(object sender, EventArgs e)
            {
                popover.ShowAll();
            }

            void OnMenuSelected(string menu)
            {
                Console.WriteLine("Selected " + menu);
                chooseServer.Text = "Server: " + menu;
                if (menu == "Local")
                {
                    server = "";
                }
                else
                {
                    server = ipAddress;
                }
                popover.Hide();
            }
        }
    }
}
