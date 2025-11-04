import { useState, useEffect, useRef } from "react";
import { Button } from "../ui/button";
import { Play, Pause, RotateCcw, Download } from "lucide-react";
import { observer } from "mobx-react-lite";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";

interface DraftScrubberProps {
  minDraft: number;
  maxDraft: number;
  currentDraft: number;
  onDraftChange: (draft: number) => void;
  onStartRecording?: () => void;
  onStopRecording?: () => void;
  isRecording?: boolean;
  disabled?: boolean;
}

/**
 * Draft scrubber control with animation playback
 * Allows users to scrub through draft values and animate the waterplane
 */
export const DraftScrubber = observer(function DraftScrubber({
  minDraft,
  maxDraft,
  currentDraft,
  onDraftChange,
  onStartRecording,
  onStopRecording,
  isRecording = false,
  disabled = false,
}: DraftScrubberProps) {
  const [isPlaying, setIsPlaying] = useState(false);
  const [loop, setLoop] = useState(true);
  const [speed, setSpeed] = useState(1.0); // Animation speed multiplier
  const animationRef = useRef<number | null>(null);
  const lastTimeRef = useRef<number>(0);

  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");

  // Convert from SI to display units
  const convertLength = (value: number): number => {
    if (displayUnits === "SI") return value;
    return value * 3.28084; // meters to feet
  };

  // Convert from display units to SI
  const convertToSI = (value: number): number => {
    if (displayUnits === "SI") return value;
    return value / 3.28084; // feet to meters
  };

  const displayMin = convertLength(minDraft);
  const displayMax = convertLength(maxDraft);
  const displayCurrent = convertLength(currentDraft);

  // Animation loop
  useEffect(() => {
    if (!isPlaying) {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
        animationRef.current = null;
      }
      return;
    }

    const animate = (timestamp: number) => {
      if (lastTimeRef.current === 0) {
        lastTimeRef.current = timestamp;
      }

      const deltaTime = timestamp - lastTimeRef.current;
      lastTimeRef.current = timestamp;

      // Calculate draft increment (adjust for 60fps baseline)
      const draftRange = maxDraft - minDraft;
      const draftIncrement = (draftRange / 5000) * deltaTime * speed; // 5 seconds for full range at speed=1

      let newDraft = currentDraft + draftIncrement;

      // Handle looping or stopping at max
      if (newDraft >= maxDraft) {
        if (loop) {
          newDraft = minDraft;
        } else {
          newDraft = maxDraft;
          setIsPlaying(false);
        }
      }

      onDraftChange(newDraft);
      animationRef.current = requestAnimationFrame(animate);
    };

    animationRef.current = requestAnimationFrame(animate);

    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [isPlaying, currentDraft, minDraft, maxDraft, loop, speed, onDraftChange]);

  const handlePlayPause = () => {
    if (!isPlaying) {
      lastTimeRef.current = 0;
      // If at max and not looping, reset to min
      if (currentDraft >= maxDraft && !loop) {
        onDraftChange(minDraft);
      }
    }
    setIsPlaying(!isPlaying);
  };

  const handleReset = () => {
    setIsPlaying(false);
    lastTimeRef.current = 0;
    onDraftChange(minDraft);
  };

  const handleSliderChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const displayValue = parseFloat(e.target.value);
    const siValue = convertToSI(displayValue);
    onDraftChange(siValue);
  };

  const handleStartRecording = () => {
    if (onStartRecording) {
      onStartRecording();
      // Auto-start animation when recording starts
      setIsPlaying(true);
      lastTimeRef.current = 0;
    }
  };

  const handleStopRecording = () => {
    if (onStopRecording) {
      onStopRecording();
      setIsPlaying(false);
    }
  };

  return (
    <div className="bg-card border-t border-border p-4 space-y-4">
      {/* Draft Slider */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <label className="text-xs font-medium text-foreground">Draft</label>
          <span className="text-sm font-mono font-bold text-foreground">
            {displayCurrent.toFixed(3)} {lengthUnit}
          </span>
        </div>
        <input
          type="range"
          min={displayMin}
          max={displayMax}
          step={(displayMax - displayMin) / 100}
          value={displayCurrent}
          onChange={handleSliderChange}
          disabled={disabled || isPlaying}
          className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed accent-primary"
        />
        <div className="flex justify-between text-xs text-muted-foreground">
          <span>
            {displayMin.toFixed(2)} {lengthUnit}
          </span>
          <span>
            {displayMax.toFixed(2)} {lengthUnit}
          </span>
        </div>
      </div>

      {/* Animation Controls */}
      <div className="flex items-center gap-2">
        <Button
          size="sm"
          variant={isPlaying ? "default" : "outline"}
          onClick={handlePlayPause}
          disabled={disabled}
          title={isPlaying ? "Pause Animation" : "Play Animation"}
        >
          {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
        </Button>

        <Button
          size="sm"
          variant="outline"
          onClick={handleReset}
          disabled={disabled || isPlaying}
          title="Reset to Minimum Draft"
        >
          <RotateCcw className="h-4 w-4" />
        </Button>

        {/* Loop Toggle */}
        <label className="flex items-center gap-2 ml-2 cursor-pointer">
          <input
            type="checkbox"
            checked={loop}
            onChange={(e) => setLoop(e.target.checked)}
            disabled={disabled}
            className="rounded accent-primary"
          />
          <span className="text-xs text-muted-foreground">Loop</span>
        </label>

        {/* Speed Control */}
        <div className="flex items-center gap-2 ml-auto">
          <label className="text-xs text-muted-foreground">Speed:</label>
          <select
            value={speed}
            onChange={(e) => setSpeed(parseFloat(e.target.value))}
            disabled={disabled}
            className="text-xs bg-background border border-border rounded px-2 py-1"
          >
            <option value="0.5">0.5x</option>
            <option value="1.0">1.0x</option>
            <option value="1.5">1.5x</option>
            <option value="2.0">2.0x</option>
          </select>
        </div>
      </div>

      {/* Recording Controls */}
      <div className="flex items-center gap-2 pt-2 border-t border-border">
        {!isRecording ? (
          <Button
            size="sm"
            variant="outline"
            onClick={handleStartRecording}
            disabled={disabled}
            className="flex items-center gap-2"
          >
            <Download className="h-4 w-4" />
            <span className="text-xs">Record GIF</span>
          </Button>
        ) : (
          <Button
            size="sm"
            variant="destructive"
            onClick={handleStopRecording}
            className="flex items-center gap-2"
          >
            <div className="w-2 h-2 bg-white rounded-full animate-pulse" />
            <span className="text-xs">Stop Recording</span>
          </Button>
        )}
        {isRecording && (
          <span className="text-xs text-muted-foreground ml-2">
            Recording animation... (this may take a moment)
          </span>
        )}
      </div>
    </div>
  );
});
