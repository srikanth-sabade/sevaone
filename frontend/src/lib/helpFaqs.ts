import type { Faq } from '../types'

/**
 * Static, bilingual citizen FAQ set for /help (and the Chat discovery FAQ match).
 *
 * Kept in the frontend rather than behind /api/v1/help/faqs on purpose: it is a small,
 * slow-changing, translation-reviewed body of copy, and keeping it local means /help works
 * on an offline Grama One kiosk with no backend reachable. If a DB-backed FAQ admin is ever
 * built, `selectHelpFaqs` is the single call site to swap.
 */

export type HelpFaqCategory =
  | 'certificates'
  | 'sakala'
  | 'fees'
  | 'tracking'
  | 'login'
  | 'grievance'
  | 'helplines'

/** Display order for the category groups on /help. */
export const HELP_FAQ_CATEGORY_ORDER: readonly HelpFaqCategory[] = [
  'certificates',
  'sakala',
  'fees',
  'tracking',
  'login',
  'grievance',
  'helplines',
]

export const HELP_FAQ_CATEGORY_LABEL: Record<HelpFaqCategory, { en: string; kn: string }> = {
  certificates: { en: 'Certificates & documents', kn: 'ಪ್ರಮಾಣಪತ್ರಗಳು ಮತ್ತು ದಾಖಲೆಗಳು' },
  sakala: { en: 'Sakala timelines', kn: 'ಸಕಾಲ ಕಾಲಮಿತಿಗಳು' },
  fees: { en: 'Fees & payments', kn: 'ಶುಲ್ಕ ಮತ್ತು ಪಾವತಿಗಳು' },
  tracking: { en: 'Tracking an application', kn: 'ಅರ್ಜಿ ಸ್ಥಿತಿ ಪರಿಶೀಲನೆ' },
  login: { en: 'Login & SevaOne ID', kn: 'ಲಾಗಿನ್ ಮತ್ತು ಸೇವಾಒನ್ ಐಡಿ' },
  grievance: { en: 'Grievances', kn: 'ದೂರುಗಳು' },
  helplines: { en: 'Helplines & counters', kn: 'ಸಹಾಯವಾಣಿ ಮತ್ತು ಕೌಂಟರ್‌ಗಳು' },
}

type BilingualFaq = {
  readonly id: string
  readonly category: HelpFaqCategory
  readonly question: { en: string; kn: string }
  readonly answer: { en: string; kn: string }
  readonly relatedServices?: readonly string[]
  /** Extra match terms (English + Kannada colloquial) that need not appear in the visible copy. */
  readonly keywords?: readonly string[]
}

