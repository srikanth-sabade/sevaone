import {
  Banknote,
  Briefcase,
  Factory,
  FileText,
  GraduationCap,
  HeartHandshake,
  IdCard,
  Landmark,
  LayoutGrid,
  ShieldCheck,
  ShoppingBasket,
  Sprout,
  Zap,
} from 'lucide-react'
import type { MessageKey } from '../i18n'
import { SERVICE_CATEGORY } from './serviceCategoryMap'

export type ServiceCategoryId =
  | 'certificates'
  | 'land'
  | 'licences'
  | 'loans'
  | 'education'
  | 'agriculture'
  | 'utilities'
  | 'food'
  | 'labour'
  | 'business'
  | 'welfare'
  | 'environment'
  | 'other'

export const SERVICE_CATEGORIES: { id: ServiceCategoryId; nameKey: MessageKey; icon: typeof FileText }[] = [
  { id: 'certificates', nameKey: 'catCertificates', icon: FileText },
  { id: 'land', nameKey: 'catLand', icon: Landmark },
  { id: 'licences', nameKey: 'catLicenses', icon: IdCard },
  { id: 'loans', nameKey: 'catLoans', icon: Banknote },
  { id: 'education', nameKey: 'catEducation', icon: GraduationCap },
  { id: 'agriculture', nameKey: 'catAgriculture', icon: Sprout },
  { id: 'utilities', nameKey: 'catUtility', icon: Zap },
  { id: 'food', nameKey: 'catFood', icon: ShoppingBasket },
  { id: 'labour', nameKey: 'catEmployment', icon: Briefcase },
  { id: 'business', nameKey: 'catBusiness', icon: Factory },
  { id: 'welfare', nameKey: 'catPensions', icon: HeartHandshake },
  { id: 'environment', nameKey: 'catSafety', icon: ShieldCheck },
  { id: 'other', nameKey: 'catOther', icon: LayoutGrid },
]

export const isCategoryId = (v: unknown): v is ServiceCategoryId =>
  typeof v === 'string' && SERVICE_CATEGORIES.some((c) => c.id === v)

export const categoryOf = (serviceCode: string): ServiceCategoryId => SERVICE_CATEGORY[serviceCode] ?? 'other'
