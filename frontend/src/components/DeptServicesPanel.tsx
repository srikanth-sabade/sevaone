import { useState } from 'react'
import { FileSignature, IndianRupee, ShieldCheck } from 'lucide-react'
import { toast } from 'sonner'
import { api } from '../api'
import { Button } from './ui/button'
import { useT } from '../hooks/useT'

type Props = {
  readonly departmentCode?: string
  readonly serviceCode?: string
  readonly signerName?: string
  /** When true, copy clarifies assisted (seva.opr + bound citizen) auth. */
  readonly assisted?: boolean
}

export function DeptServicesPanel({
  departmentCode = 'revenue',
  serviceCode = 'SVC001',
  signerName = 'Citizen',
  assisted = false,
}: Props) {
  const { t } = useT()
  const [busy, setBusy] = useState<string | null>(null)
  const [lastDoc, setLastDoc] = useState<string | null>(null)

  const run = async (key: string, fn: () => Promise<void>) => {
    setBusy(key)
    try {
      await fn()
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Request failed')
    } finally {
      setBusy(null)
    }
  }

  return (
    <div className="rounded-2xl border border-border bg-card p-5 shadow-sm">
      <h2 className="text-lg font-semibold text-[color:var(--gov-navy)]">{t('deptPanelTitle')}</h2>
      <p className="mt-1 text-sm text-muted-foreground">
        DigiLocker, Khajane-II payment and CDAC eSign via SevaOne BFF → {departmentCode} connector.
        {assisted
          ? ' Assisted mode uses the bound counter citizen (no separate citizen login).'
          : ''}
      </p>
      <div className="mt-4 flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="rounded-lg"
          disabled={!!busy}
          onClick={() =>
            void run('dl', async () => {
              const doc = await api.digilockerFetch(departmentCode, 'Aadhaar')
              setLastDoc(doc.documentId)
              toast.success(doc.found ? `Fetched ${doc.documentId}` : doc.note)
            })
          }
        >
          <ShieldCheck className="mr-1.5 h-4 w-4" />
          {busy === 'dl' ? 'Fetching…' : 'DigiLocker fetch'}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="rounded-lg"
          disabled={!!busy}
          onClick={() =>
            void run('pay', async () => {
              const pay = await api.initiatePayment(departmentCode, {
                serviceCode,
                amountInr: 15,
                purpose: `${serviceCode} fee`,
              })
              toast.success(`${pay.status}: ${pay.paymentId} (${pay.gateway})`)
            })
          }
        >
          <IndianRupee className="mr-1.5 h-4 w-4" />
          {busy === 'pay' ? 'Paying…' : 'Pay ₹15'}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          className="rounded-lg"
          disabled={!!busy}
          onClick={() =>
            void run('esign', async () => {
              const docId = lastDoc ?? `${serviceCode}-draft`
              const signed = await api.requestESign(departmentCode, {
                documentId: docId,
                signerName,
              })
              toast.success(`${signed.status}: ${signed.requestId}`)
            })
          }
        >
          <FileSignature className="mr-1.5 h-4 w-4" />
          {busy === 'esign' ? 'Signing…' : 'Request eSign'}
        </Button>
      </div>
      {lastDoc && <p className="mt-3 text-xs text-muted-foreground">Last DigiLocker doc: {lastDoc}</p>}
    </div>
  )
}