const FAQS: readonly BilingualFaq[] = [
  {
    id: 'cert-income-docs',
    category: 'certificates',
    question: {
      en: 'What documents do I need for an Income Certificate?',
      kn: 'ಆದಾಯ ಪ್ರಮಾಣಪತ್ರಕ್ಕೆ ಯಾವ ದಾಖಲೆಗಳು ಬೇಕು?',
    },
    answer: {
      en: 'Aadhaar card, a ration card or other proof of residence, a recent passport-size photograph, and proof of income — a salary slip or Form 16 for salaried applicants, or a self-declaration of agricultural income otherwise.',
      kn: 'ಆಧಾರ್ ಕಾರ್ಡ್, ಪಡಿತರ ಚೀಟಿ ಅಥವಾ ವಾಸಸ್ಥಳದ ಇತರ ದಾಖಲೆ, ಇತ್ತೀಚಿನ ಪಾಸ್‌ಪೋರ್ಟ್ ಅಳತೆಯ ಭಾವಚಿತ್ರ, ಮತ್ತು ಆದಾಯದ ದಾಖಲೆ — ವೇತನದಾರರಿಗೆ ವೇತನ ಚೀಟಿ ಅಥವಾ ಫಾರ್ಮ್ 16, ಇಲ್ಲದಿದ್ದರೆ ಕೃಷಿ ಆದಾಯದ ಸ್ವಯಂ-ಘೋಷಣೆ.',
    },
    relatedServices: ['RD-SSP-001'],
    keywords: ['nadakacheri', 'ಆದಾಯ', 'income proof', 'salary'],
  },
  {
    id: 'cert-caste-docs',
    category: 'certificates',
    question: {
      en: 'What documents are needed for a Caste Certificate?',
      kn: 'ಜಾತಿ ಪ್ರಮಾಣಪತ್ರಕ್ಕೆ ಯಾವ ದಾಖಲೆಗಳು ಬೇಕು?',
    },
    answer: {
      en: 'Aadhaar card, proof of residence, a recent photograph, and a caste reference — the applicant’s or father’s existing caste certificate, an extract from school records showing caste, or a declaration from two witnesses of the same caste from the village.',
      kn: 'ಆಧಾರ್ ಕಾರ್ಡ್, ವಾಸಸ್ಥಳದ ದಾಖಲೆ, ಇತ್ತೀಚಿನ ಭಾವಚಿತ್ರ, ಮತ್ತು ಜಾತಿಯ ಆಧಾರ — ಅರ್ಜಿದಾರ ಅಥವಾ ತಂದೆಯ ಈಗಿನ ಜಾತಿ ಪ್ರಮಾಣಪತ್ರ, ಜಾತಿ ನಮೂದಿಸಿದ ಶಾಲಾ ದಾಖಲೆಯ ಉದ್ಧೃತ, ಅಥವಾ ಗ್ರಾಮದ ಅದೇ ಜಾತಿಯ ಇಬ್ಬರು ಸಾಕ್ಷಿಗಳ ಘೋಷಣೆ.',
    },
    relatedServices: ['RD-SSP-002'],
    keywords: ['caste', 'ಜಾತಿ', 'sc', 'st', 'obc'],
  },
  {
    id: 'cert-rtc-online',
    category: 'certificates',
    question: {
      en: 'Can I get my RTC (Pahani) online without visiting an office?',
      kn: 'ಕಚೇರಿಗೆ ಹೋಗದೆ ಆರ್‌ಟಿಸಿ (ಪಹಣಿ) ಆನ್‌ಲೈನ್‌ನಲ್ಲಿ ಪಡೆಯಬಹುದೇ?',
    },
    answer: {
      en: 'Yes. A view-and-download RTC is available online through the Bhoomi service and does not need an office visit. A certified copy for legal use is delivered within the Sakala timeline after applying.',
      kn: 'ಹೌದು. ವೀಕ್ಷಣೆ ಮತ್ತು ಡೌನ್‌ಲೋಡ್ ಆರ್‌ಟಿಸಿ ಭೂಮಿ ಸೇವೆ ಮೂಲಕ ಆನ್‌ಲೈನ್‌ನಲ್ಲಿ ಲಭ್ಯವಿದ್ದು, ಕಚೇರಿ ಭೇಟಿ ಬೇಕಿಲ್ಲ. ಕಾನೂನು ಬಳಕೆಗೆ ದೃಢೀಕೃತ ಪ್ರತಿ ಅರ್ಜಿ ಸಲ್ಲಿಸಿದ ನಂತರ ಸಕಾಲ ಕಾಲಮಿತಿಯೊಳಗೆ ದೊರೆಯುತ್ತದೆ.',
    },
    relatedServices: ['RD-SSP-013'],
    keywords: ['pahani', 'ಪಹಣಿ', 'land record', 'bhoomi', 'rtc'],
  },
  {
    id: 'cert-ration-add-member',
    category: 'certificates',
    question: {
      en: 'How do I add a newborn or a spouse to my ration card?',
      kn: 'ಪಡಿತರ ಚೀಟಿಗೆ ನವಜಾತ ಶಿಶು ಅಥವಾ ಸಂಗಾತಿಯನ್ನು ಹೇಗೆ ಸೇರಿಸುವುದು?',
    },
    answer: {
      en: 'Use the "Member addition to ration card" service under Food & Civil Supplies. For a child, attach the birth certificate and Aadhaar enrolment number; for a spouse, attach the marriage certificate and a surrender certificate from their earlier card.',
      kn: 'ಆಹಾರ ಮತ್ತು ನಾಗರಿಕ ಸರಬರಾಜು ಇಲಾಖೆಯ "ಪಡಿತರ ಚೀಟಿಗೆ ಸದಸ್ಯ ಸೇರ್ಪಡೆ" ಸೇವೆ ಬಳಸಿ. ಮಗುವಿಗೆ ಜನನ ಪ್ರಮಾಣಪತ್ರ ಮತ್ತು ಆಧಾರ್ ನೋಂದಣಿ ಸಂಖ್ಯೆ; ಸಂಗಾತಿಗೆ ಮದುವೆ ಪ್ರಮಾಣಪತ್ರ ಮತ್ತು ಹಿಂದಿನ ಚೀಟಿಯ ಸರೆಂಡರ್ ಪ್ರಮಾಣಪತ್ರ ಲಗತ್ತಿಸಿ.',
    },
    keywords: ['ration', 'ಪಡಿತರ', 'ahara', 'bpl', 'add member'],
  },
  {
    id: 'sakala-what-is',
    category: 'sakala',
    question: {
      en: 'What is Sakala and how does it protect me?',
      kn: 'ಸಕಾಲ ಎಂದರೇನು ಮತ್ತು ಅದು ನನ್ನನ್ನು ಹೇಗೆ ರಕ್ಷಿಸುತ್ತದೆ?',
    },
    answer: {
      en: 'Under the Karnataka Sakala Services Act, notified services carry a legal delivery deadline. If a department misses it, you can escalate for compensation and the officer responsible can be penalised. Every service page on SevaOne shows its Sakala timeline.',
      kn: 'ಕರ್ನಾಟಕ ಸಕಾಲ ಸೇವೆಗಳ ಕಾಯ್ದೆಯಡಿ, ಅಧಿಸೂಚಿತ ಸೇವೆಗಳಿಗೆ ಕಾನೂನುಬದ್ಧ ವಿತರಣಾ ಗಡುವು ಇರುತ್ತದೆ. ಇಲಾಖೆ ಅದನ್ನು ಮೀರಿದರೆ ಪರಿಹಾರಕ್ಕಾಗಿ ಮೇಲ್ಮನವಿ ಸಲ್ಲಿಸಬಹುದು ಮತ್ತು ಸಂಬಂಧಪಟ್ಟ ಅಧಿಕಾರಿಗೆ ದಂಡ ವಿಧಿಸಬಹುದು. ಸೇವಾಒನ್‌ನ ಪ್ರತಿ ಸೇವಾ ಪುಟ ಅದರ ಸಕಾಲ ಕಾಲಮಿತಿ ತೋರಿಸುತ್ತದೆ.',
    },
    keywords: ['sakala', 'ಸಕಾಲ', 'deadline', 'guarantee', 'delay'],
  },
  {
    id: 'sakala-certificate-timeline',
    category: 'sakala',
    question: {
      en: 'How long do Caste and Income Certificates take?',
      kn: 'ಜಾತಿ ಮತ್ತು ಆದಾಯ ಪ್ರಮಾಣಪತ್ರಗಳಿಗೆ ಎಷ್ಟು ಸಮಯ ಬೇಕು?',
    },
    answer: {
      en: 'The standard Sakala timeline for Caste and Income Certificates is about 14 to 21 working days from a complete application. If it is not delivered in time, raise a grievance from the Grievance page.',
      kn: 'ಪೂರ್ಣ ಅರ್ಜಿಯಿಂದ ಜಾತಿ ಮತ್ತು ಆದಾಯ ಪ್ರಮಾಣಪತ್ರಗಳಿಗೆ ಪ್ರಮಾಣಿತ ಸಕಾಲ ಕಾಲಮಿತಿ ಸುಮಾರು 14 ರಿಂದ 21 ಕೆಲಸದ ದಿನಗಳು. ಸಮಯಕ್ಕೆ ದೊರೆಯದಿದ್ದರೆ ದೂರು ಪುಟದಿಂದ ದೂರು ಸಲ್ಲಿಸಿ.',
    },
    relatedServices: ['RD-SSP-001', 'RD-SSP-002'],
    keywords: ['ಕಾಲಮಿತಿ', 'days', 'how long', 'timeline'],
  },
  {
    id: 'sakala-missed-deadline',
    category: 'sakala',
    question: {
      en: 'The Sakala deadline has passed and I have not received my document. What now?',
      kn: 'ಸಕಾಲ ಗಡುವು ಮುಗಿದಿದೆ ಆದರೆ ನನ್ನ ದಾಖಲೆ ಸಿಕ್ಕಿಲ್ಲ. ಈಗ ಏನು ಮಾಡಬೇಕು?',
    },
    answer: {
      en: 'File a grievance from the Grievance page with your acknowledgement number. A missed Sakala deadline is automatically eligible for escalation to the competent officer, and you may claim the compensation the Act provides.',
      kn: 'ನಿಮ್ಮ ಸ್ವೀಕೃತಿ ಸಂಖ್ಯೆಯೊಂದಿಗೆ ದೂರು ಪುಟದಿಂದ ದೂರು ಸಲ್ಲಿಸಿ. ಮೀರಿದ ಸಕಾಲ ಗಡುವು ಸಮರ್ಥ ಅಧಿಕಾರಿಗೆ ಮೇಲ್ಮನವಿಗೆ ಸ್ವಯಂಚಾಲಿತವಾಗಿ ಅರ್ಹವಾಗಿದ್ದು, ಕಾಯ್ದೆ ನೀಡುವ ಪರಿಹಾರವನ್ನು ಕ್ಲೇಮ್ ಮಾಡಬಹುದು.',
    },
    keywords: ['compensation', 'escalate', 'ಪರಿಹಾರ', 'late', 'delay'],
  },
  {
    id: 'fees-how-much',
    category: 'fees',
    question: {
      en: 'How much does a certificate cost?',
      kn: 'ಪ್ರಮಾಣಪತ್ರಕ್ಕೆ ಎಷ್ಟು ಶುಲ್ಕ?',
    },
    answer: {
      en: 'Most revenue certificates carry a statutory fee of roughly Rs. 25 to Rs. 40. Scholarships and most welfare schemes are free. The exact fee is shown on each service page before you apply.',
      kn: 'ಹೆಚ್ಚಿನ ಕಂದಾಯ ಪ್ರಮಾಣಪತ್ರಗಳಿಗೆ ಸುಮಾರು ರೂ. 25 ರಿಂದ ರೂ. 40 ಶಾಸನಬದ್ಧ ಶುಲ್ಕ. ವಿದ್ಯಾರ್ಥಿವೇತನ ಮತ್ತು ಹೆಚ್ಚಿನ ಕಲ್ಯಾಣ ಯೋಜನೆಗಳು ಉಚಿತ. ಅರ್ಜಿ ಸಲ್ಲಿಸುವ ಮೊದಲು ನಿಖರ ಶುಲ್ಕವನ್ನು ಪ್ರತಿ ಸೇವಾ ಪುಟದಲ್ಲಿ ತೋರಿಸಲಾಗುತ್ತದೆ.',
    },
    keywords: ['fee', 'cost', 'ಶುಲ್ಕ', 'charge', 'price'],
  },
  {
    id: 'fees-payment-failed',
    category: 'fees',
    question: {
      en: 'My payment was debited but the application did not go through. What should I do?',
      kn: 'ಹಣ ಕಡಿತವಾಗಿದೆ ಆದರೆ ಅರ್ಜಿ ಸಲ್ಲಿಕೆಯಾಗಿಲ್ಲ. ಏನು ಮಾಡಬೇಕು?',
    },
    answer: {
      en: 'Do not pay again immediately. A failed transaction with a debit is normally auto-reversed within 5 to 7 working days. If it is not, raise a grievance with the payment reference and the date, and keep the bank SMS as proof.',
      kn: 'ತಕ್ಷಣ ಮತ್ತೆ ಪಾವತಿಸಬೇಡಿ. ಹಣ ಕಡಿತದೊಂದಿಗೆ ವಿಫಲ ವಹಿವಾಟು ಸಾಮಾನ್ಯವಾಗಿ 5 ರಿಂದ 7 ಕೆಲಸದ ದಿನಗಳಲ್ಲಿ ಸ್ವಯಂ ಹಿಂತಿರುಗುತ್ತದೆ. ಇಲ್ಲದಿದ್ದರೆ ಪಾವತಿ ಉಲ್ಲೇಖ ಮತ್ತು ದಿನಾಂಕದೊಂದಿಗೆ ದೂರು ಸಲ್ಲಿಸಿ, ಬ್ಯಾಂಕ್ ಎಸ್ಎಂಎಸ್ ಅನ್ನು ಸಾಕ್ಷಿಯಾಗಿ ಇಟ್ಟುಕೊಳ್ಳಿ.',
    },
    keywords: ['payment', 'refund', 'ಪಾವತಿ', 'debited', 'failed', 'transaction'],
  },
  {
    id: 'track-lost-ack',
    category: 'tracking',
    question: {
      en: 'I lost my acknowledgement number. Can I still track my application?',
      kn: 'ನನ್ನ ಸ್ವೀಕೃತಿ ಸಂಖ್ಯೆ ಕಳೆದುಹೋಗಿದೆ. ಇನ್ನೂ ಅರ್ಜಿ ಸ್ಥಿತಿ ಪರಿಶೀಲಿಸಬಹುದೇ?',
    },
    answer: {
      en: 'Yes. On the Track page use "Track by mobile number", enter the mobile number you applied with, verify the OTP, and every application linked to that number is listed with its current status.',
      kn: 'ಹೌದು. ಟ್ರ್ಯಾಕ್ ಪುಟದಲ್ಲಿ "ಮೊಬೈಲ್ ಸಂಖ್ಯೆಯಿಂದ ಟ್ರ್ಯಾಕ್" ಬಳಸಿ, ಅರ್ಜಿ ಸಲ್ಲಿಸಿದ ಮೊಬೈಲ್ ಸಂಖ್ಯೆ ನಮೂದಿಸಿ, ಒಟಿಪಿ ಪರಿಶೀಲಿಸಿ; ಆ ಸಂಖ್ಯೆಗೆ ಸಂಬಂಧಿಸಿದ ಎಲ್ಲಾ ಅರ್ಜಿಗಳು ಅವುಗಳ ಪ್ರಸ್ತುತ ಸ್ಥಿತಿಯೊಂದಿಗೆ ಪಟ್ಟಿಯಾಗುತ್ತವೆ.',
    },
    keywords: ['track', 'ಟ್ರ್ಯಾಕ್', 'acknowledgement', 'lost', 'status', 'ಸ್ಥಿತಿ'],
  },
  {
    id: 'track-what-stages',
    category: 'tracking',
    question: {
      en: 'What do the tracking stages mean?',
      kn: 'ಟ್ರ್ಯಾಕಿಂಗ್ ಹಂತಗಳ ಅರ್ಥವೇನು?',
    },
    answer: {
      en: 'An application moves through Submitted, Under verification, Approved (or Returned for clarification), and Delivered. If it is "Returned", open it to see what document or detail the office has asked for and resubmit.',
      kn: 'ಅರ್ಜಿ ಸಲ್ಲಿಸಲಾಗಿದೆ, ಪರಿಶೀಲನೆಯಲ್ಲಿ, ಅನುಮೋದಿತ (ಅಥವಾ ಸ್ಪಷ್ಟನೆಗಾಗಿ ಹಿಂತಿರುಗಿಸಲಾಗಿದೆ), ಮತ್ತು ವಿತರಿಸಲಾಗಿದೆ — ಈ ಹಂತಗಳ ಮೂಲಕ ಸಾಗುತ್ತದೆ. "ಹಿಂತಿರುಗಿಸಲಾಗಿದೆ" ಎಂದಿದ್ದರೆ, ಕಚೇರಿ ಕೇಳಿದ ದಾಖಲೆ ಅಥವಾ ವಿವರವನ್ನು ನೋಡಿ ಮರುಸಲ್ಲಿಸಿ.',
    },
    keywords: ['stages', 'ಹಂತ', 'status', 'returned', 'timeline'],
  },
  {
    id: 'login-what-is-id',
    category: 'login',
    question: {
      en: 'Do I need to create an account to use SevaOne?',
      kn: 'ಸೇವಾಒನ್ ಬಳಸಲು ಖಾತೆ ರಚಿಸಬೇಕೇ?',
    },
    answer: {
      en: 'You can search services, read requirements, and track by mobile number without signing in. Signing in with your mobile number and an OTP is needed only to apply, so your applications stay linked to you.',
      kn: 'ಸೈನ್ ಇನ್ ಮಾಡದೆ ಸೇವೆಗಳನ್ನು ಹುಡುಕಬಹುದು, ಅವಶ್ಯಕತೆಗಳನ್ನು ಓದಬಹುದು ಮತ್ತು ಮೊಬೈಲ್ ಸಂಖ್ಯೆಯಿಂದ ಟ್ರ್ಯಾಕ್ ಮಾಡಬಹುದು. ಅರ್ಜಿ ಸಲ್ಲಿಸಲು ಮಾತ್ರ ಮೊಬೈಲ್ ಸಂಖ್ಯೆ ಮತ್ತು ಒಟಿಪಿ ಮೂಲಕ ಸೈನ್ ಇನ್ ಬೇಕು, ಇದರಿಂದ ನಿಮ್ಮ ಅರ್ಜಿಗಳು ನಿಮಗೆ ಜೋಡಿಸಲ್ಪಡುತ್ತವೆ.',
    },
    keywords: ['login', 'ಲಾಗಿನ್', 'account', 'register', 'otp', 'sign in'],
  },
  {
    id: 'login-otp-not-received',
    category: 'login',
    question: {
      en: 'I am not receiving the login OTP. What can I do?',
      kn: 'ಲಾಗಿನ್ ಒಟಿಪಿ ಬರುತ್ತಿಲ್ಲ. ಏನು ಮಾಡಬಹುದು?',
    },
    answer: {
      en: 'Wait for the 60-second timer and use Resend OTP. Check that the number is a 10-digit Indian mobile with signal, and that SMS is not blocked under DND for transactional messages. At a Grama One or Karnataka One counter, the operator can assist.',
      kn: '60 ಸೆಕೆಂಡ್ ಟೈಮರ್‌ಗಾಗಿ ಕಾಯಿರಿ ಮತ್ತು ಒಟಿಪಿ ಮರುಕಳುಹಿಸಿ ಬಳಸಿ. ಸಂಖ್ಯೆ 10-ಅಂಕಿಯ ಭಾರತೀಯ ಮೊಬೈಲ್ ಆಗಿದ್ದು ಸಿಗ್ನಲ್ ಇದೆಯೇ, ವಹಿವಾಟು ಸಂದೇಶಗಳಿಗೆ ಡಿಎನ್‌ಡಿ ಅಡಿ ಎಸ್ಎಂಎಸ್ ನಿರ್ಬಂಧಿಸಿಲ್ಲವೇ ಎಂದು ಪರಿಶೀಲಿಸಿ. ಗ್ರಾಮ ಒನ್ ಅಥವಾ ಕರ್ನಾಟಕ ಒನ್ ಕೌಂಟರ್‌ನಲ್ಲಿ ಆಪರೇಟರ್ ಸಹಾಯ ಮಾಡಬಹುದು.',
    },
    keywords: ['otp', 'ಒಟಿಪಿ', 'sms', 'not received', 'resend', 'dnd'],
  },
  {
    id: 'grievance-how-to-file',
    category: 'grievance',
    question: {
      en: 'How do I file a grievance about a delayed or rejected application?',
      kn: 'ವಿಳಂಬವಾದ ಅಥವಾ ತಿರಸ್ಕೃತ ಅರ್ಜಿ ಬಗ್ಗೆ ದೂರು ಹೇಗೆ ಸಲ್ಲಿಸುವುದು?',
    },
    answer: {
      en: 'Open the Grievance page, choose the department, and describe the issue with your acknowledgement number. You will get a grievance reference to track the response.',
      kn: 'ದೂರು ಪುಟ ತೆರೆಯಿರಿ, ಇಲಾಖೆ ಆಯ್ಕೆಮಾಡಿ, ನಿಮ್ಮ ಸ್ವೀಕೃತಿ ಸಂಖ್ಯೆಯೊಂದಿಗೆ ಸಮಸ್ಯೆ ವಿವರಿಸಿ. ಪ್ರತಿಕ್ರಿಯೆ ಟ್ರ್ಯಾಕ್ ಮಾಡಲು ದೂರು ಉಲ್ಲೇಖ ಸಂಖ್ಯೆ ದೊರೆಯುತ್ತದೆ.',
    },
    keywords: ['grievance', 'ದೂರು', 'complaint', 'rejected', 'delay'],
  },
  {
    id: 'grievance-response-time',
    category: 'grievance',
    question: {
      en: 'How long does a grievance take to be resolved?',
      kn: 'ದೂರು ಪರಿಹಾರಕ್ಕೆ ಎಷ್ಟು ಸಮಯ ಬೇಕು?',
    },
    answer: {
      en: 'Most grievances are acknowledged within 3 working days and addressed within 15. If there is no response, the grievance can be escalated to the next higher officer from the same reference.',
      kn: 'ಹೆಚ್ಚಿನ ದೂರುಗಳನ್ನು 3 ಕೆಲಸದ ದಿನಗಳಲ್ಲಿ ಸ್ವೀಕರಿಸಲಾಗುತ್ತದೆ ಮತ್ತು 15 ದಿನಗಳಲ್ಲಿ ಪರಿಹರಿಸಲಾಗುತ್ತದೆ. ಪ್ರತಿಕ್ರಿಯೆ ಇಲ್ಲದಿದ್ದರೆ ಅದೇ ಉಲ್ಲೇಖದಿಂದ ದೂರನ್ನು ಮುಂದಿನ ಹಿರಿಯ ಅಧಿಕಾರಿಗೆ ಮೇಲ್ಮನವಿ ಸಲ್ಲಿಸಬಹುದು.',
    },
    keywords: ['grievance', 'ದೂರು', 'resolution', 'escalate', 'response'],
  },
  {
    id: 'help-toll-free',
    category: 'helplines',
    question: {
      en: 'What are the toll-free helpline numbers?',
      kn: 'ಟೋಲ್-ಫ್ರೀ ಸಹಾಯವಾಣಿ ಸಂಖ್ಯೆಗಳು ಯಾವುವು?',
    },
    answer: {
      en: 'Seva Sindhu / SevaOne helpline 1902 operates 24x7. For emergencies dial 112, and for ambulance dial 108.',
      kn: 'ಸೇವಾ ಸಿಂಧು / ಸೇವಾಒನ್ ಸಹಾಯವಾಣಿ 1902 ದಿನದ 24 ಗಂಟೆ ಕಾರ್ಯನಿರ್ವಹಿಸುತ್ತದೆ. ತುರ್ತು ಸಂದರ್ಭದಲ್ಲಿ 112, ಆಂಬ್ಯುಲೆನ್ಸ್‌ಗಾಗಿ 108 ಡಯಲ್ ಮಾಡಿ.',
    },
    keywords: ['helpline', '1902', '112', '108', 'ಸಹಾಯವಾಣಿ', 'toll free', 'phone'],
  },
  {
    id: 'help-counter-hours',
    category: 'helplines',
    question: {
      en: 'Where and when can I get in-person help?',
      kn: 'ಖುದ್ದಾಗಿ ಸಹಾಯ ಎಲ್ಲಿ ಮತ್ತು ಯಾವಾಗ ಸಿಗುತ್ತದೆ?',
    },
    answer: {
      en: 'Grama One counters in rural areas and Karnataka One / Bangalore One centres in towns and cities handle SevaOne services in assisted mode. They are generally open 8:00 AM to 8:00 PM, Monday to Saturday.',
      kn: 'ಗ್ರಾಮೀಣ ಪ್ರದೇಶಗಳಲ್ಲಿ ಗ್ರಾಮ ಒನ್ ಕೌಂಟರ್‌ಗಳು ಮತ್ತು ಪಟ್ಟಣ ಹಾಗೂ ನಗರಗಳಲ್ಲಿ ಕರ್ನಾಟಕ ಒನ್ / ಬೆಂಗಳೂರು ಒನ್ ಕೇಂದ್ರಗಳು ಸಹಾಯ ವಿಧಾನದಲ್ಲಿ ಸೇವಾಒನ್ ಸೇವೆಗಳನ್ನು ನಿರ್ವಹಿಸುತ್ತವೆ. ಸಾಮಾನ್ಯವಾಗಿ ಸೋಮವಾರದಿಂದ ಶನಿವಾರ ಬೆಳಿಗ್ಗೆ 8:00 ರಿಂದ ರಾತ್ರಿ 8:00 ರವರೆಗೆ ತೆರೆದಿರುತ್ತವೆ.',
    },
    keywords: ['grama one', 'karnataka one', 'bangalore one', 'ಗ್ರಾಮ ಒನ್', 'counter', 'kiosk', 'hours'],
  },
]

