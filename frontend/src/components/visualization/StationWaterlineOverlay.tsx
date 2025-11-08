import { useMemo } from "react";
import * as THREE from "three";
import { Line } from "@react-three/drei";
import type { OffsetsGrid } from "../../types/hydrostatics";

interface StationWaterlineOverlayProps {
  offsetsGrid: OffsetsGrid;
  showStations?: boolean;
  showWaterlines?: boolean;
}

/**
 * Renders station and waterline curves as overlay lines on 3D hull.
 */
export function StationWaterlineOverlay({
  offsetsGrid,
  showStations = true,
  showWaterlines = true,
}: StationWaterlineOverlayProps) {
  const { stations, waterlines, offsets } = offsetsGrid;

  // Generate station lines (vertical curves at each x-position)
  const stationLines = useMemo(() => {
    if (!showStations || stations.length === 0) return [];

    return stations.map((stationX, stIdx) => {
      const points: THREE.Vector3[] = [];

      // Trace station curve from keel to deck (each waterline)
      for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx++) {
        const z = waterlines[wlIdx];
        const y = offsets[stIdx][wlIdx];

        // Port side
        points.push(new THREE.Vector3(stationX, -y, z));
      }

      // Mirror for starboard (reverse order for continuous line)
      for (let wlIdx = waterlines.length - 1; wlIdx >= 0; wlIdx--) {
        const z = waterlines[wlIdx];
        const y = offsets[stIdx][wlIdx];

        // Starboard side
        points.push(new THREE.Vector3(stationX, y, z));
      }

      return { key: `station-${stIdx}`, points };
    });
  }, [stations, waterlines, offsets, showStations]);

  // Generate waterline curves (horizontal curves at each z-position)
  const waterlineLines = useMemo(() => {
    if (!showWaterlines || waterlines.length === 0) return [];

    return waterlines.map((wlZ, wlIdx) => {
      const points: THREE.Vector3[] = [];

      // Trace waterline from stern to bow (each station) - Port side
      for (let stIdx = 0; stIdx < stations.length; stIdx++) {
        const x = stations[stIdx];
        const y = offsets[stIdx][wlIdx];

        points.push(new THREE.Vector3(x, -y, wlZ)); // Port
      }

      // Continue from bow to stern on starboard side
      for (let stIdx = stations.length - 1; stIdx >= 0; stIdx--) {
        const x = stations[stIdx];
        const y = offsets[stIdx][wlIdx];

        points.push(new THREE.Vector3(x, y, wlZ)); // Starboard
      }

      // Close the loop
      if (points.length > 0) {
        points.push(points[0]);
      }

      return { key: `waterline-${wlIdx}`, points };
    });
  }, [stations, waterlines, offsets, showWaterlines]);

  return (
    <group>
      {/* Station lines */}
      {stationLines.map((line) => (
        <Line
          key={line.key}
          points={line.points}
          color="#3b82f6"
          lineWidth={1.5}
          transparent
          opacity={0.8}
        />
      ))}

      {/* Waterline curves */}
      {waterlineLines.map((line) => (
        <Line
          key={line.key}
          points={line.points}
          color="#06b6d4"
          lineWidth={1.5}
          transparent
          opacity={0.8}
        />
      ))}
    </group>
  );
}
