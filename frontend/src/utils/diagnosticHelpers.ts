import type { SolverDiagnostics } from "../types/sizing";

/**
 * Infers which wizard step is most likely causing the issue based on solver diagnostics.
 *
 * Step mapping:
 * - Step 1: Vessel Requirements (cargo basis, cargo value, cargo density)
 * - Step 2: Speed & Environment (service speed, sea margin, environment)
 * - Step 3: Constraints (beam, draft, LOA, airdraft limits)
 * - Step 4: Options & Review (solver options, review)
 *
 * @param diagnostics - The solver diagnostics from the failed run
 * @returns The wizard step number (1-4) to start at
 */
export function inferProblematicStep(diagnostics?: SolverDiagnostics): number {
  if (!diagnostics) {
    return 1; // Default to first step if no diagnostics
  }

  // Check for speed/Froude number issues
  if (diagnostics.estimatedFroudeNumber !== undefined) {
    const fn = diagnostics.estimatedFroudeNumber;

    // Extreme Froude numbers suggest speed issues
    if (fn > 0.5 || fn < 0.1) {
      return 2; // Speed & Environment step
    }
  }

  // Check suggestions for keywords
  const suggestionsText = diagnostics.suggestions.join(" ").toLowerCase();

  if (suggestionsText.includes("speed") || suggestionsText.includes("froude")) {
    return 2; // Speed & Environment step
  }

  if (
    suggestionsText.includes("beam") ||
    suggestionsText.includes("draft") ||
    suggestionsText.includes("constraint") ||
    suggestionsText.includes("loa")
  ) {
    return 3; // Constraints step
  }

  if (
    suggestionsText.includes("cargo") ||
    suggestionsText.includes("displacement") ||
    suggestionsText.includes("capacity")
  ) {
    return 1; // Vessel Requirements step
  }

  // Check failure reasons
  const failureReasonsText = diagnostics.failureReasons.join(" ").toLowerCase();

  if (
    failureReasonsText.includes("beam") ||
    failureReasonsText.includes("draft") ||
    failureReasonsText.includes("constraint")
  ) {
    return 3; // Constraints step
  }

  if (failureReasonsText.includes("speed") || failureReasonsText.includes("froude")) {
    return 2; // Speed & Environment step
  }

  // Check if displacement is extremely large or small relative to typical vessels
  if (diagnostics.targetDisplacementT) {
    const displacementT = diagnostics.targetDisplacementT;

    // Very small displacement (< 100t) or very large (> 500,000t) suggest cargo issues
    if (displacementT < 100 || displacementT > 500000) {
      return 1; // Vessel Requirements step
    }
  }

  // If many families failed closure without specific hints, might be constraints
  if (diagnostics.familiesFailedClosure > diagnostics.totalFamiliesConsidered / 2) {
    // More than half failed - likely constraints are too tight
    return 3;
  }

  // Default to first step if we can't determine the issue
  return 1;
}

/**
 * Gets a human-readable explanation of why a particular step was selected
 */
export function getStepInferenceReason(diagnostics?: SolverDiagnostics): string {
  if (!diagnostics) {
    return "Review your vessel requirements";
  }

  const step = inferProblematicStep(diagnostics);

  switch (step) {
    case 1:
      return "Review your cargo capacity and vessel requirements";
    case 2:
      return "Review your speed requirements and environmental conditions";
    case 3:
      return "Review your physical constraints (beam, draft, LOA)";
    case 4:
      return "Review your solver options";
    default:
      return "Review all parameters";
  }
}
