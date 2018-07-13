﻿using Mixer.Base.Util;
using MixItUp.Base.Commands;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    public enum CommandActionTypeEnum
    {
        [Name("Run Command")]
        RunCommand,
        [Name("Enable/Disable Command")]
        EnableDisableCommand,
    }

    [DataContract]
    public class CommandAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return CommandAction.asyncSemaphore; } }

        [DataMember]
        public CommandActionTypeEnum CommandActionType { get; set; }

        [DataMember]
        public Guid CommandID { get; set; }

        [DataMember]
        public string CommandArguments { get; set; }

        public CommandAction() : base(ActionTypeEnum.Command) { }

        public CommandAction(CommandActionTypeEnum commandActionType, CommandBase command, string commandArguments)
            : this()
        {
            this.CommandActionType = commandActionType;
            this.CommandID = command.ID;
            this.CommandArguments = commandArguments;
        }

        protected override async Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            if (this.CommandActionType == CommandActionTypeEnum.RunCommand)
            {
                CommandBase command = ChannelSession.AllEnabledCommands.FirstOrDefault(c => c.ID.Equals(this.CommandID));
                if (command != null)
                {
                    IEnumerable<string> newArguments = null;
                    if (!string.IsNullOrEmpty(this.CommandArguments))
                    {
                        string processedMessage = await this.ReplaceStringWithSpecialModifiers(this.CommandArguments, user, arguments);
                        newArguments = processedMessage.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    await command.Perform(user, newArguments, this.GetExtraSpecialIdentifiers());
                }
            }
            else if (this.CommandActionType == CommandActionTypeEnum.EnableDisableCommand)
            {
                CommandBase command = ChannelSession.AllCommands.FirstOrDefault(c => c.ID.Equals(this.CommandID));
                if (command != null)
                {
                    command.IsEnabled = !command.IsEnabled;
                }
            }
        }
    }
}