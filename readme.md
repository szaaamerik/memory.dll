## DESCRIPTION
Andy's fork of [memory.dll](https://github.com/erfg12/memory.dll) with (hopefully) faster code, more features, and better practices.
Some changes from the original:
   * Pretty sure I broke 32-bit support (might have to fix that eventually)
   * Use generics instead of a string to write most types
      - means you can write more types!
   * Improved read/write performance
   * Fun types like `External<T>`, `Instruction`, and `Detour` to make your life easier
   * More "code cave" (detour) types (5 byte jmp near, 14 byte jmp far, and 16 byte call far)
   * Programmed in .NET 7
TODO:
   * Improve AOB scan performance
   * Convert nuint to nint because I was not thinking straight
   * Improve `Read/WriteAnyMemory<T>()` by [bridging generic constraints](https://github.com/dotnet/csharplang/discussions/6308) (when that eventually becomes possible)
   * Pretty sure I broke 32-bit support (might have to fix that eventually)
   * Add .ini support back
   
Create great PC game cheat trainers in C# with this easy to use library! This library will be available on NuGet at some point, includes XML IntelliSense docs and this code repo will eventually provide new build releases every commit when I feel like setting that up. For support please check the [wiki tab](https://github.com/NoSkillPureAndy/memory.andy.dll/wiki) in this repo.

- For legacy Windows operating systems, check out [memory_legacy.dll](https://github.com/erfg12/memory_legacy.dll)

- For MacOS operating systems, check out [memory.dylib](https://github.com/erfg12/memory.dylib)

- For 32-bit operating systems, check out [the original memory.dll](https://github.com/erfg12/memory.dll)

## FEATURES
* Check if process is running (ID or name) and open, all in 1 function.
* 32bit and 64bit games supported.
* AoB scanning with full & partial masking.
    * _Example: "?? ?? ?? ?5 ?? ?? 5? 00 ?? A9 C3 3B ?? 00 50 00"_
* Inject DLLs and create named pipes to communicate with them.
    * See [this wiki article](https://github.com/erfg12/memory.dll/wiki/Using-Named-Pipes) for more info.
* Write to addresses with many different value types.
    * _Example: byte, 2bytes, bytes, float, int, string, double or long_
* you cannot use an Optional external .ini file for code storage.
* Address structures are flexible. Can use modules, offsets and/or pointers. 
    * _Example: "game.exe+0x12345678,0x12,0x34,0x56"_
* Freeze values (infinte loop writing in threads)
* Bind memory addresses to UI elements
* more

## DOCUMENTATION
[Wiki Pages](https://github.com/NoSkillPureAndy/memory.andy.dll/wiki)
