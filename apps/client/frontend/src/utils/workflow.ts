/** Human-readable label for a workflow status */
export const getStatusLabel = (status: string): string => {
  const labels: Record<string, string> = {
    Draft: 'Draft',
    PendingApproval: 'Pending Approval',
    Approved: 'Approved',
    Rejected: 'Rejected',
    InvoiceCreated: 'Invoice Created',
    SentForPayment: 'Sent for Payment',
    Paid: 'Paid',
    Cancelled: 'Cancelled',
    Terminated: 'Terminated',
  }
  return labels[status] ?? status
}

/** Tailwind colour classes for a workflow status badge */
export const getStatusColor = (status: string): string => {
  const colors: Record<string, string> = {
    Draft: 'bg-gray-100 text-gray-800',
    PendingApproval: 'bg-yellow-100 text-yellow-800',
    Approved: 'bg-green-100 text-green-800',
    Rejected: 'bg-red-100 text-red-800',
    InvoiceCreated: 'bg-blue-100 text-blue-800',
    SentForPayment: 'bg-purple-100 text-purple-800',
    Paid: 'bg-emerald-100 text-emerald-800',
    Cancelled: 'bg-orange-100 text-orange-800',
    Terminated: 'bg-red-200 text-red-900',
  }
  return colors[status] ?? 'bg-gray-100 text-gray-800'
}

/** Human-readable label for a workflow event type */
export const getEventLabel = (eventType: string): string => {
  const labels: Record<string, string> = {
    QuoteCreated: 'Quote Created',
    SentForApproval: 'Sent for Approval',
    Approved: 'Approved',
    Rejected: 'Rejected',
    QuoteModified: 'Quote Modified',
    ResentForApproval: 'Resent for Approval',
    ResentForPayment: 'Resent for Payment',
    ConvertedToInvoice: 'Converted to Invoice',
    InvoiceCreated: 'Invoice Created',
    SentForPayment: 'Sent for Payment',
    MarkedAsPaid: 'Marked as Paid',
    Cancelled: 'Cancelled',
    Terminated: 'Terminated',
    OverdueReminderSent: 'Overdue Reminder Sent',
  }
  return labels[eventType] ?? eventType
}

const QUOTE_PDF_EVENTS = new Set([
  'QuoteCreated', 'SentForApproval', 'ResentForApproval',
  'Approved', 'Rejected', 'QuoteModified',
])

const INVOICE_PDF_EVENTS = new Set([
  'ConvertedToInvoice', 'InvoiceCreated', 'SentForPayment',
  'ResentForPayment', 'MarkedAsPaid',
])

/** Returns which document type a timeline event is linked to, or null if none */
export const getEventPdfType = (eventType: string): 'quote' | 'invoice' | null => {
  if (QUOTE_PDF_EVENTS.has(eventType)) return 'quote'
  if (INVOICE_PDF_EVENTS.has(eventType)) return 'invoice'
  return null
}
