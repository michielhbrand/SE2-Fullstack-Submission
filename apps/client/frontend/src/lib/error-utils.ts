/**
 * Extracts a human-readable error message from an API error response.
 *
 * Handles 4 cases in priority order:
 * 1. ASP.NET Core validation `errors` object on the error itself
 * 2. NSwag Problem Details thrown directly (error.detail / error.title)
 * 3. Axios response.data as raw JSON string (needs JSON.parse)
 * 4. ASP.NET Core validation errors nested inside response.data
 * 5. Fallback chain: detail → title → message
 */
export function extractErrorMessage(error: unknown, defaultMessage: string): string {
  const err = error as Record<string, unknown> | null

  // Case 1: validation errors on the error object itself (highest priority)
  if (err?.errors && typeof err.errors === 'object') {
    const validationMessages: string[] = []
    for (const field in err.errors as Record<string, unknown>) {
      const fieldErrors = (err.errors as Record<string, unknown>)[field]
      if (Array.isArray(fieldErrors)) {
        validationMessages.push(...fieldErrors)
      }
    }
    if (validationMessages.length > 0) {
      return validationMessages.join('. ')
    }
  }

  // Case 2: NSwag-generated client throws the parsed Problem Details object directly
  if (err?.detail || err?.title) {
    return (err.detail as string) || (err.title as string) || defaultMessage
  }

  // Case 3: Axios error with response data
  let errorData = (err?.response as Record<string, unknown> | undefined)?.data

  // If data is a string, try to parse it as JSON
  if (typeof errorData === 'string') {
    try {
      errorData = JSON.parse(errorData)
    } catch {
      // Keep errorData as the raw string if JSON parsing fails
    }
  }

  const data = errorData as Record<string, unknown> | undefined

  // Case 4: Check for ASP.NET Core validation errors in response data
  if (data?.errors && typeof data.errors === 'object') {
    const validationMessages: string[] = []
    for (const field in data.errors as Record<string, unknown>) {
      const fieldErrors = (data.errors as Record<string, unknown>)[field]
      if (Array.isArray(fieldErrors)) {
        validationMessages.push(...fieldErrors)
      }
    }
    if (validationMessages.length > 0) {
      return validationMessages.join('. ')
    }
  }

  // Fallback chain
  return (data?.detail as string)
    || (data?.title as string)
    || (data?.message as string)
    || (err?.message as string)
    || defaultMessage
}
