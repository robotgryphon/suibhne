using System;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Api.Irc;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.Core {
  public class BotServerConnection {
	public IrcConnection Connection;

	public ServerConfig Configuration;

	public ExtensionRegistry Extensions;

	public Boolean Connected {
	  get { return Connection.Connected; }
	  protected set { }
	}

	#region Event Handlers

	public delegate void ServerConnectionEvent(BotServerConnection connection);

	public event ServerConnectionEvent OnConnectionComplete;


	public delegate void IrcCommandEvent(BotServerConnection connection,IrcMessage message);

	public event IrcCommandEvent OnCommandRecieved;

	#endregion

	public BotServerConnection(ServerConfig config, ExtensionRegistry exts) { 

	  this.Configuration = config;

	  this.Extensions = exts;

	  this.Connection = new IrcConnection(config.Server);

	  this.Connection.OnMessageRecieved += HandleMessageRecieved;
	  this.Connection.OnConnectionComplete += (conn) => {
		Console.WriteLine("Connection complete on server " + Configuration.Server.hostname);

		if (this.OnConnectionComplete != null) {
		  OnConnectionComplete(this);
		}

		foreach (IrcLocation location in Configuration.AutoJoinChannels) {
		  Connection.JoinChannel(location);
		}
	  };
	}

	public Boolean IsBotOperator(String user) {
	  foreach (String nick in Configuration.Operators)
		if (nick.ToLower() == user.ToLower())
		  return true;

	  return false;
	}

	protected void HandleCommand(IrcMessage message) {
	  if (this.OnCommandRecieved != null) {
		OnCommandRecieved(this, message);
	  }

	  String command = message.message.Split(new char[]{ ' ' })[0].ToLower().TrimStart(new char[]{ '!' }).TrimEnd();
	  Console.WriteLine("Command recieved from " + message.sender + ": " + command);

	  IrcMessage response = new IrcMessage(message.location, Connection.CurrentNickname, "Response");
	  response.type = MessageType.ChannelMessage;

	  switch (command) {
	  case "exts":

		string[] extCmdParts = message.message.Split(new char[]{ ' ' }, 3);
		switch (extCmdParts.Length) {
		case 1:
		  response.message = "Invalid Parameters. Format: !plugins [command]";
		  Connection.SendMessage(response);
		  break;

		case 2:
		  switch (extCmdParts[1].ToLower()) {
		  case "list":
			response.message = "Not yet reimplemented.";
			break;

		  default:
			response.message = "Unknown command.";
			break;
		  }

		  Connection.SendMessage(response);

		  break;
		}

		break;
			

	  case "raw":
		if (IsBotOperator(message.sender.ToLower())) {
		  string rawCommand = message.message.Split(new char[]{ ' ' }, 2)[1];
		  Connection.SendRaw(rawCommand);
		} else {
		  response.message = "You are not a bot operator. No permission to execute raw commands.";
		  Connection.SendMessage(response);
		}
		break;

	  default:
					// TODO: Check plugin registry for additional command support here?
		break;
	  }
	}

	public void Connect() {
	  this.Connection.Connect();
	}

	public void Disconnect() {
	  this.Connection.Disconnect();
	}

	protected void HandleMessageRecieved(IrcConnection conn, IrcMessage message) {
	  Console.WriteLine(message.ToString());

	  if (message.message.StartsWith("!"))
		HandleCommand(message);
	}
  }
}