/** Locale-neutral search over question, answer, category label and hidden keywords. */
function faqHaystack(faq: BilingualFaq): string {
  return [
    faq.question.en,
    faq.question.kn,
    faq.answer.en,
    faq.answer.kn,
    HELP_FAQ_CATEGORY_LABEL[faq.category].en,
    HELP_FAQ_CATEGORY_LABEL[faq.category].kn,
    ...(faq.keywords ?? []),
    ...(faq.relatedServices ?? []),
  ]
    .join(' ')
    .toLowerCase()
}

function localise(faq: BilingualFaq, lang: 'en' | 'kn'): Faq {
  return {
    id: faq.id,
    category: HELP_FAQ_CATEGORY_LABEL[faq.category][lang],
    question: faq.question[lang],
    answer: faq.answer[lang],
    relatedServices: [...(faq.relatedServices ?? [])],
  }
}

/**
 * FAQs for the current language, optionally filtered by a free-text query. Order follows
 * HELP_FAQ_CATEGORY_ORDER then declaration order, so callers can group by `category` directly.
 */
export function selectHelpFaqs(lang: 'en' | 'kn', query?: string): Faq[] {
  const q = query?.trim().toLowerCase()
  const terms = q ? q.split(/\s+/).filter(Boolean) : []
  const matched = terms.length
    ? FAQS.filter((faq) => {
        const hay = faqHaystack(faq)
        return terms.every((term) => hay.includes(term))
      })
    : FAQS
  return [...matched]
    .sort(
      (a, b) =>
        HELP_FAQ_CATEGORY_ORDER.indexOf(a.category) - HELP_FAQ_CATEGORY_ORDER.indexOf(b.category),
    )
    .map((faq) => localise(faq, lang))
}
