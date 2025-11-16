import { useMemo } from "react";
import * as THREE from "three";
import type { OffsetsGrid } from "../../types/hydrostatics";

interface EnhancedHull3DProps {
  offsetsGrid: OffsetsGrid;
  color?: string;
  opacity?: number;
  wireframe?: boolean;
}

/**
 * Enhanced hull rendering from actual offsets grid data.
 * Generates triangulated surface mesh from stations × waterlines.
 */
export function EnhancedHull3D({
  offsetsGrid,
  color = "#6b7280",
  opacity = 0.8,
  wireframe = false,
}: EnhancedHull3DProps) {
  const hullGeometry = useMemo(() => {
    return generateHullFromOffsets(offsetsGrid);
  }, [offsetsGrid]);

  return (
    <mesh geometry={hullGeometry} castShadow receiveShadow>
      <meshStandardMaterial
        color={color}
        opacity={opacity}
        transparent={opacity < 1}
        side={THREE.DoubleSide}
        wireframe={wireframe}
        roughness={0.6}
        metalness={0.2}
      />
    </mesh>
  );
}

/**
 * Generate hull mesh geometry from offsets grid.
 * Creates structured quad mesh with proper triangulation.
 */
function generateHullFromOffsets(offsetsGrid: OffsetsGrid): THREE.BufferGeometry {
  const geometry = new THREE.BufferGeometry();
  const vertices: number[] = [];
  const indices: number[] = [];

  const { stations, waterlines, offsets } = offsetsGrid;

  if (stations.length === 0 || waterlines.length === 0) {
    return geometry;
  }

  const numStations = stations.length;
  const numWaterlines = waterlines.length;

  // Generate vertices - For each station and waterline, create port and starboard vertices
  // Coordinate convention (match Vessel3DViewer):
  // X = transverse (half-breadth), Y = vertical (waterline height), Z = longitudinal (station position)
  for (let stationIdx = 0; stationIdx < numStations; stationIdx++) {
    const stationZ = stations[stationIdx]; // Longitudinal position (Z)

    for (let wlIdx = 0; wlIdx < numWaterlines; wlIdx++) {
      const waterlineY = waterlines[wlIdx]; // Vertical position (Y)
      const halfBreadth = offsets[stationIdx][wlIdx]; // Transverse half-breadth (X magnitude)

      // Port side (negative X)
      vertices.push(-halfBreadth, waterlineY, stationZ);

      // Starboard side (positive X)
      vertices.push(halfBreadth, waterlineY, stationZ);
    }
  }

  // Generate triangle indices
  // For each quad (station[i] to station[i+1], waterline[j] to waterline[j+1]), create 2 triangles
  const verticesPerStation = numWaterlines * 2; // Port + starboard

  for (let stationIdx = 0; stationIdx < numStations - 1; stationIdx++) {
    for (let wlIdx = 0; wlIdx < numWaterlines - 1; wlIdx++) {
      // Port side quad
      const a = stationIdx * verticesPerStation + wlIdx * 2;
      const b = a + verticesPerStation;
      const c = a + 2;
      const d = b + 2;

      // Keep consistent winding across both sides
      // Triangle 1
      indices.push(a, b, c);
      // Triangle 2
      indices.push(b, d, c);

      // Starboard side quad
      const a2 = a + 1;
      const b2 = b + 1;
      const c2 = c + 1;
      const d2 = d + 1;

      // Triangle 1
      indices.push(a2, b2, c2);
      // Triangle 2
      indices.push(b2, d2, c2);
    }
  }

  // Set geometry attributes
  geometry.setAttribute("position", new THREE.Float32BufferAttribute(vertices, 3));
  geometry.setIndex(indices);
  geometry.computeVertexNormals(); // Smooth shading
  geometry.computeBoundingBox();

  return geometry;
}
