import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { Vessel } from "../../types/hydrostatics";
import CreateVesselDialog from "../../components/hydrostatics/CreateVesselDialog";
import { useStore } from "../../stores";
import { ThemeToggle } from "../../components/ThemeToggle";
import { Footer } from "../../components/Footer";

export const VesselsList = observer(function VesselsList() {
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [vessels, setVessels] = useState<Vessel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);

  const loadVessels = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await vesselsApi.list();
      setVessels(data.vessels);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load vessels");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadVessels();
  }, []);

  const handleVesselCreated = () => {
    setIsCreateDialogOpen(false);
    loadVessels();
  };

  const handleVesselClick = (vesselId: string) => {
    navigate(`/hydrostatics/vessels/${vesselId}`);
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffHours < 1) return "Just now";
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? "s" : ""} ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Main Navigation Header */}
      <header className="border-b border-border bg-card/80 backdrop-blur-sm sticky top-0 z-50">
        <div className="px-4 py-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>
            </div>
            <div className="flex items-center space-x-2">
              <ThemeToggle />
              <button
                onClick={handleHome}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
              >
                <svg
                  className="h-4 w-4 mr-1.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                  />
                </svg>
                Home
              </button>
              <button
                onClick={handleLogout}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
              >
                <svg
                  className="h-4 w-4 mr-1.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
                  />
                </svg>
                Sign out
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Page Header */}
      <div className="bg-card shadow border-b border-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-card-foreground">Hydrostatics</h1>
              <p className="mt-1 text-sm text-muted-foreground">
                Manage vessels and compute hydrostatic properties
              </p>
            </div>
            <button
              onClick={() => setIsCreateDialogOpen(true)}
              className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
            >
              <svg
                className="-ml-1 mr-2 h-5 w-5"
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fillRule="evenodd"
                  d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                  clipRule="evenodd"
                />
              </svg>
              New Vessel
            </button>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1">
        {error && (
          <div
            className="mb-4 bg-destructive/10 border border-destructive/30 text-destructive px-4 py-3 rounded relative"
            role="alert"
          >
            <strong className="font-bold">Error: </strong>
            <span className="block sm:inline">{error}</span>
          </div>
        )}

        {vessels.length === 0 ? (
          <div className="text-center py-12">
            <svg
              className="mx-auto h-12 w-12 text-muted-foreground"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 13h6m-3-3v6m-9 1V7a2 2 0 012-2h6l2 2h6a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z"
              />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-foreground">No vessels</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Get started by creating a new vessel.
            </p>
            <div className="mt-6">
              <button
                onClick={() => setIsCreateDialogOpen(true)}
                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
              >
                <svg
                  className="-ml-1 mr-2 h-5 w-5"
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                >
                  <path
                    fillRule="evenodd"
                    d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                    clipRule="evenodd"
                  />
                </svg>
                New Vessel
              </button>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {vessels.map((vessel) => (
              <div
                key={vessel.id}
                onClick={() => handleVesselClick(vessel.id)}
                className="bg-card overflow-hidden shadow rounded-lg hover:shadow-md transition-shadow duration-200 cursor-pointer border border-border"
              >
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <svg
                        className="h-10 w-10 text-primary"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                        />
                      </svg>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-muted-foreground truncate">
                          Vessel
                        </dt>
                        <dd className="text-lg font-semibold text-card-foreground">
                          {vessel.name}
                        </dd>
                      </dl>
                    </div>
                  </div>
                  {vessel.description && (
                    <p className="mt-3 text-sm text-muted-foreground line-clamp-2">
                      {vessel.description}
                    </p>
                  )}
                  <div className="mt-4 grid grid-cols-3 gap-2 text-xs">
                    <div>
                      <span className="text-muted-foreground">Lpp:</span>
                      <span className="ml-1 font-medium text-card-foreground">{vessel.lpp}m</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">B:</span>
                      <span className="ml-1 font-medium text-card-foreground">{vessel.beam}m</span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">T:</span>
                      <span className="ml-1 font-medium text-card-foreground">
                        {vessel.designDraft}m
                      </span>
                    </div>
                  </div>
                  <div className="mt-3 text-xs text-muted-foreground">
                    Updated {formatDate(vessel.updatedAt)}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Footer */}
      <Footer />

      {/* Create Vessel Dialog */}
      {isCreateDialogOpen && (
        <CreateVesselDialog
          isOpen={isCreateDialogOpen}
          onClose={() => setIsCreateDialogOpen(false)}
          onVesselCreated={handleVesselCreated}
        />
      )}
    </div>
  );
});

export default VesselsList;
