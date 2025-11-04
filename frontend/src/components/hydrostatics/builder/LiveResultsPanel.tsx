import { useMemo } from "react";

interface LiveResultsPanelProps {
  loa: number;
  beam: number;
  draft: number;
  cb: number;
}

function fmt(n: number, digits = 2): string {
  if (Number.isNaN(n) || !Number.isFinite(n)) return "—";
  return n.toFixed(digits);
}

export function LiveResultsPanel({ loa, beam, draft, cb }: LiveResultsPanelProps) {
  const rho = 1.025; // seawater density (t/m³)

  const displacement = useMemo(() => {
    return rho * cb * loa * beam * draft;
  }, [rho, cb, loa, beam, draft]);

  const lbr = useMemo(() => loa / beam, [loa, beam]);

  const lcb = useMemo(() => {
    // Crude heuristic for LCB from FP
    const base = 0.52;
    const tweak = (0.6 - cb) * 0.05 + (lbr - 6) * 0.002;
    return Math.min(0.6, Math.max(0.44, base + tweak));
  }, [cb, lbr]);

  return (
    <div className="bg-card rounded-2xl shadow border border-border p-4 sticky top-4">
      <h3 className="text-sm font-semibold mb-3">Live Results (approx.)</h3>
      <div className="space-y-2 text-sm">
        <div className="rounded-xl border border-border p-3 flex items-center justify-between">
          <div>
            <div className="text-xs text-muted-foreground">Displacement</div>
            <div className="text-lg font-semibold">{fmt(displacement, 1)} t</div>
          </div>
          <div className="text-[10px] text-muted-foreground">ρ=1.025</div>
        </div>

        <div className="rounded-xl border border-border p-3 grid grid-cols-2 gap-2">
          <div>
            <div className="text-xs text-muted-foreground">Cb</div>
            <div className="text-base font-semibold">{fmt(cb, 2)}</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">LCB (from FP)</div>
            <div className="text-base font-semibold">{fmt(lcb * 100, 1)}%</div>
          </div>
        </div>

        <div className="rounded-xl border border-border p-3 grid grid-cols-3 gap-2">
          <div>
            <div className="text-xs text-muted-foreground">L</div>
            <div className="text-base font-semibold">{fmt(loa, 1)} m</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">B</div>
            <div className="text-base font-semibold">{fmt(beam, 1)} m</div>
          </div>
          <div>
            <div className="text-xs text-muted-foreground">T</div>
            <div className="text-base font-semibold">{fmt(draft, 1)} m</div>
          </div>
        </div>
      </div>

      <div className="text-[11px] text-muted-foreground mt-3">
        Quick Create hides advanced sections on mobile automatically.
      </div>
    </div>
  );
}
