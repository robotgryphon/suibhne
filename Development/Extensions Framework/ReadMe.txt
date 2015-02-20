# ABOUT THE EXTENSIONS SYSTEM
***

## Permissions System

The permissions system is used when the bot first sends out registration messages to the extension applications.

It is handled by a request in the format `<token> 1`. Respond to this request with the following format:

> 00000010 <TOKEN> <EXTENSION_RUNTYPE> <PERMISSIONS_BYTE> <EXTENSION_NAME>

So, written out, that may be: `00000010 0000-0000-00000001 1 11110000 Example Extension`


#### Extension Byte:

This tells the bot whether to expect the extension to keep running after its first call, or whether it only runs once.

This is useful for extensions like Nickserv that only need to send a response once. If the extension only runs once, the byte is 0. All other extensions
should return 1.

#### The Permissions Byte:

This is used by the bot to put the extension into proper categories.

- The first bit is whether or not the extension handles connections. Activate this bit if you need to listen in on connection events such as finishing a connection, disconnecting, pings, and reconnecting.
- The second bit is used for message input and output. Enable this bit if you need to intercept recieved messages.
- The third bit is used for user watching. Enable this bit if you listen in on user joins, parts, nick changes, or quits.
- The fourth bit is used for location watching. Use this to watch for channel joins, queries, parts, etc.
- The final four bits are currently unused at the moment, and completely ignored. Pad the byte with four zeroes.

***

## The Communication Code System

Like how IRC has a numeric code system for responses, Suibhne uses a similar system for returning data to extensions.
The following is a numeric list of responses expected by the extensions system. This is also available in the enum `ResponseCodes`
under `Extension`, where specific details are listed.

#### Client-side

Numeric Code | Parameters
-----------------------------------
1  | 1 token runtype permissions <name>
10 | 10 token connid
11-14 | No client response.

#### Server-side

Numeric Code | Parameters
-------------------------
1 | 1 token
10 | 10 token connid status <name>
11-14 | 11 token connid