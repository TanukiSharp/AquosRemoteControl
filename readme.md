# Overview

AQUOS TVs by Sharp offer a fairly simple protocol to communicate with (and thus control) TVs over a TCP connection.

Models of TV seems to differ quite a lot whether they are sold in Japan or outside of Japan.
All experiments are did were done with a [LC-45US40](http://www.sharp.co.jp/aquos/products/lc45us40_spec.html "LC-45US40") sold in Japan. (note that US in the reference is not related to the US country at all)

I couldn't find the equivalent sold outside of Japan.

# Implementation

The commands part is documented by Sharp, so there were really no problem about that, but I still faced two problems beside that.

To solve them, this implementation has been performed entirely the 'trial and error' way. I prefer this naming over 'reverse engineering'.

## Problem 1: Login

The way to login to TV is not documented at all (or at least I couldn't find any) so hereafter is the result of my findings.

If you didn't set user and password on your TV, then there is no need for login.
That means setting user and password fields to empty on your TV since there is such 'disable login' option.
In this case, you can start to send commands to TV right away after connection has been established.

If any of the user and password is set (at least one character) then it is necessary to perform a login.
Performing a login means you have to `read` the socket just after connection has been established.
That also means if you intend to `read` whereas you didn't set user and password on your TV, then the `read` will hang forever, since you are waiting for TV to send something, and TV is waiting for you to send something. Unless you make your read to timeout (which can be a pain), you are going to experience a great 'poker face' moment.

There is no way (or at least I couldn't find any) to query the TV whether login is necessary or not. So what you have to do is that your TV and your control client are configured the same way.

## Problem 2: Bug?

When the TV responds, it is supposed to send `OK\r` or `ERR\r`, but instead some versions of the firmware had a bug and just sent `OK` or `ERR`, and the `\r` came at the beginning of the next reponse.

This made it very hard to properly split commands using the `\r` character, since TCP is a stream-based protocol, we may receive several commands at once, or a not-yet-complete command. For example, it could be possible to receive `ER` in a `read`, and `R\r` in the next `read`. This has been fixed for my TV, but still this bug remains for the login phase.

The login first sends `Login:` on `read`, then the controller have to send `<username>\r`, and the TV responds `\r\nPassword:`, the controller have to send `<password>\r` and the TV responds `\r\n` and `OK\r` in two distinct chunks.

I'm not sure if the `\n` part is normal because documentation never mentioned anything else than `\r` (well login is not documented though), neither why the order or 'words' and '\r' separators are messed up for login.

# Commands

Hereafter is an extract of the commands page of a documentation I could find in English. (Japanese documentations I found are far less descriptive)

[AQUOS TV Commands](documents/aquos_commands.pdf "AQUOS TV Commands")

# Sample console app

All parameters you need to change in order to test it are located in the `TestConsoleApp` project (make sure it is set as startup project), in the `Program.cs` file.

Replace the `Address` and `Port` constants by the one matching your TV network settings.
If you want to disable the login, simply set both `Username` and `Password` to `null` or to an empty string.

When you run it, what it does is simply to login, turn the TV on, toggle mute 10 times (once every second) and then finally turn the TV off.

If you start the program while your TV is off, since TVs usually take quite a long time to warm up, you may see only a few mute toggles, and then see it turn off.

# Build and run

In order to build this solution, you need .NET Core SDK 1.0.1 or higher.

You can find SDKs at https://www.microsoft.com/net/download/core#/sdk

Go to the folder with the solution and run:

```
dotnet restore
dotnet build
```

Then to run it:

```
cd TestConsoleApp
dotnet run
```

This has been tested on Windows 10 and Linux CentOS 7.
