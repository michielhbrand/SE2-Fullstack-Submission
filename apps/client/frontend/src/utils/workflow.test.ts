import { describe, it, expect } from 'vitest'
import { getStatusLabel, getStatusColor, getEventLabel, getEventPdfType } from './workflow'

describe('getStatusLabel', () => {
  it('returns human-readable label for known statuses', () => {
    expect(getStatusLabel('Draft')).toBe('Draft')
    expect(getStatusLabel('PendingApproval')).toBe('Pending Approval')
    expect(getStatusLabel('InvoiceCreated')).toBe('Invoice Created')
    expect(getStatusLabel('SentForPayment')).toBe('Sent for Payment')
    expect(getStatusLabel('Paid')).toBe('Paid')
  })

  it('returns the raw value as fallback for unknown status', () => {
    expect(getStatusLabel('SomeUnknownStatus')).toBe('SomeUnknownStatus')
  })
})

describe('getStatusColor', () => {
  it('returns colour classes for known statuses', () => {
    expect(getStatusColor('Draft')).toContain('gray')
    expect(getStatusColor('Rejected')).toContain('red')
    expect(getStatusColor('Paid')).toContain('emerald')
  })

  it('returns default grey classes for unknown status', () => {
    expect(getStatusColor('Unknown')).toBe('bg-gray-100 text-gray-800')
  })
})

describe('getEventLabel', () => {
  it('returns human-readable label for known event types', () => {
    expect(getEventLabel('QuoteCreated')).toBe('Quote Created')
    expect(getEventLabel('SentForApproval')).toBe('Sent for Approval')
    expect(getEventLabel('ConvertedToInvoice')).toBe('Converted to Invoice')
    expect(getEventLabel('MarkedAsPaid')).toBe('Marked as Paid')
  })

  it('returns raw event type as fallback for unknown', () => {
    expect(getEventLabel('CustomEvent')).toBe('CustomEvent')
  })
})

describe('getEventPdfType', () => {
  it('returns "quote" for quote-related events', () => {
    expect(getEventPdfType('QuoteCreated')).toBe('quote')
    expect(getEventPdfType('SentForApproval')).toBe('quote')
    expect(getEventPdfType('ResentForApproval')).toBe('quote')
    expect(getEventPdfType('Approved')).toBe('quote')
    expect(getEventPdfType('Rejected')).toBe('quote')
    expect(getEventPdfType('QuoteModified')).toBe('quote')
  })

  it('returns "invoice" for invoice-related events', () => {
    expect(getEventPdfType('InvoiceCreated')).toBe('invoice')
    expect(getEventPdfType('ConvertedToInvoice')).toBe('invoice')
    expect(getEventPdfType('SentForPayment')).toBe('invoice')
    expect(getEventPdfType('ResentForPayment')).toBe('invoice')
    expect(getEventPdfType('MarkedAsPaid')).toBe('invoice')
  })

  it('returns null for unrelated events', () => {
    expect(getEventPdfType('Cancelled')).toBeNull()
    expect(getEventPdfType('Terminated')).toBeNull()
    expect(getEventPdfType('UnknownEvent')).toBeNull()
  })
})
