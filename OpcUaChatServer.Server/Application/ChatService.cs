using System;
using System.ComponentModel.Composition;

namespace OpcUaChatServer.Server.Application;
[Export]
public class ChatService
{
    [Import]
    private Logger m_logger = null;

    public event Action<ChatLog> Posted;
    public event Action<uint> PostCountChanged;

    public void Post(string name, string content)
    {
        this.m_logger.Info($"name: {name}, content: {content}");
        Posted?.Invoke(new ChatLog(name, content));
        this.PostCount++;
    }

    public uint PostCount
    {
        get => this.m_postCount;
        set
        {
            this.m_postCount = value;
            PostCountChanged?.Invoke(this.m_postCount);
        }
    }
    private uint m_postCount;
}
