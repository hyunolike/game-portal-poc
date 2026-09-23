import "server-only";
import { unstable_rethrow } from "next/navigation";
import { AdminApiError, describeError } from "./errors.ts";

export interface ActionState {
  error?: string;
  fieldErrors?: Record<string, string[]>;
  ok?: string;
}

/**
 * Server Action 공통 에러 처리. redirect()/notFound() 는 내부적으로 예외를 던지므로 삼키지 않고 다시 던진다.
 */
export async function runAction(fn: () => Promise<ActionState | void>): Promise<ActionState> {
  try {
    return (await fn()) ?? {};
  } catch (error) {
    unstable_rethrow(error);
    return {
      error: describeError(error),
      fieldErrors: error instanceof AdminApiError ? error.fieldErrors : undefined,
    };
  }
}
