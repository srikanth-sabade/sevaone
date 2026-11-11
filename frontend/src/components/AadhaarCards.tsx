// Reads the signed e-Aadhaar XML from DigiLocker (the Aadhaar has no PDF) and draws the front and address cards,
// English with Kannada beneath, like the mobile app does. No official logos are used.
// shortcut: the XML signature is not verified here, only displayed; verify server-side if this is ever used as proof.
type Aadhaar = {
  name: string
  nameLocal: string
  dob: string
  gender: [string, string]
  number: string
  photo: string | null
  address: string
  addressLocal: string
}

const ADDRESS_ORDER = ['house', 'street', 'landmark', 'loc', 'vtc', 'po', 'subdist', 'dist', 'state']

function addressText(el: Element | null): string {
  if (!el) return ''
  const parts = ADDRESS_ORDER.map((k) => (el.getAttribute(k) ?? '').trim()).filter(Boolean)
  const pin = (el.getAttribute('pc') ?? '').trim()
  return pin ? `${parts.join(', ')} - ${pin}` : parts.join(', ')
}

export function parseAadhaar(xml: string): Aadhaar | null {
  const doc = new DOMParser().parseFromString(xml, 'application/xml')
  const uid = doc.getElementsByTagName('UidData')[0]
  if (!uid) return null
  const poi = uid.getElementsByTagName('Poi')[0]
  const local = uid.getElementsByTagName('LData')[0] ?? null
  const digits = (uid.getAttribute('uid') ?? '').replace(/\D/g, '')
  const photo = (uid.getElementsByTagName('Pht')[0]?.textContent ?? '').replace(/\s/g, '')
  const g = (poi?.getAttribute('gender') ?? '').trim().toUpperCase()
  return {
    name: (poi?.getAttribute('name') ?? '').trim(),
    nameLocal: (local?.getAttribute('name') ?? '').trim(),
    dob: (poi?.getAttribute('dob') ?? '').trim(),
    gender: g === 'M' ? ['Male', 'ಪುರುಷ'] : g === 'F' ? ['Female', 'ಮಹಿಳೆ'] : g === 'T' ? ['Transgender', 'ತೃತೀಯ ಲಿಂಗ'] : [g, ''],
    number: digits.length >= 4 ? `XXXX XXXX ${digits.slice(-4)}` : '',
    photo: photo ? `data:image/jpeg;base64,${photo}` : null,
    address: addressText(uid.getElementsByTagName('Poa')[0] ?? null),
    addressLocal: addressText(local),
  }
}

function Shell({ top, bottom, number, footer, children }: { top: string; bottom: string; number: string; footer?: string; children: React.ReactNode }) {
  return (
    <div className="overflow-hidden rounded-2xl border border-border bg-white text-[#1B2430] shadow-sm">
      <div className="space-y-1 px-4 pt-4 text-center text-sm font-bold">
        <div className="rounded-full bg-[#F2A154] py-1">{top}</div>
        <div className="rounded-full bg-[#7DBB5C] py-1">{bottom}</div>
      </div>
      <div className="px-4 py-4">{children}</div>
      {number && <div className="text-center text-2xl font-extrabold tracking-widest">{number}</div>}
      <div className="mt-2 h-0.5 bg-[#D32F2F]" />
      {footer ? <div className="py-2.5 text-center text-sm font-bold text-[#D32F2F]">{footer}</div> : <div className="h-3" />}
    </div>
  )
}

function Bi({ en, kn, className = '' }: { en: string; kn: string; className?: string }) {
  return (
    <div className={className}>
      {kn && kn !== en && <div>{kn}</div>}
      <div>{en}</div>
    </div>
  )
}

export function AadhaarCards({ data }: { data: Aadhaar }) {
  return (
    <div className="space-y-3">
      <Shell top="ಭಾರತ ಸರ್ಕಾರ" bottom="Government of India" number={data.number} footer="ಆಧಾರ್  —  AADHAAR">
        <div className="flex gap-4">
          <div className="h-32 w-24 shrink-0 bg-[#8A8A8A]">
            {data.photo && <img src={data.photo} alt="" className="h-full w-full object-cover" />}
          </div>
          <div className="min-w-0 space-y-2 text-sm">
            <Bi en={data.name} kn={data.nameLocal} className="text-base font-bold" />
            <Bi en={`DOB: ${data.dob}`} kn={`ಜನ್ಮ ದಿನಾಂಕ: ${data.dob}`} />
            <Bi en={data.gender[0]} kn={data.gender[1]} className="font-semibold" />
          </div>
        </div>
      </Shell>
      <Shell top="ಭಾರತೀಯ ವಿಶಿಷ್ಟ ಗುರುತು ಪ್ರಾಧಿಕಾರ" bottom="Unique Identification Authority of India" number={data.number}>
        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <div className="font-bold">ವಿಳಾಸ:</div>
            <div className="mt-1 break-words">{data.addressLocal || data.address}</div>
          </div>
          <div>
            <div className="font-bold">Address:</div>
            <div className="mt-1 break-words">{data.address || data.addressLocal}</div>
          </div>
        </div>
      </Shell>
      <p className="text-xs text-muted-foreground">
        Read from the signed e-Aadhaar in your DigiLocker; the signature is not checked here.
      </p>
    </div>
  )
}
