import type { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';

export interface LogoutButtonProps {
  children?: ReactNode;
  className?: string;
  onLoggedOut?: () => void;
}

/** Minimal logout trigger. Consuming apps supply their own label/styling via children/className. */
export function LogoutButton({ children = 'Sign out', className, onLoggedOut }: LogoutButtonProps) {
  const { logout } = useAuth();

  const handleClick = async () => {
    await logout();
    onLoggedOut?.();
  };

  return (
    <button type="button" onClick={handleClick} className={className}>
      {children}
    </button>
  );
}
