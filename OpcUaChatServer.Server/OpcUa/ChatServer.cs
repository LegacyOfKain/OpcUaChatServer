// this file is based on
// https://github.com/OPCFoundation/UA-.NETStandard/blob/be65403945d4f0bf366f3299222fcb6cb08d6cb5/SampleApplications/Samples/Opc.Ua.Sample/SampleServer.cs

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

#define CUSTOM_NODE_MANAGER

using Opc.Ua;
using Opc.Ua.Server;
using OpcUaChatServer.Server.Application;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace OpcUaChatServer.Server;
/// <summary>
/// A class which implements an instance of a UA server.
/// </summary>
public partial class ChatServer : StandardServer
{
    [Import]
    private Logger m_logger = null;

    public ChatServer()
    {
        MefManager.Container.ComposeParts(this);
    }

    #region Overridden Methods
    /// <summary>
    /// Initializes the server before it starts up.
    /// </summary>
    /// <remarks>
    /// This method is called before any startup processing occurs. The sub-class may update the 
    /// configuration object or do any other application specific startup tasks.
    /// </remarks>
    protected override void OnServerStarting(ApplicationConfiguration configuration)
    {
        Utils.Trace("The server is starting.");

        base.OnServerStarting(configuration);
        
        // it is up to the application to decide how to validate user identity tokens.
        // this function creates validator for X509 identity tokens.
        CreateUserIdentityValidators(configuration);
    }

    /// <summary>
    /// Called after the server has been started.
    /// </summary>
    protected override void OnServerStarted(IServerInternal server)
    {
        base.OnServerStarted(server);
        
        // request notifications when the user identity is changed. all valid users are accepted by default.
        server.SessionManager.ImpersonateUser += new ImpersonateEventHandler(SessionManager_ImpersonateUser);
    }
    
    /// <summary>
    /// Creates the node managers for the server.
    /// </summary>
    /// <remarks>
    /// This method allows the sub-class create any additional node managers which it uses. The SDK
    /// always creates a CoreNodeManager which handles the built-in nodes defined by the specification.
    /// Any additional NodeManagers are expected to handle application specific nodes.
    /// 
    /// Applications with small address spaces do not need to create their own NodeManagers and can add any
    /// application specific nodes to the CoreNodeManager. Applications should use custom NodeManagers when
    /// the structure of the address space is stored in another system or when the address space is too large
    /// to keep in memory.
    /// </remarks>
    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        Debug.WriteLine("Creating the Node Managers.");

        List<INodeManager> nodeManagers = new List<INodeManager>();

        // create the custom node managers.
        nodeManagers.Add(new ChatServerNodeManager(server, configuration));
        
        // create master node manager.
        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }

    /// <summary>
    /// Loads the non-configurable properties for the application.
    /// </summary>
    /// <remarks>
    /// These properties are exposed by the server but cannot be changed by administrators.
    /// </remarks>
    protected override ServerProperties LoadServerProperties()
    {
        var thisAssembly = GetType().Assembly;
        ServerProperties properties = new ServerProperties();

        properties.ManufacturerName = "cactuaroid";
        properties.ProductName      = "OpcUaChatServer";
        properties.ProductUri       = "https://github.com/cactuaroid/OpcUaChatServer";
        properties.SoftwareVersion  = thisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        properties.BuildNumber      = thisAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        properties.BuildDate        = File.GetLastWriteTimeUtc(thisAssembly.Location);

        // TBD - All applications have software certificates that need to added to the properties.

        // for (int ii = 0; ii < certificates.Count; ii++)
        // {
        //    properties.SoftwareCertificates.Add(certificates[ii]);
        // }

        return properties; 
    }
    #endregion
}
