# Suibhne Common Library

### The Network Platform
Suibhne uses a set of core files to create network connectors. Using them, you can create extensions that can work on much broader scope than a single service, such as IRC or Facebook Chat.

If you create a parrot extension, for example, that just repeats back what's said at it, the extension system can automatically apply that to EVERY service that handles messages. *Code for one, code for all* is our goal here.

### Multi-Language Support
This will need to be addressed more. The way extensions are designed, any language that supports sockets and byte management should be able to have extensions written in them. The two I can think of right off the top of my head are Java and anything in the .NET platform (Visual Basic, C#, etc).

.NET has first-priority support, since the application is written in it. Faster time to support the base network and all that.

### Extension Installation
For extensions to be installed, they must listen in for an argument flag in the entry point: `--install <base dir>`, where `<base dir>` is the configuration root directory. It needs to pass the information off to the Extension Installer, located in the Suibhne Common lib. That installer will pick up what it needs from there.