import type { Logger } from '../types';

/** Default logger: intentionally does nothing. Foundation ships no logging implementation. */
export const noopLogger: Logger = {
  debug: () => undefined,
  info: () => undefined,
  warn: () => undefined,
  error: () => undefined,
};

let activeLogger: Logger = noopLogger;

/**
 * Plugs a real logger implementation in (e.g. from a future UIPlatform.Logging package).
 * Call once at app bootstrap, alongside configureApp().
 */
export function setLogger(logger: Logger): void {
  activeLogger = logger;
}

/** Returns the active logger. Foundation code and future packages should log through this. */
export function getLogger(): Logger {
  return activeLogger;
}
