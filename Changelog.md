# Suibhne Updates and Changelog

## Version 2.4 (Latest)
#### Added concept of user access levels.
This system enables network and bot admins to specify permissions for command usage. It's on a basis of a byte- 0 is unauthorized, 1 is Basic, 100 is Authenticated, and 250+ is a bot administrator. Specific networks should set auth levels accordingly.

For example, the IRC connector sets Voiced users to 110, HalfOps to 120, Operators to 130, Admins to 140, and Channel Owners to 150. This leaves a small gap for specific groups.

#### Updated configuration system
* Networks are all defined under `<config base>/Networks` now. 
* If there is a blank `disabled` file in the network root, that network will not automatically set itself up and will be skipped in startup.

#### Network Connectors
Networks are now working through connectors. This lets the bot handle different IM and messaging types all under a single application. For example, one could create an IRC connector and a Twitch.tv connector and have them both running on a similar, shared core codebase.

This lets extension developers keep their code in a uniform manner, and release connector-specific extensions if necessary. The dice extension used as an example extension here is a good example of the code- it doesn't NEED to know how all the connectors send messages. It just knows to pass a message along- the connectors handle the rest.
