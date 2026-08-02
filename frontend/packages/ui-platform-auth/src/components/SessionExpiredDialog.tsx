import type { ReactNode } from 'react';
import { authStore } from '../stores/authStore';

export interface SessionExpiredDialogProps {
  /** Called when the user acknowledges (e.g. navigate to login). Required — dialog has no default action. */
  onAcknowledge: () => void;
  title?: ReactNode;
  message?: ReactNode;
}

/**
 * Renders nothing until the session ends due to expiry (not explicit logout), then shows a
 * blocking dialog. Mount once near the app root, alongside AuthProvider.
 */
export function SessionExpiredDialog({
  onAcknowledge,
  title = 'Session expired',
  message = 'Your session has expired. Please sign in again to continue.',
}: SessionExpiredDialogProps) {
  const sessionExpired = authStore((s) => s.sessionExpired);

  if (!sessionExpired) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-lg shadow-lg p-6 max-w-sm w-full flex flex-col gap-3">
        <h2 className="text-lg font-semibold">{title}</h2>
        <p className="text-sm text-gray-600">{message}</p>
        <button
          type="button"
          onClick={onAcknowledge}
          className="self-end rounded px-3 py-2 bg-blue-600 text-white"
        >
          Sign in
        </button>
      </div>
    </div>
  );
}
