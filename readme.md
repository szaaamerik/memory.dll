## Description
Andy's modified version of the original [memory.dll](https://github.com/erfg12/memory.dll), hopefully delivering a faster, and better-practiced library for process memory manipulation.


The key changes and features of this fork include:

* Enhanced memory read/write performance
* Introduction of new data types like External<T>, Instruction, and Detour to simplify your programming tasks
* Expansion of "code cave" (detour) options, such as 5-byte jmp near, 14-byte jmp far, and 16-byte call far
* Programmed in .NET 7


## TODO List
To further improve this library, the following things are on the TODO list:

* Convert nuint to nint
* Fix potential issues with 32-bit support
* Restore .ini file support
* Add the capability to use parameter arrays (varargs) instead of string pointers, e.g., "base+10,20,34,5C"
* Miscellaneous improvements

## Overview
Create powerful PC game cheat trainers in C# with ease using this library.

Eventually, this library will be available on NuGet. 

Comes equipped with XML IntelliSense documentation. 

Expect new build releases with every commit when set up.

## Platform Support
* For legacy Windows operating systems, check out [memory_legacy.dll](https://github.com/erfg12/memory_legacy.dll).
* For MacOS operating systems, check out [memory.dylib](https://github.com/erfg12/memory.dylib).
* For 32-bit operating systems, consider the original [memory.dll](https://github.com/erfg12/memory.dll).


## Key Features
* Check if a process is running, using either its ID or name, and open it in a single function call.
* AoB (Array of Bytes) and Signature scanning with full and partial masking.
* Inject DLLs and create named pipes for communication.
* Writing to addresses with a wide range of value types, including byte, 2 bytes, bytes, float, int, string, double, and long.
* Flexible address structures that allow the use of modules, offsets, and pointers, such as `"game.exe+0x12345678,0x12,0x34,0x56"`.
* Freeze values with infinite loop writing threads.
* Bind memory addresses to UI elements and more.
