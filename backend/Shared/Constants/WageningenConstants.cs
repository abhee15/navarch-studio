namespace Shared.Constants;

/// <summary>
/// Wageningen B-Series propeller polynomial coefficients
/// Source: MARIN systematic series (Oosterveld & van Oossanen, 1975)
/// 33-term regression for KT (thrust) and KQ (torque)
/// </summary>
public static class WageningenConstants
{
    /// <summary>
    /// All 33 polynomial terms for Wageningen B-Series
    /// Formula: KT/KQ = Σ C * J^s * (AE/A0)^t * (P/D)^u * Z^v
    /// </summary>
    public static readonly WageningenCoefficient[] Coefficients = new[]
    {
        new WageningenCoefficient { Term=1,  s=0, t=0, u=0, v=0, C_KT= 0.00880496,  C_KQ=-0.00204554 },
        new WageningenCoefficient { Term=2,  s=0, t=1, u=0, v=0, C_KT=-0.204554,    C_KQ= 0.00166351 },
        new WageningenCoefficient { Term=3,  s=0, t=2, u=0, v=0, C_KT= 0.166351,    C_KQ=-0.00048169 },
        new WageningenCoefficient { Term=4,  s=0, t=0, u=1, v=0, C_KT= 0.158114,    C_KQ= 0.0055581 },
        new WageningenCoefficient { Term=5,  s=0, t=1, u=1, v=0, C_KT=-0.147581,    C_KQ=-0.00010119 },
        new WageningenCoefficient { Term=6,  s=0, t=2, u=1, v=0, C_KT=-0.481497,    C_KQ= 0.0013365 },
        new WageningenCoefficient { Term=7,  s=0, t=0, u=2, v=0, C_KT= 0.415437,    C_KQ= 0.00204554 },
        new WageningenCoefficient { Term=8,  s=0, t=1, u=2, v=0, C_KT= 0.0144043,   C_KQ=-0.000638407 },
        new WageningenCoefficient { Term=9,  s=0, t=0, u=0, v=1, C_KT=-0.0530054,   C_KQ= 0.00055581 },
        new WageningenCoefficient { Term=10, s=0, t=1, u=0, v=1, C_KT= 0.0143481,   C_KQ=-0.00303818 },
        new WageningenCoefficient { Term=11, s=0, t=2, u=0, v=1, C_KT= 0.0606826,   C_KQ=-0.00108671 },
        new WageningenCoefficient { Term=12, s=0, t=0, u=1, v=1, C_KT=-0.0125894,   C_KQ= 0.000638407 },
        new WageningenCoefficient { Term=13, s=0, t=1, u=1, v=1, C_KT= 0.0109689,   C_KQ=-0.000173869 },
        new WageningenCoefficient { Term=14, s=0, t=0, u=2, v=1, C_KT=-0.133698,    C_KQ= 0.00055581 },
        new WageningenCoefficient { Term=15, s=0, t=0, u=0, v=2, C_KT= 0.00638407,  C_KQ=-0.00006384 },
        new WageningenCoefficient { Term=16, s=0, t=1, u=0, v=2, C_KT=-0.00132718,  C_KQ= 0.00025975 },
        new WageningenCoefficient { Term=17, s=0, t=0, u=1, v=2, C_KT= 0.0168424,   C_KQ=-0.00016846 },
        new WageningenCoefficient { Term=18, s=0, t=0, u=0, v=3, C_KT=-0.00052227,  C_KQ= 0.00011346 },
        new WageningenCoefficient { Term=19, s=1, t=0, u=0, v=0, C_KT= 0.0,         C_KQ= 0.00064135 },
        new WageningenCoefficient { Term=20, s=1, t=1, u=0, v=0, C_KT= 0.0,         C_KQ=-0.00055581 },
        new WageningenCoefficient { Term=21, s=1, t=2, u=0, v=0, C_KT= 0.0,         C_KQ= 0.00056894 },
        new WageningenCoefficient { Term=22, s=1, t=0, u=1, v=0, C_KT= 0.0,         C_KQ=-0.00054259 },
        new WageningenCoefficient { Term=23, s=1, t=1, u=1, v=0, C_KT= 0.0,         C_KQ= 0.00046483 },
        new WageningenCoefficient { Term=24, s=1, t=0, u=2, v=0, C_KT= 0.0,         C_KQ=-0.00046483 },
        new WageningenCoefficient { Term=25, s=1, t=0, u=0, v=1, C_KT= 0.0,         C_KQ= 0.00033526 },
        new WageningenCoefficient { Term=26, s=1, t=1, u=0, v=1, C_KT= 0.0,         C_KQ=-0.00032844 },
        new WageningenCoefficient { Term=27, s=1, t=0, u=1, v=1, C_KT= 0.0,         C_KQ= 0.00022871 },
        new WageningenCoefficient { Term=28, s=1, t=0, u=0, v=2, C_KT= 0.0,         C_KQ=-0.00012315 },
        new WageningenCoefficient { Term=29, s=2, t=0, u=0, v=0, C_KT= 0.0,         C_KQ= 0.0000311 },
        new WageningenCoefficient { Term=30, s=2, t=1, u=0, v=0, C_KT= 0.0,         C_KQ=-0.0000264 },
        new WageningenCoefficient { Term=31, s=2, t=0, u=1, v=0, C_KT= 0.0,         C_KQ= 0.0000231 },
        new WageningenCoefficient { Term=32, s=2, t=0, u=0, v=1, C_KT= 0.0,         C_KQ=-0.0000132 },
        new WageningenCoefficient { Term=33, s=2, t=0, u=0, v=2, C_KT= 0.0,         C_KQ= 0.0000057 }
    };

    /// <summary>
    /// Parameter ranges for Wageningen B-Series
    /// </summary>
    public static class ParameterRanges
    {
        public const double J_Min = 0.0;
        public const double J_Max = 1.5;
        public const double J_Typical = 0.7;

        public const int Z_Min = 2;
        public const int Z_Max = 7;
        public const int Z_Typical = 4;

        public const double AeA0_Min = 0.3;
        public const double AeA0_Max = 1.05;
        public const double AeA0_Typical = 0.55;

        public const double PD_Min = 0.5;
        public const double PD_Max = 1.4;
        public const double PD_Typical = 1.0;
    }
}

/// <summary>
/// Single Wageningen polynomial coefficient term
/// </summary>
public class WageningenCoefficient
{
    public int Term { get; init; }
    public int s { get; init; } // J exponent
    public int t { get; init; } // AE/A0 exponent
    public int u { get; init; } // P/D exponent
    public int v { get; init; } // Z exponent
    public double C_KT { get; init; } // KT coefficient
    public double C_KQ { get; init; } // KQ coefficient
}
