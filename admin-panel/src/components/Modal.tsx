import { X } from 'lucide-react'
import { useEffect, useRef, type ReactNode } from 'react'
import { createPortal } from 'react-dom'

type ModalProps = {
  readonly open: boolean
  readonly title: string
  readonly description?: string
  readonly onClose: () => void
  readonly children: ReactNode
  readonly size?: 'lg' | 'xl'
}

export function Modal({ open, title, description, onClose, children, size = 'lg' }: ModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null)
  const onCloseRef = useRef(onClose)
  onCloseRef.current = onClose

  useEffect(() => {
    if (!open) return
    const previousOverflow = document.body.style.overflow
    const previousActive = document.activeElement instanceof HTMLElement ? document.activeElement : null
    document.body.style.overflow = 'hidden'
    dialogRef.current?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onCloseRef.current()
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', handleKeyDown)
      previousActive?.focus()
    }
  }, [open])

  if (!open) return null

  return createPortal(
    <div className="fixed inset-0 z-[90] flex min-h-screen items-center justify-center overflow-y-auto bg-[color:var(--gov-navy)]/55 p-4 backdrop-blur-sm sm:p-6" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
      <div ref={dialogRef} role="dialog" aria-modal="true" aria-labelledby="modal-title" tabIndex={-1} className={`w-full ${size === 'xl' ? 'max-w-5xl' : 'max-w-2xl'} overflow-hidden rounded-[2rem] border border-white/80 bg-white shadow-[0_28px_90px_rgba(8,35,65,0.28)] outline-none`}>
        <div className="flex items-start justify-between gap-5 border-b border-border bg-gradient-to-br from-[color:var(--gov-navy)] to-[color:var(--gov-blue-dark)] px-6 py-5 text-white sm:px-8">
          <div>
            <h2 id="modal-title" className="text-xl font-bold tracking-tight">{title}</h2>
            {description && <p className="mt-1.5 max-w-xl text-sm leading-6 text-white/75">{description}</p>}
          </div>
          <button type="button" onClick={onClose} aria-label="Close dialog" className="rounded-full p-2 text-white/70 transition hover:bg-white/10 hover:text-white"><X className="h-5 w-5" /></button>
        </div>
        <div className="max-h-[calc(100vh-10rem)] overflow-y-auto p-6 sm:p-8">{children}</div>
      </div>
    </div>,
    document.body,
  )
}
