import { observer } from "mobx-react-lite";
import { useState } from "react";
import { Button } from "../../ui/button";
import { seakeepingStore } from "../../../stores/SeakeepingStore";
import toast from "react-hot-toast";

export const SeaStatePanel = observer(() => {
  const [hs, setHs] = useState(3.0);
  const [tp, setTp] = useState(8.0);
  const [heading, setHeading] = useState(180);
  const [spectrum, setSpectrum] = useState<"JONSWAP" | "PM">("JONSWAP");
  const [gamma, setGamma] = useState(3.3);

  const handleAnalyze = async () => {
    if (!seakeepingStore.raoResults) {
      toast.error("Calculate RAOs first");
      return;
    }

    await seakeepingStore.analyzeMotion({
      significantHeight: hs,
      peakPeriod: tp,
      heading: heading,
      spectrum: spectrum,
      gamma: spectrum === "JONSWAP" ? gamma : undefined,
    });
  };

  return (
    <div className="p-4 space-y-4 border-t border-border">
      <div>
        <h3 className="font-semibold text-sm mb-3">Sea State Configuration</h3>

        <div className="space-y-3">
          {/* Significant Height */}
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Significant Height Hs (m)
            </label>
            <input
              type="range"
              value={hs}
              onChange={(e) => setHs(Number(e.target.value))}
              min={0}
              max={12}
              step={0.5}
              className="w-full mt-1"
            />
            <div className="text-right text-xs text-muted-foreground">{hs.toFixed(1)} m</div>
          </div>

          {/* Peak Period */}
          <div>
            <label className="text-xs font-medium text-muted-foreground">Peak Period Tp (s)</label>
            <input
              type="range"
              value={tp}
              onChange={(e) => setTp(Number(e.target.value))}
              min={2}
              max={20}
              step={0.5}
              className="w-full mt-1"
            />
            <div className="text-right text-xs text-muted-foreground">{tp.toFixed(1)} s</div>
          </div>

          {/* Heading */}
          <div>
            <label className="text-xs font-medium text-muted-foreground">Heading (degrees)</label>
            <input
              type="range"
              value={heading}
              onChange={(e) => setHeading(Number(e.target.value))}
              min={0}
              max={180}
              step={15}
              className="w-full mt-1"
            />
            <div className="text-right text-xs text-muted-foreground">
              {heading}° (
              {heading === 0
                ? "Following"
                : heading === 90
                  ? "Beam"
                  : heading === 180
                    ? "Head"
                    : "Oblique"}
              )
            </div>
          </div>

          {/* Spectrum Type */}
          <div>
            <label className="text-xs font-medium text-muted-foreground mb-2 block">
              Wave Spectrum
            </label>
            <div className="flex space-x-2">
              <Button
                variant={spectrum === "JONSWAP" ? "default" : "outline"}
                size="sm"
                onClick={() => setSpectrum("JONSWAP")}
                className="flex-1"
              >
                JONSWAP
              </Button>
              <Button
                variant={spectrum === "PM" ? "default" : "outline"}
                size="sm"
                onClick={() => setSpectrum("PM")}
                className="flex-1"
              >
                PM
              </Button>
            </div>
          </div>

          {/* Gamma (JONSWAP only) */}
          {spectrum === "JONSWAP" && (
            <div>
              <label className="text-xs font-medium text-muted-foreground">
                Peak Enhancement γ
              </label>
              <input
                type="number"
                value={gamma}
                onChange={(e) => setGamma(Number(e.target.value))}
                step={0.1}
                min={1.0}
                max={7.0}
                className="w-full px-3 py-2 mt-1 border border-input rounded-md text-sm"
              />
            </div>
          )}

          {/* Analyze Button */}
          <Button
            onClick={handleAnalyze}
            disabled={seakeepingStore.isAnalyzing || !seakeepingStore.raoResults}
            className="w-full"
            variant="secondary"
          >
            {seakeepingStore.isAnalyzing ? (
              <>
                <span className="animate-spin mr-2">⏳</span>
                Analyzing...
              </>
            ) : (
              "Analyze Motion"
            )}
          </Button>
        </div>
      </div>
    </div>
  );
});
