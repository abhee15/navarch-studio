import { useState, useEffect, useRef } from "react";
import { observer } from "mobx-react-lite";
import { Sun, Moon, Ruler, Activity, LogOut, ChevronRight } from "lucide-react";
import { useStore } from "../stores";
import { useTheme } from "../contexts/ThemeContext";
import { settingsStore } from "../stores/SettingsStore";
import { UserAvatar } from "./UserAvatar";

interface UserProfileMenuProps {
  onOpenSettings?: () => void;
  onLogout: () => void;
}

export const UserProfileMenu = observer(({ onOpenSettings, onLogout }: UserProfileMenuProps) => {
  const { authStore } = useStore();
  const { theme, toggleTheme } = useTheme();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Handle click outside to close menu
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        menuRef.current &&
        buttonRef.current &&
        !menuRef.current.contains(event.target as Node) &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false);
        buttonRef.current?.focus();
      }
    };

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
      document.addEventListener("keydown", handleEscape);
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [isOpen]);

  const handleToggleMenu = () => {
    setIsOpen(!isOpen);
  };

  const handleThemeToggle = () => {
    toggleTheme();
    // Keep menu open after theme toggle
  };

  const handleUnitSystemClick = () => {
    setIsOpen(false);
    onOpenSettings?.();
  };

  const handleSignOut = () => {
    setIsOpen(false);
    onLogout();
  };

  const user = authStore.user;
  const userName = user?.name || "User";
  const userEmail = user?.email || "";
  const preferredUnits = settingsStore.preferredUnits;

  return (
    <div className="relative">
      {/* Avatar Button */}
      <button
        ref={buttonRef}
        onClick={handleToggleMenu}
        className="flex items-center space-x-2 focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 rounded-full transition-all hover:ring-2 hover:ring-gray-300 dark:hover:ring-gray-600"
        aria-label="User menu"
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <UserAvatar name={userName} email={userEmail} size="md" />
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div
          ref={menuRef}
          className="absolute right-0 mt-2 w-72 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-[100] animate-in fade-in slide-in-from-top-2 duration-200"
          role="menu"
          aria-orientation="vertical"
        >
          {/* Profile Header */}
          <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center space-x-3">
              <UserAvatar name={userName} email={userEmail} size="lg" />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-gray-900 dark:text-white truncate">
                  {userName}
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{userEmail}</p>
              </div>
            </div>
          </div>

          {/* Menu Items */}
          <div className="py-1">
            {/* Theme Toggle */}
            <button
              onClick={handleThemeToggle}
              className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              role="menuitem"
            >
              <div className="flex items-center space-x-3">
                {theme === "light" ? (
                  <Moon className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                ) : (
                  <Sun className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                )}
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Theme</span>
              </div>
              <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">
                {theme === "light" ? "Light" : "Dark"}
              </span>
            </button>

            {/* Unit System */}
            <button
              onClick={handleUnitSystemClick}
              className="w-full px-4 py-3 flex items-center justify-between hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              role="menuitem"
            >
              <div className="flex items-center space-x-3">
                <Ruler className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Unit System
                </span>
              </div>
              <div className="flex items-center space-x-1">
                <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">
                  {preferredUnits}
                </span>
                <ChevronRight className="h-4 w-4 text-gray-400" />
              </div>
            </button>

            {/* Activity (Placeholder - Hidden on mobile) */}
            <button
              className="hidden md:flex w-full px-4 py-3 items-center justify-between hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors cursor-not-allowed opacity-60"
              role="menuitem"
              disabled
            >
              <div className="flex items-center space-x-3">
                <Activity className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Activity
                </span>
              </div>
              <span className="text-xs bg-blue-100 dark:bg-blue-900 text-blue-600 dark:text-blue-300 px-2 py-1 rounded-full font-medium">
                Coming Soon
              </span>
            </button>
          </div>

          {/* Sign Out */}
          <div className="border-t border-gray-200 dark:border-gray-700 py-1">
            <button
              onClick={handleSignOut}
              className="w-full px-4 py-3 flex items-center space-x-3 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-red-600 dark:text-red-400"
              role="menuitem"
            >
              <LogOut className="h-5 w-5" />
              <span className="text-sm font-medium">Sign out</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
});
