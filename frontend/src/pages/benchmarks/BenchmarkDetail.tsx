import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import toast from "react-hot-toast";
import { getErrorMessage } from "../../types/errors";
import { benchmarksApi } from "../../services/benchmarksApi";
import type { BenchmarkCaseDetails, ValidateRequest, MetricResult } from "../../types/benchmarks";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";

export const BenchmarkDetail = observer(function BenchmarkDetail() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const [details, setDetails] = useState<BenchmarkCaseDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [validating, setValidating] = useState(false);
  const [validationResults, setValidationResults] = useState<MetricResult[]>([]);

  const [validateForm, setValidateForm] = useState<ValidateRequest>({
    fr: undefined,
    ct: undefined,
    sinkage: undefined,
    trim: undefined,
    cw: undefined,
  });

  const loadDetails = async () => {
    if (!slug) return;
    try {
      setLoading(true);
      setError(null);
      const data = await benchmarksApi.getCase(slug);
      setDetails(data);
    } catch (err) {
      setError(getErrorMessage(err));
      toast.error("Failed to load benchmark details");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDetails();
  }, [slug]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleValidate = async () => {
    if (!slug) return;
    try {
      setValidating(true);
      const results = await benchmarksApi.validate(slug, validateForm);
      setValidationResults(results);
      toast.success("Validation completed");
    } catch (err) {
      toast.error(getErrorMessage(err));
    } finally {
      setValidating(false);
    }
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  if (loading) {
    return (
      <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-900">
        <AppHeader
          left={<h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>}
          right={
            <button
              onClick={handleHome}
              className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
            >
              Dashboard
            </button>
          }
        />
        <main className="flex-1 container mx-auto px-4 py-8 max-w-6xl">
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <p className="mt-4 text-gray-600 dark:text-gray-400">Loading benchmark...</p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  if (error || !details) {
    return (
      <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-900">
        <AppHeader
          left={<h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>}
          right={
            <button
              onClick={handleHome}
              className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
            >
              Dashboard
            </button>
          }
        />
        <main className="flex-1 container mx-auto px-4 py-8 max-w-6xl">
          <div className="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md">
            <p className="text-red-800 dark:text-red-200">{error || "Benchmark not found"}</p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={<h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>}
        right={
          <button
            onClick={handleHome}
            className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
          >
            Dashboard
          </button>
        }
      />
      <main className="flex-1 container mx-auto px-4 py-8 max-w-6xl">
        <div className="mb-6">
          <button
            onClick={() => navigate("/benchmarks")}
            className="text-blue-600 dark:text-blue-400 hover:underline mb-4"
          >
            ← Back to benchmarks
          </button>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">{details.title}</h1>
          {details.description && (
            <p className="text-gray-600 dark:text-gray-400">{details.description}</p>
          )}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left column: Info */}
          <div className="space-y-6">
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                Test Points
              </h2>
              {details.testPoints.length > 0 ? (
                <div className="space-y-2">
                  {details.testPoints.map((tp) => (
                    <div key={tp.id} className="flex justify-between text-sm">
                      <span className="text-gray-600 dark:text-gray-400">
                        Fr = {tp.fr.toFixed(3)}
                      </span>
                      {tp.vm && (
                        <span className="text-gray-500 dark:text-gray-500">
                          Vm = {tp.vm.toFixed(3)} m/s
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-gray-500 dark:text-gray-500 text-sm">No test points defined</p>
              )}
            </div>

            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                Acceptance Criteria
              </h2>
              {details.metricRefs.length > 0 ? (
                <div className="space-y-3">
                  {details.metricRefs.map((ref) => (
                    <div key={ref.id} className="text-sm">
                      <div className="font-medium text-gray-900 dark:text-white">
                        {ref.metric}
                        {ref.fr !== null && ref.fr !== undefined && ` @ Fr = ${ref.fr.toFixed(3)}`}
                      </div>
                      {ref.valueNum !== null && ref.valueNum !== undefined && (
                        <div className="text-gray-600 dark:text-gray-400">
                          Reference: {ref.valueNum.toFixed(6)} {ref.unit || ""}
                        </div>
                      )}
                      {ref.tolRel !== null && ref.tolRel !== undefined && (
                        <div className="text-gray-600 dark:text-gray-400">
                          Tolerance: ±{(ref.tolRel * 100).toFixed(1)}%
                        </div>
                      )}
                      {ref.figureRef && (
                        <div className="text-blue-600 dark:text-blue-400 text-xs mt-1">
                          {ref.figureRef}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-gray-500 dark:text-gray-500 text-sm">
                  No acceptance criteria defined
                </p>
              )}
            </div>
          </div>

          {/* Right column: Validation */}
          <div className="space-y-6">
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">Validate</h2>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Froude Number (Fr)
                  </label>
                  <input
                    type="number"
                    step="0.001"
                    value={validateForm.fr ?? ""}
                    onChange={(e) =>
                      setValidateForm({
                        ...validateForm,
                        fr: e.target.value ? parseFloat(e.target.value) : undefined,
                      })
                    }
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                    placeholder="0.260"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Total Resistance (Ct)
                  </label>
                  <input
                    type="number"
                    step="0.000001"
                    value={validateForm.ct ?? ""}
                    onChange={(e) =>
                      setValidateForm({
                        ...validateForm,
                        ct: e.target.value ? parseFloat(e.target.value) : undefined,
                      })
                    }
                    className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                    placeholder="0.003711"
                  />
                </div>
                <button
                  onClick={handleValidate}
                  disabled={validating || !validateForm.fr}
                  className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {validating ? "Validating..." : "Run Validation"}
                </button>
              </div>
            </div>

            {validationResults.length > 0 && (
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 border border-gray-200 dark:border-gray-700">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                  Validation Results
                </h2>
                <div className="space-y-3">
                  {validationResults.map((result, idx) => (
                    <div
                      key={idx}
                      className={`p-3 rounded-md ${
                        result.passed
                          ? "bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800"
                          : "bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800"
                      }`}
                    >
                      <div className="flex justify-between items-center">
                        <span className="font-medium text-gray-900 dark:text-white">
                          {result.metric}
                        </span>
                        <span
                          className={`text-sm font-semibold ${
                            result.passed
                              ? "text-green-700 dark:text-green-300"
                              : "text-red-700 dark:text-red-300"
                          }`}
                        >
                          {result.passed ? "PASS" : "FAIL"}
                        </span>
                      </div>
                      {result.actual !== undefined && (
                        <div className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                          Actual: {result.actual.toFixed(6)}
                        </div>
                      )}
                      {result.reference !== undefined && (
                        <div className="text-sm text-gray-600 dark:text-gray-400">
                          Reference: {result.reference.toFixed(6)}
                        </div>
                      )}
                      {result.relError !== undefined && (
                        <div className="text-sm text-gray-600 dark:text-gray-400">
                          Error: {(result.relError * 100).toFixed(2)}%
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
});
