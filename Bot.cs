using System;
using Gtk;
using Newtonsoft.Json;
namespace app;

public class Bot
{
	public delegate void OnMessage(Signal signal);
	public delegate void OnUserJoin(Signal signal);
	public delegate void OnUserLeave(Signal signal);

    Client client = new Client();
	public string name = "";
	public bool connected = false;
	public OnMessage onMessage;
	public OnUserJoin onUserJoin;
	public OnUserLeave onUserLeave;
	public string prefix = "";
	public bool caseSensitive = true;

    public Bot(string botName, string BotToken)
	{
		name = botName;
		client.Init("", 5050, botName);
        client.token = BotToken;
        client.SendMessage(BotToken, MessageType.BotClient, botName);
		client.OnConnected += OnConnected;
		onMessage += EmptyMessageEvent;
		onUserJoin += EmptyMessageEvent;
		onUserLeave += EmptyMessageEvent;
        Thread receive = new Thread(ReceiveSignals);
        receive.Start();
    }

    void EmptyMessageEvent(Signal message)
	{
		// empty signal
	}

	public void RegisterSignal(SignalType signalType)
	{
		// tell the server that you want to receive a certain type of signals
		client.SendRequest(Request.register_signal, "", signalType);
	}

	public void RequestListenOnChatroom(string chatroom)
	{
		client.SendRequest(Request.listen_on_chatroom, chatroom, SignalType.NONE);
	}

	void OnConnected(object sender, ClientEventArgs clientHandler)
	{
		Console.WriteLine("-BOT CONNECTION SUCCESS-");
		connected = true;
	}

    public async void ReceiveSignals()
    {
        Console.WriteLine("BOT entered thread for server");

        while (true)
        {
			Signal signal = await Task.Run(() => client?.ReceiveSignals() ?? new Signal());
			switch (signal.signal)
			{
				case SignalType.ON_MESSAGE:
					onMessage(signal);
					break;

				case SignalType.ON_USER_JOIN:
					onUserJoin(signal);
					break;

				case SignalType.ON_USER_LEAVE:
					onUserLeave(signal);
					break;
			}
        }
    }

	public void RequestSendMessage(string content, string chatroom)
	{
		BotMessage msg = new BotMessage()
		{
			content = content,
			chatroom = chatroom
		};
		client.SendRequest(Request.send_message, msg.asJson(), SignalType.NONE);
	}

	public void MainLoop()
	{
        while (true)
        {
            // wait for traffic
        }
    }
}

public enum SignalType: int
{
	ON_MESSAGE,
	ON_USER_JOIN,
	ON_USER_LEAVE,
	ON_USER_MENTION,
	ON_SERVER_CRASH,
	NONE,
	ON_ERROR
}

public enum Request: int
{
	login,
	register_signal,
	send_message,
	listen_on_chatroom
}

[Serializable]
public class Signal
{
	public SignalType signal;
	public User user = new User();
	public string content = "";
	public string time = "";

	public string asJson()
	{
		return JsonConvert.SerializeObject(this);
	}

	public static Signal fromJson(string jsonString)
	{
		try
		{
            return (Signal)(JsonConvert.DeserializeObject(jsonString, typeof(Signal)) ?? new Signal());
        }
        catch (JsonReaderException)
		{
			Console.WriteLine("An error occurred deserializing the Signal json string.");
			return new Signal();
		}
    }

	public Signal(SignalType signal = SignalType.ON_MESSAGE)
	{
		this.signal = signal;
	}
}

public class RequestMessage
{
	public Request request;
	public string content = "";
	public SignalType signalType;

    public string asJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class BotMessage
{
	public string chatroom = "";
	public string content = "";

	public string asJson()
	{
		return JsonConvert.SerializeObject(this);
	}
}


[Serializable]
public class User
{
	public string username = "";
	public string joinDate = "";
	public string bio = "";
	public string chatroom = "";
}
