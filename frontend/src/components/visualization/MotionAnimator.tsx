import { useRef, useMemo, useState } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import type { RaoResultDto, SeaStateDto } from "../../types/seakeeping";

interface MotionAnimatorProps {
  children: React.ReactNode;
  raoResults: RaoResultDto;
  seaState: SeaStateDto;
  isPlaying: boolean;
  speedMultiplier?: number; // 1.0 = real-time, 2.0 = 2x speed, etc.
}

interface MotionFrame {
  heave: number; // m
  pitch: number; // radians
  roll: number; // radians
}

/**
 * Animates hull motion based on RAO results and sea state.
 * Pre-computes time series for smooth 60 FPS playback.
 */
export function MotionAnimator({
  children,
  raoResults,
  seaState,
  isPlaying,
  speedMultiplier = 1.0,
}: MotionAnimatorProps) {
  const groupRef = useRef<THREE.Group>(null);
  const [currentTime, setCurrentTime] = useState(0);

  // Pre-compute 60 seconds of motion at 30 FPS
  const timeSeriesData = useMemo(() => {
    return generateTimeSeries(raoResults, seaState, 60, 30);
  }, [raoResults, seaState]);

  // Animation frame callback
  useFrame((_state, delta) => {
    if (!isPlaying || !groupRef.current || timeSeriesData.length === 0) return;

    // Advance time
    const newTime = (currentTime + delta * speedMultiplier) % 60;
    setCurrentTime(newTime);

    // Get motion at current time
    const frameIndex = Math.floor(newTime * 30) % timeSeriesData.length;
    const motion = timeSeriesData[frameIndex];

    // Apply transformations
    groupRef.current.position.z = motion.heave; // Vertical translation
    groupRef.current.rotation.y = motion.pitch; // Pitch rotation (around y-axis)
    groupRef.current.rotation.x = motion.roll; // Roll rotation (around x-axis)
  });

  return <group ref={groupRef}>{children}</group>;
}

/**
 * Generate time series of motions using RAOs and wave spectrum.
 * Uses linear superposition of regular wave components.
 */
function generateTimeSeries(
  raos: RaoResultDto,
  seaState: SeaStateDto,
  duration: number,
  fps: number
): MotionFrame[] {
  const frames = duration * fps;
  const timeSeries: MotionFrame[] = [];

  // Generate random phases for each frequency component
  const phases = raos.frequency.map(() => Math.random() * 2 * Math.PI);

  // For each frame
  for (let frame = 0; frame < frames; frame++) {
    const t = frame / fps;
    let heave = 0;
    let pitch = 0;
    let roll = 0;

    // Sum over frequency components
    for (let i = 0; i < raos.frequency.length; i++) {
      const omega = raos.frequency[i];
      const waveAmp = getWaveAmplitude(seaState, omega);

      // Heave: sum of sinusoidal components
      heave += raos.heaveRao[i] * waveAmp * Math.cos(omega * t + phases[i]);

      // Pitch (convert to radians)
      pitch += raos.pitchRao[i] * waveAmp * Math.cos(omega * t + phases[i]);

      // Roll (convert to radians)
      roll += raos.rollRao[i] * waveAmp * Math.cos(omega * t + phases[i]);
    }

    timeSeries.push({ heave, pitch, roll });
  }

  return timeSeries;
}

/**
 * Get wave amplitude at a given frequency from sea state spectrum.
 * For JONSWAP/PM, amplitude ζₐ(ω) = √(2 S(ω) Δω)
 */
function getWaveAmplitude(seaState: SeaStateDto, omega: number): number {
  const Hs = seaState.significantHeight;
  const Tp = seaState.peakPeriod;
  const omegaP = (2 * Math.PI) / Tp;

  // Simplified spectrum amplitude (not exact, but reasonable for animation)
  // S(ω) ≈ Hs² * f(ω/ωₚ) where f is the spectral shape
  const ratio = omega / omegaP;

  // Simplified Pierson-Moskowitz shape
  const spectrumValue =
    ((5 * Math.pow(Hs, 2) * Math.pow(omegaP, 4)) / (16 * Math.pow(omega, 5))) *
    Math.exp(-1.25 * Math.pow(ratio, -4));

  // Amplitude from spectrum (assuming Δω = 0.05 rad/s)
  const deltaOmega = 0.05;
  const amplitude = Math.sqrt(2 * spectrumValue * deltaOmega);

  return amplitude;
}
