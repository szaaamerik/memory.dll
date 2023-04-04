using System.Linq;

namespace Memory.Types;

public class Instruction : MemoryObject
{
    private readonly byte[] _originalBytes, _realOriginalBytes, _newBytes, _nopBytes, _retBytes;
    private readonly bool _toggleWithRet;
    private readonly string _signature;
    private readonly int _signatureOffset;
    private nuint _signatureAddress;

    public bool IsPatched =>
        !M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length)
            .SequenceEqual(_originalBytes);

    public Instruction(string address, string offsets, byte[] originalBytes, byte[] newBytes = null,
        bool toggleWithRet = false, string signature = "", int signatureOffset = 0, Mem m = null)
        : base(address, offsets, m)
    {
        _originalBytes = originalBytes;
        _newBytes = newBytes;
        _toggleWithRet = toggleWithRet;
        _signature = signature;
        _signatureOffset = signatureOffset;
        _realOriginalBytes = M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length);


        _retBytes = new byte[_originalBytes.Length];
        _retBytes[0] = 0xC3;
        for (int i = 1; i < _retBytes.Length; i++)
            _retBytes[i] = 0x90;

        _nopBytes = new byte[_originalBytes.Length];
        for (int i = 0; i < _nopBytes.Length; i++)
            _nopBytes[i] = 0x90;

        if (BytesAtAddressAreCorrect || signature == "") return;

        _signatureAddress = M.ScanForSig(_signature).FirstOrDefault();
        if (signatureOffset > 0)
            _signatureAddress += (uint) signatureOffset;
        else
            _signatureAddress -= (uint) -signatureOffset; //i think this is necessary because it's unsigned, but i'm not sure and too lazy to test
        
        UpdateAddressUsingSignature();
    }

    public void Nop() => M.WriteArrayMemory(AddressPtr, _nopBytes);

    public void Restore() => M.WriteArrayMemory(AddressPtr, _originalBytes);

    public void Patch() => M.WriteArrayMemory(AddressPtr, _newBytes);

    public void Return() => M.WriteArrayMemory(AddressPtr, _retBytes);

    public void Toggle()
    {
        byte[] currentBytes = M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length);

        switch (true)
        {
            case true when currentBytes.SequenceEqual(_nopBytes) || currentBytes.SequenceEqual(_newBytes) ||
                           currentBytes.SequenceEqual(_retBytes):
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

    public bool BytesAtAddressAreCorrect => _originalBytes.SequenceEqual(_realOriginalBytes);

    public void UpdateAddressUsingSignature()
    {
        _signatureAddress = M.ScanForSig(_signature, resultLimit: 1).FirstOrDefault();
        if (_signatureOffset > 0)
            _signatureAddress += (uint) _signatureOffset;
        else
            _signatureAddress -= (uint) _signatureOffset; //i think this is necessary because it's unsigned, but i'm not sure and too lazy to test

        AddressPtr = _signatureAddress;
    }
}