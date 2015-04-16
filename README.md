# Versioning and Changelog #

## Version 2.0: The Language Update ##
* Initial move to C#/NET language.
* Basic connection created.
* Everything ever hardcoded.

## Version 2.1: The Logical Update ##
* Separating code into logical chunks, conforming to changes with Raindrop API

## Version 2.2: The Extension Update ##
### Version 2.2.1 ###
* Initial extension system.
* Based on DLL assembly loading. Extensions hooked directly into the connection events during runtime.

### Version 2.2.2 ###
* Extensions now separated into a client-server system. 
* Extensions register EVERYTHING through sockets and response code bytes.

### Version 2.2.3 ###
* Extensions changed to create a routing system with GUIDs. All routing has an origin and destination guid.

### Version 2.2.4: Current Revision ###
* Extension command mapping changing to be loaded from file system instead of over sockets.
* Extension permanence. An extension will always use the same identifier once it's installed.
* Command mapping changes. Command handlers are added through attributes now, instead of manually coded in.

