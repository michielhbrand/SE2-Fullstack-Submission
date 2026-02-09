/**
 * Extracts error message from an API error response
 * @param error - The error object from the API call
 * @param fallbackMessage - Default message if no specific error details are found
 * @returns A user-friendly error message
 */
export function getErrorMessage(
  error: any,
  fallbackMessage: string = "An error occurred",
): string {
  if (error?.errors && typeof error.errors === "object") {
    const validationErrors = extractValidationErrors(error.errors);
    if (validationErrors.length > 0) {
      return validationErrors.join(", ");
    }
  }

  if (error?.result?.errors && typeof error.result.errors === "object") {
    const validationErrors = extractValidationErrors(error.result.errors);
    if (validationErrors.length > 0) {
      return validationErrors.join(", ");
    }
  }

  if (error?.detail) {
    return error.detail;
  }

  if (error?.result?.detail) {
    return error.result.detail;
  }

  if (error?.title) {
    return error.title;
  }

  if (error?.result?.title) {
    return error.result.title;
  }

  if (error?.response?.data?.detail) {
    return error.response.data.detail;
  }

  if (error?.response?.data?.title) {
    return error.response.data.title;
  }

  if (error?.message) {
    return error.message;
  }

  return fallbackMessage;
}

/**
 * Extracts validation error messages from the errors object
 * @param errors - The errors object from the API response
 * @returns An array of error messages
 */
function extractValidationErrors(errors: Record<string, any>): string[] {
  const messages: string[] = [];

  for (const [_field, fieldErrors] of Object.entries(errors)) {
    if (Array.isArray(fieldErrors)) {
      messages.push(...fieldErrors);
    } else if (typeof fieldErrors === "string") {
      messages.push(fieldErrors);
    }
  }

  return messages;
}
