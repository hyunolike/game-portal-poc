import type { ActionState } from "@/lib/action";

export function FormMessage({ state }: { state: ActionState }) {
  if (state.error) {
    const fields = Object.entries(state.fieldErrors ?? {});
    return (
      <div className="notice notice--error" role="alert">
        <p>{state.error}</p>
        {fields.length > 0 && (
          <ul>
            {fields.map(([field, errors]) => (
              <li key={field}>
                <code>{field}</code> {errors.join(" ")}
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  }
  if (state.ok) {
    return (
      <div className="notice notice--ok" role="status">
        <p>{state.ok}</p>
      </div>
    );
  }
  return null;
}
