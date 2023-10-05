namespace OpenMud.Mudpiler.RuntimeEnvironment.Operators;

public enum DmlBinary
{
    LogicalOr,
    LogicalAnd,

    Addition, //A + B	A.operator+(B)	
    Subtraction, //A - B	A.operator-(B)	
    Multiplication, //A * B	A.operator*(B)	
    Division, //A / B	A.operator/(B)	
    Modulo, //A % B	A.operator%(B)	
    IntModulo, //A %% B 515	A.operator%%(B)	
    Power, //A ** B	A.operator**(B)	
    BitwiseOr, //A | B	A.operator|(B)	
    BitwiseAnd, //A & B	A.operator&(B)	
    BitwiseXor, //A ^ B	A.operator^(B)	
    BitShiftLeft, //A << B (shift)	A.operator<<(B)	
    BitShiftRight, //A >> B (shift)	A.operator>>(B)	

    //Comparisons (return true or false)
    Equals, //==
    NotEqual, //==
    Equivalent, //A ~= B	A.operator~=(B)	
    NotEquivalent, //A ~! B	A.operator~!(B)	
    LessThan, //A < B	A.operator<(B)	
    GreaterThanEq, //A >= B	A.operator>=(B)	
    GreaterThan, //A > B	A.operator>(B)	
    LessThanEq, //A <= B	A.operator<=(B)	

    //List access
    ArrayIndex, //A[idx]	A.operator[](idx)	Used for reading a list value

    //Other
    Turn, //turn(A, B) 514	A.operator_turn(B)	

    In
}

public enum DmlUnary
{
    Logical,
    Negate, //-A	A.operator-()	Same proc as subtraction, but has no arguments
    BitwiseInvert, //~A	A.operator~()
    AsString, //"[A]" 515	A.operator""()	Specifies a custom way for converting A to text (see notes below)
    Not //"[A]" 515	A.operator""()	Specifies a custom way for converting A to text (see notes below)
}

public enum DmlBinaryAssignment
{
    //Assignments with side effects (return value defaults to src)
    AugAddition, //A += B	A.operator+=(B)	
    AugSubtraction, //A -= B	A.operator--(B)	
    AugMultiplication, //A *= B	A.operator*=(B)	
    AugDivision, //A /= B	A.operator/=(B)	
    AugModulo, //A %= B	A.operator%=(B)	
    AugIntModulo, //A %%= B 515	A.operator%%=(B)	
    AugBitwiseOr, //A |= B	A.operator|=(B)	
    AugBitewiseAnd, //A &= B	A.operator&=(B)	
    AugBitwiseXor, //A ^= B	A.operator^=(B)	
    AugBitShiftLeft, //A <<= B	A.operator<<=(B)	
    AugBitShiftRight, //A >>= B	A.operator>>=(B),


    Assignment, //A = B
    CopyInto //A := B	A.operator:=(B)	
}

public enum DmlUnaryAssignment
{
    PreIncrement, //++A	A.operator++()	
    PreDecrement, //--A	A.operator--()	
    PostDecrement, //A--	A.operator--()	
    PostIncrement //A++	A.operator++(1)	
}

public enum DmlTernery
{
    //List access
    ArrayEmplace, //A[idx] = B	A.operator[]=(idx, B)	Used for writing a list value; ignores return value
    ArrayEmplaceCopyInto
}

public static class DmlOperation
{
    private static readonly Dictionary<string, DmlUnary> UnArith = new()
    {
        { "-", DmlUnary.Negate },
        { "!", DmlUnary.Not },
        { "~", DmlUnary.BitwiseInvert }
    };

    private static readonly Dictionary<string, DmlUnaryAssignment> UnAsnArith = new();

    private static readonly Dictionary<string, DmlUnaryAssignment> UnAsnArithPre = new()
    {
        { "++", DmlUnaryAssignment.PreIncrement },
        { "--", DmlUnaryAssignment.PreDecrement }
    };

    private static readonly Dictionary<string, DmlUnaryAssignment> UnAsnArithPost = new()
    {
        { "++", DmlUnaryAssignment.PostIncrement },
        { "--", DmlUnaryAssignment.PostDecrement }
    };

    private static readonly Dictionary<string, DmlBinaryAssignment> BinAsnArith = new()
    {
        { "=", DmlBinaryAssignment.Assignment },
        { ":=", DmlBinaryAssignment.CopyInto },
        { "+=", DmlBinaryAssignment.AugAddition },
        { "-=", DmlBinaryAssignment.AugSubtraction },
        { "*=", DmlBinaryAssignment.AugMultiplication },
        { "/=", DmlBinaryAssignment.AugDivision },
        { "%=", DmlBinaryAssignment.AugModulo },
        { "%%=", DmlBinaryAssignment.AugIntModulo },
        { "|=", DmlBinaryAssignment.AugBitwiseOr },
        { "&=", DmlBinaryAssignment.AugBitewiseAnd },
        { "^=", DmlBinaryAssignment.AugBitwiseXor },
        { "<<=", DmlBinaryAssignment.AugBitShiftLeft },
        { ">>=", DmlBinaryAssignment.AugBitShiftRight }
    };

    private static readonly Dictionary<string, DmlBinary> BinArith = new()
    {
        { "-", DmlBinary.Subtraction },
        { "+", DmlBinary.Addition },
        { "<<", DmlBinary.BitShiftLeft },
        { ">>", DmlBinary.BitShiftLeft },
        { "^", DmlBinary.BitwiseXor },
        { "|", DmlBinary.BitwiseOr },
        { "&", DmlBinary.BitwiseAnd },
        { "*", DmlBinary.Multiplication },
        { "**", DmlBinary.Power },
        { "%", DmlBinary.Modulo },
        { "%%", DmlBinary.IntModulo },
        { "/", DmlBinary.Division },
        { ">", DmlBinary.GreaterThan },
        { "<", DmlBinary.LessThan },
        { ">=", DmlBinary.GreaterThanEq },
        { "<=", DmlBinary.LessThanEq },
        { "==", DmlBinary.Equals },
        { "~=", DmlBinary.Equivalent },
        { "||", DmlBinary.LogicalOr },
        { "&&", DmlBinary.LogicalAnd },
        { "!=", DmlBinary.NotEqual },
        { "[]", DmlBinary.ArrayIndex },
        { "in", DmlBinary.In }
    };

    private static readonly Dictionary<string, DmlTernery> TernArith = new()
    {
        { "[]:=", DmlTernery.ArrayEmplaceCopyInto },
        { "[]=", DmlTernery.ArrayEmplace }
    };

    public static DmlBinary ParseBinary(string op)
    {
        return BinArith[op];
    }

    public static DmlUnary ParseUnary(string op)
    {
        return UnArith[op];
    }

    public static DmlTernery ParseTernery(string op)
    {
        return TernArith[op];
    }

    public static DmlBinaryAssignment ParseBinaryAsn(string op)
    {
        return BinAsnArith[op];
    }

    public static DmlUnaryAssignment ParseUnaryAsn(string op, bool isPre)
    {
        return isPre ? UnAsnArithPre[op] : UnAsnArithPost[op];
    }

    public static DmlUnaryAssignment ParseUnaryAsn(string op)
    {
        return UnAsnArith[op];
    }

    public static bool IsPost(DmlUnaryAssignment asn)
    {
        return asn == DmlUnaryAssignment.PostIncrement || asn == DmlUnaryAssignment.PostDecrement;
    }

    public static bool IsOperandsReversed(DmlBinary op)
    {
        return op == DmlBinary.In;
    }
}