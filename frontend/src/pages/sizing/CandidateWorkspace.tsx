import React, { useEffect } from "react";
import { observer } from "mobx-react-lite";
import { useParams, useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";

export const CandidateWorkspace: React.FC = observer(() => {
  const { candidateId } = useParams<{ candidateId: string }>();
  const navigate = useNavigate();
  const { sizingStore } = useStore();

  const candidate = sizingStore.selectedCandidate;

  useEffect(() => {
    if (candidateId && (!candidate || candidate.id !== candidateId)) {
      // Load candidate details
      // For now, candidate should already be in store from results page
      const found = sizingStore.candidates.find((c) => c.id === candidateId);
      if (found) {
        sizingStore.selectCandidate(candidateId);
      }
    }
  }, [candidateId, candidate, sizingStore]);

  if (!candidate) {
    return (
      <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
        <AppHeader />
        <main className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <p className="text-gray-600 dark:text-gray-400">Loading candidate...</p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  // Parse flags
  let flags: string[] = [];
  try {
    flags = JSON.parse(candidate.flagsJson);
  } catch {
    // Ignore
  }

  const hasConstraintViolations = flags.some(
    (f) => f.includes("constrained") || f.includes("exceeded")
  );

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader />

      <main className="flex-1">
        {/* Toolbar */}
        <div className="border-b border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
          <div className="mx-auto max-w-7xl px-4 py-4 sm:px-6 lg:px-8">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-4">
                <Button variant="outline" size="sm" onClick={() => navigate(-1)}>
                  ← Back
                </Button>
                <div>
                  <h2 className="text-xl font-bold text-gray-900 dark:text-white capitalize">
                    {candidate.hullFamily.replace("_", " ")}
                  </h2>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Rank #{candidate.rank} • Score: {(candidate.score * 100).toFixed(1)}%
                  </p>
                </div>
              </div>

              <div className="flex space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => sizingStore.exportCandidate(candidate.id, "json")}
                >
                  Export JSON
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => sizingStore.exportCandidate(candidate.id, "csv")}
                >
                  Export CSV
                </Button>
                <Button
                  size="sm"
                  onClick={async () => {
                    const vesselId = await sizingStore.pushToHydrostatics(candidate.id);
                    navigate(`/hydrostatics/vessels/${vesselId}/workspace`);
                  }}
                >
                  Push to Hydrostatics
                </Button>
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            {/* Left: Dimensions & Coefficients */}
            <div className="space-y-6">
              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">
                  Principal Dimensions
                </h3>
                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Lpp:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.lppM.toFixed(2)} m
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Lwl:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.lwlM.toFixed(2)} m
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">LOA:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.loaM.toFixed(2)} m
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Beam:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.bM.toFixed(2)} m
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Draft:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.tM.toFixed(2)} m
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Depth:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.dM.toFixed(2)} m
                    </dd>
                  </div>
                </dl>
              </div>

              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">
                  Form Coefficients
                </h3>
                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Block Coeff (Cb):</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.cb.toFixed(4)}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Prismatic (Cp):</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.cp.toFixed(4)}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Waterplane (Cwp):</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.cwp.toFixed(4)}
                    </dd>
                  </div>
                  {candidate.cm && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">Midship (Cm):</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.cm.toFixed(4)}
                      </dd>
                    </div>
                  )}
                </dl>
              </div>
            </div>

            {/* Center: Performance KPIs */}
            <div className="space-y-6">
              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">Performance</h3>
                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Displacement:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.displacementT.toFixed(0)} tonnes
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Froude Number:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.fn.toFixed(4)}
                    </dd>
                  </div>
                  {candidate.lwlOverLambda && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">Lwl/λ:</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.lwlOverLambda.toFixed(2)}
                      </dd>
                    </div>
                  )}
                  {candidate.ehpKw && (
                    <>
                      <div className="flex justify-between">
                        <dt className="text-gray-600 dark:text-gray-400">EHP:</dt>
                        <dd className="font-medium text-gray-900 dark:text-white">
                          {candidate.ehpKw.toFixed(0)} kW
                        </dd>
                      </div>
                      <div className="flex justify-between">
                        <dt className="text-gray-600 dark:text-gray-400">SHP:</dt>
                        <dd className="font-medium text-gray-900 dark:text-white">
                          {candidate.shpKw?.toFixed(0)} kW
                        </dd>
                      </div>
                    </>
                  )}
                </dl>
              </div>

              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">
                  Stability (Preliminary)
                </h3>
                <dl className="space-y-3 text-sm">
                  {candidate.kbM && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">KB:</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.kbM.toFixed(2)} m
                      </dd>
                    </div>
                  )}
                  {candidate.lcbPctLpp && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">LCB (% Lpp):</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.lcbPctLpp.toFixed(2)}%
                      </dd>
                    </div>
                  )}
                  {candidate.gmEstM && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">GMt (est):</dt>
                      <dd
                        className={`font-medium ${
                          candidate.gmEstM > 1.0
                            ? "text-green-600 dark:text-green-400"
                            : "text-yellow-600 dark:text-yellow-400"
                        }`}
                      >
                        {candidate.gmEstM.toFixed(2)} m
                      </dd>
                    </div>
                  )}
                </dl>
                <p className="mt-4 text-xs text-gray-500 dark:text-gray-400">
                  Preliminary estimates. Push to Hydrostatics for detailed stability analysis.
                </p>
              </div>
            </div>

            {/* Right: Flags & Warnings */}
            <div className="space-y-6">
              {flags.length > 0 && (
                <div
                  className={`rounded-lg p-6 shadow ${
                    hasConstraintViolations
                      ? "bg-yellow-50 dark:bg-yellow-900/20"
                      : "bg-blue-50 dark:bg-blue-900/20"
                  }`}
                >
                  <h3
                    className={`font-semibold mb-4 ${
                      hasConstraintViolations
                        ? "text-yellow-900 dark:text-yellow-300"
                        : "text-blue-900 dark:text-blue-300"
                    }`}
                  >
                    {hasConstraintViolations ? "⚠️ Warnings" : "ℹ️ Flags"}
                  </h3>
                  <ul className="space-y-2 text-sm">
                    {flags.map((flag) => (
                      <li
                        key={flag}
                        className={`flex items-start space-x-2 ${
                          hasConstraintViolations
                            ? "text-yellow-800 dark:text-yellow-400"
                            : "text-blue-800 dark:text-blue-400"
                        }`}
                      >
                        <span>•</span>
                        <span className="capitalize">{flag.replace(/_/g, " ")}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">Quick Actions</h3>
                <div className="space-y-3">
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => sizingStore.exportCandidate(candidate.id, "json")}
                  >
                    📄 Export JSON
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => sizingStore.exportCandidate(candidate.id, "csv")}
                  >
                    📊 Export CSV
                  </Button>
                  <Button
                    className="w-full bg-blue-600 hover:bg-blue-700"
                    onClick={async () => {
                      const vesselId = await sizingStore.pushToHydrostatics(candidate.id);
                      navigate(`/hydrostatics/vessels/${vesselId}/workspace`);
                    }}
                  >
                    🚢 Push to Hydrostatics
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => {
                      // TODO: Implement push to resistance
                      console.log("Push to resistance");
                    }}
                  >
                    ⚡ Analyze Resistance
                  </Button>
                </div>
              </div>

              <div className="rounded-lg bg-gray-100 p-6 dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">📐 Coming Soon</h3>
                <ul className="space-y-2 text-sm text-gray-600 dark:text-gray-400">
                  <li>• 3D hull visualization (react-three-fiber)</li>
                  <li>• 2D plan/profile/sections view</li>
                  <li>• Interactive parameter sliders</li>
                  <li>• Live re-solve on adjustment</li>
                  <li>• DXF/IGES export</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  );
});
