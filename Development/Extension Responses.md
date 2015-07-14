### Message
{
	"*responseCode*": "message.recieve",
	"*contents*": "Message Contents.",
    "*location*": {
        "*id*": "<guid>",
        "*type*": "public",                   // Can also be "private" or "user"
        "*is_action*": false,                 // OPTIONAL: If true, will send message as an action
        "*target*": "Delenas"                 // OPTIONAL: If set and type is user will send message to this DisplayName
    },
	"*sender*": {
		"*DisplayName*": "Delenas", 
		"*Username*": "delenas" 
	}
}

### UserEvent - user.join, user.leave, user.quit, user.namechange
{
	"responseCode": "user.join",
	"location": {
        "id": "<guid>",
        "type": "public"
    },
	"user": {
		"DisplayName": "Delenas",
		"Username": "delenas"
	}
}

### Command
{
	"*responseCode*": "command.recieve",
	"*network*": "<guid>",
	"*arguments*": "command arguments",
    "*location*": {
        "*id*": "<guid>",
        "*type*": "public"
    },
    "*sender*": {
        "*DisplayName*": "Delenas",
        "*Username*": "delenas"
    }
}

### InformationRequest - info.request, info.response
{
    "*responseCode*": "info.request",
    "*requestType*": "location.id",
    "*params*": {
        "*networkName*": "<string>",
        "*locationName*": "<string>"
    }
}

