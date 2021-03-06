﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JsonRpc.Standard.Contracts;
using LanguageServer.VsCode.Contracts;
using LanguageServer.VsCode.Contracts.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MwLanguageServer.Services
{

    [JsonRpcScope(MethodPrefix = "workspace/")]
    public class WorkspaceService : LanguageServiceBase
    {
        private readonly ClientProxy client;

        public WorkspaceService(ClientProxy client)
        {
            this.client = client;
        }

        [JsonRpcMethod(IsNotification = true)]
        public void DidChangeConfiguration(SettingsRoot settings)
        {
            Session.Settings = settings.WikitextLanguageServer;
            foreach (var doc in Session.DocumentStates.Values)
            {
                doc.RequestAnalysis();
            }
        }

        [JsonRpcMethod(IsNotification = true)]
        public async Task DidChangeWatchedFiles(ICollection<FileEvent> changes)
        {
            foreach (var change in changes)
            {
                if (!change.Uri.IsFile) continue;
                var localPath = change.Uri.AbsolutePath;
                if (string.Equals(Path.GetExtension(localPath), ".mediawiki"))
                {
                    // If the file has been removed, we will clear the lint result about it.
                    // Note that pass null to PublishDiagnostics may mess up the client.
                    if (change.Type == FileChangeType.Deleted)
                    {
                        await client.Document.PublishDiagnostics(change.Uri, Diagnostic.EmptyDiagnostics);
                    }
                }
            }
        }

        [JsonRpcMethod]
        public async Task ExecuteCommand(string command, JToken arguments)
        {
            switch (command)
            {
                case ServerCommands.DumpPageInfoStore:
                    await client.Window.LogMessage(MessageType.Info,
                        JsonConvert.SerializeObject(Session.PageInfoStore.Dump()));
                    break;
            }
        }
    }
}
