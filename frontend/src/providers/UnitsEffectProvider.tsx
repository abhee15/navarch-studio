import { PropsWithChildren, useEffect } from "react";
import { autorun } from "mobx";
import { settingsStore } from "../stores/SettingsStore";

export function UnitsEffectProvider({ children }: PropsWithChildren) {
  useEffect(() => {
    const dispose = autorun(() => {
      const units = settingsStore.preferredUnits;
      // Broadcast a custom event so pages/components can refresh if needed
      window.dispatchEvent(new CustomEvent("units:changed", { detail: { units } }));
    });
    return () => dispose();
  }, []);

  return <>{children}</>;
}
