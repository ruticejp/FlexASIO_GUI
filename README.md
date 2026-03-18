⚠️ **This repository is a fork/continuation of the original project by `dechamps` (https://github.com/dechamps/FlexASIO_GUI).**

This fork is maintained by `rutice` (https://github.com/ruticejp) and includes ongoing improvements such as .NET 10/11 support, installer updates, and UTF‑8 handling fixes. The original author is credited and respected; this is an independent continuation in the absence of an accepted PR upstream.

This is a small GUI to make the configuration of https://github.com/dechamps/FlexASIO a bit quicker.

It should pick up your existing $Usersprofile/FlexASIO.toml file and read the basic parameters. Not all of them have been implemented yet...

To run, please make sure you have [.NET Desktop Runtime 10.x (or higher)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.

v0.36 adds a registry key with the install path to HKEY_LOCAL_MACHINE\SOFTWARE\Fabrikat\FlexASIOGUI\Install\Installpath

It also makes most settings optional so that default settings are not overwritten.

![image](https://user-images.githubusercontent.com/6930367/118895016-a4746a80-b905-11eb-806c-7fd3fee4fcd1.png)
