import { Component, type ErrorInfo, type ReactNode } from 'react';
import type { Logger } from '../types';
import { getLogger } from '../services/logger';

export interface AppErrorBoundaryProps {
  children: ReactNode;
  /** Renders the fallback UI. Receives the caught error and a reset callback. */
  fallback: (error: Error, reset: () => void) => ReactNode;
  /** Optional logger override; defaults to the globally configured logger (no-op unless set). */
  logger?: Logger;
  /** Optional additional callback, e.g. for a future telemetry package. */
  onError?: (error: Error, info: ErrorInfo) => void;
}

interface AppErrorBoundaryState {
  error: Error | null;
}

/**
 * Generic React error boundary. Contains no application, entity, or route awareness —
 * callers supply their own fallback UI and may opt into the logging extension point.
 */
export class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
  state: AppErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): AppErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    const logger = this.props.logger ?? getLogger();
    logger.error(error.message, error, { componentStack: info.componentStack ?? undefined });
    this.props.onError?.(error, info);
  }

  private reset = (): void => {
    this.setState({ error: null });
  };

  render(): ReactNode {
    if (this.state.error) {
      return this.props.fallback(this.state.error, this.reset);
    }
    return this.props.children;
  }
}
