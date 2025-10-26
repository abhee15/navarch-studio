import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { LogOut, ShoppingBag, Settings } from "lucide-react";
import { useStore } from "../stores";
import { Button } from "../components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../components/ui/card";
import { UserSettingsDialog } from "../components/UserSettingsDialog";
import { settingsStore } from "../stores/SettingsStore";

export const DashboardPage: React.FC = observer(() => {
  const { authStore } = useStore();
  const navigate = useNavigate();
  const [showSettings, setShowSettings] = useState(false);

  useEffect(() => {
    settingsStore.loadSettings();
  }, []);

  const handleLogout = async () => {
    await authStore.logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {/* Header/Navigation */}
      <header className="border-b bg-white/80 backdrop-blur-sm sticky top-0 z-50">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <div className="rounded-lg bg-primary p-2">
                <ShoppingBag className="h-5 w-5 text-primary-foreground" />
              </div>
              <div>
                <h1 className="text-xl font-bold">Dashboard</h1>
                <p className="text-sm text-muted-foreground">
                  Welcome back, {authStore.user?.name}
                </p>
              </div>
            </div>
            <div className="flex items-center space-x-2">
              <Button variant="outline" size="sm" onClick={() => setShowSettings(true)}>
                <Settings className="mr-2 h-4 w-4" />
                Settings
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={handleLogout}
                disabled={authStore.loading}
              >
                <LogOut className="mr-2 h-4 w-4" />
                Sign out
              </Button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        {/* Quick Access Cards */}
        <div className="mb-8">
          <h2 className="text-2xl font-bold mb-4">Applications</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
            <Card
              className="hover:shadow-lg transition-shadow duration-200 cursor-pointer"
              onClick={() => navigate("/hydrostatics/vessels")}
            >
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="rounded-lg bg-blue-500/10 p-3 mb-2">
                    <svg
                      className="h-6 w-6 text-blue-600"
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
                </div>
                <CardTitle>Hydrostatics</CardTitle>
                <CardDescription>
                  Naval architecture hydrostatic calculations and analysis
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Button className="w-full" variant="outline">
                  Open Hydrostatics →
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>

      </main>

      {/* Footer */}
      <footer className="border-t bg-white/80 backdrop-blur-sm mt-12">
        <div className="container mx-auto px-4 py-6">
          <p className="text-center text-sm text-muted-foreground">
            © 2024 Your Company. All rights reserved.
          </p>
        </div>
      </footer>

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});
