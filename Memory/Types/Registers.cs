// ReSharper disable InconsistentNaming
namespace Memory;

/// <summary>
/// Bytes used to construct registers by performing bitwise OR operations.
/// _OP(1, 2, 3) is the operand of the register.
/// Example usages:
/// byte[] mov_edx_eax = {Instructions.MOV, Registers.xdx_op1 | Registers.xax_op2};
/// byte[] mov_edx_eax = ConstructInstructionBytes(Instructions.MOV, Registers.xdx_op1, Registers.xax_op2);
/// </summary>
public enum Registers
{
    xax_op2 = 0b000,
    xcx_op2 = 0b001,
    xdx_op2 = 0b010,
    xbx_op2 = 0b011,
    xsp_op2 = 0b100,
    xbp_op2 = 0b101,
    xsi_op2 = 0b110,
    xdi_op2 = 0b111,
    
    xax_op1 = 0b000,
    xcx_op1 = xcx_op2 << 3,
    xdx_op1 = xdx_op2 << 3,
    xbx_op1 = xbx_op2 << 3,
    xsp_op1 = xsp_op2 << 3,
    xbp_op1 = xbp_op2 << 3,
    xsi_op1 = xsi_op2 << 3,
    xdi_op1 = xdi_op2 << 3,
    
    xmm0_op2 = 0b000,
    xmm1_op2 = 0b001,
    xmm2_op2 = 0b010,
    xmm3_op2 = 0b011,
    xmm4_op2 = 0b100,
    xmm5_op2 = 0b101,
    xmm6_op2 = 0b110,
    xmm7_op2 = 0b111,
}