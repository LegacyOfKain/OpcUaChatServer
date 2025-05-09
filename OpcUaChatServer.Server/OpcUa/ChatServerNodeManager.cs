// this file is based on
// https://github.com/OPCFoundation/UA-.NETStandard/blob/be65403945d4f0bf366f3299222fcb6cb08d6cb5/SampleApplications/Samples/Opc.Ua.Sample/Boiler/BoilerNodeManager.cs

/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using Opc.Ua;
using Opc.Ua.Server;
using OpcUaChatServer.Server.Application;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;

namespace OpcUaChatServer.Server;
// Application logics are implemented separately on ChatServerNodeManager.Setup.cs

/// <summary>
/// A node manager for chat server.
/// </summary>
public partial class ChatServerNodeManager : CustomNodeManager2
{
    [Import]
    private ChatService m_chatService = null;

    #region Constructors
    /// <summary>
    /// Initializes the node manager.
    /// </summary>
    public ChatServerNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration)
    {
        MefManager.Container.ComposeParts(this);

        // update the namespaces.
        var namespaceUris = new List<string>();
        namespaceUris.Add(Namespaces.OpcUaChatServer);
        namespaceUris.Add(Namespaces.OpcUaChatServer + "/Instance");
        this.NamespaceUris = namespaceUris;

        this.m_typeNamespaceIndex = this.Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
        this.m_namespaceIndex = this.Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);

        this.m_lastUsedId = 0;

        // Set ChatService instance as SystemHandle so that
        // methods of the nodes can easily access it via given ISystemContext.
        this.SystemContext.SystemHandle = this.m_chatService;
    }
    #endregion

    #region INodeIdFactory Members
    /// <summary>
    /// Creates the NodeId for the specified node.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="node">The node.</param>
    /// <returns>The new NodeId.</returns>
    public override NodeId New(ISystemContext context, NodeState node)
    {
        var id = Utils.IncrementIdentifier(ref this.m_lastUsedId);
        return new NodeId(id, this.m_namespaceIndex);
    }
    #endregion

    #region INodeManager Members
    /// <summary>
    /// Does any initialization required before the address space can be used.
    /// </summary>
    /// <remarks>
    /// The externalReferences is an out parameter that allows the node manager to link to nodes
    /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
    /// should have a reference to the root folder node(s) exposed by this node manager.  
    /// </remarks>
    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (this.Lock)
        {
            base.CreateAddressSpace(externalReferences);

            this.SetupNodes();
        }
    }

    /// <summary>
    /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
    /// </summary>
    protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
    {
        // Set your .uanodes file to Build Action = Embedded Resource
        // resourcePath parameter of LoadFromBinaryResource() is project name + directory + file name

        var predefinedNodes = new NodeStateCollection();
        predefinedNodes.LoadFromBinaryResource(context, "OpcUaChatServer.Server." + "OpcUa.Model.Published." + "OpcUaChatServer.PredefinedNodes.uanodes", this.GetType().GetTypeInfo().Assembly, true);
        return predefinedNodes;
    }

    /// <summary>
    /// Replaces the generic node with a node specific to the model.
    /// </summary>
    protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
    {
        var passiveNode = predefinedNode as BaseObjectState;

        if (passiveNode == null)
        {
            return predefinedNode;
        }

        var typeId = passiveNode.TypeDefinitionId;

        if (!this.IsNodeIdInNamespace(typeId) || typeId.IdType != IdType.Numeric)
        {
            return predefinedNode;
        }

        switch ((uint)typeId.Identifier)
        {
            // Write cases in same way for all defined ObjectTypes

            case ObjectTypes.ChatLogsType:
                {
                    if (passiveNode is ChatLogsState)
                    {
                        break;
                    }

                    var activeNode = new ChatLogsState(passiveNode.Parent);
                    activeNode.Create(context, passiveNode);

                    if (passiveNode.Parent != null)
                    {
                        passiveNode.Parent.ReplaceChild(context, activeNode);
                    }

                    return activeNode;
                }

            case ObjectTypes.ChatLogEventType:
                {
                    if (passiveNode is ChatLogEventState)
                    {
                        break;
                    }

                    var activeNode = new ChatLogEventState(passiveNode.Parent);
                    activeNode.Create(context, passiveNode);

                    if (passiveNode.Parent != null)
                    {
                        passiveNode.Parent.ReplaceChild(context, activeNode);
                    }

                    return activeNode;
                }
        }

        return predefinedNode;
    }

    #endregion

    #region Private Fields
    private ushort m_namespaceIndex;
    private ushort m_typeNamespaceIndex;
    private long m_lastUsedId;
    #endregion
}
