using System;

namespace OpcUaChatServer.Server.Application;
public class ChatLog
{
    public ChatLog(string name, string content)
    {
        this.At = DateTime.Now;
        this.Name = name;
        this.Content = content;
    }

    public DateTime At { get; }
    public string Name { get; }
    public string Content { get; }
}
