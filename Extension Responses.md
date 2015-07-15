### Message
{
	"**responseCode**": "message.recieve",
	"**contents**": "Message Contents.",
	"**location**": {
		"*id*": "<guid>",
		"*type*": "(public|private)_(message|action|notice)"
	},
	"**sender**": {
		"*DisplayName*": "Delenas", 
		"*Username*": "delenas" 
	}
}

### UserEvent - user.join, user.leave, user.quit, user.namechange
user.namechange has an additional param under user: "LastDisplayName".

{
	"**responseCode**": "user.join",
	"**location**": "<guid>",
	"**user**": {
		"DisplayName": "Delenas",
		"Username": "delenas"
	}
}

### Command
{
	"**responseCode**": "command.recieve",
	"**handler**": "commandHandler",
	"**arguments**": "command arguments",
	"**location**": {
		"*id*": "<guid>",
		"*type*": "public"
	},
	"**sender**": {
		"*DisplayName*": "Delenas",
		"*Username*": "delenas"
	}
}

### InformationRequest - info.request, info.response
{
	"**responseCode**": "info.request",
	"**requestType**": "location.id",
	"**params**": {
		"*networkName*": "<string>",
		"*locationName*": "<string>"
	}
}

### ExtensionEvent - extension.activate, extension.shutdown
{
	"**responseCode**": "extension.activate",
	"**extid**": "<guid>"
}
