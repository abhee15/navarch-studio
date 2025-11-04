import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import toast from "react-hot-toast";
import { getErrorMessage } from "../../types/errors";
import { benchmarksApi } from "../../services/benchmarksApi";
import type { BenchmarkCase } from "../../types/benchmarks";
import { AppHeader } from "../../components/AppHeader";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { useStore } from "../../stores";

export const BenchmarksList = observer(function BenchmarksList() {
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [cases, setCases] = useState<BenchmarkCase[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showSettings, setShowSettings] = useState(false);

  const loadCases = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await benchmarksApi.listCases();
      setCases(data);
    } catch (err) {
      setError(getErrorMessage(err));
      toast.error("Failed to load benchmarks");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCases();
  }, []);

  const handleCaseClick = (slug: string) => {
    navigate(`/benchmarks/${slug}`);
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen flex flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={<h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>}
        right={
          <>
            <button
              onClick={handleHome}
              className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
            >
              <svg className="h-4 w-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                />
              </svg>
              Home
            </button>
            <UserProfileMenu onOpenSettings={() => setShowSettings(true)} onLogout={handleLogout} />
          </>
        }
      />
      <main className="flex-1 container mx-auto px-4 py-8 max-w-6xl">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
            Benchmark Datasets
          </h1>
          <p className="text-gray-600 dark:text-gray-400">
            Reference datasets for validation: KCS, KVLCC2, and Wigley hull
          </p>
        </div>

        {error && (
          <div className="mb-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-md">
            <p className="text-red-800 dark:text-red-200">{error}</p>
          </div>
        )}

        {loading ? (
          <div className="text-center py-12">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            <p className="mt-4 text-gray-600 dark:text-gray-400">Loading benchmarks...</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {cases.map((benchmark) => (
              <div
                key={benchmark.slug}
                onClick={() => handleCaseClick(benchmark.slug)}
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md hover:shadow-lg transition-shadow cursor-pointer p-6 border border-gray-200 dark:border-gray-700"
              >
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                  {benchmark.title}
                </h2>
                {benchmark.description && (
                  <p className="text-gray-600 dark:text-gray-400 text-sm">
                    {benchmark.description}
                  </p>
                )}
                <div className="mt-4 text-blue-600 dark:text-blue-400 text-sm font-medium">
                  View details →
                </div>
              </div>
            ))}
          </div>
        )}

        {!loading && cases.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-600 dark:text-gray-400">No benchmarks available</p>
          </div>
        )}
      </main>

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});

