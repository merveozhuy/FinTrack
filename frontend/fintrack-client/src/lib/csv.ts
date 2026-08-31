/** Builds a CSV file (Excel-friendly, UTF-8 BOM) and triggers a download in the browser. */
export function downloadCsv(filename: string, header: string[], rows: Array<Array<string | number>>): void {
  const escape = (value: string | number) => {
    const text = String(value ?? '')
    return /[",\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text
  }

  const lines = [header, ...rows].map((row) => row.map(escape).join(','))
  const blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  URL.revokeObjectURL(url)
}
