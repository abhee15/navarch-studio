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
  for (let i = 0; i < numStations; i++) {
    const x = stations[i]; // Station x-position

    for (let j = 0; j < numWaterlines; j++) {
      const z = waterlines[j]; // Waterline z-position
      const halfBreadth = offsets[i][j]; // Half-breadth (y-offset)

      // Port side (negative y)
      vertices.push(x, -halfBreadth, z);

      // Starboard side (positive y)
      vertices.push(x, halfBreadth, z);
    }
  }

  // Generate triangle indices
  // For each quad (station[i] to station[i+1], waterline[j] to waterline[j+1]), create 2 triangles
  const verticesPerStation = numWaterlines * 2; // Port + starboard

  for (let i = 0; i < numStations - 1; i++) {
    for (let j = 0; j < numWaterlines - 1; j++) {
      // Port side quad
      const a = i * verticesPerStation + j * 2;
      const b = a + verticesPerStation;
      const c = a + 2;
      const d = b + 2;

      // Triangle 1 (a, c, b)
      indices.push(a, c, b);
      // Triangle 2 (b, c, d)
      indices.push(b, c, d);

      // Starboard side quad
      const a2 = a + 1;
      const b2 = b + 1;
      const c2 = c + 1;
      const d2 = d + 1;

      // Triangle 1 (a2, b2, c2)
      indices.push(a2, b2, c2);
      // Triangle 2 (b2, d2, c2)
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
