using Gtk;
namespace app;

public partial class Program
{
    static List<Message> notifications = new List<Message>();
    static void ParseLabelForMentions(Label lbl)
    {
        // turn the mentions a certain color
        // potentially add a notification system
        // also, when the mention is your name,
        // make it green

        lbl.UseMarkup = true;

        var split = lbl.Text.Split();

        for (int i = 0; i < split.Length; i++)
        {
            if (split[i].StartsWith('@'))
            {
                string color;
                if (username == split[i][1..])
                {
                    color = "#6bf27f"; // green
                    Notify(currentMessage);
                    notifications.Add(currentMessage);
                }
                else
                {
                    color = "#6ba8f2"; // blue
                }
                split[i] = $"<span foreground = '{color}'>" + split[i];
                split[i] += "</span>";
            }
        }

        lbl.Markup = string.Join(" ", split);
    }

    static void Notify(Message msg)
    {
        Console.WriteLine($"{msg.username} mentioned you in a message: {msg.cnt}");
    }
}
