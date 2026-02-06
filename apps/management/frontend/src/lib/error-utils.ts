/**
 * Extracts a user-friendly error message from an API error response
 * @param error - The error object from the API call
 * @param fallbackMessage - Default message if no specific error details are found
 * @returns A user-friendly error message
 */
export function getErrorMessage(error: any, fallbackMessage: string = "An error occurred"): string {
  // Check for validation errors with errors object (ASP.NET Core validation)
  if (error?.errors && typeof error.errors === 'object') {
    const validationErrors = extractValidationErrors(error.errors);
    if (validationErrors.length > 0) {
      return validationErrors.join(', ');
    }
  }
  
  // Check if it's an ApiException with a result object containing errors
  if (error?.result?.errors && typeof error.result.errors === 'object') {
    const validationErrors = extractValidationErrors(error.result.errors);
    if (validationErrors.length > 0) {
      return validationErrors.join(', ');
    }
  }
  
  // Check if the error object itself is the Problem Details object (direct from NSwag)
  if (error?.detail) {
    return error.detail;
  }
  
  // Check if it's an ApiException with a result object containing detail
  if (error?.result?.detail) {
    return error.result.detail;
  }
  
  // Check for title in the error object itself
  if (error?.title) {
    return error.title;
  }
  
  // Check for title in the result object
  if (error?.result?.title) {
    return error.result.title;
  }
  
  // Check for error in response.data (axios error structure)
  if (error?.response?.data?.detail) {
    return error.response.data.detail;
  }
  
  if (error?.response?.data?.title) {
    return error.response.data.title;
  }
  
  // Check for a direct message property
  if (error?.message) {
    return error.message;
  }
  
  // Return the fallback message
  return fallbackMessage;
}

/**
 * Extracts validation error messages from the errors object
 * @param errors - The errors object from the API response
 * @returns An array of error messages
 */
function extractValidationErrors(errors: Record<string, any>): string[] {
  const messages: string[] = [];
  
  for (const [field, fieldErrors] of Object.entries(errors)) {
    if (Array.isArray(fieldErrors)) {
      // If it's an array of error messages
      messages.push(...fieldErrors);
    } else if (typeof fieldErrors === 'string') {
      // If it's a single error message string
      messages.push(fieldErrors);
    }
  }
  
  return messages;
}
