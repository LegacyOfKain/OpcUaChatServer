using Opc.Ua;

namespace OpcUaChatServer.Server;
public partial class ChatServerNodeManager
{
    private ChatLogsState m_chatLogsState;

    private void SetupNodes()
    {
        this.m_chatLogsState = this.FindPredefinedNode<ChatLogsState>(Objects.ChatLogs);

        this.m_chatLogsState.Post.OnCall = this.ChatLogsState_Post;
        this.m_chatService.Posted += this.ChatService_Posted;
        this.m_chatService.PostCountChanged += this.ChatService_PostCountChanged;
    }

    private TNodeState FindPredefinedNode<TNodeState>(uint id)
        where TNodeState : NodeState
    {
        return (TNodeState)base.FindPredefinedNode(new NodeId(id, this.m_typeNamespaceIndex), typeof(TNodeState));
    }

    private ServiceResult ChatLogsState_Post(ISystemContext context, MethodState method, NodeId objectId, string name, string content)
    {
        this.m_chatService.Post(name, content);

        return ServiceResult.Good;
    }

    private void ChatService_Posted(Application.ChatLog obj)
    {
        if (!this.m_chatLogsState.AreEventsMonitored) { return; }

        // compose an event data
        var e = new ChatLogEventState(null);
        var message = new TranslationInfo(
            "ChatLogEventType",
            "en-US",
            "New chat log has been posted for '{0}'.",
            this.m_chatLogsState.DisplayName);
        e.Initialize(
            this.SystemContext,
            this.m_chatLogsState,
            EventSeverity.MediumLow,
            new LocalizedText(message));
        e.ChatLog = new ChatLogState(e);
        e.ChatLog.Value = new ChatLog();
        e.ChatLog.Value.At = obj.At;
        e.ChatLog.Value.Name = obj.Name;
        e.ChatLog.Value.Content = obj.Content;

        this.m_chatLogsState.ReportEvent(this.SystemContext, e);
    }

    private void ChatService_PostCountChanged(uint obj)
    {
        this.m_chatLogsState.PostCount.Value = obj;

        // ClearChangeMasks() does not only clear change masks but also reports changes to monitored items.
        // enum NodeStateChangeMasks: Indicates what has changed in a node.
        this.m_chatLogsState.ClearChangeMasks(this.SystemContext, true);
    }
}
