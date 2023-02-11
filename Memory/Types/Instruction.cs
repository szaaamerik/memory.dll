using System.Linq;

namespace Memory.Types;

public class Instruction : MemoryObject
{
    private readonly byte[] _originalBytes, _newBytes, _nopBytes, _retBytes;
    private readonly bool _toggleWithRet;
    private bool _hasBeenPatchedBefore;
    private readonly string _signature;

    public bool IsPatched =>
        !M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length)
            .SequenceEqual(_originalBytes);

    public Instruction(string address, string offsets, byte[] ogBytes, byte[] newBytes = null, bool toggleWithRet = false, string sig = "", Mem m = null)
        : base(address, offsets, m)
    {
        _originalBytes = ogBytes;
        _newBytes = newBytes;
        _toggleWithRet = toggleWithRet;
        _signature = sig;
        

        _retBytes = new byte[_originalBytes.Length];
        _retBytes[0] = 0xC3;
        for (int i = 1; i < _retBytes.Length; i++)
            _retBytes[i] = 0x90;

        _nopBytes = new byte[_originalBytes.Length];
        for (int i = 0; i < _nopBytes.Length; i++)
            _nopBytes[i] = 0x90;
    }

    public void Nop()
    {
        M.WriteArrayMemory(AddressPtr, _nopBytes);
        _hasBeenPatchedBefore = true;
    }

    public void Restore() => M.WriteArrayMemory(AddressPtr, _originalBytes);

    public void Patch() => M.WriteArrayMemory(AddressPtr, _newBytes);

    public void Return() => M.WriteArrayMemory(AddressPtr, _retBytes);

    public void Toggle()
    {
        byte[] currentBytes = M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length);

        switch (true)
        {
            case true when currentBytes.SequenceEqual(_nopBytes) || currentBytes.SequenceEqual(_newBytes) || currentBytes.SequenceEqual(_retBytes):
                Restore();
                break;
            case true when _newBytes != null:
                Patch();
                break;
            case true when _toggleWithRet:
                Return();
                break;
            default:
                Nop();
                break;
        }
    }
    
    public bool AreBytesAtAddressCorrect()
    {
        byte[] currentBytes = M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length);
        return currentBytes.SequenceEqual(_originalBytes);
        //TODO: Read from exe if it's been patched
    }
}