import { useState, useMemo, useEffect, useRef, useCallback } from "react";
import { observer } from "mobx-react-lite";
import { Vessel3DViewer, type Vessel3DViewerRef } from "../../Vessel3DViewer";
import { HydrostaticsHUD } from "../../HydrostaticsHUD";
import { DraftScrubber } from "../../DraftScrubber";
import { settingsStore } from "../../../../stores/SettingsStore";
import type { VesselDetails, HydroResult } from "../../../../types/hydrostatics";
import { hydrostaticsApi } from "../../../../services/hydrostaticsApi";
import { GIFCapture, downloadBlob } from "../../../../utils/gifCapture";
import toast from "react-hot-toast";

interface AnimatedWaterplane3DPanelProps {
  vessel: VesselDetails | null;
  loadcaseId?: string;
  kg?: number;
  lcg?: number;
}

/**
 * Convert value from SI (meters) to display units
 */
function convertFromSI(value: number, unitSystem: "SI" | "Imperial"): number {
  if (unitSystem === "SI") return value;
  return value * 3.28084; // meters to feet
}

/**
 * Convert value from display units to SI (meters)
 */
function convertToSI(value: number, unitSystem: "SI" | "Imperial"): number {
  if (unitSystem === "SI") return value;
  return value / 3.28084; // feet to meters
}

