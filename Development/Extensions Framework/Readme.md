# ABOUT THE EXTENSIONS SYSTEM

## Permissions System

The permissions system is used when the bot first sends out registration messages to the extension applications.
It is handled by a request in the format <token> 1. Respond to this request with the following format:

> <TOKEN> <EXTENSION_RUNTYPE> <PERMISSIONS_BYTE> <EXTENSION_NAME>

So, written out, that may be: "0000-0000-00000001 1 11110000 Example Extension"


### The extension type byte:

This tells the bot whether to expect the extension to keep running after its first call, or whether it only runs once.
This is useful for extensions like Nickserv that only need to send a response once.


## About the permissions byte:

This is used by the bot to put the extension into proper categories.

- The first bit is whether or not the extension handles connections. Activate this bit if you need to listen in on connection events such as finishing a connection, disconnecting, pings, and reconnecting.
- The second bit is used for message input and output. Enable this bit if you need to intercept recieved messages.
- The third bit is used for user watching. Enable this bit if you listen in on user joins, parts, nick changes, or quits.
- The fourth bit is used for location watching. Use this to watch for channel joins, queries, parts, etc.
- The final four bits are currently unused at the moment, and completely ignored.