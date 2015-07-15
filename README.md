# Versioning and Changelog #
## Version 2.5
### Extension changes
* Extension communication system is now done through a single JSON object. Examples of the requests can be found in the `Extension Responses` file.
* Extension command handlers are now passed directly through the JSON object, under the key "handler". No more identifiers needed.
* Extensions are now installed to a database under the configuration root directory.

#### New event hooks
*User events*: Allows extensions to catch user-related events happening on networks. For this to work, you must include the `UserEventHandler` attribute on your main class, or, in the compiled install file, it's under handlers as `User`.

* **user.join**: User joining a location.
* **user.leave**: User leaving a location.
* **user.quit**: User quitting a network.
* **user.namechange**: User changing their display name on a network.

*Message events*

* **message.recieve**: Returning from an older build, this allows extensions to catch incoming messages on networks. For this to work, the attribute `MessageHandler` MUST be applied to the extension class on installation. In the compiled install file, this is signalled by the handler `Message:Recieve`.


### Interface Changes
* Interface is now done in WPF instead of WinForms. This should be a bit nicer to work with, but for now it's a basic console window.


## Version 2.4: The overhaul update
### Extension changes
* Extension command mapping changing to be loaded from file system instead of over sockets.
* Extension permanence. An extension will always use the same identifier once it's installed.
* Command mapping changes. Command handlers are added through attributes now, instead of manually coded in.

### User access levels
This system enables network and bot admins to specify permissions for command usage. It's on a basis of a byte- 0 is unauthorized, 1 is Basic, 100 is Authenticated, and 250+ is a bot administrator. Specific networks should set auth levels accordingly.

For example, the IRC connector sets Voiced users to 110, HalfOps to 120, Operators to 130, Admins to 140, and Channel Owners to 150. This leaves a small gap for specific groups.

### Updated configuration system
* Networks are all defined under `<config base>/Networks` now. 
* If there is a blank `disabled` file in the network root, that network will not automatically set itself up and will be skipped in startup.

### Network Connectors
Networks are now working through connectors. This lets the bot handle different IM and messaging types all under a single application. For example, one could create an IRC connector and a Twitch.tv connector and have them both running on a similar, shared core codebase.

This lets extension developers keep their code in a uniform manner, and release connector-specific extensions if necessary. The dice extension used as an example extension here is a good example of the code- it doesn't NEED to know how all the connectors send messages. It just knows to pass a message along- the connectors handle the rest.


## Version 2.2: The Extension Update
### Extension System
* Initial extension system.
* Extensions separated into a client-server system. 
* Extensions register themselves through sockets and response code bytes.
* Extensions use a routing system with GUIDs. All routing has an origin guid that determines where to send information back to.

## Version 2.1: The Logical Update
### Code Organization
Separating code into logical chunks, conforming to changes with Base API.

## Version 2.0: The Language Update
* Initial move to C#/NET language.
* Basic connection created.
* Everything ever hardcoded.
