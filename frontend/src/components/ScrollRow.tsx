import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode } from 'react'

// Horizontal row moved with prev/next buttons instead of a scrollbar.
export function ScrollRow({ children, className = '' }: { children: ReactNode; className?: string }) {
  const ref = useRef<HTMLDivElement>(null)
  const [edge, setEdge] = useState({ left: false, right: false })
  const update = () => {
    const el = ref.current
    if (el) setEdge({ left: el.scrollLeft > 4, right: el.scrollLeft + el.clientWidth < el.scrollWidth - 4 })
  }
  useEffect(() => {
    update()
    const el = ref.current
    if (!el) return
    const ro = new ResizeObserver(update)
    ro.observe(el)
    return () => ro.disconnect()
  })
  const go = (dir: 1 | -1) => ref.current?.scrollBy({ left: dir * ref.current.clientWidth * 0.8, behavior: 'smooth' })
  const btn = 'absolute top-1/2 z-10 flex h-9 w-9 -translate-y-1/2 cursor-pointer items-center justify-center rounded-full border border-border bg-white text-[color:var(--gov-navy)] shadow-md transition hover:bg-secondary'
  return (
    <div className="relative">
      <div ref={ref} onScroll={update} className={`hide-scrollbar flex snap-x gap-3 overflow-x-auto ${className}`}>
        {children}
      </div>
      {edge.left && (
        <button type="button" aria-label="Previous" onClick={() => go(-1)} className={`${btn} -left-3`}>
          <ChevronLeft className="h-5 w-5" />
        </button>
      )}
      {edge.right && (
        <button type="button" aria-label="Next" onClick={() => go(1)} className={`${btn} -right-3`}>
          <ChevronRight className="h-5 w-5" />
        </button>
      )}
    </div>
  )
}
