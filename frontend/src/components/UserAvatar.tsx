import { User } from "lucide-react";

interface UserAvatarProps {
  name?: string;
  email?: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

/**
 * Generate initials from name
 * Takes first letter of first name and first letter of last name
 * Falls back to first two characters if only one name part
 */
const getInitials = (name: string): string => {
  const parts = name.trim().split(" ");
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
};

/**
 * Generate consistent color from string
 * Uses a hash function to ensure same string always gets same color
 */
const getAvatarColor = (str: string): string => {
  const colors = [
    "bg-blue-500",
    "bg-purple-500",
    "bg-pink-500",
    "bg-indigo-500",
    "bg-green-500",
    "bg-yellow-500",
    "bg-red-500",
    "bg-teal-500",
  ];
  const hash = str.split("").reduce((acc, char) => acc + char.charCodeAt(0), 0);
  return colors[hash % colors.length];
};

export const UserAvatar = ({ name, email, size = "md", className = "" }: UserAvatarProps) => {
  const sizeClasses = {
    sm: "h-8 w-8 text-xs",
    md: "h-10 w-10 text-sm",
    lg: "h-12 w-12 text-base",
  };

  // Determine what to display
  const displayName = name || email || "User";
  const initials = getInitials(displayName);
  const colorClass = getAvatarColor(displayName);

  // If no name or email, show generic user icon
  if (!name && !email) {
    return (
      <div
        className={`${sizeClasses[size]} ${colorClass} rounded-full flex items-center justify-center ${className}`}
        aria-label="User avatar"
      >
        <User className="h-5 w-5 text-white" />
      </div>
    );
  }

  return (
    <div
      className={`${sizeClasses[size]} ${colorClass} rounded-full flex items-center justify-center font-semibold text-white ${className}`}
      aria-label={`Avatar for ${displayName}`}
      title={displayName}
    >
      {initials}
    </div>
  );
};
