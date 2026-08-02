export interface LoadingScreenProps {
  label?: string;
}

/** Generic full-viewport loading indicator. Used by AuthGuard while auth state initializes. */
export function LoadingScreen({ label = 'Loading…' }: LoadingScreenProps) {
  return (
    <div className="fixed inset-0 flex items-center justify-center">
      <div className="flex flex-col items-center gap-2">
        <div className="h-8 w-8 rounded-full border-2 border-gray-300 border-t-blue-600 animate-spin" />
        <span className="text-sm text-gray-500">{label}</span>
      </div>
    </div>
  );
}
