import type { Me } from '../types'

type Notification = {
  id: string
  channel: string
  subject: string
  body: string
  createdAt: string
  sentAt?: string | null
}

type Activity = {
  id: string
  action: string
  resource: string
  detail?: string | null
  at: string
}

const SEEDED_MOBILE = '9876543210'

const notifications: Notification[] = [
  {
    id: 'seed-9876543210-notification-1',
    channel: 'SMS',
    subject: 'Income Certificate application received',
    body: 'Your application KAR-REV-2026-00421 has been received by the Revenue Department.',
    createdAt: '2026-08-24T09:15:00+05:30',
    sentAt: '2026-08-24T09:15:08+05:30',
  },
  {
    id: 'seed-9876543210-notification-2',
    channel: 'Email',
    subject: 'Documents verified',
    body: 'Your Aadhaar and income proof have been verified for your Income Certificate application.',
    createdAt: '2026-08-23T16:40:00+05:30',
    sentAt: '2026-08-23T16:40:11+05:30',
  },
  {
    id: 'seed-9876543210-notification-3',
    channel: 'SMS',
    subject: 'Field verification scheduled',
    body: 'A Village Accountant will verify your application within the next three working days.',
    createdAt: '2026-08-22T11:30:00+05:30',
    sentAt: '2026-08-22T11:30:09+05:30',
  },
  {
    id: 'seed-9876543210-notification-4',
    channel: 'Email',
    subject: 'Application is under review',
    body: 'Your application KAR-REV-2026-00421 is being reviewed by the Tahsildar office.',
    createdAt: '2026-08-21T14:05:00+05:30',
    sentAt: '2026-08-21T14:05:07+05:30',
  },
  {
    id: 'seed-9876543210-notification-5',
    channel: 'SMS',
    subject: 'Keep your reference number handy',
    body: 'Use KAR-REV-2026-00421 to track your application and receive service updates.',
    createdAt: '2026-08-20T10:10:00+05:30',
    sentAt: '2026-08-20T10:10:06+05:30',
  },
]

const activity: Activity[] = [
  {
    id: 'seed-9876543210-activity-1',
    action: 'application.submit',
    resource: 'revenue/SVC001',
    detail: 'KAR-REV-2026-00421',
    at: '2026-08-24T09:12:00+05:30',
  },
  {
    id: 'seed-9876543210-activity-2',
    action: 'apply.token.issued',
    resource: 'revenue/SVC001',
    at: '2026-08-24T09:06:00+05:30',
  },
  {
    id: 'seed-9876543210-activity-3',
    action: 'auth.profile.complete',
    resource: 'profile',
    at: '2026-08-23T16:35:00+05:30',
  },
  {
    id: 'seed-9876543210-activity-4',
    action: 'department.session.open',
    resource: 'revenue/SVC001',
    at: '2026-08-23T16:31:00+05:30',
  },
  {
    id: 'seed-9876543210-activity-5',
    action: 'auth.login',
    resource: 'session',
    at: '2026-08-23T16:28:00+05:30',
  },
]

function isSeededUser(me: Me | null): boolean {
  return me?.mobile?.replace(/\D/g, '') === SEEDED_MOBILE
}

export function seededNotifications(me: Me | null, apiNotifications: Notification[] | undefined): Notification[] {
  return isSeededUser(me) ? notifications : (apiNotifications ?? [])
}

export function seededActivity(me: Me | null, apiActivity: Activity[] | undefined): Activity[] {
  return isSeededUser(me) ? activity : (apiActivity ?? [])
}
