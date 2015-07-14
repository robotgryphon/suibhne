### Message
{
	"**responseCode**": "message.recieve",
	"**contents**": "Message Contents.",
    "**location**": {
        "*id*": "<guid>",
        "*type*": "public/private/user"
    },
	"**sender**": {
		"*DisplayName*": "Delenas", 
		"*Username*": "delenas" 
	}
}

### UserEvent - user.join, user.leave, user.quit, user.namechange
{
    "**responseCode**": "user.join",
    "**location**": {
        "id": "<guid>",
        "type": "public"
    },
	"**user**": {
		"DisplayName": "Delenas",
		"Username": "delenas"
	}
}

### Command
{
	"**responseCode**": "command.recieve",
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
    "**id**": "<guid>"
}
