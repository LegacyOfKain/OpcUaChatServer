﻿// this file is based on 
// https://github.com/OPCFoundation/UA-.NETStandard/blob/be65403945d4f0bf366f3299222fcb6cb08d6cb5/SampleApplications/Samples/NetCoreConsoleServer/Program.cs

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
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using OpcUaChatServer.Server;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace OpcUaChatServer;
public class ServerLauncher
{
    ChatServer server;
    Task status;
    DateTime lastEventTime;
    int serverRunTime = Timeout.Infinite;
    static bool autoAccept = false;
    static ExitCode exitCode;

    public ServerLauncher(bool _autoAccept, int _stopTimeout)
    {
        MefManager.Container.ComposeParts(this);

        autoAccept = _autoAccept;
        this.serverRunTime = _stopTimeout == 0 ? Timeout.Infinite : _stopTimeout * 1000;
    }

    public void Run()
    {
        try
        {
            exitCode = ExitCode.ErrorServerNotStarted;
            this.ConsoleSampleServer().Wait();
            Console.WriteLine("Server started. Press Ctrl-C to exit...");
            exitCode = ExitCode.ErrorServerRunning;
        }
        catch (Exception ex)
        {
            Utils.Trace("ServiceResultException:" + ex.Message);
            Console.WriteLine("Exception: {0}", ex.Message);
            exitCode = ExitCode.ErrorServerException;
            return;
        }

        var quitEvent = new ManualResetEvent(false);
        try
        {
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                quitEvent.Set();
                eArgs.Cancel = true;
            };
        }
        catch
        {
        }

        // wait for timeout or Ctrl-C
        quitEvent.WaitOne(this.serverRunTime);

        if (this.server != null)
        {
            Console.WriteLine("Server stopped. Waiting for exit...");

            using (var _server = this.server)
            {
                // Stop status thread
                this.server = null;
                this.status.Wait();
                // Stop server and dispose
                _server.Stop();
            }
        }

        exitCode = ExitCode.Ok;
    }

    public static ExitCode ExitCode { get => exitCode; }

    private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
    {
        if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
        {
            e.Accept = autoAccept;
            if (autoAccept)
            {
                Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            }
            else
            {
                Console.WriteLine("Rejected Certificate: {0}", e.Certificate.Subject);
            }
        }
    }

    private async Task ConsoleSampleServer()
    {
        var application = new ApplicationInstance();

        application.ApplicationName = "OpcUaChatServer";
        application.ApplicationType = ApplicationType.Server;
        application.ConfigSectionName = "OpcUaChatServer";

        // load the application configuration.
        var config = await application.LoadApplicationConfiguration(false);

        // https://github.com/OPCFoundation/UA-ModelCompiler/issues/109
        // add the encodable types defined in the shared information model library.
        application.ApplicationConfiguration.CreateMessageContext().Factory.AddEncodeableTypes(typeof(Namespaces).Assembly);

        // check the application certificate and create if not available.
        var haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0, ushort.MaxValue); // almost unlimited
        if (!haveAppCertificate)
        {
            throw new Exception("Application instance certificate invalid!");
        }

        if (!config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
        {
            config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
        }

        // start the server.
        this.server = new ChatServer();
        await application.Start(this.server);

        // start the status thread
        this.status = Task.Run(new Action(this.StatusThread));

        // print notification on session events
        this.server.CurrentInstance.SessionManager.SessionActivated += this.EventStatus;
        this.server.CurrentInstance.SessionManager.SessionClosing += this.EventStatus;
        this.server.CurrentInstance.SessionManager.SessionCreated += this.EventStatus;

    }

    private void EventStatus(Session session, SessionEventReason reason)
    {
        this.lastEventTime = DateTime.UtcNow;
        this.PrintSessionStatus(session, reason.ToString());
    }

    void PrintSessionStatus(Session session, string reason, bool lastContact = false)
    {
        lock (session.DiagnosticsLock)
        {
            var item = String.Format("{0,9}:{1,20}:", reason, session.SessionDiagnostics.SessionName);
            if (lastContact)
            {
                item += String.Format("Last Event:{0:HH:mm:ss}", session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
            }
            else
            {
                if (session.Identity != null)
                {
                    item += String.Format(":{0,20}", session.Identity.DisplayName);
                }
                item += String.Format(":{0}", session.Id);
            }
            Console.WriteLine(item);
        }
    }

    private async void StatusThread()
    {
        while (this.server != null)
        {
            if (DateTime.UtcNow - this.lastEventTime > TimeSpan.FromMilliseconds(6000))
            {
                var sessions = this.server.CurrentInstance.SessionManager.GetSessions();
                for (var ii = 0; ii < sessions.Count; ii++)
                {
                    var session = sessions[ii];
                    this.PrintSessionStatus(session, "-Status-", true);
                }
                this.lastEventTime = DateTime.UtcNow;
            }
            await Task.Delay(1000);
        }
    }
}
