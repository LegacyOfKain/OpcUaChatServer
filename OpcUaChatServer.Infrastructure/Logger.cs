using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.CompilerServices;

namespace OpcUaChatServer;
[Export]
public class Logger
{
    private readonly List<string> m_logs = new List<string>();

    public event Action<string> Logged;

    public void Info(object obj, [CallerFilePath] string filePath = "", [CallerMemberName] string name = "")
    {
        var log = $"{DateTime.Now}: {Path.GetFileName(filePath)} - {name}: {obj}";

        this.m_logs.Add(log);
        Logged?.Invoke(log);
    }
}
