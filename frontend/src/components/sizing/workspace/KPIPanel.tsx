import type { CandidateDesign } from "../../../types/sizing";

interface KPIPanelProps {
  candidate: CandidateDesign;
}

/**
 * KPI Summary Panel - Key Performance Indicators
 *
 * Displays critical metrics with visual indicators
 */
export const KPIPanel: React.FC<KPIPanelProps> = ({ candidate }) => {
  // Parse flags
  let flags: string[] = [];
  try {
    flags = candidate.flagsJson ? JSON.parse(candidate.flagsJson) : [];
  } catch {
    flags = [];
  }

  const hasWarnings = flags.length > 0;
  const hasCritical = flags.some(
    (f) =>
      f.includes("loa_exceeded") ||
      f.includes("beam_constrained") ||
      f.includes("draft_constrained")
  );

  // Calculate ratios
  const lOverB = candidate.lppM / candidate.bM;
  const bOverT = candidate.bM / candidate.tM;
  const dOverT = candidate.dM / candidate.tM;
  const lwlOverLambda = candidate.lwlOverLambda || 0;

  const metrics = [
    {
      category: "Principal Dimensions",
      items: [
        { label: "Length (Lpp)", value: candidate.lppM.toFixed(2), unit: "m", color: "blue" },
        { label: "Beam", value: candidate.bM.toFixed(2), unit: "m", color: "blue" },
        { label: "Draft", value: candidate.tM.toFixed(2), unit: "m", color: "blue" },
        { label: "Depth", value: candidate.dM.toFixed(2), unit: "m", color: "blue" },
        {
          label: "LOA (est.)",
          value: candidate.loaM?.toFixed(2) || "N/A",
          unit: "m",
          color: "blue",
        },
      ],
    },
    {
      category: "Form Coefficients",
      items: [
        { label: "Block Coeff. (Cb)", value: candidate.cb.toFixed(3), unit: "", color: "purple" },
        { label: "Prismatic (Cp)", value: candidate.cp.toFixed(3), unit: "", color: "purple" },
        { label: "Waterplane (Cwp)", value: candidate.cwp.toFixed(3), unit: "", color: "purple" },
        {
          label: "Midship (Cm)",
          value: candidate.cm?.toFixed(3) || "N/A",
          unit: "",
          color: "purple",
        },
      ],
    },
    {
      category: "Dimensional Ratios",
      items: [
        { label: "L/B", value: lOverB.toFixed(2), unit: "", color: "cyan" },
        { label: "B/T", value: bOverT.toFixed(2), unit: "", color: "cyan" },
        { label: "D/T", value: dOverT.toFixed(2), unit: "", color: "cyan" },
        { label: "Lwl/λ", value: lwlOverLambda.toFixed(2), unit: "", color: "cyan" },
      ],
    },
    {
      category: "Performance",
      items: [
        {
          label: "Displacement",
          value: candidate.displacementT.toFixed(0),
          unit: "t",
          color: "green",
        },
        { label: "Froude Number", value: candidate.fn.toFixed(3), unit: "", color: "green" },
        { label: "EHP", value: candidate.ehpKw?.toFixed(0) || "N/A", unit: "kW", color: "green" },
        {
          label: "SHP (est.)",
          value: candidate.shpKw?.toFixed(0) || "N/A",
          unit: "kW",
          color: "green",
        },
      ],
    },
    {
      category: "Stability (Estimates)",
      items: [
        { label: "KB", value: candidate.kbM?.toFixed(2) || "N/A", unit: "m", color: "orange" },
        {
          label: "LCB (% Lpp)",
          value: candidate.lcbPctLpp?.toFixed(1) || "N/A",
          unit: "%",
          color: "orange",
        },
        {
          label: "GM (est.)",
          value: candidate.gmEstM?.toFixed(2) || "N/A",
          unit: "m",
          color: "orange",
        },
      ],
    },
  ];

  return (
    <div className="space-y-6">
      {/* Score Card */}
      <div className="rounded-lg bg-gradient-to-br from-blue-500 to-cyan-600 p-6 text-white shadow-xl">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium opacity-90">Overall Score</p>
            <p className="text-4xl font-bold mt-1">{candidate.score.toFixed(1)}</p>
            <p className="text-xs opacity-75 mt-1">Rank #{candidate.rank}</p>
          </div>
          <div className="text-right">
            <div className="inline-block rounded-lg bg-white/20 px-4 py-2">
              <p className="text-xs font-medium opacity-90">Hull Family</p>
              <p className="text-lg font-bold mt-1">{candidate.hullFamily}</p>
            </div>
          </div>
        </div>

        {/* Status Indicator */}
        {candidate.isSelected && (
          <div className="mt-4 inline-flex items-center gap-2 rounded-full bg-green-500 px-3 py-1 text-sm font-semibold">
            <span>✓</span>
            <span>Selected Design</span>
          </div>
        )}
      </div>

      {/* Flags/Warnings */}
      {hasWarnings && (
        <div
          className={`rounded-lg p-4 ${
            hasCritical
              ? "bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500"
              : "bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500"
          }`}
        >
          <div className="flex items-start gap-3">
            <span className="text-2xl">{hasCritical ? "⚠️" : "ℹ️"}</span>
            <div className="flex-1">
              <p
                className={`font-semibold ${
                  hasCritical
                    ? "text-red-800 dark:text-red-300"
                    : "text-yellow-800 dark:text-yellow-300"
                }`}
              >
                {hasCritical ? "Constraint Violations" : "Solver Notes"}
              </p>
              <ul
                className={`mt-2 space-y-1 text-sm ${
                  hasCritical
                    ? "text-red-700 dark:text-red-400"
                    : "text-yellow-700 dark:text-yellow-400"
                }`}
              >
                {flags.map((flag, idx) => (
                  <li key={idx} className="flex items-start gap-2">
                    <span>•</span>
                    <span>{flag.replace(/_/g, " ")}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Metrics Groups */}
      {metrics.map((group) => (
        <div
          key={group.category}
          className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 overflow-hidden shadow"
        >
          <div className="bg-gray-50 dark:bg-gray-900 px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <h4 className="font-semibold text-gray-900 dark:text-white text-sm">
              {group.category}
            </h4>
          </div>
          <div className="divide-y divide-gray-200 dark:divide-gray-700">
            {group.items.map((item) => (
              <div
                key={item.label}
                className="px-4 py-3 flex items-center justify-between hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
              >
                <span className="text-sm text-gray-600 dark:text-gray-400">{item.label}</span>
                <span
                  className={`font-bold tabular-nums text-${item.color}-600 dark:text-${item.color}-400`}
                >
                  {item.value} <span className="text-xs font-normal opacity-75">{item.unit}</span>
                </span>
              </div>
            ))}
          </div>
        </div>
      ))}

      {/* Export Actions */}
      <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 shadow">
        <h4 className="font-semibold text-gray-900 dark:text-white text-sm mb-3">Quick Actions</h4>
        <div className="grid grid-cols-2 gap-2">
          <button className="px-3 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors flex items-center justify-center gap-2">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
            <span>Export JSON</span>
          </button>
          <button className="px-3 py-2 text-sm bg-green-600 hover:bg-green-700 text-white rounded-lg transition-colors flex items-center justify-center gap-2">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
            <span>Export CSV</span>
          </button>
          <button className="px-3 py-2 text-sm bg-purple-600 hover:bg-purple-700 text-white rounded-lg transition-colors flex items-center justify-center gap-2">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M14 10l-2 1m0 0l-2-1m2 1v2.5M20 7l-2 1m2-1l-2-1m2 1v2.5M14 4l-2-1-2 1M4 7l2-1M4 7l2 1M4 7v2.5M12 21l-2-1m2 1l2-1m-2 1v-2.5M6 18l-2-1v-2.5M18 18l2-1v-2.5"
              />
            </svg>
            <span>Send to Hydro</span>
          </button>
          <button className="px-3 py-2 text-sm bg-orange-600 hover:bg-orange-700 text-white rounded-lg transition-colors flex items-center justify-center gap-2">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M13 10V3L4 14h7v7l9-11h-7z"
              />
            </svg>
            <span>Resistance</span>
          </button>
        </div>
      </div>
    </div>
  );
};
