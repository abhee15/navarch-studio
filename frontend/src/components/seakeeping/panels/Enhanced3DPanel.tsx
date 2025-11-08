import { observer } from "mobx-react-lite";
import { useState } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, PerspectiveCamera, Grid } from "@react-three/drei";
import { seakeepingStore } from "../../../stores/SeakeepingStore";
import { EnhancedHull3D } from "../../visualization/EnhancedHull3D";
import { StationWaterlineOverlay } from "../../visualization/StationWaterlineOverlay";
import { InteractiveCoGMarker } from "../../visualization/InteractiveCoGMarker";
import { MotionAnimator } from "../../visualization/MotionAnimator";
import { Button } from "../../ui/button";
import { Play, Pause, RotateCcw } from "lucide-react";

export const Enhanced3DPanel = observer(() => {
  const [useOffsetsHull, setUseOffsetsHull] = useState(true);
  const [showStations, setShowStations] = useState(true);
  const [showWaterlines, setShowWaterlines] = useState(true);
  const [isAnimating, setIsAnimating] = useState(false);
  const [animationSpeed, setAnimationSpeed] = useState(1.0);

  const vessel = seakeepingStore.vesselSnapshot;
  const raos = seakeepingStore.raoResults;

  if (!vessel) {
    return (
      <div className="flex items-center justify-center h-96 bg-card border border-border rounded-lg">
        <p className="text-muted-foreground">No vessel data</p>
      </div>
    );
  }

  const handleCoGDragEnd = (newPosition: [number, number, number]) => {
    console.log("CoG moved to:", newPosition);
    // TODO: Trigger stability recalculation if needed
  };

  const canAnimate = raos != null && seakeepingStore.motionResponse != null;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Enhanced 3D Visualization</h2>
          <p className="text-sm text-muted-foreground">
            Hull rendering from offsets with station/waterline overlays
          </p>
        </div>

        {/* Controls */}
        <div className="flex space-x-2">
          <Button
            variant={useOffsetsHull ? "default" : "outline"}
            size="sm"
            onClick={() => setUseOffsetsHull(!useOffsetsHull)}
          >
            Use Offsets
          </Button>
          <Button
            variant={showStations ? "default" : "outline"}
            size="sm"
            onClick={() => setShowStations(!showStations)}
          >
            Stations
          </Button>
          <Button
            variant={showWaterlines ? "default" : "outline"}
            size="sm"
            onClick={() => setShowWaterlines(!showWaterlines)}
          >
            Waterlines
          </Button>
        </div>
      </div>

      {/* 3D Canvas */}
      <div
        className="bg-gray-900 dark:bg-gray-950 rounded-lg overflow-hidden"
        style={{ height: "600px" }}
      >
        <Canvas shadows gl={{ antialias: true }}>
          <PerspectiveCamera makeDefault position={[50, 40, 50]} fov={45} />
          <OrbitControls enableDamping dampingFactor={0.05} minDistance={10} maxDistance={200} />

          {/* Lighting */}
          <ambientLight intensity={0.6} />
          <directionalLight position={[10, 10, 5]} intensity={1.0} castShadow />
          <directionalLight position={[-10, 5, -5]} intensity={0.4} />

          {/* Grid */}
          <Grid args={[100, 20]} cellColor="#6b7280" sectionColor="#4b5563" fadeDistance={80} />

          {/* Hull with optional animation */}
          {raos && seakeepingStore.motionResponse && isAnimating ? (
            <MotionAnimator
              raoResults={raos}
              seaState={seakeepingStore.motionResponse.seaState}
              isPlaying={isAnimating}
              speedMultiplier={animationSpeed}
            >
              {useOffsetsHull && vessel.offsetsGrid ? (
                <EnhancedHull3D offsetsGrid={vessel.offsetsGrid} />
              ) : (
                <mesh>
                  <boxGeometry args={[vessel.lpp, vessel.beam, vessel.draft]} />
                  <meshStandardMaterial color="#6b7280" />
                </mesh>
              )}
            </MotionAnimator>
          ) : (
            <>
              {useOffsetsHull && vessel.offsetsGrid ? (
                <EnhancedHull3D offsetsGrid={vessel.offsetsGrid} />
              ) : (
                <mesh>
                  <boxGeometry args={[vessel.lpp, vessel.beam, vessel.draft]} />
                  <meshStandardMaterial color="#6b7280" />
                </mesh>
              )}
            </>
          )}

          {/* Overlays */}
          {vessel.offsetsGrid && (
            <StationWaterlineOverlay
              offsetsGrid={vessel.offsetsGrid}
              showStations={showStations}
              showWaterlines={showWaterlines}
            />
          )}

          {/* Interactive markers */}
          <InteractiveCoGMarker
            position={[vessel.lpp / 2, 0, vessel.draft / 2]}
            label="CoG"
            color="#ef4444"
            onDragEnd={handleCoGDragEnd}
          />

          <InteractiveCoGMarker
            position={[vessel.lpp / 2, 0, vessel.draft / 3]}
            label="CoB"
            color="#10b981"
            onDragEnd={handleCoGDragEnd}
          />
        </Canvas>
      </div>

      {/* Animation Controls */}
      {canAnimate && (
        <div className="bg-card border border-border rounded-lg p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <Button variant="outline" size="sm" onClick={() => setIsAnimating(!isAnimating)}>
                {isAnimating ? (
                  <>
                    <Pause className="h-4 w-4 mr-2" />
                    Pause
                  </>
                ) : (
                  <>
                    <Play className="h-4 w-4 mr-2" />
                    Play Animation
                  </>
                )}
              </Button>

              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setIsAnimating(false);
                  // TODO: Reset time
                }}
              >
                <RotateCcw className="h-4 w-4 mr-2" />
                Reset
              </Button>
            </div>

            {/* Speed Control */}
            <div className="flex items-center space-x-3">
              <span className="text-sm text-muted-foreground">Speed:</span>
              <input
                type="range"
                min={0.5}
                max={5}
                step={0.5}
                value={animationSpeed}
                onChange={(e) => setAnimationSpeed(Number(e.target.value))}
                className="w-32"
              />
              <span className="text-sm font-medium w-8">{animationSpeed}x</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
});
