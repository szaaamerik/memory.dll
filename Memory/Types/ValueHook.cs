namespace Memory;

public class ValueHook
{
    void b()
    {
        byte[] MOV_EDX_EAX = {0xc3, 0x2 | 0xc0};
    }
}