export const AnimatedWaterplane3DPanel = observer(function AnimatedWaterplane3DPanel({
  vessel,
  loadcaseId,
  kg = 0,
  lcg = 0,
}: AnimatedWaterplane3DPanelProps) {
  const displayUnits = settingsStore.preferredUnits;
  const viewerRef = useRef<Vessel3DViewerRef>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const gifCaptureRef = useRef<GIFCapture | null>(null);
  const frameIntervalRef = useRef<NodeJS.Timeout | null>(null);

  // Convert vessel dimensions from SI (stored) to display units
  const [localLpp, setLocalLpp] = useState<number>(
    vessel ? convertFromSI(vessel.lpp, displayUnits) : 100
  );
  const [localBeam, setLocalBeam] = useState<number>(
    vessel ? convertFromSI(vessel.beam, displayUnits) : 20
  );
  const [localDesignDraft, setLocalDesignDraft] = useState<number>(
    vessel ? convertFromSI(vessel.designDraft, displayUnits) : 10
  );

  // Draft control
  const minDraft = 0.1;
  const maxDraft = vessel ? vessel.designDraft * 1.5 : 15;
  const [currentDraft, setCurrentDraft] = useState<number>(vessel?.designDraft || 5);

  // Hydrostatics results
  const [currentResult, setCurrentResult] = useState<HydroResult | null>(null);
  const [isLoadingHydro, setIsLoadingHydro] = useState(false);

  // Recording state
  const [isRecording, setIsRecording] = useState(false);

  // Update local state when vessel changes
  useEffect(() => {
    if (vessel) {
      setLocalLpp(convertFromSI(vessel.lpp, displayUnits));
      setLocalBeam(convertFromSI(vessel.beam, displayUnits));
      setLocalDesignDraft(convertFromSI(vessel.designDraft, displayUnits));
      setCurrentDraft(vessel.designDraft);
    }
  }, [vessel, displayUnits]);

  // Convert local values back to SI for 3D viewer
  const lppSI = useMemo(() => convertToSI(localLpp, displayUnits), [localLpp, displayUnits]);
  const beamSI = useMemo(() => convertToSI(localBeam, displayUnits), [localBeam, displayUnits]);
  const designDraftSI = useMemo(
    () => convertToSI(localDesignDraft, displayUnits),
    [localDesignDraft, displayUnits]
  );

  // Auto-fit view when dimensions change significantly
  useEffect(() => {
    const timer = setTimeout(() => {
      viewerRef.current?.fitToView();
    }, 100);
    return () => clearTimeout(timer);
  }, [lppSI, beamSI, designDraftSI]);

  const fetchHydrostatics = useCallback(
    async (draft: number) => {
      if (!vessel) return;

      try {
        setIsLoadingHydro(true);
        const result = await hydrostaticsApi.computeSingle(vessel.id, loadcaseId, draft);
        setCurrentResult(result);
      } catch (error) {
        console.error("Failed to compute hydrostatics:", error);
        // Don't show toast during animation/recording to avoid clutter
        if (!isRecording) {
          toast.error("Failed to compute hydrostatics");
        }
      } finally {
        setIsLoadingHydro(false);
      }
    },
    [vessel, loadcaseId, isRecording]
  );

  // Fetch hydrostatics when draft changes (debounced)
  useEffect(() => {
    if (!vessel) return;

    const timer = setTimeout(() => {
      fetchHydrostatics(currentDraft);
    }, 100); // Debounce to avoid too many API calls during animation

    return () => clearTimeout(timer);
  }, [currentDraft, vessel, loadcaseId, fetchHydrostatics]);

  const handleDraftChange = useCallback((newDraft: number) => {
    setCurrentDraft(newDraft);
  }, []);

  // GIF Recording
  const handleStartRecording = useCallback(() => {
    // Get the canvas element from the 3D viewer
    const canvas = document.querySelector("canvas") as HTMLCanvasElement;
    if (!canvas) {
      toast.error("Canvas not found");
      return;
    }

    canvasRef.current = canvas;
    gifCaptureRef.current = new GIFCapture({
      width: 800,
      height: 600,
      quality: 10,
      workers: 2,
    });

    gifCaptureRef.current.start();
    setIsRecording(true);
    setCurrentDraft(minDraft); // Reset to start

    // Capture frames at 10fps (100ms interval)
    frameIntervalRef.current = setInterval(() => {
      if (gifCaptureRef.current && canvasRef.current) {
        gifCaptureRef.current.addFrame(canvasRef.current, 100);
      }
    }, 100);

    toast.success("Recording started");
  }, [minDraft]);

  const handleStopRecording = useCallback(async () => {
    if (frameIntervalRef.current) {
      clearInterval(frameIntervalRef.current);
      frameIntervalRef.current = null;
    }

    if (!gifCaptureRef.current) {
      toast.error("No recording in progress");
      return;
    }

    try {
      toast.loading("Generating GIF...", { id: "gif-generation" });
      const blob = await gifCaptureRef.current.stop();

      // Generate filename with timestamp
      const timestamp = new Date().toISOString().replace(/[:.]/g, "-").slice(0, -5);
      const filename = `waterplane-animation-${vessel?.name || "vessel"}-${timestamp}.gif`;

      downloadBlob(blob, filename);
      toast.success("GIF downloaded successfully!", { id: "gif-generation" });
    } catch (error) {
      console.error("Failed to generate GIF:", error);
      toast.error("Failed to generate GIF", { id: "gif-generation" });
    } finally {
      setIsRecording(false);
      gifCaptureRef.current = null;
      canvasRef.current = null;
    }
  }, [vessel]);

  if (!vessel) {
    return (
      <div className="flex items-center justify-center h-full">
        <p className="text-sm text-muted-foreground">No vessel data</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full relative">
      {/* 3D Viewer with HUD Overlay */}
      <div className="flex-1 min-h-0 relative">
        <Vessel3DViewer
          ref={viewerRef}
          lpp={lppSI}
          beam={beamSI}
          designDraft={designDraftSI}
          draft={currentDraft}
          kb={currentResult?.kBz}
          lcb={currentResult?.lCBx}
          kg={kg}
          lcg={lcg}
          currentResult={currentResult}
        />

        {/* HUD Overlay */}
        <HydrostaticsHUD
          currentResult={currentResult}
          draft={currentDraft}
          isAnimating={isRecording}
        />
      </div>

      {/* Draft Scrubber Controls */}
      <DraftScrubber
        minDraft={minDraft}
        maxDraft={maxDraft}
        currentDraft={currentDraft}
        onDraftChange={handleDraftChange}
        onStartRecording={handleStartRecording}
        onStopRecording={handleStopRecording}
        isRecording={isRecording}
        disabled={isLoadingHydro}
      />
    </div>
  );
});
