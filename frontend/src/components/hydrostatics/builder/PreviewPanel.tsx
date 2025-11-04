import { useMemo } from "react";

interface PreviewPanelProps {
  loa: number;
  beam: number;
  draft: number;
  cb: number;
  hullFamily: string;
}

function fmt(n: number, digits = 2): string {
  if (Number.isNaN(n) || !Number.isFinite(n)) return "—";
  return n.toFixed(digits);
}

export function PreviewPanel({ loa, beam, draft, cb, hullFamily }: PreviewPanelProps) {
  const lbr = useMemo(() => loa / beam, [loa, beam]);
  const btr = useMemo(() => beam / draft, [beam, draft]);

  const lcb = useMemo(() => {
    const base = 0.52;
    const tweak = (0.6 - cb) * 0.05 + (lbr - 6) * 0.002;
    return Math.min(0.6, Math.max(0.44, base + tweak));
  }, [cb, lbr]);

  const previewPath = useMemo(() => {
    const L = 600;
    const T = 140;
    const fwdRake = Math.max(8, 24 - lbr * 1.2);
    const aftRake = Math.max(8, 22 - lbr);
    const sheer = 18 + (1 - cb) * 18;
    const keelFair = 10 + (1 - cb) * 12;
    const xLCB = L * lcb;

    const p = [
      `M 0 ${T - keelFair}`,
      `C ${L * 0.1} ${T - keelFair - sheer}, ${L * 0.2} ${T - 6}, ${L * 0.33} ${T - 2}`,
      `S ${xLCB} ${T - 2}, ${L * 0.66} ${T - 6}`,
      `S ${L * 0.9} ${T - keelFair - sheer}, ${L} ${T - keelFair}`,
      `L ${L - aftRake} ${T}`,
      `L ${fwdRake} ${T}`,
      "Z",
    ];

    return p.join(" ");
  }, [cb, lbr, lcb]);

  return (
    <div className="bg-card rounded-2xl shadow border border-border p-4 h-full">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-semibold">Preview</h3>
        <div className="text-[11px] text-muted-foreground">Live rendering (not to scale)</div>
      </div>

      <div className="aspect-[16/9] w-full rounded-xl bg-slate-100 dark:bg-slate-900 border border-border grid place-items-center overflow-hidden">
        <svg viewBox="0 0 600 180" className="w-full h-full">
          <defs>
            <linearGradient id="hull" x1="0" x2="0" y1="0" y2="1">
              <stop
                offset="0%"
                stopOpacity="0.95"
                className="text-slate-700 dark:text-slate-300"
                stopColor="currentColor"
              />
              <stop
                offset="100%"
                stopOpacity="0.85"
                className="text-slate-700 dark:text-slate-300"
                stopColor="currentColor"
              />
            </linearGradient>
          </defs>

          {/* Waterline */}
          <line
            x1="0"
            y1="140"
            x2="600"
            y2="140"
            stroke="currentColor"
            className="text-sky-400"
            strokeDasharray="4 4"
          />

          {/* Hull side profile */}
          <path d={previewPath} fill="url(#hull)" />

          {/* LCB marker */}
          <line
            x1={600 * lcb}
            y1={20}
            x2={600 * lcb}
            y2={160}
            stroke="currentColor"
            className="text-rose-500"
            strokeDasharray="6 6"
          />
          <text x={600 * lcb + 6} y={30} fontSize="10" className="fill-rose-600 dark:fill-rose-400">
            LCB ≈ {fmt(lcb * 100, 1)}%
          </text>
        </svg>
      </div>

      <div className="grid grid-cols-3 gap-3 mt-3 text-xs text-muted-foreground">
        <div className="rounded-lg border border-border p-2">
          <div className="font-medium">L/B</div>
          <div className="text-foreground text-base">{fmt(lbr, 2)}</div>
        </div>
        <div className="rounded-lg border border-border p-2">
          <div className="font-medium">B/T</div>
          <div className="text-foreground text-base">{fmt(btr, 2)}</div>
        </div>
        <div className="rounded-lg border border-border p-2">
          <div className="font-medium">Family</div>
          <div className="text-foreground text-base">{hullFamily}</div>
        </div>
      </div>

      <p className="text-[11px] text-muted-foreground mt-3">
        Preview uses stylistic geometry for feedback only. Engineering outputs are in the Results
        panel.
      </p>
    </div>
  );
}
